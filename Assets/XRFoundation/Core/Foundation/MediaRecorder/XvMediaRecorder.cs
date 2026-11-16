using System.Collections;
using UnityEngine;

namespace Singray.Foundation
{
    /// <summary>Existing hand-menu controls for recording, screenshots, camera preview and RTSP.</summary>
    public class XvMediaRecorder : MonoBehaviour
    {
        [SerializeField] private XvMediaRecorderManager xvMediaRecorderManager;
        [SerializeField] private XvCameraManager xvCameraManager;
        [SerializeField] private XvRTSPStreamerManager xvRTSPStreamerManager;
        public XvMediaRecorderManager XvMediaRecorderManager => Resolve(ref xvMediaRecorderManager);
        public XvCameraManager XvCameraManager => Resolve(ref xvCameraManager);
        public XvRTSPStreamerManager XvRTSPStreamerManager => Resolve(ref xvRTSPStreamerManager);
        private T Resolve<T>(ref T value) where T : Component
        {
            if (value == null) value = FindObjectOfType<T>(true);
            if (value == null) value = new GameObject(typeof(T).Name).AddComponent<T>();
            return value;
        }
        private XvMedioRecordTips tips;
        private TextMesh RtspTips, VideoRecordTips, ScreenshotTips, cameraTips;
        private TextMesh RtspStreamingText, VideoRecordingText, cameraText;
        private bool isScreenshot, cameraRequested, streamingRequested;
        private Coroutine screenshotRoutine;
        private int generation;
        private const string Buttons = "HandMenu/MenuContent/ButtonCollection/";
        private TextMesh Label(string path) => transform.Find(Buttons + path)?.GetComponent<TextMesh>();
        private void Awake()
        {
            RtspTips = Label("RtspStreaming/IconAndText/RtspTips");
            VideoRecordTips = Label("VideoRecording/IconAndText/VideoRecordTips");
            ScreenshotTips = Label("SaveScreenshot/IconAndText/ScreenshotTips");
            cameraTips = Label("Camera/IconAndText/CameraTips");
            RtspStreamingText = Label("RtspStreaming/IconAndText/RtspStreamingText");
            VideoRecordingText = Label("VideoRecording/IconAndText/VideoRecordingText");
            cameraText = Label("Camera/IconAndText/TextMeshPro");
            tips = transform.Find("HandMenu/MenuContent/XvMedioRecordTips")?.GetComponent<XvMedioRecordTips>();
            foreach (var button in GetComponentsInChildren<Microsoft.MixedReality.Toolkit.UI.Interactable>(true))
                Singray.UI.MrtkClickGate.Install(button);
        }
        private static void Show(TextMesh label, bool visible) { if (label != null) label.gameObject.SetActive(visible); }
        private static void Text(TextMesh label, string text) { if (label != null) label.text = text; }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A)) SaveScreenshot();
            if (Time.frameCount % 5 != 0) return;
            bool streaming = xvRTSPStreamerManager != null && xvRTSPStreamerManager.IsStreeaming;
            bool recording = xvMediaRecorderManager != null && xvMediaRecorderManager.IsVideoRecording();
            Show(cameraTips, cameraRequested); Show(RtspTips, streaming); Show(VideoRecordTips, recording);
            Show(ScreenshotTips, isScreenshot || (xvMediaRecorderManager != null && xvMediaRecorderManager.IsTakingScreenshots()));
            Text(cameraText, cameraRequested ? "Stop Camera" : "Start Camera");
            Text(RtspStreamingText, streaming ? "Stop Streaming" : "Start Streaming");
            Text(VideoRecordingText, recording ? "Stop Recording" : "Start Recording");
        }
        private void OnDisable() { ShutdownSample(); }
        private void OnApplicationPause(bool paused) { if (paused) ShutdownSample(); }
        public void ShutdownSample()
        {
            generation++;
            if (screenshotRoutine != null) StopCoroutine(screenshotRoutine);
            screenshotRoutine = null; isScreenshot = false;
            Show(ScreenshotTips, false);
            if (tips != null) tips.HideTips();
            if (streamingRequested && xvRTSPStreamerManager != null) xvRTSPStreamerManager.StopRtspStreaming();
            streamingRequested = false;
            if (cameraRequested) Singray.Foundation.XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, this);
            cameraRequested = false;
            // Teardown must not instantiate services while the scene is being destroyed.
            if (xvMediaRecorderManager != null) xvMediaRecorderManager.StopCapture(true);
        }
        public void ARCameraStreaming()
        {
            if (!isActiveAndEnabled) return;
            if (cameraRequested)
            {
                cameraRequested = false;
                Singray.Foundation.XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, this);
            }
            else { XvCameraManager.AcquireCapture(XvCameraStreamType.ARCameraStream, this); cameraRequested = true; }
            Show(cameraTips, cameraRequested);
            Text(cameraText, cameraRequested ? "Stop Camera" : "Start Camera");
        }
        public void RtspStreaming()
        {
            if (!isActiveAndEnabled) return;
            var manager = XvRTSPStreamerManager;
            if (manager.IsStreeaming) { manager.StopRtspStreaming(); streamingRequested = false; }
            else { manager.StartRtspStreaming(); streamingRequested = manager.IsStreeaming; }
            Show(RtspTips, manager.IsStreeaming);
            Text(RtspStreamingText, manager.IsStreeaming ? "Stop Streaming" : "Start Streaming");
        }
        public void VideoRecording()
        {
            if (!isActiveAndEnabled) return;
            var manager = XvMediaRecorderManager;
            int request = generation;
            if (manager.IsVideoRecording())
                manager.StopRecording(path =>
                {
                    if (this == null || !isActiveAndEnabled || generation != request) return;
                    if (tips != null) tips.ShowTips(string.IsNullOrEmpty(path) ? "Recording failed or canceled" : "Saved to: " + path, 2f);
                });
            else manager.StartRecording();
            Show(VideoRecordTips, manager.IsVideoRecording());
            Text(VideoRecordingText, manager.IsVideoRecording() ? "Stop Recording" : "Start Recording");
        }
        public void SaveScreenshot()
        {
            if (!isActiveAndEnabled || isScreenshot || XvMediaRecorderManager.IsTakingScreenshots()) return;
            isScreenshot = true; Show(ScreenshotTips, true);
            screenshotRoutine = StartCoroutine(RelayScreenshot(generation));
        }
        private IEnumerator RelayScreenshot(int request)
        {
            yield return new WaitForSeconds(1);
            for (int i = 3; i >= 0; i--)
            {
                if (tips != null) tips.ShowTips(i.ToString(), 200);
                yield return new WaitForSeconds(1f);
            }
            screenshotRoutine = null;
            if (tips != null) tips.HideTips();
            XvMediaRecorderManager.SaveScreenshot(path =>
            {
                if (this == null || !isActiveAndEnabled || generation != request) return;
                if (tips != null) tips.ShowTips(string.IsNullOrEmpty(path) ? "Photo capture failed or canceled" : "Saved to: " + path, 1f);
                Show(ScreenshotTips, false); isScreenshot = false;
            });
        }
    }
}
