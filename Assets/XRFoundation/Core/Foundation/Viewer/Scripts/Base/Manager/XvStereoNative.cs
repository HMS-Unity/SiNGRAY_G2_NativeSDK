using System;
using System.Runtime.InteropServices;

namespace Singray.Foundation
{
    // ABI verified against the bundled ARM64 wrapper's DWARF; retain API.cs signatures.
    internal static class XvStereoNative
    {
        private const string Library = "xslam-unity-wrapper";
        [DllImport(Library, EntryPoint = "xslam_start_stereo_stream")] private static extern void StartNative();
        [DllImport(Library, EntryPoint = "xslam_stop_stereo_stream")] private static extern void StopNative();
        [DllImport(Library, EntryPoint = "xv_get_fe_camera_intrinsics_param")]
        private static extern void CalibrationSizesNative(ref int cameraCount, ref int modelCount);
        [DllImport(Library, EntryPoint = "xslam_get_stereo_image_by_index")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ReadNative(int index, IntPtr data, int width, int height, ref double timestamp);

        internal delegate bool ReadFrame(int index, IntPtr data, int width, int height, ref double timestamp);
        internal static Func<bool> Ready = () =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        };
        internal static Action Start = StartNative;
        internal static Action Stop = StopNative;
        internal static Func<int> Width = API.xslam_get_stereo_width;
        internal static Func<int> Height = API.xslam_get_stereo_height;
        internal static ReadFrame Read = (int index, IntPtr data, int width, int height, ref double timestamp) =>
            index == 0 ? API.xslam_get_left_image(data, width, height, ref timestamp) :
            index == 1 ? API.xslam_get_right_image(data, width, height, ref timestamp) :
            ReadNative(index, data, width, height, ref timestamp);
        internal static Func<int> CameraCount = () =>
        {
            int cameras = 0, models = 0;
            // First output is Calibration entry count. Query only after Ready.
            CalibrationSizesNative(ref cameras, ref models);
            return cameras;
        };
    }
}
