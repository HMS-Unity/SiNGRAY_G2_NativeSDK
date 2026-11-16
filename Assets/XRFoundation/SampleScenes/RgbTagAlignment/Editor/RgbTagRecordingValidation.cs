using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
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
    /// <summary>Controlled lifecycle checks. Five-byte fixture exports are not playable MP4s.</summary>
    [InitializeOnLoad]
    public static class RgbTagRecordingValidation
    {
        private const string Output = RgbTagAlignmentBuilder.Output + "/Recording";
        private const string RunKey = "Singray.RgbTagAlignment.RecordingValidation";
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly List<string> assertions = new List<string>();
        private static readonly FieldInfo readyField = typeof(RgbTagPalmRecording).GetField("readyNative", Static);
        private static object originalReady;
        private static IEnumerator checks;
        private static int lastFrame;
        static RgbTagRecordingValidation() { EditorApplication.playModeStateChanged += PlayState; }
        private static void Require(bool pass, string message)
        { if (!pass) throw new InvalidOperationException(message); assertions.Add(message); }
        private static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Instance).GetValue(obj);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Instance).SetValue(obj, value);
        private static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Instance).Invoke(obj, args);
        private static void Save(string state, Exception error = null)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/runtime-checks.txt", state + "\nAssertions: " + assertions.Count + "\n" + string.Join("\n", assertions) +
                (error == null ? "" : "\nFAILURE\n" + error));
        }
        [MenuItem("Singray/RGB Tag Alignment/Validate Recording Lifecycle")]
        public static void CheckRuntime()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty || string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                throw new InvalidOperationException("Save the current single scene and stop Play Mode before running checks.");
            SessionState.SetString(RunKey + ".scene", SceneManager.GetActiveScene().path);
            SessionState.SetBool(RunKey, true);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RgbTagAlignmentBuilder.ContentPath), scene);
            var camera = new GameObject("Validation Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.nearClipPlane = 0.01f;
            EditorApplication.isPlaying = true;
        }
        private static void PlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                assertions.Clear(); originalReady = readyField.GetValue(null);
                var demo = UnityEngine.Object.FindObjectOfType<RgbTagAlignmentDemo>();
                checks = RecordingChecks(demo).GetEnumerator(); lastFrame = -1; EditorApplication.update += Tick;
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
            if (originalReady != null) readyField.SetValue(null, originalReady);
            originalReady = null; checks = null;
        }
        private static void Tick()
        {
            if (checks == null || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (checks.MoveNext()) return;
                Save("PASS"); Debug.Log("[RGB-TAG-REC] Controlled runtime PASS: " + assertions.Count);
            }
            catch (Exception e) { Save("FAIL", e); Debug.LogException(e); }
            Cleanup(); EditorApplication.isPlaying = false;
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
        private static IEnumerable<object> WaitForSave(RgbTagPalmRecording controls)
        {
            double deadline = EditorApplication.timeSinceStartup + 15;
            while (controls.IsSaving && EditorApplication.timeSinceStartup < deadline) yield return null;
            Require(!controls.IsSaving, "Asynchronous recorder finalization completes.");
        }
        private static void VerifyAndRetainFixture(RgbTagPalmRecording controls)
        {
            string path = controls.LastSavedPath;
            Require(!string.IsNullOrEmpty(path) && File.Exists(path) && new FileInfo(path).Length == 5, "SDK exported a nonempty five-byte fixture (not playable video).");
            string safe = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Screenshots")) + Path.DirectorySeparatorChar;
            Require(Path.GetFullPath(path).StartsWith(safe, StringComparison.OrdinalIgnoreCase), "Fixture export is inside the known workspace Screenshots directory.");
            string target = Path.GetFullPath(Output + "/Fixtures/" + Path.GetFileName(path)); Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Move(path, target); // Retain the test file only under local evidence.
        }
        private static IEnumerable<object> RecordingChecks(RgbTagAlignmentDemo demo)
        {
            var controls = demo.GetComponent<RgbTagPalmRecording>();
            var palm = demo.GetComponentInChildren<HandConstraintPalmUp>(true);
            palm.enabled = false; palm.GetComponent<SolverHandler>().enabled = false;
            var content = palm.transform.Find("Menu Content").gameObject;
            Require(!controls.IsPreparing && !controls.IsSaving && Get<XvMediaRecorderManager>(controls, "recorder") == null && controls.ButtonLabel == "Record", "Recording starts dormant without camera/encoder creation.");
            controls.ToggleRecording();
            Require(Get<XvMediaRecorderManager>(controls, "recorder") == null && !controls.IsPreparing, "Unready SDK refuses preparation before creating capture services.");
            readyField.SetValue(null, (Func<bool>)(() => true));
            palm.OnFirstHandDetected.Invoke(); Require(content.activeSelf, "Copied first-hand event reveals this menu.");
            controls.ToggleRecording(); yield return null; yield return null;
            var media = Get<XvMediaRecorderManager>(controls, "recorder"); var capture = media.XvMRVideoCaptureManager; var replay = media.GetComponent<ReplayCam>();
            Require(controls.IsPreparing && controls.ButtonLabel == "Cancel" && capture.CaptureType == CaptureType.MR && !media.IsCameraFrameReady(), "Record waits for a real RGB frame in MR mode and offers Cancel.");
            Require(Get<IMediaRecorder>(replay, "recorder") == null, "No encoder is created while RGB has no real frame.");
            Require(ReferenceEquals(Get<XvCameraManager>(demo, "cameraManager"), capture.CameraManager), "Independent RGB detector and recorder resolve the same SDK camera manager.");
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
            demo.StopDetection(); demo.UnlockPlane(); Require(media.IsVideoRecording(), "Stop detection and Unlock leave recording active.");
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
            Require(!replay.recordMicrophone && !replay.recordAudio && replay.maxRecordingDimension == 1280, "This recorder is video-only at maximum 1280 pixels; shared prefab defaults were not edited.");
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
