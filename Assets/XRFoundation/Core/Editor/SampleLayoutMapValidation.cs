using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Singray.Foundation
{
    public static class SampleLayoutMapValidation
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private const string Output = "Builds/SampleLayoutMapValidation";
        private static int checks;
        public static void Run()
        {
            checks = 0;
            Directory.CreateDirectory(Output);
            ValidateMapReset();

            File.WriteAllText(Output + "/validation.txt", "PASS: " + checks +
                " checks. Map reset: nonblocking, serialized, new pose required, failure/exception/timeout, scene exit. " +
                "" +
                "Native SLAM recovery requires G2 verification.\n");
            Debug.Log("Sample layout/map validation PASSED: " + checks + " checks.");
        }
        private static object Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, Instance).Invoke(target, args);
        private static void Require(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        private static void ValidateMapReset()
        {
            Type type = typeof(XvSpatialMapManager);
            FieldInfo Ready = type.GetField("readyNative", Static), Reset = type.GetField("resetNative", Static),
                Pose = type.GetField("poseTimestamp", Static), TaskField = type.GetField("nativeResetTask", Static);
            object oldReady = Ready.GetValue(null), oldReset = Reset.GetValue(null), oldPose = Pose.GetValue(null), oldTask = TaskField.GetValue(null);
            var root = new GameObject("Map reset validation"); root.SetActive(false);
            var manager = root.AddComponent<XvSpatialMapManager>();
            var otherRoot = new GameObject("Other map scene"); otherRoot.SetActive(false);
            var other = otherRoot.AddComponent<XvSpatialMapManager>();
            var release = new ManualResetEventSlim(false);
            var entered = new ManualResetEventSlim(false);
            int calls = 0;
            long timestamp = 100;
            bool Busy() => (bool)type.GetProperty("IsResetting", Instance).GetValue(manager);
            Task<bool> Current() => (Task<bool>)TaskField.GetValue(null);
            void Finish() { Require(SpinWait.SpinUntil(() => Current().IsCompleted, 2000), "Mock reset worker did not finish."); Invoke(manager, "PollReset"); }
            try
            {
                Ready.SetValue(null, (Func<bool>)(() => true));
                Pose.SetValue(null, (Func<long>)(() => timestamp));
                Reset.SetValue(null, (Func<bool>)(() => { Interlocked.Increment(ref calls); entered.Set(); release.Wait(); return true; }));
                manager.StartSlamMap();
                Require(entered.Wait(2000) && Busy() && !Current().IsCompleted, "Reset did not return while native work was blocked.");
                manager.StartSlamMap(); other.StartSlamMap();
                Require(calls == 1, "Concurrent map resets were submitted.");
                Require(manager.SaveSlamMap() == null, "Save was accepted while reset was pending.");
                release.Set(); Finish();
                Require(Busy(), "Old tracking pose was accepted after reset.");
                timestamp = 101; Invoke(manager, "PollReset");
                Require(!Busy(), "Fresh pose did not finish reset.");
                Debug.Log("Map validation: expected failure-path diagnostics follow.");
                Reset.SetValue(null, (Func<bool>)(() => false));
                manager.StartSlamMap(); Finish();
                Require(!Busy() && ((string)type.GetProperty("ResetStatus", Instance).GetValue(manager)).Contains("failed"), "Native reset failure was hidden.");
                Reset.SetValue(null, (Func<bool>)(() => { throw new InvalidOperationException("mock native reset failure"); }));
                manager.StartSlamMap(); Finish();
                Require(!Busy(), "Faulted reset left the manager permanently busy.");
                release.Reset(); entered.Reset();
                Reset.SetValue(null, (Func<bool>)(() => { Interlocked.Increment(ref calls); entered.Set(); release.Wait(); return true; }));
                manager.StartSlamMap(); Require(entered.Wait(2000), "Timeout worker did not start.");
                type.GetField("resetDeadline", Instance).SetValue(manager, -1f);
                Invoke(manager, "PollReset");
                Require(Busy() && (bool)type.GetField("resetTimedOut", Instance).GetValue(manager), "Timeout discarded an in-flight native reset.");
                int previousCalls = calls;
                Invoke(manager, "OnDisable");
                other.StartSlamMap();
                Require(calls == previousCalls, "Leaving a scene allowed overlapping native work.");
                release.Set();
                Require(SpinWait.SpinUntil(() => Current().IsCompleted, 2000), "Pending reset did not drain on scene exit.");
            }
            finally
            {
                release.Set();
                var current = Current(); if (current != null) SpinWait.SpinUntil(() => current.IsCompleted, 2000);
                Ready.SetValue(null, oldReady); Reset.SetValue(null, oldReset); Pose.SetValue(null, oldPose); TaskField.SetValue(null, oldTask);
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(otherRoot);
                release.Dispose(); entered.Dispose();
            }
        }
    }
}
