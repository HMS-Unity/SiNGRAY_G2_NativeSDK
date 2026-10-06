using AOT;
using UnityEngine;
using Singray.Engine;
using Singray.SystemEvents;
using static Singray.Foundation.XvSystemSetting;

namespace Singray.Foundation
{
    /// <summary>
    /// Provides methods to read and configure system parameters
    /// </summary>
    public sealed class XvSystemSettingManager : MonoBehaviour
    {
        private XvSystemSettingManager() { }

        private int level = 6;
        private static device_stream_callback userEventCallback;

        /// <summary>
        /// Get the current brightness
        /// </summary>
        /// <returns></returns>
        public int GetBrightnessLevel() {
            return level;
        }
        /// <summary>
        /// Set glasses brightness
        /// </summary>
        /// <param name="level">1~9</param>
        public void SetBrightnessLevel(int level)
        {
            this.level = level;


#if UNITY_EDITOR
            return;
#endif
            XvSystemSetting.xslam_display_set_brightnesslevel(level);
            MyDebugTool.LogDiagnostic("DISPLAY", "brightness", $"brightnessLevel={level}", 0f);
        }

        /// <summary>
        /// Set the current IPD
        /// </summary>
        /// <param name="ipd">55mm~75mm</param>
        public void SetIPD(float ipd)
        {
#if UNITY_EDITOR
            return;
#endif

            float nowIpd = GetIPD();
            bool nativeUpdated = XvXRAndroidDevice.updateCalibra((2 * ipd - nowIpd) / 10);
            MyDebugTool.LogDiagnostic(
                "DISPLAY",
                "ipd-native-calibration",
                $"requestedIpdMm={ipd:F2} currentIpdMm={nowIpd:F2} nativeUpdated={nativeUpdated}",
                0f);
            XvXRManager.SDK.GetDevice().setFedDis((2 * ipd - nowIpd) / 10);
            XvXREye.EDI = 0;
        }
        /// <summary>
        /// Get the current IPD
        /// </summary>
        /// <returns>Value in millimeters (mm)</returns>
        public float GetIPD()
        {
#if UNITY_EDITOR
            return 0;
#endif
            API.stereo_pdm_calibration fed = XvXRManager.SDK.GetDevice().GetFed();
            float nowIpd = (float)(fed.calibrations[1].extrinsic.translation[0] - fed.calibrations[0].extrinsic.translation[0]);
            nowIpd *= 1000;
            return nowIpd;
        }

        /// <summary>
        /// Listen for glasses button, wear-state and ambient-light events
        /// </summary>
        /// <param name="cb"></param>
        public void XSlamStartEventStream(device_stream_callback cb) {

#if UNITY_EDITOR
            return;
#endif
            userEventCallback = cb;
            int result = xslam_start_event_stream(OnDiagnosticDeviceEvent);
            MyDebugTool.LogDiagnostic("DEVICE_EVENT", "start", $"xslam_start_event_stream result={result}", 0f);
        }

        [MonoPInvokeCallback(typeof(device_stream_callback))]
        private static void OnDiagnosticDeviceEvent(XvEvent xvEvent)
        {
            string category = xvEvent.type == 6 ? "LIGHT" : "DEVICE_EVENT";
            MyDebugTool.LogDiagnostic(
                category,
                "event-" + xvEvent.type,
                $"type={xvEvent.type} state={xvEvent.state} hostTs={xvEvent.hostTimestamp:F6} edgeTsUs={xvEvent.edgeTimestampUs}",
                0f);
            userEventCallback?.Invoke(xvEvent);
        }


        public void XSlamStopEventStream()
        {
            xslam_stop_event_stream();
            userEventCallback = null;
            MyDebugTool.LogDiagnostic("DEVICE_EVENT", "stop", "event stream stopped", 0f);
        }

       //Get the current volume
        public int GetVolumeCurrent()
        {
#if UNITY_EDITOR
            return 0;
#endif
            return AndroidConnection.getVolumeCurr();
        }

        //Get the maximum volume
        public int GetVolumeMax()
        {
#if UNITY_EDITOR
            return 0;
#endif
            return AndroidConnection.getVolumeMax();
        }
        /// <summary>
        /// Adjust volume
        /// </summary>
        /// <param name="direction">-1 = decrease volume, 1 = increase volume</param>
        /// <returns></returns>
        public int AdjustVolume(int direction)
        {
#if UNITY_EDITOR
            return 0;
#endif
            return AndroidConnection.adjustVolume(direction);

        }

    }
}
