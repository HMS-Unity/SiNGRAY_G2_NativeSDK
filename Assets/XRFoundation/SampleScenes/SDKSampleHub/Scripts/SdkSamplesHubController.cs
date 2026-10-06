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

        private Coroutine transitionRoutine;
        private GameObject activeSampleModule;

        private void Awake()
        {
            SetupRecordingButton();
            // The recorder belongs to the hub, not to an individual sample. Create it
            // before any sample module opens so it cannot bind to a temporary sensor
            // camera that will be destroyed during navigation.
            EnsureMediaRecorder();
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
            mediaRecorderManager.StartRecording();
            SetRecordButtonLabel(true);
            SetStatus("MR recording started");
        }

        private void StopMrRecording()
        {
            if (mediaRecorderManager == null || !mediaRecorderManager.IsVideoRecording())
            {
                SetRecordButtonLabel(false);
                return;
            }

            SetStatus("Saving MR recording...");
            mediaRecorderManager.StopRecording(path =>
            {
                mediaRecorderManager.StopCapture(true);
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

            ConfigureQuickButton(content.Find("Samples"), -0.17f);
            ConfigureQuickButton(content.Find("Home"), -0.055f);
            ConfigureQuickButton(content.Find("Hide"), 0.055f);
            ConfigureQuickButton(recordTransform, 0.17f);

            Transform backplate = content.Find("MRTK Backplate");
            if (backplate != null)
            {
                Vector3 scale = backplate.localScale;
                scale.x = 0.46f;
                backplate.localScale = scale;
            }

            Interactable recordInteractable = recordTransform.GetComponent<Interactable>();
            if (recordInteractable == null)
            {
                recordInteractable = recordTransform.GetComponentInChildren<Interactable>(true);
            }
            if (recordInteractable != null)
            {
                recordInteractable.OnClick.RemoveAllListeners();
                recordInteractable.OnClick.AddListener(ToggleMrRecording);
            }

            recordButtonConfig = recordTransform.GetComponent<ButtonConfigHelper>();
            if (recordButtonConfig == null)
            {
                recordButtonConfig = recordTransform.GetComponentInChildren<ButtonConfigHelper>(true);
            }
            SetRecordButtonLabel(false);
        }

        private static void ConfigureQuickButton(Transform button, float x)
        {
            if (button == null)
            {
                return;
            }
            Vector3 position = button.localPosition;
            position.x = x;
            button.localPosition = position;
        }

        private void SetRecordButtonLabel(bool isRecording)
        {
            if (recordButtonConfig != null)
            {
                recordButtonConfig.MainLabelText = isRecording ? "Stop & Save" : "Start MR Rec";
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
            StopMrRecording();
        }
    }
}
