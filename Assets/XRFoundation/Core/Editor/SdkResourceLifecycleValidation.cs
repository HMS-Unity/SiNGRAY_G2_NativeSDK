using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Singray.Foundation.SampleScenes;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Singray.Foundation
{
    // Explicitly invoked only. Uses a preview scene; never opens or saves customer/sample scenes.
    public static class SdkResourceLifecycleValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;

        [MenuItem("Singray/Validation/Resource Lifecycle %&F9")]
        public static void Run()
        {
            const string output = "Builds/ResourceLifecycle";
            Directory.CreateDirectory(output);
            checks = 0;
            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject root = new GameObject("Resource lifecycle validation");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                CheckPointBuffer(root, output);
                CheckRgbTexture();
                CheckMeshWait(root);
                CheckScreenshotGate(root);
                var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
                {
                    target = BuildTarget.Android,
                    group = BuildTargetGroup.Android,
                    options = ScriptCompilationOptions.None
                }, output + "/AndroidScripts");
                Require(result.assemblies != null && result.assemblies.Count > 0,
                    "Android script compilation produced no assemblies.");
                string report = $"PASS: {checks} resource lifecycle checks; Android script compilation: {result.assemblies.Count} assemblies.\n" +
                    "Editor checks cover bounded point buffering and writer cleanup, RGB texture replacement/stop, " +
                    "nonblocking mesh wait/timeout, and screenshot fresh-frame/timeout gates.\n" +
                    "G2 native streams, rendering, file capture and device lifecycle still require hardware testing.\n";
                File.WriteAllText(output + "/validation.txt", report);
                Debug.Log(report);
            }
            catch (Exception error)
            {
                File.WriteAllText(output + "/validation.txt", "FAIL: " + error);
                Debug.LogException(error);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void CheckPointBuffer(GameObject root, string output)
        {
            var cloud = root.AddComponent<XvParticlesCloudPoint>();
            cloud.ps = root.AddComponent<ParticleSystem>();
            var samples = new Vector3[5000];
            for (int frame = 0; frame < 200; frame++)
            {
                for (int i = 0; i < samples.Length; i++) samples[i] = new Vector3(frame, i, 1);
                cloud.StartDraw(samples);
                int count = Get<List<Vector3>>(cloud, "t_FilterVec").Count;
                if (count > 131072) throw new Exception("Point buffer grew beyond its cap.");
            }
            var buffered = Get<List<Vector3>>(cloud, "t_FilterVec");
            int first = Get<int>(cloud, "bufferStart");
            Require(buffered.Count == 131072 && buffered[(first + buffered.Count - 1) % buffered.Count].x == 199
                && buffered[first].x == 68 && buffered[first].y == 4640,
                "Point buffer must retain recent samples at a fixed cap.");
            cloud.ClearPointCloud();
            Require(buffered.Count == 0 && Get<List<Vector3>>(cloud, "FilterVec").Count == 0,
                "Point clear must discard pending samples.");
            cloud.StartDraw(new[] { Vector3.one });
            Set(cloud, "_saveFolder", Path.GetFullPath(output));
            cloud.savePointCloud();
            var writer = Get<StreamWriter>(cloud, "_currentWriter");
            Require(writer != null && buffered.Count == 0, "Point save must drain buffered data.");
            Call(cloud, "OnDisable");
            Require(Get<StreamWriter>(cloud, "_currentWriter") == null, "Disable must release the ASC writer.");
            bool disposed = false;
            try { writer.WriteLine("must not write"); } catch (ObjectDisposedException) { disposed = true; }
            Require(disposed, "ASC writer remained open after disable.");
        }

        private static void CheckRgbTexture()
        {
            var rgb = new XvARCamera(new XvARCameraParameter(), null);
            try
            {
                Call(rgb, "EnsureTexture", 16, 8);
                var first = Get<Texture2D>(rgb, "tex");
                Call(rgb, "EnsureTexture", 16, 8);
                Require(Get<Texture2D>(rgb, "tex") == first, "Unchanged RGB size must reuse its texture.");
                Call(rgb, "EnsureTexture", 8, 4);
                Require(first == null, "Replaced RGB texture was not released.");
                Require(Get<byte[]>(rgb, "pixelBytes").Length == 8 * 4 * 4, "RGB byte buffer size mismatch.");
                for (int i = 0; i < 40; i++) Call(rgb, "EnsureTexture", 8 + i % 2, 4);
                var last = Get<Texture2D>(rgb, "tex");
                rgb.needOpenCamera = true;
                rgb.StopCapture(); // isOpen is false: no device/native call.
                Require(last == null && Get<byte[]>(rgb, "pixelBytes") == null && !rgb.needOpenCamera,
                    "RGB stop must release buffers and cancel a pending start.");
                Call(rgb, "EnsureTexture", 16, 8);
                Require(Get<Texture2D>(rgb, "tex") != null, "RGB buffers must be recreatable after stop.");
            }
            finally { rgb.StopCapture(); }
        }

        private static void CheckMeshWait(GameObject root)
        {
            var mesh = root.AddComponent<XvSpatialMeshManager>();
            var wait = (IEnumerator)Call(mesh, "StartDetectionWhenReady", Time.realtimeSinceStartup + 10f);
            Require(wait.MoveNext() && wait.MoveNext() && wait.Current == null && !mesh.IsDetecting,
                "Unready mesh startup must yield rather than block or report detecting.");
            ((IDisposable)wait).Dispose();
            mesh.StopMeshDetection();
            Require(!mesh.IsDetecting && Get<Coroutine>(mesh, "startRoutine") == null,
                "Mesh cancellation must leave no pending startup.");
            var timeout = (IEnumerator)Call(mesh, "StartDetectionWhenReady", Time.realtimeSinceStartup - 1f);
            Require(timeout.MoveNext() && !timeout.MoveNext() && !mesh.IsDetecting,
                "Mesh readiness timeout must exit without starting detection.");
        }

        private static void CheckScreenshotGate(GameObject root)
        {
            // Keep components inactive so their device/recorder Awake/Start paths never run.
            var capture = root.AddComponent<XvMRVideoCaptureManager>();
            var recorder = root.AddComponent<XvMediaRecorderManager>();
            Set(capture, "hasFrame", true);
            Set(capture, "<FrameSequence>k__BackingField", 5UL);
            var wait = (IEnumerator)Call(recorder, "CaptureScreenshotWhenReady", capture, 5UL,
                Time.realtimeSinceStartup + 10f);
            Require(wait.MoveNext() && wait.MoveNext() && wait.Current == null,
                "A stale frame must not satisfy a new photo request.");
            Set(capture, "<FrameSequence>k__BackingField", 6UL);
            Require(wait.MoveNext() && wait.Current is WaitForEndOfFrame,
                "Fresh camera data must wait for compositing before JPG capture.");
            int callbacks = 0;
            UnityAction<string> failure = path => { callbacks++; Require(path == null, "Failed photo returned a path."); };
            Set(recorder, "pendingScreenshotCallback", failure);
            Set(capture, "hasFrame", false);
            Require(!wait.MoveNext() && callbacks == 1,
                "Capture stopped before rendering must fail without starting JPG.");
            Set(recorder, "pendingScreenshotCallback", failure);
            var timeout = (IEnumerator)Call(recorder, "CaptureScreenshotWhenReady", capture, 6UL,
                Time.realtimeSinceStartup - 1f);
            Require(timeout.MoveNext() && !timeout.MoveNext() && callbacks == 2 && !recorder.IsTakingScreenshots(),
                "Photo timeout must notify once and clear pending state.");
            Call(capture, "onFrameArrived", new cameraData());
            Require(!capture.HasFrame, "An empty camera event must not be accepted as a real frame.");
        }

        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
    }
}
