using System;
using System.IO;
using Singray.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Singray.InputSystem.Editor
{
    [InitializeOnLoad]
    public static class SingrayKeyInputPlayValidation
    {
        const string Active = "Singray.KeyInput.PlayValidation";
        const string Original = "Singray.KeyInput.OriginalScene";
        static SingrayKeyInput primary, secondary;
        static int phase, frame, primaryClicks, secondaryClicks, inspectorClicks, rawPackets, cancelled;
        static double deadline;
        static bool tested;

        static SingrayKeyInputPlayValidation()
        {
            if (SessionState.GetBool(Active, false)) Attach();
        }

        [MenuItem("Singray/Input/Run Key Input Play Validation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isDirty || string.IsNullOrEmpty(scene.path) || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
                throw new Exception("Save the active scene and use a single scene before Play validation.");
            SessionState.SetString(Original, scene.path);
            SessionState.SetBool(Active, true);
            EditorSceneManager.OpenScene("Assets/XRFoundation/SampleScenes/KeyInput/KeyInput.unity");
            Attach();
            EditorApplication.isPlaying = true;
        }
        static void Attach()
        {
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                phase = frame = primaryClicks = secondaryClicks = inspectorClicks = rawPackets = cancelled = 0;
                tested = false; primary = null;
            }
            if (state == PlayModeStateChange.EnteredEditMode && tested)
            {
                SessionState.SetBool(Active, false);
                EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed;
                EditorSceneManager.OpenScene(SessionState.GetString(Original, ""));
            }
        }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Tick()
        {
            if (tested || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup > deadline) { Finish("FAIL: Play validation timed out."); return; }
            try
            {
                if (primary == null)
                {
                    if (Time.frameCount < 3) return;
                    primary = UnityEngine.Object.FindObjectOfType<SingrayKeyInput>();
                    Check(primary != null && primary.IsListening && primary.IsSimulation, "The runtime simulation subscription did not start.");
                    secondary = new GameObject("Second key input consumer").AddComponent<SingrayKeyInput>();
                    primary.KeyClick += e => { if (e.Key == SingrayKeyCode.Confirm) primaryClicks++; };
                    secondary.KeyClick += e => { if (e.Key == SingrayKeyCode.Confirm) secondaryClicks++; };
                    primary.OnConfirmClick.AddListener(() => inspectorClicks++);
                    primary.RawEvent += _ => rawPackets++;
                    primary.KeyCancelled += _ => cancelled++;
                    primary.SimulateKeyClick(SingrayKeyCode.Confirm);
                    frame = Time.frameCount; phase = 1; return;
                }
                if (Time.frameCount - frame < 3) return;
                switch (phase)
                {
                    case 1:
                        Check(primaryClicks == 1 && secondaryClicks == 1 && inspectorClicks == 1 && rawPackets == 2 && primary.HasReceivedEvents, "C# / UnityEvent / raw-event delivery failed.");
                        primary.SimulateKeyDown(SingrayKeyCode.VolumeUp); phase = 2; break;
                    case 2:
                        Check(primary.IsPressed(SingrayKeyCode.VolumeUp) && secondary.IsPressed(SingrayKeyCode.VolumeUp), "Held-key state failed.");
                        primary.enabled = false;
                        Check(cancelled == 1 && !primary.IsPressed(SingrayKeyCode.VolumeUp) && secondary.IsPressed(SingrayKeyCode.VolumeUp), "Per-component disable cancellation failed.");
                        secondary.SimulateKeyUp(SingrayKeyCode.VolumeUp); phase = 3; break;
                    case 3:
                        Check(!secondary.IsPressed(SingrayKeyCode.VolumeUp), "The remaining consumer stopped receiving input.");
                        primary.enabled = true; primary.SimulateKeyClick(SingrayKeyCode.Confirm); phase = 4; break;
                    case 4:
                        Check(primaryClicks == 2 && secondaryClicks == 2 && inspectorClicks == 2, "Re-enable caused missing or duplicate event delivery.");
                        primary.KeyUp += e => { if (e.Key == SingrayKeyCode.Confirm) primary.enabled = false; };
                        primary.SimulateKeyClick(SingrayKeyCode.Confirm); phase = 5; break;
                    case 5:
                        Check(primaryClicks == 2 && inspectorClicks == 2 && secondaryClicks == 3, "Disabling on KeyUp did not suppress a late click or interrupted another listener.");
                        Finish("PASS: runtime pump creation; simulation queue; C# and UnityEvent delivery; raw events; two consumers; held state; disable cancellation; re-enable; disable-on-release click suppression.\n");
                        return;
                }
                frame = Time.frameCount;
            }
            catch (Exception error) { Finish("FAIL: " + error); Debug.LogException(error); }
        }
        static void Finish(string result)
        {
            Directory.CreateDirectory("Builds/KeyInput");
            File.WriteAllText("Builds/KeyInput/play-validation.txt", result);
            tested = true;
            EditorApplication.isPlaying = false;
        }
    }
}
