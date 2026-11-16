using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes.Editor
{
    /// <summary>Only writes this demo's assets; never changes Hub or build-scene settings.</summary>
    public static class AprilTagPlacementBuilder
    {
        public const string Folder = "Assets/XRFoundation/SampleScenes/AprilTagPlacement";
        public const string ScenePath = Folder + "/Scenes/AprilTagPlacement.unity";
        public const string ContentPath = Folder + "/Prefabs/AprilTagPlacementDemo.prefab";
        public const string HandPath = Folder + "/Prefabs/MrtkHandCube.prefab";
        public const string ResultPath = Folder + "/Prefabs/AprilTagCube.prefab";
        private const string SourceScene = "Assets/XRFoundation/SampleScenes/MRTK2/Scenes/MRTK2.unity";
        private const string ButtonPath = "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2_32x96.prefab";

        private static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Refs(UnityEngine.Object target, string field, IEnumerable<UnityEngine.Object> values)
        {
            var all = values.ToArray(); var so = new SerializedObject(target); var prop = so.FindProperty(field); prop.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static TMP_Text Text(Transform parent, string name, string message, Vector3 position, Vector2 size, float fontSize = 20)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas)); go.layer = 5; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = Vector3.one * 0.001f;
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 20;
            ((RectTransform)go.transform).sizeDelta = size;
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); textGo.layer = 5; textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
            text.text = message; text.fontSize = fontSize; text.color = Color.white; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center; text.enableWordWrapping = false; text.rectTransform.sizeDelta = size;
            return text;
        }
        private static Interactable Button(Transform parent, string label, Vector3 position, float width = 0.14f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if (asset == null) throw new FileNotFoundException(ButtonPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent); go.name = label; go.transform.localPosition = position;
            go.transform.localScale = new Vector3(width / 0.096f, 1.3f, 1);
            var config = go.GetComponent<ButtonConfigHelper>(); config.MainLabelText = label;
            config.SeeItSayItLabelEnabled = false; config.IconStyle = ButtonIconStyle.None;
            var so = new SerializedObject(config);
            var original = so.FindProperty("mainLabelText").objectReferenceValue as TMP_Text;
            if (original != null) original.gameObject.SetActive(false);
            Text(parent, label + " Label", label, position + new Vector3(0, 0, -0.004f), new Vector2(width * 1000 - 8, 38), 19);
            var click = go.GetComponent<Interactable>(); click.OnClick = new UnityEvent();
            Singray.UI.MrtkClickGate.Install(click); return click;
        }
        private static Interactable ActionButton(Transform parent, string label, Vector3 position, UnityAction action, float width = 0.14f)
        {
            var button = Button(parent, label, position, width); UnityEventTools.AddPersistentListener(button.OnClick, action); return button;
        }
        private static void Record(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
        }

        private static void PrepareHandCube()
        {
            var source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Additive);
            GameObject clone = null;
            try
            {
                var original = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ObjectManipulator>(true))
                    .Single(m => m.name == "Cube1" && m.transform.parent.name == "ManipulationExamples");
                clone = UnityEngine.Object.Instantiate(original.gameObject); clone.name = "MrtkHandCube";
                if (PrefabUtility.IsPartOfPrefabInstance(clone)) PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (Transform child in clone.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); clone.transform.localScale = Vector3.one * 0.16f;
                var mesh = clone.GetComponent<MeshFilter>().sharedMesh;
                if ((mesh.bounds.size - Vector3.one).sqrMagnitude > 0.00001f) throw new InvalidOperationException("Official sample mesh must have unit edges.");
                var manipulator = clone.GetComponent<ObjectManipulator>(); manipulator.HostTransform = clone.transform;
                manipulator.ManipulationType = ManipulationHandFlags.OneHanded | ManipulationHandFlags.TwoHanded;
                manipulator.TwoHandedManipulationType = TransformFlags.Move | TransformFlags.Rotate | TransformFlags.Scale;
                manipulator.ReleaseBehavior = 0;
                manipulator.OnManipulationStarted = new ManipulationEvent(); manipulator.OnManipulationEnded = new ManipulationEvent();
                manipulator.OnHoverEntered = new ManipulationEvent(); manipulator.OnHoverExited = new ManipulationEvent();
                var body = clone.GetComponent<Rigidbody>(); body.useGravity = false; body.isKinematic = true;
                var limit = clone.GetComponent<MinMaxScaleConstraint>() ?? clone.AddComponent<MinMaxScaleConstraint>();
                limit.RelativeToInitialState = false; limit.ScaleMinimum = 0.02f; limit.ScaleMaximum = 0.8f;
                PrefabUtility.SaveAsPrefabAsset(clone, HandPath);
            }
            finally { if (clone != null) UnityEngine.Object.DestroyImmediate(clone); EditorSceneManager.CloseScene(source, true); }
        }
        private static void PrepareResult()
        {
            var root = new GameObject("AprilTagCube");
            try
            {
                var view = root.AddComponent<AprilTagCubeView>();
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "Cube"; cube.transform.SetParent(root.transform, false);
                cube.transform.localScale = Vector3.one * 0.16f;
                cube.transform.localPosition = new Vector3(0.08f, -0.08f, -0.08f);
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
                cube.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Packages/com.microsoft.mixedreality.toolkit.standardassets/Materials/MRTK_Standard_Azure.mat") ??
                    AssetDatabase.LoadAssetAtPath<GameObject>(HandPath).GetComponent<Renderer>().sharedMaterial;
                var label = Text(root.transform, "Tag Label", "Tag --", new Vector3(0, 0.15f, 0), new Vector2(420, 100), 16);
                Ref(view, "cube", cube.transform); Ref(view, "cubeRenderer", cube.GetComponent<Renderer>()); Ref(view, "label", label);
                PrefabUtility.SaveAsPrefabAsset(root, ResultPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [MenuItem("Singray/AprilTag Placement/Apply Half-Edge Offset")]
        public static void ApplyHalfEdgeOffset()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(ResultPath);
            try
            {
                var view = root.GetComponent<AprilTagCubeView>();
                var data = new SerializedObject(view);
                data.FindProperty("centerOffsetInEdges").vector3Value = new Vector3(0.5f, -0.5f, -0.5f);
                data.ApplyModifiedPropertiesWithoutUndo();
                var visual = (Transform)data.FindProperty("cube").objectReferenceValue;
                visual.localPosition = new Vector3(0.5f, -0.5f, -0.5f) * visual.localScale.x;
                PrefabUtility.SaveAsPrefabAsset(root, ResultPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
        private static void PrepareContent()
        {
            var root = new GameObject("AprilTagPlacementDemo");
            try
            {
                var demo = root.AddComponent<AprilTagPlacementDemo>();
                var service = new GameObject("Tag Service"); service.transform.SetParent(root.transform, false);
                var manager = service.AddComponent<XvTagRecognizerManager>();
                manager.Size = 0.16;
                var results = new GameObject("Results").transform; results.SetParent(root.transform, false);
                var hand = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HandPath), root.transform);
                hand.transform.localPosition = new Vector3(-0.29f, -0.055f, 0.95f);
                var handText = Text(root.transform, "Hand Label", "Grab & Scale\n16 cm", new Vector3(-0.29f, 0.08f, 0.95f), new Vector2(240, 64), 20);
                var follow = handText.transform.parent.gameObject.AddComponent<HandCubeLabel>(); Ref(follow, "target", hand.GetComponent<Renderer>());
                var panel = new GameObject("Controls").transform; panel.SetParent(root.transform, false); panel.localPosition = new Vector3(0.22f, -0.025f, 1.25f);
                var bg = new GameObject("Backplate", typeof(RectTransform), typeof(Canvas)); bg.layer = 5; bg.transform.SetParent(panel, false);
                bg.transform.localPosition = new Vector3(0, 0, 0.012f); bg.transform.localScale = Vector3.one * 0.001f;
                bg.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace; ((RectTransform)bg.transform).sizeDelta = new Vector2(520, 650);
                var image = bg.AddComponent<Image>(); image.color = new Color(0.025f, 0.045f, 0.075f, 0.95f); image.raycastTarget = false;
                Text(panel, "Title", "APRILTAG", new Vector3(0, 0.285f, 0), new Vector2(480, 42), 29);
                var status = Text(panel, "Status", "Ready  |  Shots 0", new Vector3(0, 0.24f, 0), new Vector2(480, 32));
                var mode = Text(panel, "Mode", "Mode: Live", new Vector3(-0.13f, 0.198f, 0), new Vector2(220, 30), 17);
                ActionButton(panel, "Live", new Vector3(-0.16f, 0.152f, -0.01f), demo.Continuous);
                ActionButton(panel, "Single", new Vector3(0, 0.152f, -0.01f), demo.SingleShot);
                ActionButton(panel, "Clear", new Vector3(0.16f, 0.152f, -0.01f), demo.ClearResults);
                var size = Text(panel, "Size", "Tag: 16 cm", new Vector3(0, 0.101f, 0), new Vector2(480, 28), 17);
                var sizeButtons = new List<Interactable>(); int[] sizes = { 8, 10, 12, 16, 20, 30 };
                for (int i = 0; i < sizes.Length; i++)
                {
                    var button = Button(panel, sizes[i] + " cm", new Vector3(-0.16f + (i % 3) * 0.16f, 0.055f - (i / 3) * 0.055f, -0.01f));
                    UnityEventTools.AddIntPersistentListener(button.OnClick, demo.SetTagCentimeters, sizes[i]); sizeButtons.Add(button);
                }
                var target = Text(panel, "Target", "ID: Any", new Vector3(0, -0.05f, 0), new Vector2(480, 28), 17);
                for (int i = -1; i <= 3; i++)
                {
                    var button = Button(panel, i < 0 ? "Any" : i.ToString(), new Vector3((i - 1) * 0.096f, -0.095f, -0.01f), 0.085f);
                    UnityEventTools.AddIntPersistentListener(button.OnClick, demo.SetTargetId, i); sizeButtons.Add(button);
                }
                var scale = Text(panel, "Scale", "Cube: 1:1", new Vector3(0, -0.145f, 0), new Vector2(480, 28), 17);
                ActionButton(panel, "-", new Vector3(-0.16f, -0.19f, -0.01f), demo.Smaller);
                ActionButton(panel, "1:1", new Vector3(0, -0.19f, -0.01f), demo.ActualSize);
                ActionButton(panel, "+", new Vector3(0.16f, -0.19f, -0.01f), demo.Larger);
                ActionButton(panel, "Start", new Vector3(-0.16f, -0.25f, -0.01f), demo.StartRecognition);
                ActionButton(panel, "Stop", new Vector3(0, -0.25f, -0.01f), demo.StopRecognition);
                ActionButton(panel, "Reset Cube", new Vector3(0.16f, -0.25f, -0.01f), demo.ResetHandCube);
                Text(panel, "Pose Key", "P: SDK position | R: Unity Euler", new Vector3(0, -0.303f, 0), new Vector2(480, 25), 13);
                Ref(demo, "manager", manager); Ref(demo, "resultPrefab", AssetDatabase.LoadAssetAtPath<AprilTagCubeView>(ResultPath));
                Ref(demo, "results", results); Ref(demo, "handCube", hand.transform); Ref(demo, "handText", handText);
                Ref(demo, "statusText", status); Ref(demo, "modeText", mode); Ref(demo, "sizeText", size); Ref(demo, "scaleText", scale); Ref(demo, "targetText", target);
                Refs(demo, "sizeButtons", sizeButtons); Refs(demo, "buttons", root.GetComponentsInChildren<Interactable>(true));
                PrepareRecording(root);
                Record(root); PrefabUtility.SaveAsPrefabAsset(root, ContentPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void PrepareRecording(GameObject root)
        {
            if (root.GetComponent<AprilTagPalmRecording>() != null) return;
            const string hubPath = "Assets/XRFoundation/SampleScenes/SDKSampleHub/Scenes/SDKSamples.unity";
            var source = SceneManager.GetSceneByPath(hubPath);
            bool opened = !source.IsValid() || !source.isLoaded;
            if (opened) source = EditorSceneManager.OpenScene(hubPath, OpenSceneMode.Additive);
            GameObject menu;
            try
            {
                var original = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HandConstraintPalmUp>(true))
                    .Single(m => m.name == "Left Hand SDK Menu");
                menu = UnityEngine.Object.Instantiate(original.gameObject); menu.name = "Left Hand Recording";
                if (PrefabUtility.IsPartOfPrefabInstance(menu)) PrefabUtility.UnpackPrefabInstance(menu, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                menu.transform.SetParent(root.transform, false); menu.transform.localPosition = Vector3.zero; menu.transform.localRotation = Quaternion.identity;
            }
            finally { if (opened) EditorSceneManager.CloseScene(source, true); }
            // Keep the SDK's left-palm solver and its remapped visibility events.
            // Only this new copy gets a compact recording-only menu.
            var content = menu.transform.Find("Menu Content");
            foreach (var child in content.Cast<Transform>().ToArray())
                if (child.name != "MRTK Backplate") UnityEngine.Object.DestroyImmediate(child.gameObject);
            var plate = content.Find("MRTK Backplate");
            plate.localPosition = new Vector3(0, -0.018f, 0.014f); plate.localScale = new Vector3(0.22f, 0.12f, 0.01f);
            var control = root.AddComponent<AprilTagPalmRecording>();
            var button = ActionButton(content, "Record", new Vector3(0, 0, -0.012f), control.ToggleRecording, 0.18f);
            var buttonText = content.Find("Record Label").GetComponentInChildren<TMP_Text>(true);
            var palmStatus = Text(content, "Recording State", "Ready", new Vector3(0, -0.056f, -0.012f), new Vector2(205, 28), 16);
            var visibleStatus = Text(root.transform.Find("Controls"), "Recording Status", "", new Vector3(0.13f, 0.198f, 0), new Vector2(220, 30), 17);
            Ref(control, "recorderPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/XRFoundation/Core/Foundation/MediaRecorder/Resources/XvMediaRecorderManager.prefab"));
            Ref(control, "recordButton", button); Ref(control, "buttonText", buttonText); Ref(control, "palmStatus", palmStatus); Ref(control, "statusText", visibleStatus);
            content.gameObject.SetActive(false);
        }

        [MenuItem("Singray/AprilTag Placement/Add Left-Palm Recording")]
        public static void AddRecording()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(ContentPath);
            try { PrepareRecording(root); Record(root); PrefabUtility.SaveAsPrefabAsset(root, ContentPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Singray/AprilTag Placement/Prepare Demo")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var original = SceneManager.GetActiveScene();
            if (original.path == ScenePath || original.isDirty || string.IsNullOrEmpty(original.path))
                throw new InvalidOperationException("Save and select another scene before regenerating this demo.");
            Directory.CreateDirectory(Folder + "/Scenes"); Directory.CreateDirectory(Folder + "/Prefabs"); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                PrepareHandCube(); PrepareResult(); PrepareContent();
                foreach (var name in new[] { "MixedRealityToolkit", "XvXRInput", "XvXRManager" })
                {
                    string path = "Assets/XRFoundation/Core/Sdk/XR/Resources/" + name + ".prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) throw new FileNotFoundException(path);
                    PrefabUtility.InstantiatePrefab(prefab, scene);
                }
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ContentPath), scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save AprilTag demo.");
                AssetDatabase.SaveAssets(); Debug.Log("[TAG-DEMO] Prepared " + ScenePath);
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(original); }
        }

        [MenuItem("Singray/AprilTag Placement/Build G2 APK")]
        public static void BuildG2()
        {
            string output = Path.GetFullPath("Builds/AprilTagPlacement_20261116/OffsetPNN/G2_AprilTagPlacement_RGB_Recording_OffsetPNN.apk"); Directory.CreateDirectory(Path.GetDirectoryName(output));
            string settingsPath = "ProjectSettings/ProjectSettings.asset";
            byte[] originalSettings = File.ReadAllBytes(settingsPath);
            string product = PlayerSettings.productName, id = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android), version = PlayerSettings.bundleVersion;
            int code = PlayerSettings.Android.bundleVersionCode;
            try
            {
                PlayerSettings.productName = "G2 AprilTag Demo";
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.singray.apriltagplacement");
                PlayerSettings.bundleVersion = "1.0"; PlayerSettings.Android.bundleVersionCode = 6;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.Android, options = BuildOptions.None });
                File.WriteAllText(Path.ChangeExtension(output, ".build.txt"), "Result: " + report.summary.result + "\nErrors: " + report.summary.totalErrors +
                    "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize + "\nTime: " + report.summary.totalTime +
                    "\nScene: " + ScenePath + "\nApplication: com.singray.apriltagplacement\n");
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("AprilTag APK build failed: " + report.summary.result);
                Debug.Log("[TAG-DEMO] APK " + output);
            }
            finally
            {
                PlayerSettings.productName = product; PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, id);
                PlayerSettings.bundleVersion = version; PlayerSettings.Android.bundleVersionCode = code;
                AssetDatabase.SaveAssets(); File.WriteAllBytes(settingsPath, originalSettings);
            }
        }
    }
}
