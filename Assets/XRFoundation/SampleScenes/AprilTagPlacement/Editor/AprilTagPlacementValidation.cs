using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using NatSuite.Examples;
using NatSuite.Recorders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Singray.Foundation.SampleScenes.Editor
{
    /// <summary>Internal checks: managed fixtures do not establish native/device acceptance.</summary>
    [InitializeOnLoad]
    public static class AprilTagPlacementValidation
    {
        private const string Output = "Builds/AprilTagPlacement_20261116/OffsetPNN";
        private const string RunKey = "Singray.AprilTagPlacement.Validation";
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly List<string> assertions = new List<string>();
        private static readonly Dictionary<FieldInfo, object> originalNative = new Dictionary<FieldInfo, object>();
        private static IEnumerator checks;
        private static int lastFrame, nativeStarts, nativeStops, occupancy;
        private static XvTagRecognizerManager manager;

        static AprilTagPlacementValidation() { EditorApplication.playModeStateChanged += PlayState; }
        private static void Require(bool pass, string message)
        {
            if (!pass) throw new InvalidOperationException(message);
            assertions.Add(message);
        }
        private static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Instance).GetValue(obj);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Instance).SetValue(obj, value);
        private static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Instance).Invoke(obj, args);
        private static void Native(string name, object value)
        {
            var field = typeof(XvTagRecognizerManager).GetField(name, Static); originalNative[field] = field.GetValue(null); field.SetValue(null, value);
        }
        private static void Save(string name, string state, Exception error = null)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/" + name + ".txt", state + "\nAssertions: " + assertions.Count + "\n" + string.Join("\n", assertions) +
                (error == null ? "" : "\nFAILURE\n" + error));
        }

        [MenuItem("Singray/AprilTag Placement/Check Editor")]
        public static void CheckEditor()
        {
            assertions.Clear();
            var scene = EditorSceneManager.OpenScene(AprilTagPlacementBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var demo = roots.SelectMany(g => g.GetComponentsInChildren<AprilTagPlacementDemo>(true)).Single();
                Require(roots.Length == 4, "Standalone scene contains content and the three existing XR host prefabs.");
                Require(roots.SelectMany(g => g.GetComponentsInChildren<XvTagRecognizerManager>(true)).Count() == 1, "Exactly one recognition manager.");
                foreach (var go in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject) == 0, "No missing scripts: " + go.name);
                foreach (string field in new[] { "manager", "resultPrefab", "results", "handCube", "statusText", "sizeText", "scaleText", "handText", "modeText", "targetText" })
                    Require(new SerializedObject(demo).FindProperty(field).objectReferenceValue != null, "Saved binding: " + field);
                foreach (var text in demo.GetComponentsInChildren<TMP_Text>(true).Where(t => t.gameObject.activeInHierarchy))
                {
                    Require(text.text.All(c => c < 128), "English ASCII text: " + text.text.Replace("\n", " / "));
                    text.ForceMeshUpdate(true);
                    Require(!text.isTextOverflowing, "Text fits: " + text.text.Replace("\n", " / "));
                }
                Require(demo.GetComponentsInChildren<Interactable>(true).Length == 21, "Twenty recognition controls and one new recording button.");
                var buttons = Get<Interactable[]>(demo, "buttons");
                Require(buttons.Length == 20, "All 20 intended buttons were saved.");
                foreach (var button in buttons)
                {
                    Require(button.OnClick.GetPersistentEventCount() == 1, "One persistent action: " + button.name);
                    Require(button.OnClick.GetPersistentTarget(0) == demo && !string.IsNullOrEmpty(button.OnClick.GetPersistentMethodName(0)), "Action targets demo: " + button.name);
                    Require(button.GetComponent<PhysicalPressEventRouter>().InteractableOnClick == PhysicalPressEventRouter.PhysicalPressEventBehavior.EventOnClickCompletion,
                        "Physical press completes once: " + button.name);
                }
                var hand = Get<Transform>(demo, "handCube");
                Require((hand.GetComponent<MeshFilter>().sharedMesh.bounds.size - Vector3.one).sqrMagnitude < 0.00001f, "Official hand-cube mesh has unit edges.");
                Require((hand.lossyScale - Vector3.one * 0.16f).sqrMagnitude < 0.00001f, "Initial hand cube is 16 cm on every edge.");
                Require(hand.GetComponent<ObjectManipulator>() != null && hand.GetComponent<NearInteractionGrabbable>() != null, "Official MRTK near/far manipulator retained.");
                Require(hand.GetComponent<ObjectManipulator>().ReleaseBehavior == 0 && hand.GetComponent<Rigidbody>().isKinematic && !hand.GetComponent<Rigidbody>().useGravity,
                    "Hand cube stays in place after release.");
                var result = Get<AprilTagCubeView>(demo, "resultPrefab");
                Require(result.GetComponentInChildren<Collider>() == null && result.GetComponentInChildren<ObjectManipulator>() == null, "Saved tag cubes cannot be moved by hand.");
                Require((Get<Transform>(result, "cube").GetComponent<MeshFilter>().sharedMesh.bounds.size - Vector3.one).sqrMagnitude < 0.00001f, "Tag cube mesh has unit edges.");
                Require(result.GetComponentsInChildren<LineRenderer>(true).Length == 0, "Result prefab contains no diagnostic arrows.");
                Require(result.GetComponentsInChildren<TMP_Text>(true).All(t => t.text != "+X" && t.text != "+Y" && t.text != "+Z"), "Diagnostic axis labels are removed.");
                Require(result.CenterOffsetInEdges == new Vector3(0.5f, -0.5f, -0.5f), "Saved result prefab uses the experimental +X/-Y/-Z half-edge offset.");
                Require((Get<Transform>(result, "cube").localPosition - new Vector3(0.08f, -0.08f, -0.08f)).sqrMagnitude < 0.0000001f, "Result prefab previews (+8,-8,-8) cm XYZ for its 16 cm cube.");
                Require(!Get<bool>(demo, "single") && Mathf.Approximately(demo.DisplayScale, 1), "Default Live mode uses actual-size display.");
                Require(Get<XvTagRecognizerManager>(demo, "manager").RecognizerMode == RecognizerMode.None, "Recognition is initially stopped.");
                Require(Get<RecognizerMode>(demo, "detector") == RecognizerMode.RGB_Apriltag, "Default detector is RGB AprilTag 36h11.");
                var recording = demo.GetComponent<AprilTagPalmRecording>();
                Require(recording != null, "Independent recording controller is attached outside the hidden menu.");
                foreach (var field in new[] { "recorderPrefab", "recordButton", "buttonText", "palmStatus", "statusText" })
                    Require(new SerializedObject(recording).FindProperty(field).objectReferenceValue != null, "Recording binding: " + field);
                var recordButton = Get<Interactable>(recording, "recordButton");
                Require(recordButton.OnClick.GetPersistentEventCount() == 1 && recordButton.OnClick.GetPersistentTarget(0) == recording &&
                    recordButton.OnClick.GetPersistentMethodName(0) == "ToggleRecording", "Exactly one saved recording action.");
                var palm = demo.GetComponentInChildren<HandConstraintPalmUp>(true);
                var solver = palm.GetComponent<SolverHandler>(); var content = palm.transform.Find("Menu Content");
                Require(solver.TrackedTargetType == TrackedObjectType.HandJoint && solver.TrackedHandedness == Handedness.Left && solver.TrackedHandJoint == TrackedHandJoint.Palm,
                    "Recording menu uses the existing left-palm tracking configuration.");
                Require(!content.gameObject.activeSelf && palm.OnFirstHandDetected.GetPersistentTarget(0) == content.gameObject && palm.OnLastHandLost.GetPersistentTarget(0) == content.gameObject,
                    "Palm visibility events target the new copy's content, initially hidden.");
                Require(!recording.transform.IsChildOf(content) && content.GetComponentsInChildren<Interactable>(true).Length == 1, "Only recording is exposed; hiding menu leaves controller active.");
                var backend = Get<GameObject>(recording, "recorderPrefab");
                Require(backend.GetComponent<XvMediaRecorderManager>() != null && backend.GetComponent<ReplayCam>() != null, "Recording uses the complete existing SDK recorder prefab.");
                Require(!backend.GetComponent<ReplayCam>().recordMicrophone, "Recording integration does not enable the microphone.");
                Require(demo.GetComponentsInChildren<XvMediaRecorderManager>(true).Length == 0, "Recorder backend stays lazy until requested.");
                content.gameObject.SetActive(true);
                foreach (var text in content.GetComponentsInChildren<TMP_Text>(true).Where(t => t.gameObject.activeInHierarchy))
                { text.ForceMeshUpdate(true); Require(text.text.All(c => c < 128) && !text.isTextOverflowing, "English palm label fits: " + text.text); }
                content.gameObject.SetActive(false);
                Save("editor-checks", "PASS"); Debug.Log("[TAG-DEMO] Editor checks PASS: " + assertions.Count);
            }
            catch (Exception e) { Save("editor-checks", "FAIL", e); throw; }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [MenuItem("Singray/AprilTag Placement/Check Runtime (Injected)")]
        public static void CheckRuntime()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty || string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                throw new InvalidOperationException("Save the current single scene and stop Play Mode before running checks.");
            SessionState.SetString(RunKey + ".scene", SceneManager.GetActiveScene().path);
            SessionState.SetBool(RunKey, true);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AprilTagPlacementBuilder.ContentPath), scene);
            var camera = new GameObject("Validation Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.nearClipPlane = 0.01f;
            EditorApplication.isPlaying = true;
        }
        private static void PlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                assertions.Clear(); nativeStarts = nativeStops = occupancy = 0;
                Native("readyNative", (Func<bool>)(() => true));
                Native("startRgbNative", (Action<string, double, XvAprilTag.TagArrayCallback>)((f, s, c) => nativeStarts++));
                Native("stopRgbNative", (Action)(() => nativeStops++));
                Native("acquireRgb", (Action<XvTagRecognizerManager>)(_ => occupancy++));
                Native("releaseRgb", (Action<XvTagRecognizerManager>)(_ => occupancy--));
                checks = RuntimeChecks(); lastFrame = -1; EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(RunKey, false);
                string previous = SessionState.GetString(RunKey + ".scene", "");
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
            }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (manager != null) manager.StopTagDetector();
            foreach (var item in originalNative) item.Key.SetValue(null, item.Value);
            originalNative.Clear(); checks = null;
        }
        private static void Tick()
        {
            if (checks == null || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (checks.MoveNext()) return;
                Save("runtime-checks", "PASS"); Debug.Log("[TAG-DEMO] Injected runtime PASS: " + assertions.Count);
            }
            catch (Exception e) { Save("runtime-checks", "FAIL", e); Debug.LogException(e); }
            Cleanup(); EditorApplication.isPlaying = false;
        }
        private static IEnumerable<object> Start(AprilTagPlacementDemo demo)
        {
            demo.StartRecognition();
            int deadline = Time.frameCount + 30;
            while (Get<object>(manager, "session") == null && Time.frameCount < deadline) yield return null;
            Require(Get<object>(manager, "session") != null && occupancy == 1, "Fresh managed detector session starts and acquires one camera lease.");
            Require(manager.RecognizerMode == RecognizerMode.RGB_Apriltag && manager.TagGroupName == "36h11", "Configured RGB AprilTag mode starts with family 36h11.");
        }
        private static TagDetection Tag(int id, Vector3 position) => new TagDetection { id = id, translation = position, quaternion = new Vector4(0, 0, 0, 1), confidence = 1 };
        private static void Push(params TagDetection[] tags)
        {
            var session = Get<object>(manager, "session");
            typeof(XvTagRecognizerManager).GetMethod("Publish", Static).Invoke(null, new object[] { session, tags });
            Call(manager, "Update");
        }
        private static IEnumerator RuntimeChecks()
        {
            var demo = UnityEngine.Object.FindObjectOfType<AprilTagPlacementDemo>(); manager = Get<XvTagRecognizerManager>(demo, "manager");
            Require(Get<RecognizerMode>(demo, "detector") == RecognizerMode.RGB_Apriltag, "Runtime uses the prefab's RGB mode without a test override.");
            var results = Get<Transform>(demo, "results"); var hand = Get<Transform>(demo, "handCube");
            Require(!demo.IsRecognizing && demo.SuccessCount == 0 && results.childCount == 0, "No automatic recognition or generated results.");
            Require(demo.GetComponentsInChildren<Singray.UI.MrtkClickGate>(true).Length == 21, "All runtime buttons, including hidden Record, install the physical press-cycle guard.");
            foreach (var step in Start(demo)) yield return step;
            var firstSession = Get<object>(manager, "session"); demo.StartRecognition();
            Require(ReferenceEquals(firstSession, Get<object>(manager, "session")) && nativeStarts == 1, "Repeated Start does not duplicate the native session.");
            demo.SetTagCentimeters(30); demo.SetTargetId(7);
            Require(Math.Abs(manager.Size - 0.16) < 0.000001 && Get<int>(demo, "targetId") == -1, "Physical size and target cannot change while recognizing.");
            Require(Get<Interactable[]>(demo, "sizeButtons").All(b => !b.IsEnabled), "Size and target buttons are disabled while recognizing.");
            var bad = Tag(1, new Vector3(float.NaN, 0, 1)); var zero = Tag(2, Vector3.one); zero.quaternion = Vector4.zero;
            Push(Tag(7, new Vector3(0.7f, 0, 1)), bad, zero, Tag(3, new Vector3(-0.2f, 0, 0.8f)));
            var live = results.GetComponentInChildren<AprilTagCubeView>();
            Require(live != null && live.TagId == 3 && live.Shot == 0, "Any ID selects the lowest valid ID in the first batch.");
            Require(Mathf.Approximately(live.EdgeMeters, 0.16f), "Tag cube is 16 cm at 1:1.");
            Require((live.VisualCenter - (live.SavedPosition + new Vector3(0.08f, -0.08f, -0.08f))).sqrMagnitude < 0.0000001f,
                "Identity tag rotation shifts only the cube visual center by (+8,-8,-8) cm XYZ.");
            var lockedPosition = live.SavedPosition; Push(Tag(7, new Vector3(0.8f, 0, 1)));
            Require(live.SavedPosition == lockedPosition && live.TagId == 3, "Continuous mode never switches to another visible ID.");
            Call(manager, "Update"); Call(manager, "Update");
            Require(results.childCount == 1 && demo.SuccessCount == 0, "Cached render dispatches do not create cubes or count shots.");
            double deadline = EditorApplication.timeSinceStartup + 0.7;
            while (EditorApplication.timeSinceStartup < deadline) yield return null;
            Require(Get<string>(demo, "status") == "Lost" && Get<string>(live, "state") == "Lost", "Missing locked tag becomes Lost while retaining its pose.");
            Push(Tag(3, new Vector3(-0.1f, 0.05f, 0.9f)));
            Require(Get<string>(demo, "status") == "Tracking" && live.SavedPosition != lockedPosition, "The same tag reacquires and updates the live cube.");
            demo.Larger(); Require(Mathf.Approximately(live.EdgeMeters, 0.2f) && Math.Abs(manager.Size - 0.16) < 0.000001, "Display scaling never changes detector physical size.");
            Require((live.VisualCenter - (live.SavedPosition + live.SavedRotation * (new Vector3(0.1f, -0.1f, -0.1f)))).sqrMagnitude < 0.0000001f,
                "Display scaling recalculates half-edge offset while retaining raw tag position.");
            demo.StopRecognition(); Require(!demo.IsRecognizing && occupancy == 0 && results.childCount == 1, "Stop releases capture but keeps the live cube.");
            demo.SingleShot(); foreach (var step in Start(demo)) yield return step;
            Require(Get<object>(manager, "session") != firstSession && demo.SuccessCount == 0, "New Start has a new session and no preceding result.");
            Push((TagDetection[])null); Push(zero, bad);
            var huge = Tag(1, Vector3.one); huge.quaternion = Vector4.one * float.MaxValue;
            Push(huge);
            Require(demo.SuccessCount == 0 && demo.IsRecognizing, "Empty and invalid results cannot complete a single shot.");
            var source = Tag(3, new Vector3(0.1f, -0.05f, 1.1f)); Push(source);
            Require(demo.SuccessCount == 1 && !demo.IsRecognizing && manager.RecognizerMode == RecognizerMode.None && occupancy == 0, "First valid shot saves once and automatically stops detection.");
            var shot1 = results.GetComponentsInChildren<AprilTagCubeView>().Single(v => v.Shot == 1); var saved = shot1.SavedPosition;
            source.translation = Vector3.one * 99; source.quaternion = Vector4.zero;
            Call(demo, "OnResult", new object[] { new[] { source } });
            Require(shot1.SavedPosition == saved && demo.SuccessCount == 1 && results.childCount == 2, "Saved values survive source mutation, stop-null and late callbacks.");
            demo.SetTagCentimeters(8); foreach (var step in Start(demo)) yield return step;
            Call(demo, "OnResult", new object[] { new[] { Tag(3, Vector3.one) } });
            Require(demo.SuccessCount == 1, "An unversioned preceding callback cannot complete the newly armed shot.");
            Push(Tag(2, new Vector3(0.2f, -0.05f, 1.1f)));
            var shot2 = results.GetComponentsInChildren<AprilTagCubeView>().Single(v => v.Shot == 2);
            Require(demo.SuccessCount == 2 && results.childCount == 3 && shot2.TagId == 2, "Second success adds another cube and preserves prior results.");
            Require(Mathf.Approximately(shot1.EdgeMeters, 0.2f) && Mathf.Approximately(shot2.EdgeMeters, 0.1f), "Historical snapshots retain their own physical tag sizes.");
            demo.ActualSize(); Require(Mathf.Approximately(shot1.EdgeMeters, 0.16f) && Mathf.Approximately(shot2.EdgeMeters, 0.08f), "1:1 restores each recorded real-world edge size.");
            foreach (var step in Start(demo)) yield return step; demo.StopRecognition();
            Require(demo.SuccessCount == 2 && results.childCount == 3, "Cancel before recognition does not increase the shot count.");
            demo.SetTargetId(3); foreach (var step in Start(demo)) yield return step;
            Push(Tag(2, Vector3.one)); Require(demo.IsRecognizing && demo.SuccessCount == 2, "Explicit ID ignores a different tag.");
            demo.Continuous(); Require(!demo.IsRecognizing && !demo.Single && results.childCount == 3, "Mode change stops detection and preserves snapshots.");
            foreach (var step in Start(demo)) yield return step; Call(demo, "OnApplicationPause", true);
            Require(!demo.IsRecognizing && occupancy == 0, "Pause stops recognition and releases ownership.");
            Call(demo, "OnApplicationPause", false); Require(!demo.IsRecognizing, "Resume requires an explicit Start.");
            foreach (var step in Start(demo)) yield return step; Call(demo, "OnApplicationFocus", false);
            Require(!demo.IsRecognizing && occupancy == 0, "Focus loss stops recognition.");
            foreach (var step in Start(demo)) yield return step; demo.enabled = false;
            Require(!demo.IsRecognizing && occupancy == 0, "Disable stops recognition."); demo.enabled = true;
            for (int i = 0; i < 20; i++) demo.Larger(); Require(Mathf.Approximately(demo.DisplayScale, 4), "Display scale has a 4x upper limit.");
            for (int i = 0; i < 30; i++) demo.Smaller(); Require(Mathf.Approximately(demo.DisplayScale, 0.25f), "Display scale has a 0.25x lower limit."); demo.ActualSize();
            yield return null;
            var label = Get<TMP_Text>(shot1, "label"); var renderer = Get<Renderer>(shot1, "cubeRenderer");
            Require(label.canvas.transform.position.y > renderer.bounds.max.y, "Tag label remains above the cube in world space.");
            Require(label.text.Contains("Shot 1") && label.text.Contains("16 cm") && label.text.Contains("1:1") && label.text.Contains(" m") && label.text.Contains(" deg"), "Saved label includes ID/shot, physical/display size and pose units.");
            label.ForceMeshUpdate(); Require(!label.isTextOverflowing, "Generated snapshot pose label fits its text area.");
            var handPosition = hand.localPosition; hand.localPosition = Vector3.one; hand.localScale = Vector3.one * 0.3f; demo.ResetHandCube();
            Require(hand.localPosition == handPosition && (hand.localScale - Vector3.one * 0.16f).sqrMagnitude < 0.000001f, "Reset restores only the hand cube's initial pose and true dimensions.");
            foreach (var step in Start(demo)) yield return step; demo.ClearResults(); yield return null; yield return null;
            Require(!demo.IsRecognizing && results.childCount == 0 && demo.SuccessCount == 0 && occupancy == 0, "Clear stops recognition, deletes all tag cubes and resets the count.");
            Require(hand != null && Mathf.Approximately(hand.lossyScale.x, 0.16f), "Clear preserves the independent hand cube.");
            Require(nativeStarts == nativeStops, "Every injected detector start is matched by a stop.");
            CheckOffsetGeometry(demo);
            foreach (var step in RecordingChecks(demo)) yield return step;
        }

        private static void CheckOffsetGeometry(AprilTagPlacementDemo demo)
        {
            var probe = UnityEngine.Object.Instantiate(Get<AprilTagCubeView>(demo, "resultPrefab"));
            try
            {
                var p = new Vector3(0.2f, -0.3f, 1.1f); var q = Quaternion.Euler(25, 90, -15);
                var tag = Tag(3, p); tag.quaternion = new Vector4(q.x, q.y, q.z, q.w);
                probe.SetPose(tag, 0.16f, 1, Color.white);
                Require((probe.VisualCenter - (p + q * (new Vector3(0.08f, -0.08f, -0.08f)))).sqrMagnitude < 0.0000001f,
                    "Rotated tag applies offset in tag-local axes, not world axes.");
                Require(probe.SavedPosition == p && probe.transform.position == p && Quaternion.Angle(probe.SavedRotation, q) < 0.01f,
                    "Offset preserves stored raw translation/quaternion and root pose.");
                var cube = Get<Transform>(probe, "cube");
                Require((cube.TransformPoint(new Vector3(-0.5f, 0.5f, 0.5f)) - p).sqrMagnitude < 0.0000001f,
                    "With +X/-Y/-Z half-edge, the cube's (-X,+Y,+Z) corner stays at the raw tag origin.");
                probe.SetDisplayScale(1.25f);
                Require((probe.VisualCenter - (p + q * (new Vector3(0.1f, -0.1f, -0.1f)))).sqrMagnitude < 0.0000001f &&
                    (cube.TransformPoint(new Vector3(-0.5f, 0.5f, 0.5f)) - p).sqrMagnitude < 0.0000001f,
                    "Enlarged cube keeps the same corner anchor while offset becomes (+10,-10,-10) cm XYZ.");
                var signed = new Vector3(-0.5f, 0, 0.5f); probe.SetCenterOffset(signed);
                Require((probe.VisualCenter - (p + q * new Vector3(-0.1f, 0, 0.1f))).sqrMagnitude < 0.0000001f,
                    "Each offset axis supports negative and zero factors independently.");
                probe.SetCenterOffset(new Vector3(float.NaN, 0, 0));
                Require(probe.CenterOffsetInEdges == signed, "Invalid offset factors cannot replace the current configuration.");
                probe.SetCenterOffset(Vector3.zero); Require((probe.VisualCenter - p).sqrMagnitude < 0.0000001f,
                    "Zero offset restores original centered placement.");
                probe.SetCenterOffset(new Vector3(0.5f, -0.5f, -0.5f)); probe.SetPose(tag, 0.08f, 2, Color.white); probe.SetDisplayScale(1);
                Require((probe.VisualCenter - (p + q * (new Vector3(0.04f, -0.04f, -0.04f)))).sqrMagnitude < 0.0000001f,
                    "Eight-centimetre result uses four-centimetre half-edge offsets.");
                Require(Get<TMP_Text>(probe, "label").text.Contains("P 0.200, -0.300, 1.100 m"),
                    "Pose label still reports the original tag position, not offset cube center.");
            }
            finally { UnityEngine.Object.Destroy(probe.gameObject); }
        }

        private static int CountOwners(object target, string field)
        {
            var owners = Get<object>(target, field); return (int)owners.GetType().GetProperty("Count").GetValue(owners);
        }
        private static int RgbOwners()
        {
            var leases = typeof(XvCameraManager).Assembly.GetType("Singray.Foundation.XvCaptureLease");
            var owners = (IDictionary)leases.GetField("owners", Static).GetValue(null);
            if (!owners.Contains(XvCameraStreamType.ARCameraStream)) return 0;
            var rgb = owners[XvCameraStreamType.ARCameraStream]; return (int)rgb.GetType().GetProperty("Count").GetValue(rgb);
        }
        private static void ArmFixture(XvMediaRecorderManager media, XvMRVideoCaptureManager capture, FixtureRecorder fake)
        {
            var replay = media.GetComponent<ReplayCam>();
            media.StartCapture(); Call(capture, "AcquireCapture", replay);
            Set(replay, "finishTask", null); Set(replay, "finishing", false);
            Set(replay, "recorder", fake); Set(replay, "isRecording", true); Set(replay, "mrVideoCaptureManager", capture);
        }
        private static IEnumerable<object> WaitForSave(AprilTagPalmRecording controls)
        {
            double deadline = EditorApplication.timeSinceStartup + 15;
            while (controls.IsSaving && EditorApplication.timeSinceStartup < deadline) yield return null;
            Require(!controls.IsSaving, "Asynchronous recorder finalization completes.");
        }
        private static void VerifyAndRetainFixture(AprilTagPalmRecording controls)
        {
            string path = controls.LastSavedPath;
            Require(!string.IsNullOrEmpty(path) && File.Exists(path) && new FileInfo(path).Length == 5, "SDK exported a nonempty five-byte fixture (not playable video).");
            string safe = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Screenshots")) + Path.DirectorySeparatorChar;
            Require(Path.GetFullPath(path).StartsWith(safe, StringComparison.OrdinalIgnoreCase), "Fixture export is inside the known workspace Screenshots directory.");
            string target = Path.GetFullPath(Output + "/Fixtures/" + Path.GetFileName(path)); Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Move(path, target); // Retain the test file only under local evidence.
        }
        private static IEnumerable<object> RecordingChecks(AprilTagPlacementDemo demo)
        {
            var controls = demo.GetComponent<AprilTagPalmRecording>();
            var palm = demo.GetComponentInChildren<HandConstraintPalmUp>(true);
            palm.enabled = false; palm.GetComponent<SolverHandler>().enabled = false;
            var content = palm.transform.Find("Menu Content").gameObject;
            Require(!controls.IsPreparing && !controls.IsSaving && Get<XvMediaRecorderManager>(controls, "recorder") == null && controls.ButtonLabel == "Record", "Recording starts dormant without camera/encoder creation.");
            palm.OnFirstHandDetected.Invoke(); Require(content.activeSelf, "Copied first-hand event reveals this menu.");
            controls.ToggleRecording(); yield return null; yield return null;
            var media = Get<XvMediaRecorderManager>(controls, "recorder"); var capture = media.XvMRVideoCaptureManager; var replay = media.GetComponent<ReplayCam>();
            Require(controls.IsPreparing && controls.ButtonLabel == "Cancel" && capture.CaptureType == CaptureType.MR && !media.IsCameraFrameReady(), "Record waits for a real RGB frame in MR mode and offers Cancel.");
            Require(Get<IMediaRecorder>(replay, "recorder") == null, "No encoder is created while RGB has no real frame.");
            Require(ReferenceEquals(manager.XvCameraManager, capture.CameraManager), "RGB AprilTag and recorder resolve the same SDK camera manager.");
            var otherCapture = new object(); Call(capture, "AcquireCapture", otherCapture);
            var otherRgb = new object(); Call(capture.CameraManager, "AcquireCapture", XvCameraStreamType.ARCameraStream, otherRgb);
            controls.ToggleRecording();
            Require(!controls.IsPreparing && controls.ButtonLabel == "Record" && capture.IsOn && CountOwners(capture, "owners") == 1 && RgbOwners() == 2,
                "Cancelling releases only recording preparation; compositor and RGB owners are preserved.");
            controls.ToggleRecording();
            double deadline = EditorApplication.timeSinceStartup + 12;
            while (controls.IsPreparing && EditorApplication.timeSinceStartup < deadline) yield return null;
            Require(!controls.IsPreparing && Get<string>(controls, "status") == "No frame" && controls.ButtonLabel == "Record", "Ten-second no-frame timeout cancels without starting an encoder.");
            Require(Get<IMediaRecorder>(replay, "recorder") == null && capture.IsOn && CountOwners(capture, "owners") == 1, "Timeout retains another consumer's capture and never opens an encoder.");
            string source = Path.GetFullPath(Output + "/Fixtures/source_" + Guid.NewGuid().ToString("N") + ".mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(source)); File.WriteAllBytes(source, new byte[] { 1, 2, 3, 4, 5 });
            var fake = new FixtureRecorder(); ArmFixture(media, capture, fake);
            Set(controls, "status", "Recording"); Call(controls, "Update");
            Require(controls.ButtonLabel == "Stop" && Get<TMP_Text>(controls, "statusText").text == "REC", "Active recording shows Stop and a persistent REC indicator.");
            palm.OnLastHandLost.Invoke(); yield return null;
            Require(!content.activeSelf && controls.isActiveAndEnabled && media.IsVideoRecording() && fake.finishCalls == 0, "Hiding the palm menu does not stop the recording.");
            demo.ClearResults(); Require(media.IsVideoRecording(), "Clearing AprilTag results leaves recording active.");
            controls.ToggleRecording();
            Require(controls.IsSaving && controls.ButtonLabel == "Saving..." && !Get<Interactable>(controls, "recordButton").IsEnabled && fake.finishCalls == 1, "Stop gates the button while asynchronous saving is pending.");
            controls.ToggleRecording(); Require(fake.finishCalls == 1, "Repeated recording press during Saving cannot create another finish/start.");
            fake.completion.SetResult(source); foreach (var step in WaitForSave(controls)) yield return step;
            Require(Get<string>(controls, "status") == "Saved" && controls.ButtonLabel == "Record" && Get<Interactable>(controls, "recordButton").IsEnabled, "Successful finalization restores Record and reports Saved.");
            VerifyAndRetainFixture(controls);
            Require(capture.IsOn && CountOwners(capture, "owners") == 1 && RgbOwners() == 2 && fake.frameCommits == 0,
                "Finalization preserves other camera consumers; fixture did not encode camera frames.");
            controls.ToggleRecording(); yield return null; Call(controls, "OnApplicationPause", true);
            Require(!controls.IsPreparing && !controls.IsSaving && controls.ButtonLabel == "Record", "Pause cancels pending preparation.");
            Call(controls, "OnApplicationPause", false); Require(!controls.IsPreparing, "Resume never restarts recording automatically.");
            var paused = new FixtureRecorder(); ArmFixture(media, capture, paused);
            Call(controls, "OnApplicationPause", true);
            Require(controls.IsSaving && paused.finishCalls == 1, "Pause requests finalization of an active recording.");
            controls.enabled = false; Require(paused.finishCalls == 1, "Disable during Saving does not duplicate finalization.");
            paused.completion.SetResult(source); foreach (var step in WaitForSave(controls)) yield return step;
            VerifyAndRetainFixture(controls); controls.enabled = true;
            Require(controls.ButtonLabel == "Record" && !controls.IsPreparing && !controls.IsSaving, "Reenable stays stopped after asynchronous save.");
            Call(capture, "ReleaseCapture", otherCapture, true);
            Require(!capture.IsOn && RgbOwners() == 1, "Closing the last MR consumer still preserves an independent RGB consumer.");
            typeof(XvCameraManager).GetMethod("ReleaseCapture", Static).Invoke(null, new object[] { XvCameraStreamType.ARCameraStream, otherRgb });
            Require(RgbOwners() == 0, "All recording fixture ownership is released.");
            Require(!replay.recordMicrophone && Get<RecognizerMode>(demo, "detector") == RecognizerMode.RGB_Apriltag, "Microphone default and RGB AprilTag mode remain unchanged.");
        }
        private sealed class FixtureRecorder : IMediaRecorder
        {
            internal readonly TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            internal int finishCalls, frameCommits;
            public (int width, int height) frameSize => (2, 2);
            public void CommitFrame<T>(T[] pixels, long timestamp) where T : struct { frameCommits++; }
            public void CommitFrame(IntPtr pixels, long timestamp) { frameCommits++; }
            public void CommitSamples(float[] samples, long timestamp) { }
            public Task<string> FinishWriting() { finishCalls++; return completion.Task; }
        }
    }
}
