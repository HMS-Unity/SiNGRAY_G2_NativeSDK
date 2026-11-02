using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Singray.Foundation
{
    // ABI verified against the DWARF types in the bundled ARM64 unity-wrapper.
    // Keep the customer's API.cs signatures intact; do not interpret void return registers as bools.
    internal static class XvTofNative
    {
        private const string Library = "xslam-unity-wrapper";
        [DllImport(Library, EntryPoint = "xslam_stop_tof_stream")] private static extern void StopNative();
        [DllImport(Library, EntryPoint = "xslam_start_tof_stream")] private static extern void StartNative();
        [DllImport(Library, EntryPoint = "xslam_start_sony_tof_stream")] private static extern void SonyNative(int mode, int resolution, int fps);
        [DllImport(Library, EntryPoint = "xslam_start_tofir_stream")] private static extern void IrNative(int mode, int resolution, int fps);
        [DllImport(Library, EntryPoint = "xslam_tof_set_steam_mode")] private static extern int ModeNative(int mode);
        [DllImport(Library, EntryPoint = "xv_get_tof_info")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool InfoNative(ref int width, ref int height, ref long edgeTimestamp, ref double hostTimestamp, ref uint dataSize);

        internal delegate bool FrameInfo(ref int width, ref int height, ref long edgeTimestamp, ref double hostTimestamp, ref uint dataSize);
        internal static Func<bool> Ready = () =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        };
        internal static Action Stop = StopNative;
        internal static Action Start = StartNative;
        internal static Action<int, int, int> Sony = SonyNative;
        internal static Action<int, int, int> Ir = IrNative;
        internal static Func<int, int> Mode = ModeNative;
        internal static FrameInfo Info = InfoNative;
        internal static Func<float[], bool> ReadDepth = API.xv_get_tof_image_depth;
        internal static double TimestampFloor;

        internal static double CurrentTimestamp()
        {
            int w = 0, h = 0; long edge = 0; double timestamp = 0; uint bytes = 0;
            if (Ready() && Info(ref w, ref h, ref edge, ref timestamp, ref bytes)
                && !double.IsNaN(timestamp) && !double.IsInfinity(timestamp) && timestamp > 0)
                return timestamp;
            return 0;
        }

        internal static void Log(string phase, string detail = "")
        {
            long nativeKb = 0, pssKb = 0;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var debug = new AndroidJavaClass("android.os.Debug"))
                {
                    nativeKb = debug.CallStatic<long>("getNativeHeapAllocatedSize") / 1024;
                    pssKb = phase == "depth-frame" ? -1 : debug.CallStatic<long>("getPss");
                }
            }
            catch (Exception) { }
#endif
            Debug.Log($"[TOF-LIFE] phase={phase} nativeHeapKB={nativeKb} pssKB={pssKb} {detail}");
        }
    }
}
