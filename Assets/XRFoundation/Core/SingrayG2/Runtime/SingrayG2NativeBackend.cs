using System;
using System.Collections.Generic;
using UnityEngine;
using Singray.Foundation;

namespace Singray.G2
{
    /// <summary>Internal adapter around the supplier SDK and native ABI.</summary>
    internal sealed class SingrayG2NativeBackend : ISingrayG2Backend
    {
        private readonly Transform root;
        private readonly List<GameObject> ownedBackendObjects = new List<GameObject>();
        private readonly Vector3[] imuBuffer = new Vector3[3];
        private XvCameraManager cameraBackend;
        private XvSystemSettingManager systemBackend;
        private XvRgbdManager rgbdBackend;
        private SingrayG2RgbSettings rgbSettings = new SingrayG2RgbSettings();
        private SingrayG2TofSettings tofSettings = new SingrayG2TofSettings();
        private double imuTimestamp;
        private bool deviceEventsStarted;

        public SingrayG2NativeBackend(Transform root)
        {
            this.root = root;
            XvCameraManager.onARCameraStreamFrameArrived.AddListener(OnRgbFrame);
            XvCameraManager.onTofDepthCameraStreamFrameArrived.AddListener(OnTofDepthFrame);
            XvCameraManager.onTofIRCameraStreamFrameArrived.AddListener(OnTofInfraredFrame);
            XvCameraManager.onLeftStereoStreamFrameArrived.AddListener(OnLeftTrackingFrame);
            XvCameraManager.onRightStereoStreamFrameArrived.AddListener(OnRightTrackingFrame);
            XvCameraManager.onWebCameraStreamFrameArrived.AddListener(OnComputeCameraFrame);
        }

        public bool IsReady
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return API.xslam_ready();
#else
                return false;
#endif
            }
        }

        public bool IsSimulation => false;

        public event Action<SingrayG2SensorStream, SingrayG2CameraFrame> CameraFrameArrived;
        public event Action<SingrayG2DeviceEvent> DeviceEventArrived;

        private XvCameraManager CameraBackend
        {
            get
            {
                if (cameraBackend == null)
                {
                    cameraBackend = UnityEngine.Object.FindObjectOfType<XvCameraManager>();
                }
                if (cameraBackend == null)
                {
                    cameraBackend = CreateBackend<XvCameraManager>("SingrayG2.CameraBackend");
                }
                return cameraBackend;
            }
        }

        private XvSystemSettingManager SystemBackend
        {
            get
            {
                if (systemBackend == null)
                {
                    systemBackend = UnityEngine.Object.FindObjectOfType<XvSystemSettingManager>();
                }
                if (systemBackend == null)
                {
                    systemBackend = CreateBackend<XvSystemSettingManager>("SingrayG2.SystemBackend");
                }
                return systemBackend;
            }
        }

        private XvRgbdManager RgbdBackend
        {
            get
            {
                if (rgbdBackend == null)
                {
                    rgbdBackend = UnityEngine.Object.FindObjectOfType<XvRgbdManager>();
                }
                if (rgbdBackend == null)
                {
                    rgbdBackend = CreateBackend<XvRgbdManager>("SingrayG2.RgbdBackend");
                }
                return rgbdBackend;
            }
        }

        public void Tick(double timeSeconds)
        {
        }

        public void ConfigureRgb(SingrayG2RgbSettings settings)
        {
            rgbSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (cameraBackend != null)
            {
                ApplyRgbSettings(cameraBackend);
            }
        }

        public void ConfigureTof(SingrayG2TofSettings settings)
        {
            tofSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (cameraBackend != null)
            {
                ApplyTofSettings(cameraBackend);
            }
        }

        public void StartSensor(SingrayG2SensorStream stream)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ApplyCameraSettings();
            CameraBackend.StartCapture(ToVendorStream(stream));
            MyDebugTool.LogDiagnostic("SENSOR", "native-start-" + stream, $"backend=native stream={stream}", 0f);
#else
            MyDebugTool.LogDiagnosticWarning("SENSOR", "native-editor-start-" + stream, $"stream={stream} requires a Singray G2 Android device", 0f);
#endif
        }

        public void StopSensor(SingrayG2SensorStream stream)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            CameraBackend.StopCapture(ToVendorStream(stream));
            MyDebugTool.LogDiagnostic("SENSOR", "native-stop-" + stream, $"backend=native stream={stream}", 0f);
#else
            MyDebugTool.LogDiagnostic("SENSOR", "native-editor-stop-" + stream, $"stream={stream} is not running in Editor", 0f);
#endif
        }

        public bool IsSensorRunning(SingrayG2SensorStream stream)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return CameraBackend.IsOn(ToVendorStream(stream));
#else
            return false;
#endif
        }

        public void StartTofPointCloud()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ApplyTofSettings(CameraBackend);
            CameraBackend.StartTofPointCloud();
#else
            MyDebugTool.LogDiagnosticWarning("TOF", "native-point-cloud-editor", "point cloud requires a Singray G2 Android device", 0f);
#endif
        }

        public bool TryGetTofPointCloud(out Vector3[] points)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return CameraBackend.GetPointCloudData(out points);
#else
            points = null;
            return false;
#endif
        }

        public void StopTofPointCloud()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            CameraBackend.StopTofPointCloud();
#endif
        }

        public void StartRgbd()
        {
            ApplyCameraSettings();
            RgbdBackend.StartRgbPose();
        }

        public void StopRgbd()
        {
            RgbdBackend.StopRgbPose();
        }

        public bool TryGetRgbPixelWorldPosition(Vector2 pixel, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            return RgbdBackend.GetRgbPixel3DPose(pixel, ref worldPosition);
        }

        public int StartImu()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!API.xslam_ready())
            {
                MyDebugTool.LogDiagnosticWarning("IMU", "native-not-ready", "Singray G2 SDK is not ready", 2f);
                return -2;
            }
            int result = API.xslam_start_imu();
            MyDebugTool.LogDiagnostic("IMU", "native-start", $"backend=native result={result}", 0f);
            return result;
#else
            MyDebugTool.LogDiagnosticWarning("IMU", "native-editor", "IMU requires a Singray G2 device", 0f);
            return -1;
#endif
        }

        public int StopImu()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int result = API.xslam_stop_imu();
            MyDebugTool.LogDiagnostic("IMU", "native-stop", $"backend=native result={result}", 0f);
            return result;
#else
            return -1;
#endif
        }

        public bool TryGetImuSample(out SingrayG2ImuSample sample)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (API.xslam_get_imu_array(imuBuffer, ref imuTimestamp))
            {
                sample = new SingrayG2ImuSample(imuTimestamp, imuBuffer[0], imuBuffer[1], imuBuffer[2]);
                return true;
            }
#endif
            sample = default;
            return false;
        }

        public void StartDeviceEvents()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            SystemBackend.XSlamStartEventStream(OnVendorDeviceEvent);
            deviceEventsStarted = true;
#else
            MyDebugTool.LogDiagnosticWarning("DEVICE_EVENT", "native-editor", "device events require a Singray G2 device", 0f);
#endif
        }

        public void StopDeviceEvents()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (deviceEventsStarted)
            {
                SystemBackend.XSlamStopEventStream();
                deviceEventsStarted = false;
            }
#endif
        }

        public void SetDisplayBrightness(int level)
        {
            SystemBackend.SetBrightnessLevel(Mathf.Clamp(level, 1, 9));
        }

        public void Dispose()
        {
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(OnRgbFrame);
            XvCameraManager.onTofDepthCameraStreamFrameArrived.RemoveListener(OnTofDepthFrame);
            XvCameraManager.onTofIRCameraStreamFrameArrived.RemoveListener(OnTofInfraredFrame);
            XvCameraManager.onLeftStereoStreamFrameArrived.RemoveListener(OnLeftTrackingFrame);
            XvCameraManager.onRightStereoStreamFrameArrived.RemoveListener(OnRightTrackingFrame);
            XvCameraManager.onWebCameraStreamFrameArrived.RemoveListener(OnComputeCameraFrame);
            StopDeviceEvents();

            for (int i = 0; i < ownedBackendObjects.Count; ++i)
            {
                if (ownedBackendObjects[i] != null)
                {
                    UnityEngine.Object.Destroy(ownedBackendObjects[i]);
                }
            }
            ownedBackendObjects.Clear();
        }

        private void ApplyCameraSettings()
        {
            ApplyRgbSettings(CameraBackend);
            ApplyTofSettings(CameraBackend);
        }

        private void ApplyRgbSettings(XvCameraManager backend)
        {
            backend.XvARCameraParameter.rgbResolution = ToVendorRgbResolution(rgbSettings.resolution);
            backend.XvARCameraParameter.fps = Mathf.Clamp(rgbSettings.framesPerSecond, 1, 60);
        }

        private void ApplyTofSettings(XvCameraManager backend)
        {
            backend.XvTofCameraParameter.tofResolution = ToVendorTofResolution(tofSettings.resolution);
            backend.XvTofCameraParameter.tofFramerate = ToVendorTofFrameRate(tofSettings.frameRate);
            backend.XvTofCameraParameter.enableGamma = tofSettings.enableInfraredGamma;
        }

        private T CreateBackend<T>(string objectName) where T : Component
        {
            GameObject backendObject = new GameObject(objectName);
            backendObject.transform.SetParent(root, false);
            ownedBackendObjects.Add(backendObject);
            return backendObject.AddComponent<T>();
        }

        private void OnVendorDeviceEvent(XvEvent vendorEvent)
        {
            DeviceEventArrived?.Invoke(new SingrayG2DeviceEvent(
                vendorEvent.hostTimestamp,
                vendorEvent.edgeTimestampUs,
                vendorEvent.type,
                vendorEvent.state));
        }

        private void OnRgbFrame(cameraData data) => PublishFrame(SingrayG2SensorStream.Rgb, data);
        private void OnTofDepthFrame(cameraData data) => PublishFrame(SingrayG2SensorStream.TofDepth, data);
        private void OnTofInfraredFrame(cameraData data) => PublishFrame(SingrayG2SensorStream.TofInfrared, data);
        private void OnLeftTrackingFrame(cameraData data) => PublishFrame(SingrayG2SensorStream.LeftTrackingCamera, data);
        private void OnRightTrackingFrame(cameraData data) => PublishFrame(SingrayG2SensorStream.RightTrackingCamera, data);
        private void OnComputeCameraFrame(cameraData data) => PublishFrame(SingrayG2SensorStream.ComputeCamera, data);

        private void PublishFrame(SingrayG2SensorStream stream, cameraData data)
        {
            if (data == null)
            {
                return;
            }

            CameraParameter parameter = data.parameter;
            CameraFrameArrived?.Invoke(stream, new SingrayG2CameraFrame(
                data.texWidth,
                data.texHeight,
                data.tex,
                parameter.timeStamp,
                parameter.position,
                parameter.rotation,
                parameter.rgb_extrinsic_pos,
                parameter.rgb_extrinsic_rot,
                new Vector4(parameter.fx, parameter.fy, parameter.cx, parameter.cy),
                new Vector4(parameter.k1, parameter.k2, parameter.p1, parameter.p2),
                parameter.k3));
        }

        private static XvCameraStreamType ToVendorStream(SingrayG2SensorStream stream)
        {
            switch (stream)
            {
                case SingrayG2SensorStream.Rgb: return XvCameraStreamType.ARCameraStream;
                case SingrayG2SensorStream.TofDepth: return XvCameraStreamType.TofDepthCameraStream;
                case SingrayG2SensorStream.TofInfrared: return XvCameraStreamType.TofIRCameraStream;
                case SingrayG2SensorStream.LeftTrackingCamera: return XvCameraStreamType.LeftStereoCameraStream;
                case SingrayG2SensorStream.RightTrackingCamera: return XvCameraStreamType.RightStereoCameraStream;
                case SingrayG2SensorStream.ComputeCamera: return XvCameraStreamType.WebCameraStream;
                default: throw new ArgumentOutOfRangeException(nameof(stream), stream, null);
            }
        }

        private static RgbResolution ToVendorRgbResolution(SingrayG2RgbResolution resolution)
        {
            switch (resolution)
            {
                case SingrayG2RgbResolution.R320x240: return RgbResolution.RGB_320x240;
                case SingrayG2RgbResolution.R640x480: return RgbResolution.RGB_640x480;
                case SingrayG2RgbResolution.R1280x720: return RgbResolution.RGB_1280x720;
                case SingrayG2RgbResolution.R1920x1080: return RgbResolution.RGB_1920x1080;
                case SingrayG2RgbResolution.R2560x1920: return RgbResolution.RGB_2560x1920;
                case SingrayG2RgbResolution.R3840x2160: return RgbResolution.RGB_3840x2160;
                default: throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }

        private static TofResolution ToVendorTofResolution(SingrayG2TofResolution resolution)
        {
            switch (resolution)
            {
                case SingrayG2TofResolution.Hqvga: return TofResolution.HQVGA;
                case SingrayG2TofResolution.Qvga: return TofResolution.QVGA;
                case SingrayG2TofResolution.Vga: return TofResolution.VGA;
                default: throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }

        private static TofFramerate ToVendorTofFrameRate(SingrayG2TofFrameRate frameRate)
        {
            switch (frameRate)
            {
                case SingrayG2TofFrameRate.Fps5: return TofFramerate.FPS_5;
                case SingrayG2TofFrameRate.Fps10: return TofFramerate.FPS_10;
                case SingrayG2TofFrameRate.Fps15: return TofFramerate.FPS_15;
                case SingrayG2TofFrameRate.Fps20: return TofFramerate.FPS_20;
                case SingrayG2TofFrameRate.Fps25: return TofFramerate.FPS_25;
                case SingrayG2TofFrameRate.Fps30: return TofFramerate.FPS_30;
                default: throw new ArgumentOutOfRangeException(nameof(frameRate), frameRate, null);
            }
        }
    }
}
