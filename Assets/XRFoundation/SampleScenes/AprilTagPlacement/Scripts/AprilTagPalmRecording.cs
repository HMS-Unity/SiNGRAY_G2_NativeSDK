using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>Left-palm MR recording UI over the existing shared SDK recorder.</summary>
    public sealed class AprilTagPalmRecording : MonoBehaviour
    {
        [SerializeField] private GameObject recorderPrefab;
        [SerializeField] private Interactable recordButton;
        [SerializeField] private TMP_Text buttonText, palmStatus, statusText;
        private XvMediaRecorderManager recorder;
        private Coroutine preparing;
        private bool saving;
        private int saveGeneration;
        private string status = "Ready";
        public string LastSavedPath { get; private set; }
        public bool IsSaving => saving;
        public bool IsPreparing => preparing != null;
        public string ButtonLabel => saving ? "Saving..." : preparing != null ? "Cancel" :
            recorder != null && recorder.IsVideoRecording() ? "Stop" : "Record";

        private void OnEnable()
        {
            if (Application.isPlaying) Singray.UI.MrtkClickGate.Install(recordButton);
            Refresh();
        }
        public void ToggleRecording()
        {
            if (!isActiveAndEnabled || saving) return;
            if (preparing != null)
            {
                StopCoroutine(preparing); preparing = null;
                ReleaseUnusedCapture(); SetStatus("Cancelled"); return;
            }
            try
            {
                if (!EnsureRecorder()) { SetStatus("Unavailable"); return; }
                if (recorder.IsVideoRecording()) { FinishRecording(); return; }
                recorder.XvMRVideoCaptureManager.CaptureType = CaptureType.MR;
                preparing = StartCoroutine(PrepareRecording());
                SetStatus("Waiting");
            }
            catch (Exception error)
            {
                Debug.LogException(error, this); ReleaseUnusedCapture(); SetStatus("Failed");
            }
        }
        private bool EnsureRecorder()
        {
            if (recorder != null) return true;
            if (recorderPrefab == null) return false;
            // Resolve shared RGB/compositor services before recorder Awake queries them.
            if (FindObjectOfType<XvCameraManager>() == null)
            {
                var cameraObject = new GameObject("Demo RGB Camera"); cameraObject.transform.SetParent(transform, false);
                cameraObject.AddComponent<XvCameraManager>();
            }
            if (FindObjectOfType<XvMRVideoCaptureManager>() == null)
            {
                var capturePrefab = Resources.Load<GameObject>("XvMRVideoCaptureManager");
                if (capturePrefab == null) return false;
                var capture = Instantiate(capturePrefab, transform); capture.name = "Demo MR Capture";
                _ = capture.GetComponent<XvMRVideoCaptureManager>().CameraManager;
            }
            var instance = Instantiate(recorderPrefab, transform); instance.name = "Demo MR Recorder";
            recorder = instance.GetComponent<XvMediaRecorderManager>();
            return recorder != null;
        }
        private IEnumerator PrepareRecording()
        {
            // Yield before the caller stores the handle, including immediate failures.
            yield return null;
            bool started = false;
            try { recorder.StartCapture(); started = true; }
            catch (Exception error) { Debug.LogException(error, this); }
            if (!started)
            {
                preparing = null; ReleaseUnusedCapture(); SetStatus("Failed"); yield break;
            }
            float deadline = Time.realtimeSinceStartup + 10;
            while (!recorder.IsCameraFrameReady() && Time.realtimeSinceStartup < deadline) yield return null;
            preparing = null;
            if (!recorder.IsCameraFrameReady())
            {
                ReleaseUnusedCapture(); SetStatus("No frame"); yield break;
            }
            try
            {
                recorder.StartRecording();
                SetStatus(recorder.IsVideoRecording() ? "Recording" : "Failed");
            }
            catch (Exception error) { Debug.LogException(error, this); SetStatus("Failed"); }
            finally { ReleaseUnusedCapture(); }
        }
        private void FinishRecording()
        {
            if (saving || recorder == null) return;
            saving = true; int generation = ++saveGeneration; SetStatus("Saving...");
            try
            {
                recorder.StopRecording(path =>
                {
                    if (this == null || generation != saveGeneration) return;
                    saving = false; LastSavedPath = path;
                    ReleaseUnusedCapture(); SetStatus(string.IsNullOrEmpty(path) ? "Failed" : "Saved");
                    Debug.Log("[TAG-REC] " + (string.IsNullOrEmpty(path) ? "No saved file" : "Saved: " + path));
                });
            }
            catch (Exception error)
            { saving = false; Debug.LogException(error, this); ReleaseUnusedCapture(); SetStatus("Failed"); }
        }
        private void ReleaseUnusedCapture()
        {
            if (recorder != null && preparing == null && !saving && !recorder.IsVideoRecording())
                recorder.StopCapture(true); // Releases only this recorder's shared occupancy.
        }
        private void SetStatus(string value) { status = value; Refresh(); }
        private void Refresh()
        {
            string label = ButtonLabel;
            if (buttonText != null && buttonText.text != label) buttonText.text = label;
            if (palmStatus != null && palmStatus.text != status) palmStatus.text = status;
            string visible = status == "Ready" ? "" : status == "Recording" ? "REC" : status;
            if (statusText != null && statusText.text != visible) statusText.text = visible;
            if (recordButton != null) recordButton.IsEnabled = !saving;
        }
        private void Update()
        {
            if (status == "Recording" && recorder != null && !recorder.IsVideoRecording())
            { ReleaseUnusedCapture(); SetStatus("Stopped"); }
            Refresh();
        }
        private void Shutdown()
        {
            if (preparing != null) { StopCoroutine(preparing); preparing = null; }
            if (recorder != null && recorder.IsVideoRecording() && !saving) FinishRecording();
            ReleaseUnusedCapture();
            if (recordButton != null) recordButton.GetComponent<Singray.UI.MrtkClickGate>()?.CancelPending();
            if (!saving && status == "Waiting") SetStatus("Cancelled");
        }
        private void OnDisable() => Shutdown();
        private void OnApplicationPause(bool paused) { if (paused) Shutdown(); }
        private void OnApplicationQuit() => Shutdown();
    }
}
