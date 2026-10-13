using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>
    /// Navigation controller for the SDK Samples hub.
    /// The hub owns the shared XR rig while feature scenes are loaded additively.
    /// </summary>
    public sealed class SdkSamplesHubController : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject sampleToolbar;
        [SerializeField] private TMP_Text sectionTitle;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text activeSampleText;
        [SerializeField] private GameObject[] categoryPanels;
        [SerializeField] private Transform sampleRuntimeRoot;
        [SerializeField] private string[] sampleModuleIds;
        [SerializeField] private GameObject[] sampleModulePrefabs;
        [SerializeField] private string currentSampleId;
        [SerializeField] private XvMediaRecorderManager mediaRecorderManager;
        [SerializeField] private ButtonConfigHelper recordButtonConfig;
        [SerializeField] private ButtonConfigHelper streamButtonConfig;
        private XvRTSPStreamerManager streamManager;
        private Coroutine recordingPreparation;
        private bool savingRecording;

        private Coroutine transitionRoutine;
        private GameObject activeSampleModule;

        private void Awake()
        {
            SetupRecordingButton();
            // The recorder belongs to the hub, not to an individual sample. Create it
            // before any sample module opens so it cannot bind to a temporary sensor
            // camera that will be destroyed during navigation.
            EnsureMediaRecorder();
            EnsureStreamer();
            ShowCategory(0);
            SetSampleMode(false);
            SetStatus("Select a sample to begin");
        }

        private void Update()
        {
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) &&
                !string.IsNullOrEmpty(currentSampleId))
            {
                ReturnHome();
            }
        }

        public void ShowCategory(int categoryIndex)
        {
            if (categoryPanels == null || categoryPanels.Length == 0)
            {
                return;
            }

            categoryIndex = Mathf.Clamp(categoryIndex, 0, categoryPanels.Length - 1);
            for (int i = 0; i < categoryPanels.Length; i++)
            {
                if (categoryPanels[i] != null)
                {
                    categoryPanels[i].SetActive(i == categoryIndex);
                }
            }

            if (sectionTitle != null)
            {
                sectionTitle.text = categoryPanels[categoryIndex].name;
            }
        }

        public void LoadSample(string moduleId)
        {
            if (transitionRoutine == null && !string.IsNullOrWhiteSpace(moduleId))
            {
                transitionRoutine = StartCoroutine(SwitchSample(moduleId));
            }
        }

        public void ReturnHome()
        {
            if (transitionRoutine == null)
            {
                transitionRoutine = StartCoroutine(SwitchSample(string.Empty));
            }
        }

        public void ToggleMrRecording()
        {
            if (savingRecording) return;
            if (recordingPreparation != null)
            {
                StopCoroutine(recordingPreparation);
                recordingPreparation = null;
                ReleaseUnusedCapture();
                SetRecordButtonLabel(false);
                SetStatus("Recording cancelled");
                return;
            }
            if (!EnsureMediaRecorder())
            {
                SetStatus("MR recorder is not available");
                return;
            }

            if (mediaRecorderManager.IsVideoRecording())
            {
                StopMrRecording();
                return;
            }

            XvMRVideoCaptureManager captureManager = mediaRecorderManager.XvMRVideoCaptureManager;
            captureManager.CaptureType = CaptureType.MR;
            recordingPreparation = StartCoroutine(PrepareRecording());
        }

        private IEnumerator PrepareRecording()
        {
            SetStatus("Waiting for the MR camera...");
            if (recordButtonConfig != null) recordButtonConfig.MainLabelText = "Cancel Recording";
            mediaRecorderManager.StartCapture();
            float deadline = Time.realtimeSinceStartup + 10f;
            yield return null;
            while (!mediaRecorderManager.IsCameraFrameReady() && Time.realtimeSinceStartup < deadline)
                yield return null;
            recordingPreparation = null;
            if (!mediaRecorderManager.IsCameraFrameReady())
            {
                ReleaseUnusedCapture();
                SetRecordButtonLabel(false);
                SetStatus("No MR camera frame. Check the device camera and try again.");
                yield break;
            }
            try
            {
                mediaRecorderManager.StartRecording();
                SetRecordButtonLabel(mediaRecorderManager.IsVideoRecording());
                SetStatus("MR recording started");
            }
            catch (System.Exception error)
            {
                Debug.LogException(error, this);
                SetStatus("Recording failed. Check the device log.");
                SetRecordButtonLabel(mediaRecorderManager.IsVideoRecording());
            }
        }

        private void EnsureStreamer()
        {
            if (streamManager != null) return;
            // Own the streamer at hub level so sample navigation cannot destroy it.
            GameObject streamerObject = new GameObject("SDK Hub RTSP Streamer");
            streamerObject.transform.SetParent(transform, false);
            streamManager = streamerObject.AddComponent<XvRTSPStreamerManager>();
            streamManager.autoStreaming = false;
        }

        public void ToggleRtspStreaming()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                SetStatus("RTSP streaming requires an Android device");
                return;
            }
            EnsureStreamer();
            try
            {
                if (streamManager.IsStreeaming)
                {
                    streamManager.StopRtspStreaming();
                    ReleaseUnusedCapture();
                    SetStatus("RTSP streaming stopped");
                }
                else
                {
                    streamManager.XvMRVideoCaptureManager.CaptureType = CaptureType.MR;
                    streamManager.StartRtspStreaming();
                    SetStatus("RTSP streaming started: rtsp://<device-ip>:8554/stream/");
                }
            }
            catch (System.Exception error)
            {
                Debug.LogException(error, this);
                SetStatus("Streaming failed. Check the device log.");
            }
            if (streamButtonConfig != null)
                streamButtonConfig.MainLabelText = streamManager.IsStreeaming ? "Stop Streaming" : "Start Streaming";
        }

        private void ReleaseUnusedCapture()
        {
            if (mediaRecorderManager != null && !mediaRecorderManager.IsVideoRecording() &&
                recordingPreparation == null && !savingRecording &&
                (streamManager == null || !streamManager.IsStreeaming))
                mediaRecorderManager.StopCapture(false);
        }

        private void StopMrRecording()
        {
            if (savingRecording) return;
            if (mediaRecorderManager == null || !mediaRecorderManager.IsVideoRecording())
            {
                SetRecordButtonLabel(false);
                return;
            }

            SetStatus("Saving MR recording...");
            savingRecording = true;
            if (recordButtonConfig != null) recordButtonConfig.MainLabelText = "Saving...";
            mediaRecorderManager.StopRecording(path =>
            {
                savingRecording = false;
                ReleaseUnusedCapture();
                SetRecordButtonLabel(false);
                SetStatus(string.IsNullOrEmpty(path) ? "MR recording stopped" : "Saved: " + path);
            });
        }

        private bool EnsureMediaRecorder()
        {
            if (mediaRecorderManager != null)
            {
                return true;
            }

            mediaRecorderManager = FindObjectOfType<XvMediaRecorderManager>();
            if (mediaRecorderManager != null)
            {
                return true;
            }

            GameObject recorderPrefab = Resources.Load<GameObject>("XvMediaRecorderManager");
            if (recorderPrefab == null)
            {
                return false;
            }

            GameObject recorderObject = Instantiate(recorderPrefab, transform);
            recorderObject.name = "SDK Hub MR Recorder";
            mediaRecorderManager = recorderObject.GetComponent<XvMediaRecorderManager>();
            return mediaRecorderManager != null;
        }

        private void SetupRecordingButton()
        {
            Transform handMenu = null;
            HandConstraintPalmUp[] handMenus = FindObjectsOfType<HandConstraintPalmUp>(true);
            for (int i = 0; i < handMenus.Length; i++)
            {
                if (handMenus[i].name == "Left Hand SDK Menu")
                {
                    handMenu = handMenus[i].transform;
                    break;
                }
            }

            Transform content = handMenu == null ? null : handMenu.Find("Menu Content");
            if (content == null)
            {
                return;
            }

            Transform recordTransform = content.Find("MR Recording");
            if (recordTransform == null)
            {
                Transform template = content.Find("Hide");
                if (template == null)
                {
                    return;
                }
                recordTransform = Instantiate(template.gameObject, content).transform;
                recordTransform.name = "MR Recording";
            }

            Transform streamTransform = content.Find("RTSP Streaming");
            if (streamTransform == null)
            {
                streamTransform = Instantiate(recordTransform.gameObject, content).transform;
                streamTransform.name = "RTSP Streaming";
            }
            ConfigureQuickButton(content.Find("Samples"), -0.115f);
            ConfigureQuickButton(content.Find("Home"), 0f);
            ConfigureQuickButton(content.Find("Hide"), 0.115f);
            ConfigureQuickButton(recordTransform, -0.115f, -0.06f);
            ConfigureQuickButton(streamTransform, 0.115f, -0.06f);
            recordTransform.localScale = new Vector3(0.15f / 0.10f, 0.042f / 0.04f, 0.042f / 0.04f);
            streamTransform.localScale = recordTransform.localScale;

            Transform backplate = content.Find("MRTK Backplate");
            if (backplate != null)
            {
                Vector3 scale = backplate.localScale;
                scale.x = 0.38f;
                scale.y = 0.15f;
                backplate.localScale = scale;
                Vector3 platePosition = backplate.localPosition;
                platePosition.y = -0.03f;
                backplate.localPosition = platePosition;
            }

            Interactable recordInteractable = recordTransform.GetComponent<Interactable>();
            if (recordInteractable == null)
            {
                recordInteractable = recordTransform.GetComponentInChildren<Interactable>(true);
            }
            if (recordInteractable != null)
            {
                recordInteractable.OnClick = new UnityEngine.Events.UnityEvent();
                recordInteractable.OnClick.AddListener(ToggleMrRecording);
            }

            recordButtonConfig = recordTransform.GetComponent<ButtonConfigHelper>();
            if (recordButtonConfig == null)
            {
                recordButtonConfig = recordTransform.GetComponentInChildren<ButtonConfigHelper>(true);
            }
            SetRecordButtonLabel(false);
            Interactable streamInteractable = streamTransform.GetComponentInChildren<Interactable>(true);
            if (streamInteractable != null)
            {
                streamInteractable.OnClick = new UnityEngine.Events.UnityEvent();
                streamInteractable.OnClick.AddListener(ToggleRtspStreaming);
            }
            streamButtonConfig = streamTransform.GetComponentInChildren<ButtonConfigHelper>(true);
            if (streamButtonConfig != null) streamButtonConfig.MainLabelText = "Start Streaming";
            foreach (var button in content.GetComponentsInChildren<Interactable>(true))
                Singray.UI.MrtkClickGate.Install(button);
        }

        private static void ConfigureQuickButton(Transform button, float x, float y = 0f)
        {
            if (button == null)
            {
                return;
            }
            Vector3 position = button.localPosition;
            position.x = x;
            position.y = y;
            button.localPosition = position;
        }

        private void SetRecordButtonLabel(bool isRecording)
        {
            if (recordButtonConfig != null)
            {
                recordButtonConfig.MainLabelText = isRecording ? "Stop Recording" : "Start Recording";
            }
        }

        /// <summary>Shows the catalog from the left-hand quick menu.</summary>
        public void ShowSamples()
        {
            if (transitionRoutine != null)
            {
                return;
            }

            if (canvasRoot != null)
            {
                canvasRoot.SetActive(true);
            }

            if (!string.IsNullOrEmpty(currentSampleId))
            {
                ReturnHome();
                return;
            }

            SetSampleMode(false);
            SetStatus("Select a sample to begin");
        }

        /// <summary>Hides the large catalog while keeping the hand menu available.</summary>
        public void HideSamplesPanel()
        {
            if (transitionRoutine == null && string.IsNullOrEmpty(currentSampleId))
            {
                if (canvasRoot != null)
                {
                    canvasRoot.SetActive(false);
                }
                else if (menuRoot != null)
                {
                    menuRoot.SetActive(false);
                }
                SetStatus("Raise your left palm to reopen SDK Samples");
            }
        }

        private IEnumerator SwitchSample(string nextModuleId)
        {
            SetStatus("Preparing sample...");
            SetInteractable(false);

            if (activeSampleModule != null)
            {
                ShutdownActiveSample();
                // Give native stop commands and event unsubscriptions one frame to
                // complete before any referenced Unity objects are destroyed.
                yield return null;
                activeSampleModule.SetActive(false);
                Destroy(activeSampleModule);
                activeSampleModule = null;
                yield return null;
            }
            currentSampleId = string.Empty;

            if (string.IsNullOrEmpty(nextModuleId))
            {
                SetSampleMode(false);
                SetStatus("Select a sample to begin");
                SetInteractable(true);
                transitionRoutine = null;
                yield break;
            }

            int moduleIndex = FindModuleIndex(nextModuleId);
            if (moduleIndex < 0 || sampleModulePrefabs[moduleIndex] == null)
            {
                Debug.LogError($"SDK Samples: module '{nextModuleId}' is not configured.");
                SetSampleMode(false);
                SetStatus($"Module not available: {nextModuleId}");
                SetInteractable(true);
                transitionRoutine = null;
                yield break;
            }

            SetStatus($"Opening {nextModuleId}...");
            activeSampleModule = Instantiate(sampleModulePrefabs[moduleIndex], sampleRuntimeRoot);
            activeSampleModule.name = nextModuleId + " Sample Module";
            activeSampleModule.SetActive(true);

            currentSampleId = nextModuleId;
            if (activeSampleText != null)
            {
                activeSampleText.text = nextModuleId;
            }

            SetSampleMode(true);
            SetStatus($"Running {nextModuleId}");
            SetInteractable(true);
            transitionRoutine = null;
        }

        private void ShutdownActiveSample()
        {
            MonoBehaviour[] behaviours = activeSampleModule.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ISdkSampleLifecycle lifecycle)
                {
                    try
                    {
                        lifecycle.ShutdownSample();
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogException(exception, behaviours[i]);
                    }
                }
            }
        }

        private int FindModuleIndex(string moduleId)
        {
            if (sampleModuleIds == null || sampleModulePrefabs == null)
            {
                return -1;
            }

            int count = Mathf.Min(sampleModuleIds.Length, sampleModulePrefabs.Length);
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(sampleModuleIds[i], moduleId, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetSampleMode(bool sampleIsOpen)
        {
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(true);
            }

            if (menuRoot != null)
            {
                menuRoot.SetActive(!sampleIsOpen);
            }

            if (sampleToolbar != null)
            {
                sampleToolbar.SetActive(sampleIsOpen);
            }
        }

        private void SetInteractable(bool value)
        {
            SetRootInteractable(menuRoot, value);
            SetRootInteractable(sampleToolbar, value);
        }

        private static void SetRootInteractable(GameObject root, bool value)
        {
            if (root == null)
            {
                return;
            }

            Interactable[] buttons = root.GetComponentsInChildren<Interactable>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].IsEnabled = value;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void OnApplicationQuit()
        {
            if (streamManager != null) streamManager.StopRtspStreaming();
            StopMrRecording();
        }
    }
}
