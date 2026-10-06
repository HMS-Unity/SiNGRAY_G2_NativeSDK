using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Singray.Foundation.SampleScenes;
using Singray.UI.Input;

namespace Singray.Foundation.SampleEditor
{
    public static class SdkSamplesHubSceneBuilder
    {
        private const string SceneFolder = "Assets/XRFoundation/SampleScenes/SDKSampleHub/Scenes";
        private const string ScenePath = SceneFolder + "/SDKSamples.unity";
        private const string ModuleFolder = "Assets/XRFoundation/SampleScenes/SDKSampleHub/Modules";
        private const string SampleSceneFolder = "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/";
        private const string MrtkButtonPath = "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2_32x96.prefab";
        private const string MrtkBackplatePath = "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/UI Backplate.prefab";
        private const string MrtkKeyboardPath = "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/NonNativeKeyboard/Prefabs/NonNativeKeyboard.prefab";

        private static readonly Color Primary = new Color32(45, 212, 191, 255);
        private static readonly Color TextPrimary = new Color32(241, 245, 249, 255);
        private static readonly Color TextMuted = new Color32(148, 163, 184, 255);

        private sealed class SampleDefinition
        {
            public readonly string Title;
            public readonly string Scene;

            public SampleDefinition(string title, string scene)
            {
                Title = title;
                Scene = scene;
            }
        }

        private sealed class CategoryDefinition
        {
            public readonly string Name;
            public readonly SampleDefinition[] Samples;

            public CategoryDefinition(string name, params SampleDefinition[] samples)
            {
                Name = name;
                Samples = samples;
            }
        }

        private static readonly CategoryDefinition[] Categories =
        {
            new CategoryDefinition("Sensors & Cameras",
                new SampleDefinition("Camera Viewer", "Viewer"),
                new SampleDefinition("RGBD", "Rgbd"),
                new SampleDefinition("ToF Point Cloud", "TofPointCloud"),
                new SampleDefinition("MR Video Capture", "MRVideoCapture")),
            new CategoryDefinition("Spatial Understanding",
                new SampleDefinition("Plane Detection", "PlaneDetection"),
                new SampleDefinition("Spatial Map", "SpatialMap"),
                new SampleDefinition("Spatial Mesh", "SpatialMesh"),
                new SampleDefinition("Tag Recognizer", "TagRecognizer")),
            new CategoryDefinition("Interaction",
                new SampleDefinition("Keyboard", "Keyboard"),
                new SampleDefinition("Static Gesture", "StaticGesture"),
                new SampleDefinition("MRTK2", "MRTK2")),
            new CategoryDefinition("Media & System",
                new SampleDefinition("Media Recorder", "MediaRecorder"),
                new SampleDefinition("RTSP Streamer", "RTSPStreamer"),
                new SampleDefinition("System Settings", "SystemSetting"))
        };

        [MenuItem("Singray/XR/SDK Samples/Rebuild Hub Scene", false, 1)]
        public static void RebuildHubScene()
        {
            EnsureFolder(SceneFolder);
            EnsureFolder(ModuleFolder);
            Dictionary<string, GameObject> sampleModules = GenerateSampleModulePrefabs();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SDKSamples";

            InstantiatePrefab("Assets/XRFoundation/Core/Sdk/XR/Resources/MixedRealityToolkit.prefab");
            InstantiatePrefab("Assets/XRFoundation/Core/Sdk/XR/Resources/XvXRInput.prefab");
            InstantiatePrefab("Assets/XRFoundation/Core/Sdk/XR/Resources/XvXRManager.prefab");

            CreateLighting();
            SdkSamplesHubController controller = CreateHubInterface(sampleModules);
            CreateLeftHandMenu(controller);
            CreateInputSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Unable to save SDK Samples scene at " + ScenePath);
            }

            UpdateBuildSettings();
            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
            Debug.Log("SDK Samples Hub created: " + ScenePath);
        }

        private static SdkSamplesHubController CreateHubInterface(Dictionary<string, GameObject> sampleModules)
        {
            GameObject controllerObject = new GameObject("SDK Samples Hub");
            SdkSamplesHubController controller = controllerObject.AddComponent<SdkSamplesHubController>();

            GameObject recorderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/XRFoundation/Core/Foundation/MediaRecorder/Resources/XvMediaRecorderManager.prefab");
            if (recorderPrefab == null)
            {
                throw new FileNotFoundException("MR recorder manager prefab was not found.");
            }
            GameObject recorderObject = (GameObject)PrefabUtility.InstantiatePrefab(recorderPrefab);
            recorderObject.name = "SDK Hub MR Recorder";
            XvMediaRecorderManager recorderManager = recorderObject.GetComponent<XvMediaRecorderManager>();

            GameObject sampleRuntimeRoot = new GameObject("Active Sample Module");
            sampleRuntimeRoot.transform.SetParent(controllerObject.transform, false);

            GameObject catalog = new GameObject("SDK Samples Panel");
            catalog.transform.SetParent(controllerObject.transform, false);
            catalog.transform.position = new Vector3(0f, 0.02f, 1.35f);
            catalog.transform.localScale = Vector3.one * 1.3f;
            CreateBackplate(catalog.transform, "Main Backplate", new Vector3(0f, 0f, 0.018f), 1.20f, 0.66f);

            CreateWorldText(catalog.transform, "Eyebrow", "SINGRAY XR FOUNDATION",
                new Vector3(0f, 0.275f, -0.018f), 1.06f, 0.045f, 0.09f, TextAlignmentOptions.MidlineLeft, Primary);
            CreateWorldText(catalog.transform, "Title", "SDK Samples",
                new Vector3(0f, 0.215f, -0.018f), 1.06f, 0.07f, 0.22f, TextAlignmentOptions.MidlineLeft, TextPrimary);

            TMP_Text sectionTitle = CreateWorldText(catalog.transform, "Section Title", Categories[0].Name,
                new Vector3(0.13f, 0.125f, -0.018f), 0.78f, 0.05f, 0.13f, TextAlignmentOptions.MidlineLeft, TextPrimary);

            GameObject[] categoryPanels = new GameObject[Categories.Length];
            float[] categoryY = { 0.085f, -0.005f, -0.095f, -0.185f };
            for (int i = 0; i < Categories.Length; i++)
            {
                CategoryDefinition category = Categories[i];
                Interactable tab = CreateMrtkButton(catalog.transform, "Category " + category.Name, category.Name,
                    new Vector3(-0.445f, categoryY[i], -0.018f), 0.25f, 0.072f);
                UnityEventTools.AddIntPersistentListener(tab.OnClick, controller.ShowCategory, i);

                GameObject panel = new GameObject(category.Name);
                panel.transform.SetParent(catalog.transform, false);
                for (int sampleIndex = 0; sampleIndex < category.Samples.Length; sampleIndex++)
                {
                    SampleDefinition sample = category.Samples[sampleIndex];
                    int row = sampleIndex / 3;
                    int column = sampleIndex % 3;
                    Vector3 position = new Vector3(-0.14f + column * 0.255f, 0.035f - row * 0.105f, -0.018f);
                    Interactable sampleButton = CreateMrtkButton(panel.transform, sample.Title, sample.Title,
                        position, 0.22f, 0.078f);
                    UnityEventTools.AddStringPersistentListener(sampleButton.OnClick, controller.LoadSample, sample.Scene);
                }

                categoryPanels[i] = panel;
            }

            TMP_Text status = CreateWorldText(catalog.transform, "Status", "Select a sample to begin",
                new Vector3(0.13f, -0.265f, -0.018f), 0.78f, 0.04f, 0.08f, TextAlignmentOptions.MidlineLeft, TextMuted);

            GameObject toolbar = new GameObject("Sample Toolbar");
            toolbar.transform.SetParent(controllerObject.transform, false);
            toolbar.transform.position = new Vector3(0f, 0.35f, 1.20f);
            toolbar.transform.localScale = Vector3.one * 0.5f;
            CreateBackplate(toolbar.transform, "Toolbar Backplate", new Vector3(0f, 0f, 0.018f), 0.78f, 0.11f);
            Interactable home = CreateMrtkButton(toolbar.transform, "Back to Samples", "Back to Samples",
                new Vector3(-0.255f, 0f, -0.018f), 0.24f, 0.072f);
            UnityEventTools.AddPersistentListener(home.OnClick, controller.ReturnHome);
            TMP_Text activeSample = CreateWorldText(toolbar.transform, "Active Sample", "Sample",
                new Vector3(0.13f, 0f, -0.018f), 0.43f, 0.05f, 0.10f, TextAlignmentOptions.MidlineLeft, TextPrimary);
            toolbar.SetActive(false);

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("menuRoot").objectReferenceValue = catalog;
            serializedController.FindProperty("canvasRoot").objectReferenceValue = catalog;
            serializedController.FindProperty("sampleToolbar").objectReferenceValue = toolbar;
            serializedController.FindProperty("sectionTitle").objectReferenceValue = sectionTitle;
            serializedController.FindProperty("statusText").objectReferenceValue = status;
            serializedController.FindProperty("activeSampleText").objectReferenceValue = activeSample;
            serializedController.FindProperty("sampleRuntimeRoot").objectReferenceValue = sampleRuntimeRoot.transform;
            serializedController.FindProperty("mediaRecorderManager").objectReferenceValue = recorderManager;
            SerializedProperty panels = serializedController.FindProperty("categoryPanels");
            panels.arraySize = categoryPanels.Length;
            for (int i = 0; i < categoryPanels.Length; i++)
            {
                panels.GetArrayElementAtIndex(i).objectReferenceValue = categoryPanels[i];
            }
            SampleDefinition[] allSamples = Categories.SelectMany(category => category.Samples).ToArray();
            SerializedProperty moduleIds = serializedController.FindProperty("sampleModuleIds");
            SerializedProperty modulePrefabs = serializedController.FindProperty("sampleModulePrefabs");
            moduleIds.arraySize = allSamples.Length;
            modulePrefabs.arraySize = allSamples.Length;
            for (int i = 0; i < allSamples.Length; i++)
            {
                moduleIds.GetArrayElementAtIndex(i).stringValue = allSamples[i].Scene;
                modulePrefabs.GetArrayElementAtIndex(i).objectReferenceValue = sampleModules[allSamples[i].Scene];
            }
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        private static void CreateLeftHandMenu(SdkSamplesHubController controller)
        {
            GameObject handMenu = new GameObject("Left Hand SDK Menu");
            handMenu.transform.localScale = Vector3.one * 0.5f;
            SolverHandler solverHandler = handMenu.AddComponent<SolverHandler>();
            solverHandler.TrackedTargetType = TrackedObjectType.HandJoint;
            solverHandler.TrackedHandedness = Handedness.Left;
            solverHandler.TrackedHandJoint = TrackedHandJoint.Palm;
            solverHandler.AdditionalOffset = new Vector3(0.025f, 0f, 0.045f);
            solverHandler.AdditionalRotation = Vector3.zero;

            HandConstraintPalmUp constraint = handMenu.AddComponent<HandConstraintPalmUp>();
            // On the target G2 hand pose this is visually left of the left hand.
            constraint.SafeZone = HandConstraint.SolverSafeZone.UlnarSide;
            constraint.SafeZoneBuffer = 0.025f;
            constraint.RotationBehavior = HandConstraint.SolverRotationBehavior.LookAtMainCamera;
            constraint.RequireFlatHand = false;
            constraint.FacingCameraTrackingThreshold = 75f;
            constraint.FollowHandUntilFacingCamera = true;
            constraint.UseGazeActivation = false;

            GameObject content = new GameObject("Menu Content");
            content.transform.SetParent(handMenu.transform, false);
            CreateBackplate(content.transform, "MRTK Backplate", new Vector3(0f, 0f, 0.014f), 0.46f, 0.095f);

            Interactable samples = CreateMrtkButton(content.transform, "Samples", "Samples",
                new Vector3(-0.17f, 0f, -0.012f), 0.095f, 0.042f);
            Interactable home = CreateMrtkButton(content.transform, "Home", "Home",
                new Vector3(-0.055f, 0f, -0.012f), 0.095f, 0.042f);
            Interactable hide = CreateMrtkButton(content.transform, "Hide", "Hide",
                new Vector3(0.055f, 0f, -0.012f), 0.095f, 0.042f);
            Interactable record = CreateMrtkButton(content.transform, "MR Recording", "Start MR Rec",
                new Vector3(0.17f, 0f, -0.012f), 0.11f, 0.042f);
            UnityEventTools.AddPersistentListener(samples.OnClick, controller.ShowSamples);
            UnityEventTools.AddPersistentListener(home.OnClick, controller.ReturnHome);
            UnityEventTools.AddPersistentListener(hide.OnClick, controller.HideSamplesPanel);
            UnityEventTools.AddPersistentListener(record.OnClick, controller.ToggleMrRecording);

            ButtonConfigHelper recordConfig = record.GetComponent<ButtonConfigHelper>();
            if (recordConfig == null)
            {
                recordConfig = record.GetComponentInChildren<ButtonConfigHelper>(true);
            }
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("recordButtonConfig").objectReferenceValue = recordConfig;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddBoolPersistentListener(constraint.OnFirstHandDetected, content.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(constraint.OnLastHandLost, content.SetActive, false);
            content.SetActive(false);
        }

        private static Interactable CreateMrtkButton(Transform parent, string name, string label, Vector3 position, float width, float height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MrtkButtonPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("MRTK pressable button was not found", MrtkButtonPath);
            }

            GameObject button = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            button.name = name;
            button.transform.localPosition = position;
            button.transform.localRotation = Quaternion.identity;
            float depthScale = Mathf.Min(width / 0.10f, height / 0.04f);
            button.transform.localScale = new Vector3(width / 0.10f, height / 0.04f, depthScale);

            ButtonConfigHelper config = button.GetComponent<ButtonConfigHelper>();
            if (config == null)
            {
                config = button.GetComponentInChildren<ButtonConfigHelper>(true);
            }
            if (config != null)
            {
                config.MainLabelText = label;
                config.SeeItSayItLabelEnabled = false;
            }

            Interactable interactable = button.GetComponent<Interactable>();
            if (interactable == null)
            {
                interactable = button.GetComponentInChildren<Interactable>(true);
            }
            if (interactable == null)
            {
                throw new MissingComponentException("MRTK button prefab does not contain an Interactable component.");
            }

            return interactable;
        }

        private static void CreateBackplate(Transform parent, string name, Vector3 position, float width, float height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MrtkBackplatePath);
            if (prefab == null)
            {
                throw new FileNotFoundException("MRTK backplate was not found", MrtkBackplatePath);
            }

            GameObject backplate = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            backplate.name = name;
            backplate.transform.localPosition = position;
            backplate.transform.localRotation = Quaternion.identity;
            backplate.transform.localScale = new Vector3(width, height, 0.01f);
        }

        private static TMP_Text CreateWorldText(Transform parent, string name, string value, Vector3 position,
            float width, float height, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(TextMeshPro));
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one;

            TextMeshPro text = textObject.GetComponent<TextMeshPro>();
            text.text = value;
            GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MrtkButtonPath);
            TextMeshPro template = buttonPrefab == null
                ? null
                : buttonPrefab.GetComponentsInChildren<TextMeshPro>(true).FirstOrDefault(item => item.font != null);
            if (template != null)
            {
                text.font = template.font;
                text.fontSharedMaterial = template.fontSharedMaterial;
            }
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            return text;
        }

        private static Dictionary<string, GameObject> GenerateSampleModulePrefabs()
        {
            Dictionary<string, GameObject> modules = new Dictionary<string, GameObject>();
            HashSet<string> expectedPrefabPaths = new HashSet<string>(
                Categories.SelectMany(category => category.Samples)
                    .Select(sample => ModuleFolder + "/" + sample.Scene + "SampleModule.prefab"));

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ModuleFolder }))
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guid);
                if (existingPath.EndsWith("SampleModule.prefab") && !expectedPrefabPaths.Contains(existingPath))
                {
                    AssetDatabase.DeleteAsset(existingPath);
                }
            }

            foreach (SampleDefinition sample in Categories.SelectMany(category => category.Samples))
            {
                if (sample.Scene == "Keyboard")
                {
                    GameObject keyboardModule = CreateMrtkKeyboardModulePrefab();
                    modules.Add(sample.Scene, keyboardModule);
                    continue;
                }

                string sourcePath = SampleSceneFolder + sample.Scene + ".unity";
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("SDK sample source scene was not found", sourcePath);
                }

                Scene sourceScene = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
                try
                {
                    GameObject[] sourceRoots = sourceScene.GetRootGameObjects();
                    GameObject moduleRoot = new GameObject(sample.Scene + " Sample Module");
                    SceneManager.MoveGameObjectToScene(moduleRoot, sourceScene);
                    moduleRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                    foreach (GameObject sourceRoot in sourceRoots)
                    {
                        if (ShouldIncludeSampleRoot(sourceRoot))
                        {
                            sourceRoot.transform.SetParent(moduleRoot.transform, true);
                        }
                    }

                    if (moduleRoot.transform.childCount == 0)
                    {
                        throw new InvalidDataException("SDK sample has no module content: " + sample.Scene);
                    }

                    RemoveMissingScripts(moduleRoot);
                    moduleRoot.SetActive(false);
                    string prefabPath = ModuleFolder + "/" + sample.Scene + "SampleModule.prefab";
                    bool saved;
                    GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(moduleRoot, prefabPath, out saved);
                    if (!saved || savedPrefab == null)
                    {
                        throw new IOException("Unable to create SDK sample module: " + prefabPath);
                    }

                    modules.Add(sample.Scene, savedPrefab);
                }
                finally
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            AssetDatabase.SaveAssets();
            return modules;
        }

        private static GameObject CreateMrtkKeyboardModulePrefab()
        {
            GameObject keyboardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MrtkKeyboardPath);
            if (keyboardPrefab == null)
            {
                throw new FileNotFoundException("MRTK NonNativeKeyboard prefab was not found", MrtkKeyboardPath);
            }

            GameObject moduleRoot = new GameObject("Keyboard Sample Module");
            GameObject panel = new GameObject("MRTK Keyboard Panel");
            panel.transform.SetParent(moduleRoot.transform, false);
            panel.transform.position = new Vector3(0f, 0.05f, 1.2f);
            panel.transform.localScale = Vector3.one * 0.5f;

            CreateBackplate(panel.transform, "Keyboard Backplate", new Vector3(0f, 0f, 0.018f), 0.74f, 0.22f);
            CreateWorldText(panel.transform, "Title", "MRTK Keyboard",
                new Vector3(0f, 0.065f, -0.018f), 0.62f, 0.05f, 0.13f,
                TextAlignmentOptions.MidlineLeft, TextPrimary);
            TMP_Text output = CreateWorldText(panel.transform, "Keyboard Output", "Tap the button or a key to start typing",
                new Vector3(-0.09f, -0.025f, -0.018f), 0.44f, 0.07f, 0.075f,
                TextAlignmentOptions.MidlineLeft, TextMuted);
            Interactable openButton = CreateMrtkButton(panel.transform, "Open Keyboard", "Open Keyboard",
                new Vector3(0.245f, -0.025f, -0.018f), 0.19f, 0.07f);

            GameObject keyboardObject = (GameObject)PrefabUtility.InstantiatePrefab(keyboardPrefab, moduleRoot.transform);
            keyboardObject.name = "MRTK NonNativeKeyboard";
            NonNativeKeyboard keyboard = keyboardObject.GetComponent<NonNativeKeyboard>();
            if (keyboard == null)
            {
                throw new MissingComponentException("MRTK keyboard prefab has no NonNativeKeyboard component.");
            }

            MrtkKeyboardSample sample = moduleRoot.AddComponent<MrtkKeyboardSample>();
            SerializedObject serializedSample = new SerializedObject(sample);
            serializedSample.FindProperty("keyboard").objectReferenceValue = keyboard;
            serializedSample.FindProperty("outputText").objectReferenceValue = output;
            serializedSample.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(openButton.OnClick, sample.OpenKeyboard);

            moduleRoot.SetActive(false);
            string prefabPath = ModuleFolder + "/KeyboardSampleModule.prefab";
            bool saved;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(moduleRoot, prefabPath, out saved);
            Object.DestroyImmediate(moduleRoot);
            if (!saved || savedPrefab == null)
            {
                throw new IOException("Unable to create MRTK keyboard sample module: " + prefabPath);
            }

            return savedPrefab;
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
        }

        private static bool ShouldIncludeSampleRoot(GameObject root)
        {
            string name = root.name.Trim();
            if (name.Length == 0 || name.Trim('-').Length == 0)
            {
                return false;
            }

            switch (name)
            {
                case "MixedRealityToolkit":
                case "XvXRManager":
                case "XvXRInput":
                case "XvHeadGazeInputController":
                case "Directional Light":
                    return false;
                default:
                    return true;
            }
        }

        private static void InstantiatePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new FileNotFoundException("Required SDK prefab was not found", path);
            }

            PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void CreateInputSystem()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindObjectOfType<Camera>();
            }

            if (mainCamera == null)
            {
                throw new MissingReferenceException("The SDK XR prefab did not provide a camera for the input module.");
            }

            GameObject inputHost = mainCamera.gameObject;
            if (inputHost.GetComponent<EventSystem>() == null)
            {
                inputHost.AddComponent<EventSystem>();
            }
            if (inputHost.GetComponent<XvXRInputModule>() == null)
            {
                inputHost.AddComponent<XvXRInputModule>();
            }
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        private static void UpdateBuildSettings()
        {
            List<string> requiredScenes = new List<string> { ScenePath };

            List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>();
            HashSet<string> seen = new HashSet<string>();

            foreach (string path in requiredScenes)
            {
                if (File.Exists(path))
                {
                    result.Add(new EditorBuildSettingsScene(path, true));
                    seen.Add(path);
                }
                else
                {
                    Debug.LogWarning("SDK Samples scene is missing: " + path);
                }
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path.Contains("/Joystick/") ||
                    existing.path.EndsWith("/Joystick.unity") ||
                    existing.path.StartsWith(SampleSceneFolder) ||
                    seen.Contains(existing.path))
                {
                    continue;
                }

                result.Add(existing);
            }

            EditorBuildSettings.scenes = result.ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
