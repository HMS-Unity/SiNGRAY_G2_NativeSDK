using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Singray.Foundation;
using Singray.InputSystem;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Singray.InputSystem.Editor
{
    public static class SingrayKeyInputValidation
    {
        const string Root = "Assets/XRFoundation/Core/Input";
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        static int checks;
        static void Check(bool condition, string name)
        { if (!condition) throw new Exception(name); checks++; }
        static Type Internal(string name) => typeof(SingrayKeyInput).Assembly.GetType("Singray.InputSystem.Internal." + name, true);
        static object Call(Type type, object instance, string name, params object[] args) => type.GetMethod(name, Flags).Invoke(instance, args);

        [MenuItem("Singray/Input/Validate Key Input")]
        public static void Run()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode before validation.");
                checks = 0;
                CheckProcessor();
                CheckHub();
                CheckSubscriberIsolation();
                if (!File.Exists("Assets/XRFoundation/SampleScenes/KeyInput/KeyInput.unity")) CreateExample();
                Directory.CreateDirectory("Builds/KeyInput");
                var settings = new ScriptCompilationSettings { target = BuildTarget.Android, group = BuildTargetGroup.Android, options = ScriptCompilationOptions.None };
                var result = PlayerBuildInterface.CompilePlayerScripts(settings, "Builds/KeyInput/AndroidScripts");
                Check(result.assemblies.Count > 0, "Android player script compilation failed.");
                File.WriteAllText("Builds/KeyInput/validation.txt", "PASS: " + checks + " checks; Android assemblies: " + result.assemblies.Count + "\nKey transitions, held duration, duplicates, rotary ticks, cancellation, profile validation, shared registration, subscriber release, callback thread routing, reconnect, pause, exception isolation and simulation separation passed.\nNative start-result semantics and hardware input require device validation.\n");
                Debug.Log("SINGRAY_KEY_INPUT_VALIDATION_PASS " + checks);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Directory.CreateDirectory("Builds/KeyInput");
                File.WriteAllText("Builds/KeyInput/validation.txt", "FAIL: " + error);
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void CheckProcessor()
        {
            var type = Internal("KeyStateProcessor");
            var processor = Activator.CreateInstance(type, true);
            var keys = new List<SingrayKeyEvent>();
            var rotations = new List<SingrayKnobEvent>();
            Action<SingrayKeyEvent> keyHandler = e => keys.Add(e);
            Action<SingrayKnobEvent> knobHandler = e => rotations.Add(e);
            type.GetEvent("KeyEvent").AddEventHandler(processor, keyHandler);
            type.GetEvent("KnobEvent").AddEventHandler(processor, knobHandler);
            Call(type, processor, "Configure", new object[] { null });
            Action<int, int, long, double> send = (raw, state, timestamp, time) => Call(type, processor, "Process", new XvEvent { type = raw, state = state, edgeTimestampUs = timestamp }, time);
            send(1, 255, 1, 0); Check(keys.Count == 0, "An unmatched release generated a click.");
            send(1, 254, 1000000, 1); send(1, 254, 1100000, 1.1);
            Check(keys.Count == 1 && (bool)Call(type, processor, "IsPressed", SingrayKeyCode.Confirm), "Repeated down was not filtered.");
            send(1, 255, 1500000, 1.5);
            Check(keys.Count == 3 && keys[1].Action == SingrayKeyAction.Up && keys[2].Action == SingrayKeyAction.Click && Math.Abs(keys[2].HeldSeconds - .5f) < .001f, "Release/click order or duration was incorrect.");
            send(17, 99, 10, 2); send(17, 99, 11, 2); send(17, 101, 12, 2); send(17, 99, 10, 2);
            Check(rotations.Count == 3 && rotations[2].Steps == -1, "Rotary deduplication discarded valid ticks or retained duplicates.");
            send(17, 99, 0, 2); send(17, 99, 0, 2);
            Check(rotations.Count == 5, "Untimestamped rotary ticks were discarded.");
            send(10, 254, 20, 3); send(11, 254, 21, 3);
            Check((bool)Call(type, processor, "IsPressed", SingrayKeyCode.VolumeUp) && (bool)Call(type, processor, "IsPressed", SingrayKeyCode.VolumeDown), "Independent key state was lost.");
            int before = keys.Count; Call(type, processor, "Cancel", 4d);
            Check(keys.Count == before + 2 && keys[keys.Count - 1].Action == SingrayKeyAction.Cancelled, "Held keys were not cancelled.");
            send(10, 255, 22, 4); Check(keys.Count == before + 2, "Cancellation generated a click.");
            var profile = ScriptableObject.CreateInstance<SingrayKeyMappingProfile>();
            try
            {
                profile.bindings = new[] { new SingrayKeyMappingProfile.Binding(77, SingrayKeyCode.Back) };
                profile.downState = 7; profile.upState = 8;
                Call(type, processor, "Configure", profile);
                send(77, 7, 30, 5); send(77, 8, 31, 5.2);
                Check(keys[keys.Count - 1].Key == SingrayKeyCode.Back && keys[keys.Count - 1].RawType == 77, "Custom mapping was ignored.");
                profile.bindings = new[] { new SingrayKeyMappingProfile.Binding(77, SingrayKeyCode.Back), new SingrayKeyMappingProfile.Binding(77, SingrayKeyCode.Confirm) };
                bool rejected = false;
                try { Call(type, processor, "Configure", profile); } catch (TargetInvocationException) { rejected = true; }
                Check(rejected, "Duplicate mapping was accepted.");
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
            Call(type, processor, "Configure", new object[] { null });
            Action<SingrayKeyEvent> disableOnUp = e => { if (e.Action == SingrayKeyAction.Up) Call(type, processor, "Cancel", 6d); };
            type.GetEvent("KeyEvent").AddEventHandler(processor, disableOnUp);
            before = keys.Count; send(1, 254, 40, 6); send(1, 255, 41, 6.1);
            Check(keys.Count == before + 2, "Disabling during release generated a late click.");
        }

        static void CheckHub()
        {
            var hub = Internal("DeviceEventHub");
            Call(hub, null, "Reset");
            var readyField = hub.GetField("Ready", Flags); var startField = hub.GetField("Start", Flags); var stopField = hub.GetField("Stop", Flags);
            var oldReady = readyField.GetValue(null); var oldStart = startField.GetValue(null); var oldStop = stopField.GetValue(null);
            bool ready = true;
            int starts = 0, stops = 0, main = 0, raw = 0, simulation = 0;
            XvSystemSetting.device_stream_callback nativeCallback = null;
            readyField.SetValue(null, new Func<bool>(() => ready));
            startField.SetValue(null, new Func<XvSystemSetting.device_stream_callback, int>(cb => { nativeCallback = cb; starts++; return 123; }));
            stopField.SetValue(null, new Action(() => stops++));
            var leases = new List<IDisposable>();
            Func<Action<XvEvent>, bool, bool, IDisposable> subscribe = (callback, native, rawThread) => {
                var lease = (IDisposable)Call(hub, null, "Subscribe", callback, native, rawThread); leases.Add(lease); return lease; };
            try
            {
                var first = subscribe(_ => main++, true, false);
                var legacy = subscribe(_ => raw++, true, true);
                subscribe(_ => simulation++, false, false);
                Call(hub, null, "Tick", 10d); Call(hub, null, "Tick", 11d);
                Check(starts == 1, "Multiple consumers started multiple native streams.");
                var worker = new System.Threading.Thread(() => nativeCallback(new XvEvent { type = 1, state = 254 }));
                worker.Start(); worker.Join();
                Check(raw == 1 && main == 0, "Legacy callback threading was changed or new input ran on the callback thread.");
                Call(hub, null, "Tick", 12d);
                Check(main == 1 && simulation == 0, "Main-thread delivery or simulation separation failed.");
                first.Dispose(); Call(hub, null, "Tick", 13d);
                Check(stops == 0, "One consumer stopped another consumer's stream.");
                ready = false; Call(hub, null, "Tick", 14d); Check(stops == 1, "Disconnect did not stop the stream.");
                ready = true; Call(hub, null, "Tick", 15d); Check(starts == 2, "Reconnect did not restore the stream.");
                Call(hub, null, "Pause", true); Check(stops == 2, "Pause did not stop the stream.");
                Call(hub, null, "Pause", false); Call(hub, null, "Tick", 16d); Check(starts == 3, "Resume did not restore the stream.");
                Call(hub, null, "Simulate", new XvEvent { type = 17, state = 99 }); Call(hub, null, "Tick", 17d);
                Check(simulation == 1 && main == 1 && raw == 1, "Simulation was forwarded to native consumers.");
                legacy.Dispose();
                Call(hub, null, "Simulate", new XvEvent { type = 17, state = 99 });
                Call(hub, null, "Tick", 18d);
                Check(stops == 3, "Last native consumer did not release the stream.");
                Check(simulation == 2, "Native stream teardown discarded independent simulation events.");
            }
            finally
            {
                foreach (var lease in leases) lease.Dispose();
                Call(hub, null, "Reset");
                readyField.SetValue(null, oldReady); startField.SetValue(null, oldStart); stopField.SetValue(null, oldStop);
            }
        }

        static void CheckSubscriberIsolation()
        {
            var go = new GameObject("Key subscriber validation");
            bool logging = Debug.unityLogger.logEnabled;
            try
            {
                var input = go.AddComponent<SingrayKeyInput>();
                int delivered = 0;
                Action<SingrayKeyEvent> handlers = _ => throw new InvalidOperationException("Expected validation exception");
                handlers += _ => delivered++;
                var methods = typeof(SingrayKeyInput).GetMethods(Flags);
                MethodInfo method = Array.Find(methods, m => m.Name == "InvokeSafe" && m.IsGenericMethodDefinition);
                // Exercise the failure path without adding an expected error to the customer's Console.
                Debug.unityLogger.logEnabled = false;
                method.MakeGenericMethod(typeof(SingrayKeyEvent)).Invoke(input, new object[] { handlers, default(SingrayKeyEvent) });
                Check(delivered == 1, "A failing subscriber prevented another subscriber from receiving the event.");
            }
            finally { Debug.unityLogger.logEnabled = logging; UnityEngine.Object.DestroyImmediate(go); }
        }

        [MenuItem("Singray/Input/Create Key Input Example Scene")]
        public static void CreateExample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
            Directory.CreateDirectory(Root + "/Profiles");
            const string profilePath = Root + "/Profiles/NativeSDKDefault.asset";
            if (!AssetDatabase.LoadAssetAtPath<SingrayKeyMappingProfile>(profilePath))
            {
                var profile = ScriptableObject.CreateInstance<SingrayKeyMappingProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            const string path = "Assets/XRFoundation/SampleScenes/KeyInput/KeyInput.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var go = new GameObject("Singray Key Input");
                go.AddComponent<SingrayKeyInput>();
                Directory.CreateDirectory(Root + "/Prefabs");
                PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/SingrayKeyInput.prefab");
                var exampleType = typeof(SingrayKeyInput).Assembly.GetType("Singray.Samples.SingrayKeyInputExample");
                if (exampleType != null) go.AddComponent(exampleType);
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f, .06f, .09f);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception("Example scene could not be saved.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (original.IsValid()) SceneManager.SetActiveScene(original); }
            AssetDatabase.SaveAssets();
        }
    }
}
