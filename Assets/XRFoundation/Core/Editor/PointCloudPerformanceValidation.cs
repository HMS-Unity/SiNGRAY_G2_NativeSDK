using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Singray.Foundation.SampleScenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Singray.Foundation
{
    public static class PointCloudPerformanceValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Singray/Validation/Point Cloud Performance")]
        public static void Run()
        {
            const string output = "Builds/PointCloudPerformance";
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Point cloud validation");
            root.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var cloud = root.AddComponent<XvParticlesCloudPoint>();
                cloud.ps = root.AddComponent<ParticleSystem>();
                var samples = new Vector3[307200];
                for (int i = 0; i < samples.Length; i++) samples[i] = new Vector3(0, i, 1);
                cloud.StartDraw(samples);
                var particles = Get<ParticleSystem.Particle[]>(cloud, "allParticles");
                Require(particles.Length == 61440 && cloud.ps.particleCount == 61440, "Point density changed.");
                Require(particles[1].position == samples[5] && particles[61439].position == samples[307195],
                    "Stride/last point changed.");
                Require(particles[0].startColor.a == 255 && particles[0].startSize == 0.015f
                    && particles[0].remainingLifetime > 0, "Point appearance or lifetime changed.");

                // Warm both the ring and native SetParticles path before measuring allocations.
                for (int i = 0; i < 4; i++) cloud.StartDraw(samples);
                long probeBefore = GC.GetAllocatedBytesForCurrentThread();
                var probe = new byte[1024];
                bool allocationCounterAvailable = GC.GetAllocatedBytesForCurrentThread() - probeBefore >= 1024;
                GC.KeepAlive(probe);
                Action optimized = () => cloud.StartDraw(samples);
                Set(cloud, "colorByDistance", false);
                var blueResult = Measure(optimized, 16);
                Set(cloud, "colorByDistance", true);
                var optimizedResult = Measure(optimized, 16);
                Require(ReferenceEquals(particles, Get<ParticleSystem.Particle[]>(cloud, "allParticles")),
                    "Steady-state particle storage was replaced.");
                Require(!allocationCounterAvailable || optimizedResult.bytes == 0, "Steady-state StartDraw allocated managed memory.");

                var buffered = Get<List<Vector3>>(cloud, "t_FilterVec");
                int head = Get<int>(cloud, "bufferStart");
                Require(buffered.Count == 131072, "Recent point buffer must remain bounded.");
                Vector3 first = buffered[head];
                Vector3 last = buffered[(head + buffered.Count - 1) % buffered.Count];
                Set(cloud, "_saveFolder", Path.GetFullPath(output));
                cloud.savePointCloud();
                string saved = Get<string>(cloud, "_currentSecondFile");
                cloud.ClearPointCloud(); // Flush/close writer before inspecting ASC contents.
                var lines = File.ReadAllLines(saved);
                Require(lines.Length == 131072 && lines[0] == Asc(first) && lines[lines.Length - 1] == Asc(last),
                    "ASC must preserve chronological order across ring wraps.");
                Require(buffered.Count == 0 && Get<int>(cloud, "bufferStart") == 0 && cloud.ps.particleCount == 0,
                    "Stop/clear must reset ring and particles.");
                cloud.StartDraw(new[] { Vector3.one, Vector3.zero, Vector3.one, Vector3.zero, Vector3.one, Vector3.right });
                Require(cloud.ps.particleCount == 2, "Restart/non-multiple-of-five sampling failed.");
                cloud.StartDraw(Array.Empty<Vector3>());
                Require(cloud.ps.particleCount == 0, "Empty input left stale particles.");
                cloud.StartDraw(samples);
                particles = Get<ParticleSystem.Particle[]>(cloud, "allParticles");
                cloud.StartDraw(new[] { Vector3.one });
                Require(cloud.ps.particleCount == 1 && ReferenceEquals(particles, Get<ParticleSystem.Particle[]>(cloud, "allParticles")),
                    "Smaller frames must reuse storage and discard old particles.");

                // Reproduce the previous hot path for an Editor-only allocation/time comparison.
                var frame = new List<Vector3>();
                var history = new List<Vector3>();
                Action previous = () =>
                {
                    frame.Clear();
                    for (int i = 0; i < samples.Length; i++) if (i % 5 == 0) frame.Add(samples[i]);
                    int overflow = history.Count + frame.Count - 131072;
                    if (overflow > 0) history.RemoveRange(0, overflow);
                    history.AddRange(frame);
                    var array = new ParticleSystem.Particle[frame.Count];
                    var main = cloud.ps.main;
                    main.maxParticles = frame.Count;
                    cloud.ps.Emit(frame.Count);
                    cloud.ps.GetParticles(array);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].position = frame[i];
                        array[i].startColor = Color.blue;
                        array[i].startSize = 0.015f;
                    }
                    cloud.ps.SetParticles(array, array.Length);
                };
                for (int i = 0; i < 4; i++) previous();
                var previousResult = Measure(previous, 8);
                string allocationReport = allocationCounterAvailable
                    ? $"Measured managed allocation: previous={previousResult.bytes / 8} B/update; optimized={optimizedResult.bytes / 16} B/update.\n"
                    : "Managed allocation counter is unavailable in this Editor. Particle-array payload removed per refresh: " +
                        (System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleSystem.Particle)) * 61440) +
                        " bytes (excludes array header); replacement array is reused.\n";
                ValidateDistanceColors(cloud);
                ValidateRendering(cloud, scene, output);
                string report = "PASS: density, sampling, appearance, live particles, reused particle/list storage, " +
                    "bounded ring, ASC ordering, stop/clear, restart, empty and smaller frames; " +
                    "distance colors, near/far saturation, legacy blue option and rendered gradient.\n" +
                    $"Editor 307200 input points / 61440 particles: previous={previousResult.ms:F3} ms/update; " +
                    $"optimized={optimizedResult.ms:F3} ms/update.\n" + allocationReport +
                    $"Same current renderer: blue={blueResult.ms:F3} ms/update; distanceColor={optimizedResult.ms:F3} ms/update.\n" +
                    "Previous comparison uses the old blue-only path; current path includes distance coloring. " +
                    "Editor timings are a local comparison, not G2 FPS. Native acquisition, rendering, input and thermal behavior require device testing.\n";
                File.WriteAllText(output + "/validation.txt", report);
                Debug.Log(report);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); EditorUtility.ClearProgressBar(); }
        }

        private static void ValidateDistanceColors(XvParticlesCloudPoint cloud)
        {
            var points = new Vector3[15];
            points[0] = new Vector3(0, 0, 0.5f);
            points[5] = new Vector3(0, 0, 2.75f);
            points[10] = new Vector3(0, 0, 5);
            cloud.StartDraw(points);
            var p = Get<ParticleSystem.Particle[]>(cloud, "allParticles");
            Require(p[0].startColor.r > p[0].startColor.b && p[1].startColor.g > p[1].startColor.r &&
                p[2].startColor.b > p[2].startColor.r, "Near/middle/far colors did not show warm/green/blue.");
            Color32 near = p[0].startColor, far = p[2].startColor;
            points[0] = Vector3.zero; points[10] = new Vector3(0, 0, 50);
            cloud.StartDraw(points);
            Require(p[0].startColor.Equals(near) && p[2].startColor.Equals(far), "Color range did not saturate at its limits.");
            var main = cloud.ps.main;
            var space = main.simulationSpace;
            var originalPosition = cloud.transform.position;
            cloud.transform.position = new Vector3(0, 0, 4.5f);
            points[0] = new Vector3(0, 0, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            cloud.StartDraw(points);
            Require(p[0].startColor.Equals(far), "Local particle positions were not converted to world distance.");
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            cloud.StartDraw(points);
            Require(p[0].startColor.Equals(near), "World particle distance incorrectly included the particle transform.");
            main.simulationSpace = space;
            cloud.transform.position = originalPosition;
            var viewObject = new GameObject("Distance color origin check", typeof(Camera));
            viewObject.tag = "MainCamera";
            viewObject.transform.position = new Vector3(0, 0, 4.5f);
            try
            {
                points[0] = new Vector3(0, 0, 5);
                cloud.StartDraw(points);
                Require(p[0].startColor.Equals(near), "Distance colors did not follow the viewer's world position.");
            }
            finally { UnityEngine.Object.DestroyImmediate(viewObject); }
            Set(cloud, "colorByDistance", false);
            cloud.StartDraw(points);
            Require(p[0].startColor.Equals((Color32)Color.blue) && p[2].startColor.Equals((Color32)Color.blue), "Legacy blue option failed.");
            Set(cloud, "colorByDistance", true);
        }

        private static void ValidateRendering(XvParticlesCloudPoint cloud, UnityEngine.SceneManagement.Scene scene, string output)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/XRFoundation/SampleScenes/TofPointCloud/Prefabs/XvPointCloudDemo.prefab");
            cloud.ps.GetComponent<ParticleSystemRenderer>().sharedMaterial =
                prefab.GetComponentInChildren<ParticleSystem>(true).GetComponent<ParticleSystemRenderer>().sharedMaterial;
            cloud.ClearPointCloud();
            cloud.gameObject.SetActive(true);
            var grid = new Vector3[5000];
            for (int i = 0; i < grid.Length; i++)
            {
                float t = (i % 100) / 99f;
                grid[i] = new Vector3((i % 100 - 50) * 0.015f, (i / 100 - 25) * 0.015f, Mathf.Lerp(0.5f, 5, t));
            }
            cloud.StartDraw(grid);
            Require(cloud.ps.isPaused && cloud.ps.particleCount == 1000, "Snapshot must remain visible without automatic emission.");
            var cameraObject = new GameObject("Point cloud preview camera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 0.75f;
            camera.transform.position = new Vector3(0, 0, -3);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(640, 480, 24);
            var screenshot = new Texture2D(640, 480, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                screenshot.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
                screenshot.Apply();
                int warm = 0, green = 0, blue = 0;
                foreach (var pixel in screenshot.GetPixels32())
                {
                    if (pixel.r > 64 && pixel.r > pixel.b * 1.3f) warm++;
                    if (pixel.g > 64 && pixel.g > pixel.r * 1.3f && pixel.g > pixel.b) green++;
                    if (pixel.b > 64 && pixel.b > pixel.r * 1.3f && pixel.b > pixel.g) blue++;
                }
                Require(warm > 100 && green > 100 && blue > 100, "Paused point cloud failed to render the full color range.");
                File.WriteAllBytes(output + "/point-cloud.png", screenshot.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(screenshot);
            }
        }

        private static (long bytes, double ms) Measure(Action action, int count)
        {
            var watch = new Stopwatch();
            long before = GC.GetAllocatedBytesForCurrentThread();
            watch.Start();
            for (int i = 0; i < count; i++) action();
            watch.Stop();
            return (GC.GetAllocatedBytesForCurrentThread() - before, watch.Elapsed.TotalMilliseconds / count);
        }
        private static string Asc(Vector3 value) => $"{value.x:F6} {value.y:F6} {value.z:F6}";
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
