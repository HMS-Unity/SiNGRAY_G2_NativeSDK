using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Singray.Foundation
{
    public enum StereoCameraIndex
    {
        LeftEye = 0,
        RightEye = 1,
        Camera2 = 2,
        Camera3 = 3,
    }

    [Serializable]
    public class XvStereoCameraParameter : XvCameraParameterSetting
    {
        public StereoCameraIndex cameraIndex;
    }

    public class XvStereoCamera : XvCameraBase
    {
        private static readonly HashSet<XvStereoCamera> owners = new HashSet<XvStereoCamera>();
        private static int cameraCount = -1;
        private readonly XvStereoCameraParameter cameraParameter;
        private Color32[] pixels;
        private GCHandle pixelHandle;
        private Texture2D tex;
        private double lastTimestamp;
        private float deadline;
        private bool acquired;
        private bool published;
        private int activeIndex;

        public XvStereoCamera(XvStereoCameraParameter cameraParameter, FrameArrived frameArrived)
            : base(cameraParameter, frameArrived)
        {
            this.cameraParameter = cameraParameter ?? throw new ArgumentNullException(nameof(cameraParameter));
        }

        public override void StartCapture()
        {
            if (isOpen) return;
            if ((int)cameraParameter.cameraIndex < 0 || (int)cameraParameter.cameraIndex > 3)
                throw new ArgumentOutOfRangeException(nameof(cameraParameter.cameraIndex));
            activeIndex = (int)cameraParameter.cameraIndex;
            base.StartCapture();
            deadline = Time.realtimeSinceStartup + 10f;
            lastTimestamp = 0;
            published = false;
            TryAcquire();
        }

        private bool TryAcquire()
        {
            if (acquired) return true;
            if (!XvStereoNative.Ready()) return false;
            try
            {
                int index = activeIndex;
                if (owners.Count == 0) cameraCount = -1;
                if (index >= 2)
                {
                    if (cameraCount < 0) cameraCount = XvStereoNative.CameraCount();
                    if (cameraCount <= index || cameraCount > 16)
                    {
                        Debug.LogWarning($"[FISHEYE] index={index} unavailable; calibratedCameras={cameraCount}");
                        StopCapture();
                        return false;
                    }
                }
                if (owners.Count == 0) XvStereoNative.Start();
                owners.Add(this);
                acquired = true;
                Debug.Log($"[FISHEYE] start index={index} readers={owners.Count} calibratedCameras={cameraCount}");
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[FISHEYE] start failed index={activeIndex}: {error.Message}");
                StopCapture();
                return false;
            }
        }

        public override void StopCapture()
        {
            base.StopCapture();
            ReleaseTexture();
            lastTimestamp = 0;
            published = false;
            if (!acquired) return;
            acquired = false;
            owners.Remove(this);
            Debug.Log($"[FISHEYE] stop index={activeIndex} readers={owners.Count}");
            if (owners.Count != 0) return;
            cameraCount = -1;
            try { XvStereoNative.Stop(); }
            catch (Exception error) { Debug.LogWarning("[FISHEYE] native stop: " + error.Message); }
        }

        public override void Update()
        {
            if (!isOpen) return;
            if ((!acquired || !published) && Time.realtimeSinceStartup >= deadline)
            {
                Debug.LogWarning($"[FISHEYE] index={activeIndex} no fresh frame after 10s");
                StopCapture();
                return;
            }
            if (!TryAcquire() || !XvStereoNative.Ready()) return;
            try
            {
                int width = XvStereoNative.Width(), height = XvStereoNative.Height();
                if (width <= 0 || height <= 0 || width > 4096 || height > 4096 || (long)width * height > 4194304) return;
                if (tex == null || tex.width != width || tex.height != height) ReplaceTexture(width, height);
                double timestamp = 0;
                if (!XvStereoNative.Read(activeIndex, pixelHandle.AddrOfPinnedObject(), width, height, ref timestamp)
                    || double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp <= lastTimestamp) return;
                tex.SetPixelData(pixels, 0);
                tex.Apply(false, false);
                lastTimestamp = timestamp;
                cameraData.tex = tex;
                cameraData.texWidth = width;
                cameraData.texHeight = height;
                cameraData.parameter.timeStamp = timestamp;
                if (!published)
                    Debug.Log($"[FISHEYE] first-frame index={activeIndex} actual={width}x{height} timestamp={timestamp:F6}");
                published = true;
                frameArrived?.Invoke(cameraData);
                MyDebugTool.LogDiagnostic("FISHEYE", "frame-" + activeIndex,
                    $"index={activeIndex} frame={width}x{height} timestamp={timestamp:F6}", 2f);
            }
            catch (Exception error)
            {
                Debug.LogError($"[FISHEYE] read failed index={activeIndex}: {error.Message}");
                StopCapture();
            }
        }

        private void ReplaceTexture(int width, int height)
        {
            ReleaseTexture();
            try
            {
                pixels = new Color32[checked(width * height)];
                pixelHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Debug.Log($"[FISHEYE] texture index={activeIndex} size={width}x{height}");
            }
            catch { ReleaseTexture(); throw; }
        }

        private void ReleaseTexture()
        {
            cameraData.tex = null;
            cameraData.texWidth = cameraData.texHeight = 0;
            if (pixelHandle.IsAllocated) pixelHandle.Free();
            pixels = null;
            if (tex == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(tex);
            else UnityEngine.Object.DestroyImmediate(tex);
            tex = null;
        }
    }
}
