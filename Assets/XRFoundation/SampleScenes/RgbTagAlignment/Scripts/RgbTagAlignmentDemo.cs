using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using Singray.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>RGB-only, independent 36h11/0 corner detection and PnP. No vendor tag pose calls.</summary>
    public sealed class RgbTagAlignmentDemo : MonoBehaviour
    {
        public const float EdgeMetres = 0.08f;
        [SerializeField] private XvCameraManager cameraManager;
        [SerializeField] private Transform controls, worldPlane;
        [SerializeField] private RawImage preview;
        [SerializeField] private RgbTagOverlay overlay;
        [SerializeField] private TMP_Text statusText, valuesText;
        [SerializeField] private Interactable[] buttons;
        [Tooltip("Correct the G2 RGBA stream's horizontal reflection before detection, preview and projection. Calibration stays in camera pixel coordinates.")]
        [SerializeField] private bool correctMirroredRgbX = true;
        [SerializeField, Range(1, 15)] private float processingHz = 8;
        [SerializeField] private float frameTimeoutSeconds = 1;
        private readonly object rgbOwner = new object();
        private readonly double[] poseBuffer = new double[7];
        private bool running, acquired, calibrated, locked, worldValid;
        private int generation;
        private double lastTimestamp;
        private float nextCaptureAt, lastDisplayAt = -1;
        private float nextDiagnosticAt;
        private string lastDiagnosticState;
        private double[] calibration;
        private Matrix4x4 trackingFromRgb;
        private Texture2D previewTexture;
        private Task<Frame> pending;
        private Frame displayed;
        private byte[] reusablePixels;
        private Matrix4x4 lockedWorld, latestWorld;

        private sealed class Frame
        {
            internal byte[] pixels;
            internal int width, height, generation;
            internal double timestamp;
            internal float capturedAt;
            internal double[] k, result = new double[31];
            internal Matrix4x4 worldFromRgb;
            internal bool hasTracking;
            internal bool horizontalCorrectionApplied;
            internal string error;
        }
        public bool IsLocked => locked;
        public bool IsRunning => running;
        public Transform WorldPlane => worldPlane;

        private void OnEnable()
        {
            if (buttons != null) foreach (var button in buttons) if (button != null) Singray.UI.MrtkClickGate.Install(button);
            if (worldPlane != null) worldPlane.gameObject.SetActive(false);
            SetStatus("Ready - RGB only");
        }
        private void Start() => RecenterPanel();
        public void RecenterPanel()
        {
            var camera = Camera.main;
            if (camera == null || controls == null) return;
            controls.position = camera.transform.TransformPoint(new Vector3(-0.52f, -0.03f, 1.25f));
            controls.rotation = Quaternion.LookRotation(controls.position-camera.transform.position, Vector3.up);
        }
        public void StartDetection()
        {
            if (running) return;
            if (cameraManager == null || preview == null || overlay == null || worldPlane == null)
            { SetStatus("Missing Demo references"); return; }
            try
            {
                // Avoid creating a shared manager's pending capture while the SDK initializes.
                if (!CameraReady()) { SetStatus("SDK not ready - press Start again on G2"); return; }
                RgbTagNative.CheckAbi();
            }
            catch (Exception e) { SetStatus("Native bridge unavailable: " + e.GetType().Name); Debug.LogException(e); return; }
            generation++; calibrated = false; calibration = null; nextCaptureAt = 0;
            locked = worldValid = false; displayed = null; lastDisplayAt = -1;
            worldPlane.gameObject.SetActive(false); overlay.Clear(); preview.texture = null;
            preview.enabled = false;
            XvCameraManager.onARCameraStreamFrameArrived.AddListener(OnRgbFrame);
            try
            {
                cameraManager.AcquireCapture(XvCameraStreamType.ARCameraStream, rgbOwner);
                acquired = running = true; SetStatus("Waiting for RGB / 36h11 ID 0");
            }
            catch (Exception e) { StopDetection(); SetStatus("RGB start failed: " + e.Message); }
        }
        public void StopDetection()
        {
            generation++; running = false;
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(OnRgbFrame);
            if (acquired) { acquired = false; XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, rgbOwner); }
            locked = worldValid = false; displayed = null; lastDisplayAt = -1;
            if (worldPlane != null) worldPlane.gameObject.SetActive(false);
            if (overlay != null) overlay.Clear();
            if (preview != null) preview.texture = null;
            if (preview != null) preview.enabled = false;
            SetStatus("Stopped - Plane cleared");
        }
        private void OnDisable() { StopDetection(); ReleasePreview(); }
        private void OnApplicationPause(bool paused) { if (paused) StopDetection(); }
        private void OnApplicationFocus(bool focused) { if (!focused && Application.isPlaying) StopDetection(); }
        private void ReleasePreview()
        {
            if (previewTexture != null) { if (Application.isPlaying) Destroy(previewTexture); else DestroyImmediate(previewTexture); }
            previewTexture = null;
        }
        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            if (displayed == null && valuesText != null) valuesText.text = "36h11 / ID 0 / 80 mm\nGreen: detected   Magenta: reprojected\nAmber: locked Plane projected into RGB";
        }
        private static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        private static bool CameraReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }
        internal static bool Finite(Matrix4x4 m)
        {
            for (int i=0;i<16;i++) if (!Finite(m[i])) return false;
            return true;
        }
        private bool ReadCalibration(int width, int height)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            API.rgb_calibration raw = default;
            if (!API.readRGBCalibration(ref raw)) { SetStatus("RGB calibration read failed"); return false; }
            API.pdm model;
            if (width == 1280 && height == 720) model = raw.intrinsic720;
            else if (width == 1920 && height == 1080) model = raw.intrinsic1080;
            else if (width == 640 && height == 480) model = raw.intrinsic480;
            else { SetStatus("No matching calibration for " + width + "x" + height); return false; }
            if (model.K == null || model.K.Length != 11 || model.K[9] != width || model.K[10] != height || model.K[0] <= 0 || model.K[1] <= 0)
            { SetStatus("Calibration/image dimensions mismatch"); return false; }
            foreach (double n in model.K) if (!Finite(n)) { SetStatus("Invalid RGB intrinsics"); return false; }
            if (raw.extrinsic.rotation == null || raw.extrinsic.translation == null || raw.extrinsic.rotation.Length != 9 || raw.extrinsic.translation.Length != 3)
            { SetStatus("Missing RGB extrinsics"); return false; }
            var native = Matrix4x4.identity;
            for (int r=0;r<3;r++) for (int c=0;c<3;c++) native[r,c]=(float)raw.extrinsic.rotation[r*3+c];
            for (int i=0;i<3;i++) native[i,3]=(float)raw.extrinsic.translation[i];
            var handedness = Matrix4x4.Scale(new Vector3(1,-1,1));
            trackingFromRgb = handedness * native * handedness;
            if (!Rigid(trackingFromRgb)) { SetStatus("Invalid RGB extrinsic transform"); return false; }
            calibration = new double[9]; Array.Copy(model.K, calibration, 9);
            calibrated = true; return true;
#else
            SetStatus("Editor: use synthetic validation; live RGB requires G2"); return false;
#endif
        }
        private bool TryTracking(double timestamp, out Matrix4x4 worldFromRgb)
        {
            worldFromRgb = Matrix4x4.identity;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!API.xslam_get_pose_at(poseBuffer, timestamp)) return false;
            for (int i=0;i<7;i++) if (!Finite(poseBuffer[i])) return false;
            var q = new Quaternion(-(float)poseBuffer[0], (float)poseBuffer[1], -(float)poseBuffer[2], (float)poseBuffer[3]);
            if (Quaternion.Dot(q,q) < 0.9f || Quaternion.Dot(q,q) > 1.1f) return false;
            q = q.normalized;
            var p = new Vector3((float)poseBuffer[4], -(float)poseBuffer[5], (float)poseBuffer[6]);
            var origin = Matrix4x4.identity;
            var head = FindObjectOfType<XvXRHeadTracking>();
            if (head != null)
            {
                origin = (head.transform.parent == null ? Matrix4x4.identity : head.transform.parent.localToWorldMatrix) * Matrix4x4.Translate(head.positionOrigin);
                // A reset/recenter changes the tracking interpretation; clear/restart the test.
                if (Quaternion.Angle(XvXRManager.SDK.ZeroQuaternion, Quaternion.identity) > 0.01f) return false;
            }
            if (!Rigid(origin)) return false;
            worldFromRgb = origin * Matrix4x4.TRS(p, q, Vector3.one) * trackingFromRgb;
            return Rigid(worldFromRgb);
#else
            return false;
#endif
        }
        internal static bool Rigid(Matrix4x4 m) => Finite(m) && Mathf.Abs(m.determinant-1) < 0.02f &&
            Mathf.Abs(m.GetColumn(0).sqrMagnitude-1) < 0.02f && Mathf.Abs(m.GetColumn(1).sqrMagnitude-1) < 0.02f &&
            Mathf.Abs(m.GetColumn(2).sqrMagnitude-1) < 0.02f && Mathf.Abs(Vector3.Dot(m.GetColumn(0),m.GetColumn(1))) < 0.01f &&
            Mathf.Abs(Vector3.Dot(m.GetColumn(0),m.GetColumn(2))) < 0.01f && Mathf.Abs(Vector3.Dot(m.GetColumn(1),m.GetColumn(2))) < 0.01f;

        private void OnRgbFrame(cameraData data)
        {
            if (!running || pending != null || Time.realtimeSinceStartup < nextCaptureAt || data == null || !(data.tex is Texture2D texture)) return;
            double timestamp = data.parameter.timeStamp;
            if (!Finite(timestamp) || timestamp <= 0 || timestamp <= lastTimestamp) return;
            if (texture.width < 16 || texture.height < 16 || texture.width > 4096 || texture.height > 4096 || texture.format != TextureFormat.RGBA32)
            { SetStatus("Expected readable RGBA32 RGB frame"); return; }
            if (!calibrated && !ReadCalibration(texture.width, texture.height)) return;
            if ((int)data.parameter.width != texture.width || (int)data.parameter.height != texture.height)
            { SetStatus("Frame changed mode - stop and restart"); return; }
            try
            {
                int bytes = checked(texture.width*texture.height*4);
                var frame = new Frame { pixels = reusablePixels != null && reusablePixels.Length == bytes ? reusablePixels : new byte[bytes],
                    width=texture.width, height=texture.height, timestamp=timestamp, generation=generation,
                    capturedAt=Time.realtimeSinceStartup, k=(double[])calibration.Clone(), horizontalCorrectionApplied=correctMirroredRgbX };
                reusablePixels = null;
                texture.GetRawTextureData<byte>().CopyTo(frame.pixels);
                frame.hasTracking = TryTracking(timestamp, out frame.worldFromRgb);
                lastTimestamp=timestamp; nextCaptureAt=Time.realtimeSinceStartup+1/processingHz;
                pending = Task.Run(() => Process(frame));
            }
            catch (Exception e) { SetStatus("RGB frame failed: " + e.Message); }
        }
        private static Frame Process(Frame frame)
        {
            try
            {
                if (frame.horizontalCorrectionApplied) CorrectHorizontalReflection(frame.pixels,frame.width,frame.height);
                RgbTagNative.Detect(frame.pixels,frame.width,frame.height,frame.k,frame.result);
            }
            catch (Exception e) { frame.error=e.GetType().Name+": "+e.Message; }
            return frame;
        }
        internal static void CorrectHorizontalReflection(byte[] rgba,int width,int height)
        {
            if (rgba==null || width<=0 || height<=0 || rgba.Length!=checked(width*height*4))
                throw new ArgumentException("Expected tightly packed RGBA image.");
            for(int y=0;y<height;y++) for(int x=0;x<width/2;x++)
            {
                int left=(y*width+x)*4,right=(y*width+width-1-x)*4;
                for(int channel=0;channel<4;channel++)
                { byte value=rgba[left+channel];rgba[left+channel]=rgba[right+channel];rgba[right+channel]=value; }
            }
        }
        internal static Matrix4x4 CameraFromTag(double[] result)
        {
            var m = Matrix4x4.identity;
            for (int row=0;row<3;row++) for (int col=0;col<3;col++)
                m[row,col]=(float)result[3+row*3+col]*(row==1?-1:1)*(col==2?-1:1);
            m.SetColumn(3,new Vector4((float)result[12],-(float)result[13],(float)result[14],1));
            return m;
        }
        private void Update()
        {
            if (pending != null && pending.IsCompleted)
            {
                var task=pending; pending=null;
                if (task.IsFaulted) { SetStatus("Detector worker failed"); Debug.LogException(task.Exception); }
                else
                {
                    var frame=task.Result; reusablePixels=frame.pixels;
                    if (running && frame.generation==generation && Time.realtimeSinceStartup-frame.capturedAt <= frameTimeoutSeconds)
                        Display(frame);
                }
            }
            if (running && lastDisplayAt >= 0 && Time.realtimeSinceStartup-lastDisplayAt > frameTimeoutSeconds)
            {
                worldValid=false; overlay.Clear(); preview.texture=null; preview.enabled=false;
                if (!locked) worldPlane.gameObject.SetActive(false);
                SetStatus(locked ? "Locked - RGB stale; reference retained" : "RGB stale - live Plane hidden");
            }
        }
        private void Display(Frame frame)
        {
            displayed=frame; lastDisplayAt=frame.capturedAt;
            if (previewTexture == null || previewTexture.width != frame.width || previewTexture.height != frame.height)
            {
                ReleasePreview(); previewTexture=new Texture2D(frame.width,frame.height,TextureFormat.RGBA32,false);
                previewTexture.name="Processed RGB frame"; previewTexture.wrapMode=TextureWrapMode.Clamp;
            }
            previewTexture.LoadRawTextureData(frame.pixels); previewTexture.Apply(false,false); preview.texture=previewTexture;
            preview.enabled=true;
            var ratio=preview.GetComponent<AspectRatioFitter>(); if (ratio!=null) ratio.aspectRatio=(float)frame.width/frame.height;
            worldValid=false;
            double[] lockedPixels=null;
            if (locked && frame.hasTracking)
            {
                lockedPixels=new double[8];
                if (RgbTagNative.Project(frame.k,frame.worldFromRgb.inverse*lockedWorld,lockedPixels)!=1) lockedPixels=null;
            }
            bool found=frame.error==null && frame.result[0]==1;
            if (found)
            {
                var cameraFromTag=CameraFromTag(frame.result);
                worldValid=frame.hasTracking && Rigid(cameraFromTag) && frame.result[1] <= 3;
                if (worldValid)
                {
                    latestWorld=frame.worldFromRgb*cameraFromTag;
                    if (!locked) ApplyPlane(latestWorld);
                }
            }
            if (!worldValid && !locked) worldPlane.gameObject.SetActive(false);
            overlay.SetFrame(frame.width,frame.height,found?frame.result:null,lockedPixels);
            string state=frame.error!=null ? "Detector error: "+frame.error : !found ?
                (frame.result[0]==-3 ? "Duplicate ID 0 - pose rejected" : frame.result[0]<0 ? "Detection/pose rejected: "+frame.result[0] : "ID 0 not visible") :
                !frame.hasTracking ? "Tag found - timestamped tracking unavailable" : !worldValid ? "Tag found - pose quality rejected" : "Tracking";
            bool ambiguous=found && frame.result[2]>=0 && frame.result[2]-frame.result[1]<0.1;
            SetStatus((locked?"LOCKED | ":"LIVE | ")+state+(ambiguous?" | planar ambiguity":""));
            if(state!=lastDiagnosticState || Time.realtimeSinceStartup>=nextDiagnosticAt)
            {
                Debug.Log($"[RGB-TAG] state={state} size={frame.width}x{frame.height} timestamp={frame.timestamp:F6} correctedX={frame.horizontalCorrectionApplied} nativeStatus={frame.result[0]} tracking={frame.hasTracking} worldValid={worldValid} rmse={frame.result[1]:F3} locked={locked}");
                lastDiagnosticState=state;nextDiagnosticAt=Time.realtimeSinceStartup+2;
            }
            UpdateValues(frame,found);
        }
        private void UpdateValues(Frame frame,bool found)
        {
            string cameraPose=found ? $"RGB position (m): {frame.result[12]:F3}, {-frame.result[13]:F3}, {frame.result[14]:F3}\n"+
                $"Corner RMSE: {frame.result[1]:F3} px   Alternate: {frame.result[2]:F3} px" : "No valid independent Tag pose";
            string delta=locked && worldValid ? $"Live vs lock: {Vector3.Distance(latestWorld.GetColumn(3),lockedWorld.GetColumn(3))*1000:F1} mm / {Quaternion.Angle(latestWorld.rotation,lockedWorld.rotation):F2} deg" :
                locked ? "Reference fixed; no current comparison" : "Lock a fresh valid Plane, then walk around";
            string orientation=frame.horizontalCorrectionApplied?"X corrected":"Native X";
            if (valuesText!=null) valuesText.text=$"36h11 / ID 0 / 80 mm | {frame.width}x{frame.height} | {orientation}\n{cameraPose}\n{delta}\nGreen: detected | Magenta: fit | Amber: lock";
        }
        private void ApplyPlane(Matrix4x4 pose)
        {
            worldPlane.SetPositionAndRotation(pose.GetColumn(3),pose.rotation);
            worldPlane.localScale=Vector3.one*EdgeMetres; worldPlane.gameObject.SetActive(true);
        }
        public void LockPlane()
        {
            if (locked) return;
            if (!running || !worldValid || displayed==null || Time.realtimeSinceStartup-lastDisplayAt>frameTimeoutSeconds)
            { SetStatus("Lock requires a fresh valid world pose"); return; }
            lockedWorld=latestWorld; locked=true; ApplyPlane(lockedWorld); SetStatus("LOCKED - walk around the real Tag");
            UpdateValues(displayed,true);
        }
        public void UnlockPlane()
        {
            locked=false;
            if (running && worldValid && Time.realtimeSinceStartup-lastDisplayAt<=frameTimeoutSeconds) ApplyPlane(latestWorld);
            else worldPlane.gameObject.SetActive(false);
            SetStatus("LIVE - following independent RGB pose");
            if(displayed!=null) UpdateValues(displayed,displayed.result[0]==1);
        }
        public void SaveSnapshot()
        {
            if (displayed==null || previewTexture==null || Time.realtimeSinceStartup-lastDisplayAt>frameTimeoutSeconds)
            { SetStatus("No fresh frame to save"); return; }
            try
            {
                string directory=Path.Combine(Application.persistentDataPath,"RgbTagAlignment"); Directory.CreateDirectory(directory);
                string path=Path.Combine(directory,DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                // PNG convention is top-left; the source texture stores native top row in Unity row zero.
                var pixels=previewTexture.GetPixels32(); int w=previewTexture.width,h=previewTexture.height;
                var flipped=new Color32[pixels.Length]; for(int y=0;y<h;y++) Array.Copy(pixels,y*w,flipped,(h-1-y)*w,w);
                var image=new Texture2D(w,h,TextureFormat.RGBA32,false);
                try { image.SetPixels32(flipped); image.Apply(); File.WriteAllBytes(path+".png",image.EncodeToPNG()); }
                finally { if (Application.isPlaying) Destroy(image); else DestroyImmediate(image); }
                var report=new Snapshot { timestamp=displayed.timestamp,width=w,height=h,calibration=displayed.k,result=displayed.result,
                    hasTracking=displayed.hasTracking,worldPoseValid=worldValid,trackingFromRgb=trackingFromRgb,worldFromRgb=displayed.worldFromRgb,worldFromTag=worldValid?latestWorld:Matrix4x4.identity,
                    isLocked=locked,lockedWorldFromTag=lockedWorld,device=SystemInfo.deviceModel,
                    horizontalCorrectionApplied=displayed.horizontalCorrectionApplied,detectorError=displayed.error,
                    note="RGB only; independent OpenCV 36h11 ID0; 0.08m. PNG and detected/projected pixels use the same corrected top-left image coordinates; horizontalCorrectionApplied records source correction. Intrinsics stay in camera coordinates. Native result uses OpenCV camera/IPPE basis. World valid only if hasTracking and result[0]=1 and quality accepted. No hardware accuracy claim." };
                File.WriteAllText(path+".json",JsonUtility.ToJson(report,true));
                SetStatus("Saved snapshot: "+Path.GetFileName(path)); Debug.Log("[RGB-TAG] Saved "+path);
            }
            catch(Exception e) { SetStatus("Save failed: "+e.Message); }
        }
        [Serializable] private sealed class Snapshot
        {
            public double timestamp; public int width,height; public double[] calibration,result;
            public bool hasTracking,isLocked,worldPoseValid,horizontalCorrectionApplied; public Matrix4x4 trackingFromRgb,worldFromRgb,worldFromTag,lockedWorldFromTag;
            public string device,note,detectorError;
        }
    }
}
