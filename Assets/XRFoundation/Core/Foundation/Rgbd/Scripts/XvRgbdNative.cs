using System;
using UnityEngine;

namespace Singray.Foundation
{
    internal static class XvRgbdNative
    {
        internal delegate bool QueryPoint(ref API.Vector3F position, ref API.Vector2F pixel, double timestamp, float radius);
        internal static Func<bool> Ready = () =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        };
        internal static Action Start = API.xv_start_rgb_pixel_pose;
        internal static Action Stop = API.xv_stop_rgb_pixel_pose;
        internal static QueryPoint Query = API.xv_get_rgb_pixel_pose;
        internal static Func<API.pointer_3dpose[], Vector2[], int, double, float, bool> QueryBatch = API.xslam_start_get_rgb_pixel_buff3d_pose;
    }
}
