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

        [SerializeField]
        private XvMRVideoCaptureManager xvMRVideoCaptureManager;
        public XvMRVideoCaptureManager XvMRVideoCaptureManager {

            get {

                if (xvMRVideoCaptureManager == null)
                {
                    xvMRVideoCaptureManager = FindObjectOfType<XvMRVideoCaptureManager>();
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
            XvMRVideoCaptureManager.AcquireCapture(this);
        }
        /// <summary>
        /// Stop capturing the mixed-reality video stream.
        /// The camera is shared. Set closeCamera to false if another module still needs it.
        /// </summary>
        /// <param name="closeCamera">True closes the camera; false keeps it open</param>
        public void StopCapture(bool closeCamera=false)
        {
            try { CancelPendingScreenshot(); }
            finally
            {
                try
                {
                    CancelPendingRecording();
                    if (jpg != null) jpg.CancelScreenshot();
                    if (replayCam != null && replayCam.IsRecording) replayCam.StopRecording(null);
                }
                finally { if (xvMRVideoCaptureManager != null) xvMRVideoCaptureManager.ReleaseCapture(this, closeCamera); }
            }
        }

        /// <summary>
        /// Start recording after StartCapture has started the mixed-reality video stream.
        /// </summary>

        public void StartRecording()
        {
            if (!isActiveAndEnabled || IsVideoRecording()) return;
            StartCapture();
            if (IsCameraFrameReady()) BeginRecording();
            else recordingRoutine = StartCoroutine(RecordWhenReady(Time.realtimeSinceStartup + 10f));
        }

        private IEnumerator RecordWhenReady(float deadline)
        {
            yield return null;
            while (!IsCameraFrameReady())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    recordingRoutine = null;
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
            var output = XvMRVideoCaptureManager.CameraRenderTexture;
            replayCam.videoWidth = output.width;
            replayCam.videoHeight = output.height;
            replayCam.cam = XvMRVideoCaptureManager.BgCamera;
            replayCam.StartRecording();
        }
        private void CancelPendingRecording()
        {
            if (recordingRoutine == null) return;
            StopCoroutine(recordingRoutine); recordingRoutine = null;
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
                return;
            }
            if (!isActiveAndEnabled)
            {
                callback?.Invoke(null);
                return;
            }
            var capture = XvMRVideoCaptureManager;
            ulong previousFrame = capture.FrameSequence;
            StartCapture();
            pendingScreenshotCallback = callback;
            screenshotRoutine = StartCoroutine(CaptureScreenshotWhenReady(capture, previousFrame,
                Time.realtimeSinceStartup + 10f));
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

        public bool IsTakingScreenshots() {
            return screenshotRoutine != null || (jpg != null && jpg.IsRecording);


        }
    }
}
