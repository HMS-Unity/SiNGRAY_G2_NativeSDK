using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation
{
    public enum CaptureType { OnlyCamera, OnlyUnityScene, MR }

    /// <summary>Shared RGB/Unity compositor. Returned output textures are borrowed.</summary>
    [DisallowMultipleComponent]
    public sealed class XvMRVideoCaptureManager : MonoBehaviour
    {
        private XvMRVideoCaptureManager() { }
        [SerializeField] private XvCameraManager cameraManager;
        [SerializeField] private RawImage rgbBackground;
        [SerializeField] private CaptureType captureType = CaptureType.MR;
        [SerializeField] private bool autoCapture;
        private Camera bgCamera;
        private RenderTexture cameraRenderTexture;
        private int cullingMask;
        private bool initialized, isOn, hasFrame, hasRgbFrame, rgbAcquired;
        private double lastTimestamp;
        private Vector3 backgroundPosition;
        private readonly object rgbOwner = new object();
        private readonly HashSet<object> owners = new HashSet<object>();
        private readonly HashSet<object> outputOwners = new HashSet<object>();
        private static readonly HashSet<object> demoUiOwners = new HashSet<object>();
        internal static void RegisterDemoUi(object owner) => demoUiOwners.Add(owner);
        internal static void UnregisterDemoUi(object owner) => demoUiOwners.Remove(owner);
        internal ulong FrameSequence { get; private set; }
        public XvCameraManager CameraManager
        {
            get
            {
                if (cameraManager == null) cameraManager = GetComponentInChildren<XvCameraManager>(true);
                if (cameraManager == null) cameraManager = FindObjectOfType<XvCameraManager>();
                if (cameraManager == null) cameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
                return cameraManager;
            }
        }
        public Camera BgCamera
        {
            get
            {
                if (bgCamera == null)
                {
                    var child = transform.Find("BgCamera");
                    if (child != null) bgCamera = child.GetComponent<Camera>();
                }
                return bgCamera;
            }
        }
        public CaptureType CaptureType
        {
            get => captureType;
            set
            {
                captureType = value; Initialize();
                hasFrame = value != CaptureType.OnlyUnityScene && hasRgbFrame;
                BgCamera.enabled = isOn && (value == CaptureType.OnlyUnityScene || hasRgbFrame);
                ApplyCaptureType();
            }
        }
        public bool IsOn => isOn;
        public bool HasFrame => hasFrame;
        public RenderTexture CameraRenderTexture
        {
            get
            {
                if (cameraRenderTexture == null)
                {
                    // Output size remains stable while borrowed, including an RTSP native pointer.
                    int width = Mathf.Max(1, CameraManager.Width), height = Mathf.Max(1, CameraManager.Height);
                    cameraRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.RGB565);
                    cameraRenderTexture.name = "MR Capture Output";
                    cameraRenderTexture.Create();
                    var previous = RenderTexture.active;
                    try { RenderTexture.active = cameraRenderTexture; GL.Clear(true, true, Color.black); }
                    finally { RenderTexture.active = previous; }
                    if (BgCamera != null) BgCamera.targetTexture = cameraRenderTexture;
                    MyDebugTool.LogDiagnostic("MR-CAPTURE", "output", $"output={width}x{height}", 0f);
                }
                return cameraRenderTexture;
            }
        }
        private void Initialize()
        {
            if (initialized) return;
            if (rgbBackground == null || BgCamera == null)
                throw new InvalidOperationException("Import the complete XvMRVideoCaptureManager prefab (BgCamera and RGBBackground are required).");
            initialized = true;
            cullingMask = BgCamera.cullingMask;
            backgroundPosition = rgbBackground.transform.localPosition;
            // Hosts already configure either BGVideo or XvBGVideo on the whole Canvas.
            // Changing only the RawImage layer breaks the host camera's serialized culling mask.
            rgbBackground.enabled = false;
            BgCamera.enabled = false;
        }
        private void ApplyCaptureType()
        {
            int backgroundMask = 1 << rgbBackground.gameObject.layer;
            BgCamera.cullingMask = captureType == CaptureType.OnlyCamera ? backgroundMask :
                captureType == CaptureType.OnlyUnityScene ? cullingMask & ~backgroundMask : cullingMask;
            if (demoUiOwners.Count > 0) BgCamera.cullingMask &= ~(1 << 5);
            rgbBackground.enabled = isOn && hasFrame && captureType != CaptureType.OnlyUnityScene;
        }
        private void Awake() { Initialize(); ApplyCaptureType(); if (autoCapture) StartCapture(); }
        public void StartCapture() => AcquireCapture(this);
        public void StopCapture(bool closeCamera = false) => ReleaseCapture(this, closeCamera);
        internal void AcquireCapture(object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            Initialize();
            if (owners.Contains(owner)) return;
            if (isOn) { owners.Add(owner); return; }
            hasFrame = hasRgbFrame = false; lastTimestamp = 0;
            try
            {
                CameraManager.AcquireCapture(XvCameraStreamType.ARCameraStream, rgbOwner);
                rgbAcquired = true;
                gameObject.SetActive(true);
                rgbBackground.gameObject.SetActive(true);
                BgCamera.gameObject.SetActive(true);
                var output = CameraRenderTexture;
                XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
                XvCameraManager.onARCameraStreamFrameArrived.AddListener(onFrameArrived);
                isOn = true; owners.Add(owner);
                BgCamera.enabled = captureType == CaptureType.OnlyUnityScene;
                ApplyCaptureType();
                MyDebugTool.LogDiagnostic("MR-CAPTURE", "start", $"started consumers={owners.Count}; waiting for real RGB", 0f);
            }
            catch { owners.Remove(owner); StopSession(true); throw; }
        }
        internal void ReleaseCapture(object owner, bool closeCamera = true)
        {
            if (!owners.Remove(owner)) return;
            MyDebugTool.LogDiagnostic("MR-CAPTURE", "stop", $"released consumers={owners.Count}", 0f);
            if (owners.Count == 0)
            {
                StopSession(closeCamera);
                if (ReferenceEquals(owner, this) && gameObject.activeSelf) gameObject.SetActive(false);
            }
        }
        // Native consumers retain the output pointer even while their encoder is stopped.
        internal void RetainOutput(object owner) { outputOwners.Add(owner); var output = CameraRenderTexture; }
        internal void ReleaseOutput(object owner) { outputOwners.Remove(owner); if (!isOn && outputOwners.Count == 0) ReleaseTexture(); }
        private void StopSession(bool closeCamera)
        {
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
            isOn = hasFrame = hasRgbFrame = false; lastTimestamp = 0;
            if (rgbBackground != null)
            {
                rgbBackground.texture = null; rgbBackground.enabled = false;
                if (initialized) rgbBackground.transform.localPosition = backgroundPosition;
            }
            if (BgCamera != null) BgCamera.enabled = false;
            try
            {
                if (rgbAcquired && !closeCamera && cameraManager != null)
                    cameraManager.StartCapture(XvCameraStreamType.ARCameraStream); // Preserve legacy StopCapture(false).
            }
            finally
            {
                try { if (rgbAcquired) { rgbAcquired = false; XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, rgbOwner); } }
                finally { if (outputOwners.Count == 0) ReleaseTexture(); }
            }
        }
        private void ReleaseTexture()
        {
            if (cameraRenderTexture == null) return;
            if (BgCamera != null && BgCamera.targetTexture == cameraRenderTexture) BgCamera.targetTexture = null;
            var old = cameraRenderTexture; cameraRenderTexture = null; old.Release();
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(old); else Destroy(old);
#else
            Destroy(old);
#endif
            MyDebugTool.LogDiagnostic("MR-CAPTURE", "output-release", "output texture released", 0f);
        }
        private void OnDisable() { owners.Clear(); StopSession(true); }
        private void OnDestroy() { owners.Clear(); outputOwners.Clear(); StopSession(true); }
        private void LateUpdate()
        {
            if (!isOn) return;
            ApplyCaptureType();
            if (captureType != CaptureType.OnlyUnityScene) return;
            BgCamera.enabled = true; hasFrame = true; FrameSequence++;
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private void onFrameArrived(cameraData data)
        {
            if (!isOn || data == null || data.tex == null || rgbBackground == null) return;
            var p = data.parameter;
            if (p.timeStamp <= 0 || double.IsNaN(p.timeStamp) || double.IsInfinity(p.timeStamp) || p.timeStamp <= lastTimestamp ||
                p.width <= 0 || p.height <= 0 || !Finite(p.fx) || !Finite(p.fy) || p.fx <= 0 || p.fy <= 0 || !Finite(p.cx) || !Finite(p.cy)) return;
            lastTimestamp = p.timeStamp;
            rgbBackground.texture = data.tex;
            BgCamera.transform.localPosition = p.rgb_extrinsic_pos;
            BgCamera.transform.localRotation = p.rgb_extrinsic_rot;
            BgCamera.projectionMatrix = PerspectiveOffCenter(p.fx, p.fy, p.cx, p.cy, p.width, p.height, 0.03f, 1000f);
            transform.localPosition = p.rgb_position; transform.localRotation = p.rgb_rotation;
            // Recompute from the serialized base; do not accumulate offsets across restarts.
            rgbBackground.transform.localPosition = backgroundPosition + new Vector3(-(p.cx-p.width*0.5f), p.cy-p.height*0.5f, 0);
            hasFrame = hasRgbFrame = true; FrameSequence++;
            BgCamera.enabled = true; ApplyCaptureType();
            MyDebugTool.LogDiagnostic("MR-CAPTURE", "frame", $"rgb={data.tex.width}x{data.tex.height} calibration={p.width}x{p.height} timestamp={p.timeStamp:F6} sequence={FrameSequence} mode={captureType}", 2f);
        }
        private Matrix4x4 PerspectiveOffCenter(float fx, float fy, float u0, float v0, float w, float h, float near, float far)
        {
            var m = new Matrix4x4();
            m[0,0]=2*fx/w; m[0,2]=1-2*u0/w; m[1,1]=2*fy/h; m[1,2]=-1+2*v0/h;
            m[2,2]=-(far+near)/(far-near); m[2,3]=-2*far*near/(far-near); m[3,2]=-1;
            return m;
        }
    }
}
