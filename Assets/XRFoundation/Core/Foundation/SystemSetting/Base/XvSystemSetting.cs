using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Singray.Foundation
{
    /// <summary>
    //key = 2, state = 0: glasses removed
    //key = 2, state = 1: glasses worn
    //key = 6, state = 0: ambient light
    //key = 14, 1, 13, 3; state = 254: pressed, 255: released
    //key = 17, 18; state = 101: positive rotation, 99: negative rotation
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XvEvent
    {
        public double hostTimestamp;
        public long edgeTimestampUs;
        public int type;
        public int state;
    };
    public class XvSystemSetting 
    {
        /// <summary>
        /// Set glasses brightness
        /// </summary>
        /// <param name="level">Brightness level from 0 to 9</param>
        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_display_set_brightnesslevel(int level); //level specifies the brightness level

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_start_event_stream(device_stream_callback cb);

        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_stop_event_stream();

        public delegate void device_stream_callback(XvEvent xvEvent);
    }
}
