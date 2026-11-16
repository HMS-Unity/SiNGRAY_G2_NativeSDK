using NatSuite.Examples;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Singray.Foundation
{
    /// <summary>
    /// Provides local video recording and screenshots.
    /// </summary>
    [RequireComponent(typeof(ReplayCam))]
    [RequireComponent(typeof(JPG))]

    public sealed class XvMediaRecorderManager : MonoBehaviour
    {
        private XvMediaRecorderManager() { }
        private ReplayCam replayCam;
        private JPG jpg;
        private Coroutine screenshotRoutine;
        private Coroutine recordingRoutine;
        private UnityAction<string> pendingScreenshotCallback;
        private readonly object recordingOwner = new object();
        private readonly object screenshotOwner = new object();
        private bool stopping;

        [SerializeField]
        private XvMRVideoCaptureManager xvMRVideoCaptureManager;
        public XvMRVideoCaptureManager XvMRVideoCaptureManager {

            get {

                if (xvMRVideoCaptureManager == null)
                {
                    xvMRVideoCaptureManager = FindObjectOfType<XvMRVideoCaptureManager>(true);
                }

                if (xvMRVideoCaptureManager == null)
                {
                    GameObject newObj = Instantiate(Resources.Load<GameObject>("XvMRVideoCaptureManager"));
                    xvMRVideoCaptureManager = newObj.GetComponent<XvMRVideoCaptureManager>();
                    newObj.name = "XvMRVideoCaptureManager";
                }

                return xvMRVideoCaptureManager;
            }
        }
     
        public int Width { 
        get { return XvMRVideoCaptureManager.CameraManager.Width; }
        }
       
        public int Height { 
        get { return XvMRVideoCaptureManager.CameraManager.Height; }
        }

        private void Awake()
        {
            replayCam=GetComponent<ReplayCam>();
            jpg=GetComponent<JPG>();
            replayCam.videoWidth = Width;
            replayCam.videoHeight = Height;
            jpg.imageWidth= Width; 
            jpg.imageHeight= Height;
            jpg.cam = XvMRVideoCaptureManager.BgCamera;
            replayCam.cam = XvMRVideoCaptureManager.BgCamera;

        }


        /// <summary>
        /// Start capturing the mixed-reality video stream.
        /// </summary>
        public void StartCapture() {
            if (stopping || !isActiveAndEnabled) return;
            XvMRVideoCaptureManager.AcquireCapture(this);
        }
        /// <summary>
        /// Stop capturing the mixed-reality video stream.
        /// The camera is shared. Set closeCamera to false if another module still needs it.
        /// </summary>
        /// <param name="closeCamera">True closes the camera; false keeps it open</param>
        public void StopCapture(bool closeCamera=false)
        {
            if (stopping) return;
            stopping = true;
            try
            {
                if (!closeCamera && xvMRVideoCaptureManager != null && xvMRVideoCaptureManager.IsOn)
                    xvMRVideoCaptureManager.CameraManager.StartCapture(XvCameraStreamType.ARCameraStream);
                CancelPendingScreenshot();
            }
            finally
            {
                try
                {
                    CancelPendingRecording();
                    if (jpg != null) jpg.CancelScreenshot();
                    if (replayCam != null && replayCam.IsRecording) replayCam.StopRecording(null);
                }
                finally
                {
                    try
                    {
                        if (xvMRVideoCaptureManager != null)
                        {
                            xvMRVideoCaptureManager.ReleaseCapture(recordingOwner, true);
                            xvMRVideoCaptureManager.ReleaseCapture(screenshotOwner, true);
                            xvMRVideoCaptureManager.ReleaseCapture(this, closeCamera);
                        }
                    }
                    finally { stopping = false; }
                }
            }
        }

        /// <summary>
        /// Start recording after StartCapture has started the mixed-reality video stream.
        /// </summary>

        public void StartRecording()
        {
            if (stopping || !isActiveAndEnabled || IsVideoRecording()) return;
            try
            {
                XvMRVideoCaptureManager.AcquireCapture(recordingOwner);
                if (IsCameraFrameReady()) BeginRecording();
                else recordingRoutine = StartCoroutine(RecordWhenReady(Time.realtimeSinceStartup + 10f));
            }
            catch (System.Exception error)
            {
                if (xvMRVideoCaptureManager != null) xvMRVideoCaptureManager.ReleaseCapture(recordingOwner, true);
                Debug.LogError("[MEDIA-RECORDER] recording request failed: " + error.Message);
            }
        }

        private IEnumerator RecordWhenReady(float deadline)
        {
            yield return null;
            while (!IsCameraFrameReady())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    recordingRoutine = null;
                    if (xvMRVideoCaptureManager != null) xvMRVideoCaptureManager.ReleaseCapture(recordingOwner, true);
                    Debug.LogWarning("[MR-RECORDER] recording canceled: no real frame within 10 seconds");
                    yield break;
                }
                yield return null;
            }
            recordingRoutine = null;
            BeginRecording();
        }
        private void BeginRecording()
        {
            try
            {
                var output = XvMRVideoCaptureManager.CameraRenderTexture;
                replayCam.videoWidth = output.width;
                replayCam.videoHeight = output.height;
                replayCam.cam = XvMRVideoCaptureManager.BgCamera;
                replayCam.StartRecording();
            }
            finally { if (xvMRVideoCaptureManager != null) xvMRVideoCaptureManager.ReleaseCapture(recordingOwner, true); }
        }
        private void CancelPendingRecording()
        {
            if (recordingRoutine == null) return;
            StopCoroutine(recordingRoutine); recordingRoutine = null;
            if (xvMRVideoCaptureManager != null) xvMRVideoCaptureManager.ReleaseCapture(recordingOwner, true);
        }

        /// <summary>
        /// True once the RGB camera background has produced at least one real frame
        /// since capture was (re)started. Wait on this before StartRecording() to avoid
        /// recording the placeholder (white) background that shows before the first frame arrives.
        /// </summary>
        public bool IsCameraFrameReady()
        {
            return XvMRVideoCaptureManager.HasFrame;
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        /// <param name="callback"></param>
        public void StopRecording(UnityAction<string> callback)
        {
            if (recordingRoutine != null) { CancelPendingRecording(); callback?.Invoke(null); return; }
            replayCam.StopRecording(callback);
        }

        public bool IsVideoRecording() {
            return recordingRoutine != null || (replayCam != null && replayCam.IsRecording);
        }

        /// <summary>
        /// Capture after a fresh RGB frame arrives. Timeout/cancellation returns a null path.
        /// </summary>
        /// <param name="callback"></param>
        public void SaveScreenshot(UnityAction<string> callback)
        {
            if (IsTakingScreenshots())
            {
                Debug.LogWarning("Screenshot capture is already in progress.");
                callback?.Invoke(null);
                return;
            }
            if (stopping || !isActiveAndEnabled)
            {
                callback?.Invoke(null);
                return;
            }
            var capture = XvMRVideoCaptureManager;
            ulong previousFrame = capture.FrameSequence;
            pendingScreenshotCallback = path =>
            {
                if (capture != null) capture.ReleaseCapture(screenshotOwner, true);
                callback?.Invoke(path);
            };
            try
            {
                capture.AcquireCapture(screenshotOwner);
                screenshotRoutine = StartCoroutine(CaptureScreenshotWhenReady(capture, previousFrame,
                    Time.realtimeSinceStartup + 10f));
            }
            catch (System.Exception error)
            {
                Debug.LogError("[MEDIA-RECORDER] screenshot request failed: " + error.Message);
                CompletePendingScreenshot(null);
            }
        }

        private IEnumerator CaptureScreenshotWhenReady(XvMRVideoCaptureManager capture, ulong previousFrame, float deadline)
        {
            yield return null;
            while (capture != null && (!capture.HasFrame || capture.FrameSequence == previousFrame))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogWarning("Screenshot canceled: no fresh RGB frame within 10 seconds.");
                    CompletePendingScreenshot(null);
                    yield break;
                }
                yield return null;
            }
            if (capture == null)
            {
                CompletePendingScreenshot(null);
                yield break;
            }
            // Let the compositor render the newly received background before attaching JPG input.
            yield return new WaitForEndOfFrame();
            if (capture == null || !capture.HasFrame)
            {
                CompletePendingScreenshot(null);
                yield break;
            }
            var output = capture.CameraRenderTexture;
            jpg.imageWidth = output.width;
            jpg.imageHeight = output.height;
            jpg.cam = capture.BgCamera;
            var callback = pendingScreenshotCallback;
            pendingScreenshotCallback = null;
            screenshotRoutine = null;
            jpg.SaveScreenshot(callback);
        }

        private void CompletePendingScreenshot(string path)
        {
            var callback = pendingScreenshotCallback;
            pendingScreenshotCallback = null;
            screenshotRoutine = null;
            callback?.Invoke(path);
        }

        private void CancelPendingScreenshot()
        {
            if (screenshotRoutine == null) return;
            StopCoroutine(screenshotRoutine);
            CompletePendingScreenshot(null);
        }

        private void OnDisable() => StopCapture(true);
        private void OnApplicationPause(bool paused) { if (paused) StopCapture(true); }

        public bool IsTakingScreenshots() {
            return screenshotRoutine != null || (jpg != null && jpg.IsRecording);


        }
    }
}
