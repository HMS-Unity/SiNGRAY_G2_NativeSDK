using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Singray.Foundation
{
    public static class TofSwitchValidation
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int width, height, reads, count;
        private static double timestamp;
        private static uint size;
        private static bool race;
        private static readonly List<string> commands = new List<string>();

        private static bool Info(ref int w, ref int h, ref long edge, ref double host, ref uint bytes)
        { w = width; h = height; edge = (long)(timestamp * 1000000); host = timestamp; bytes = size; return true; }

        [MenuItem("Singray/Validation/ToF Switch")]
        public static void Run()
        {
            const string output = "Builds/TofSwitchValidation";
            Directory.CreateDirectory(output);
            var native = typeof(XvTofManager).Assembly.GetType("Singray.Foundation.XvTofNative");
            var previous = new Dictionary<FieldInfo, object>();
            foreach (var field in native.GetFields(Static)) if (!field.IsLiteral) previous[field] = field.GetValue(null);
            var singleton = typeof(XvTofManager).GetField("xvTofManager", Static);
            object oldManager = singleton.GetValue(null);
            XvTofManager manager = null;
            XvTofCamera reader = null;
            try
            {
                count = reads = 0; commands.Clear();
                width = 16; height = 8; timestamp = 100; size = (uint)(width * height * 4); race = false;
                Set(native, "Ready", (Func<bool>)(() => true));
                Set(native, "Stop", (Action)(() => commands.Add("stop")));
                Set(native, "Start", (Action)(() => commands.Add("start")));
                Set(native, "Sony", (Action<int,int,int>)((a,b,c) => commands.Add("sony:" + a)));
                Set(native, "Ir", (Action<int,int,int>)((a,b,c) => commands.Add("ir")));
                Set(native, "Mode", (Func<int,int>)(mode => { commands.Add("mode:" + mode); return 1; }));
                var info = native.GetField("Info", Static);
                info.SetValue(null, Delegate.CreateDelegate(info.FieldType, typeof(TofSwitchValidation).GetMethod("Info", Static)));
                Set(native, "TimestampFloor", 0d);
                Set(native, "ReadDepth", (Func<float[],bool>)(buffer =>
                {
                    reads++;
                    Require(buffer.Length == 640 * 480, "Native copy must reserve maximum supported capacity.");
                    for (int i = 0; i < width * height; i++) buffer[i] = 1;
                    if (race) timestamp++;
                    return true;
                }));
                singleton.SetValue(null, null);
                manager = XvTofManager.GetXvTofManager();
                var cloud = Parameters(TofStreamMode.CloudOnLeftHandSlam, SonyTofLibMode.M2MIX_DF);
                manager.StartTofStream(xvTofIRCameraParameter: cloud);
                Require(string.Join(",", commands) == "stop,mode:4,start,sony:4", "Supplier two-step cloud startup changed.");
                manager.StopTofStream();
                var depth = Parameters(TofStreamMode.DepthOnly, SonyTofLibMode.IQMIX_SF);
                depth.streamType = TofStreamType.DeapthStream;
                manager.StartCapture(xvTofCameraParameter: depth);
                Require(manager.IsOn(TofStreamType.DeapthStream) && commands.Contains("mode:0") && commands.Contains("sony:1"),
                    "Cloud -> depth must apply both new mode and Sony settings.");
                int calls = commands.Count;
                manager.StartCapture(depth);
                Require(commands.Count == calls, "Duplicate request restarted the native stream.");
                depth.streamType = TofStreamType.IRStream;
                manager.StartCapture(depth);
                manager.StopCapture(streamType: TofStreamType.IRStream);
                Require(manager.IsOn(TofStreamType.DeapthStream), "Stopping IR unexpectedly stopped the depth reader.");
                manager.StopTofStream();
                Require(!manager.IsOn(TofStreamType.DeapthStream) && !manager.modelSet, "Stop left stale reader/mode state.");

                int published = 0;
                reader = new XvTofCamera(depth, _ => published++);
                reader.StartCapture();
                reader.Update();
                Require(published == 0 && reads == 0, "Cached pre-start frame reached the RGBA/depth-copy path.");
                timestamp++;
                reader.Update();
                Require(published == 1 && reads == 1, "Fresh raw-depth frame was not published.");
                var first = Get<Texture2D>(reader, "tex");
                reader.Update();
                Require(reads == 1, "Duplicate frame was processed again.");
                width = 8; height = 4; size = (uint)(width * height * 2); timestamp++;
                reader.Update();
                Require(first == null && Get<Texture2D>(reader, "tex").width == 8 && published == 2,
                    "Resolution switch did not replace/release its texture.");
                size = (uint)(width * height * 12); timestamp++;
                reader.Update();
                Require(reads == 2 && published == 2, "Cloud/unsupported payload reached image conversion.");
                width = 10000; height = 10000; size = 400000000; timestamp++;
                reader.Update();
                Require(reads == 2, "Unbounded dimensions reached the capacity-less native copy.");
                width = 8; height = 4; size = 128; race = true; timestamp++;
                reader.Update();
                Require(published == 2, "Frame changed during native copy but was still published.");
                race = false;
                reader.Update();
                Require(published == 3, "Reader did not recover after a metadata race.");
                for (int i = 0; i < 100; i++) { timestamp++; reader.Update(); }
                Require(Get<float[]>(reader, "depthBuffer").Length == 307200, "Depth storage grew during steady capture.");
                var last = Get<Texture2D>(reader, "tex");
                reader.StopCapture(); reader.StopCapture();
                Require(last == null && Get<float[]>(reader, "depthBuffer") == null, "Stop did not release image storage.");
                reader.StartCapture();
                typeof(XvTofCamera).GetField("startedAt", Instance).SetValue(reader, Time.realtimeSinceStartup - 11);
                reader.Update();
                Require(!reader.IsOpen, "Missing first frame did not time out.");
                Set(native, "Mode", (Func<int,int>)(mode => 0));
                manager.StartTofStream(cloud);
                Require(!manager.modelSet && !(bool)typeof(XvTofManager).GetProperty("LastStartSubmitted", Instance).GetValue(manager),
                    "Rejected native mode was treated as a successful configuration.");
                Set(native, "Mode", (Func<int,int>)(mode => 1));
                Set(native, "Sony", (Action<int,int,int>)((a,b,c) => { throw new InvalidOperationException("expected injected native failure"); }));
                manager.StartTofStream(cloud);
                Require(!(bool)typeof(XvTofManager).GetProperty("LastStartSubmitted", Instance).GetValue(manager)
                    && !manager.modelSet, "Native exception did not roll back startup.");
                int commandCount = commands.Count;
                Set(native, "Stop", (Action)(() => { throw new InvalidOperationException("expected injected stop failure"); }));
                manager.StartTofStream(cloud);
                Require(commands.Count == commandCount && !manager.modelSet,
                    "A failed stop was followed by another native mode/start command.");
                File.WriteAllText(output + "/validation.txt", $"PASS: {count} assertions; mode/algorithm switch, two-step startup, duplicate request, IR/depth coexistence, stale/duplicate frame rejection, bounded raw copy, texture replacement, metadata race, timeout, cleanup and failure rollback.\nNative backend simulated; G2 mixed-scenario memory regression remains pending.\n");
                Debug.Log("ToF switch validation PASSED: " + count);
            }
            finally
            {
                reader?.StopCapture();
                manager?.StopTofStream();
                singleton.SetValue(null, oldManager);
                foreach (var entry in previous) entry.Key.SetValue(null, entry.Value);
            }
        }
        private static XvTofCameraParameter Parameters(TofStreamMode mode, SonyTofLibMode sony) => new XvTofCameraParameter
            { tofStreamMode = mode, sonyTofLibMode = sony, tofResolution = TofResolution.QVGA, tofFramerate = TofFramerate.FPS_30 };
        private static void Set(Type type, string name, object value) => type.GetField(name, Static).SetValue(null, value);
        private static T Get<T>(object value, string name) => (T)value.GetType().GetField(name, Instance).GetValue(value);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); count++; }
    }
}
