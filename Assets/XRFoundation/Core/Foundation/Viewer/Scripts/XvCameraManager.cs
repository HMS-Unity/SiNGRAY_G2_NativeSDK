
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

using Quaternion = UnityEngine.Quaternion;

namespace Singray.Foundation
{
    /// <summary>
    /// Provides camera image data and camera start/stop controls
    /// ToF camera
    /// AR glasses camera
    /// Compute unit camera
    /// Left and right fisheye cameras
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XvCameraManager : MonoBehaviour
    {

        private XvCameraManager() { }


        public XvARCameraParameter XvARCameraParameter = new XvARCameraParameter()
        {
            rgbResolution = RgbResolution.RGB_1280x720,
            exposureMode=ExposureMode.AutoExposure,
            exposureGain=255,
            exposureTimeMs=8,
            fps = 30,

        };
        public XvWebCameraParameter XvWebCameraParameter = new XvWebCameraParameter()
        {
            width = 1280,
            height = 720,
            fps = 30,
        };

        public XvTofCameraParameter XvTofCameraParameter = new XvTofCameraParameter()
        {
            streamType = TofStreamType.DeapthStream,
            tofFramerate = TofFramerate.FPS_30,
            sonyTofLibMode = SonyTofLibMode.IQMIX_SF,
            tofResolution = TofResolution.QVGA,
            enableGamma = false,

        };





        /// <summary>
        /// Width and height
        /// </summary>
        private int requestedWidth = 1920;
        private int requestedHeight = 1080;

        public int Width
        {
            get
            {
                switch (XvARCameraParameter.rgbResolution)
                {
                    case RgbResolution.RGB_1920x1080:
                        requestedWidth = 1920;
                        requestedHeight = 1080;

                        break;
                    case RgbResolution.RGB_1280x720:
                        requestedWidth = 1280;
                        requestedHeight = 720;

                        break;
                    case RgbResolution.RGB_640x480:
                        requestedWidth = 640;
                        requestedHeight = 480;
                        break;
                    case RgbResolution.RGB_320x240:
                        requestedWidth = 320;
                        requestedHeight = 240;

                        break;
                    case RgbResolution.RGB_2560x1920:
                        requestedWidth = 2560;
                        requestedHeight = 1920;

                        break;
                    case RgbResolution.RGB_3840x2160:
                        requestedWidth = 3840;
                        requestedHeight = 2160;
                        break;
                    default:
                        break;
                }

                return requestedWidth;

            }


        }
        public int Height
        {
            get
            {
                switch (XvARCameraParameter.rgbResolution)
                {
                    case RgbResolution.RGB_1920x1080:
                        requestedWidth = 1920;
                        requestedHeight = 1080;

                        break;
                    case RgbResolution.RGB_1280x720:
                        requestedWidth = 1280;
                        requestedHeight = 720;

                        break;
                    case RgbResolution.RGB_640x480:
                        requestedWidth = 640;
                        requestedHeight = 480;
                        break;
                    case RgbResolution.RGB_320x240:
                        requestedWidth = 320;
                        requestedHeight = 240;

                        break;
                    case RgbResolution.RGB_2560x1920:
                        requestedWidth = 2560;
                        requestedHeight = 1920;
                        break;
                    case RgbResolution.RGB_3840x2160:
                        requestedWidth = 3840;
                        requestedHeight = 2160;
                        break;
                    default:
                        break;
                }

                return requestedHeight;

            }


        }

        public int Fps
        {
            get { return XvARCameraParameter.fps; }

        }

        /// <summary>
        /// Camera frame callback
        /// </summary>
        public static UnityEvent<cameraData> onARCameraStreamFrameArrived = new UnityEvent<cameraData>();
        public static UnityEvent<cameraData> onLeftStereoStreamFrameArrived = new UnityEvent<cameraData>();
        public static UnityEvent<cameraData> onRightStereoStreamFrameArrived = new UnityEvent<cameraData>();
        public static UnityEvent<cameraData> onTofDepthCameraStreamFrameArrived = new UnityEvent<cameraData>();
        public static UnityEvent<cameraData> onTofIRCameraStreamFrameArrived = new UnityEvent<cameraData>();
        public static UnityEvent<cameraData> onWebCameraStreamFrameArrived = new UnityEvent<cameraData>();

        private readonly HashSet<XvCameraStreamType> pendingCaptures = new HashSet<XvCameraStreamType>();
        private float rgbCaptureRequestedAt = -1f;
        private float tofDepthCaptureRequestedAt = -1f;
        private float tofIrCaptureRequestedAt = -1f;
        private float lastRgbFrameAt = -1f;
        private float lastTofDepthFrameAt = -1f;
        private float lastTofIrFrameAt = -1f;

        private void Awake()
        {
            onARCameraStreamFrameArrived.RemoveListener(OnDiagnosticRgbFrame);
            onTofDepthCameraStreamFrameArrived.RemoveListener(OnDiagnosticTofDepthFrame);
            onTofIRCameraStreamFrameArrived.RemoveListener(OnDiagnosticTofIrFrame);
            onARCameraStreamFrameArrived.AddListener(OnDiagnosticRgbFrame);
            onTofDepthCameraStreamFrameArrived.AddListener(OnDiagnosticTofDepthFrame);
            onTofIRCameraStreamFrameArrived.AddListener(OnDiagnosticTofIrFrame);
        }

        /// <summary>
        /// Start camera
        /// </summary>
        /// <param name="cameraType"></param>
        public void StartCapture(XvCameraStreamType cameraType)
        {

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!API.xslam_ready())
            {
                pendingCaptures.Add(cameraType);
                MarkCaptureRequested(cameraType);
                MyDebugTool.LogDiagnosticWarning(
                    "SENSOR",
                    "capture-pending-" + cameraType,
                    $"capture={cameraType} queued because SDK is not ready",
                    2f);
                return;
            }
            pendingCaptures.Remove(cameraType);
#endif

            MarkCaptureRequested(cameraType);
            MyDebugTool.LogDiagnostic("SENSOR", "capture-start-" + cameraType, DescribeCapture(cameraType), 0f);
            switch (cameraType)
            {
                case XvCameraStreamType.WebCameraStream:


                    XvWebCameraManager.GetXvWebCameraManager().StartCapture(XvWebCameraParameter);
                    break;
                case XvCameraStreamType.ARCameraStream:
                    XvARCameraManager.GetXvARCameraManager().StartCapture(XvARCameraParameter);
                    break;
                case XvCameraStreamType.TofDepthCameraStream:

                    XvTofCameraParameter.streamType = TofStreamType.DeapthStream;

                    XvTofManager.GetXvTofManager().StartCapture(XvTofCameraParameter);
                    break;
                case XvCameraStreamType.TofIRCameraStream:

                    XvTofCameraParameter.streamType = TofStreamType.IRStream;

                    XvTofManager.GetXvTofManager().StartCapture(XvTofCameraParameter);
                    break;
                case XvCameraStreamType.LeftStereoCameraStream:

                    XvStereoCameraParameter XvLeftStereoCameraParameter = new XvStereoCameraParameter()
                    {
                        cameraIndex = StereoCameraIndex.LeftEye
                    };


                    XvStereoCameraManager.GetXvStereoCameraManager(true).StartCapture(XvLeftStereoCameraParameter);
                    break;
                case XvCameraStreamType.RightStereoCameraStream:
                    XvStereoCameraParameter XvRightStereoCameraParameter = new XvStereoCameraParameter()
                    {
                        cameraIndex = StereoCameraIndex.RightEye
                    };

                    XvStereoCameraManager.GetXvStereoCameraManager(false).StartCapture(XvRightStereoCameraParameter);

                    break;
                default:
                    break;
            }

        }
        /// <summary>
        /// Stop camera
        /// </summary>
        /// <param name="cameraType"></param>
        public void StopCapture(XvCameraStreamType cameraType)
        {
            pendingCaptures.Remove(cameraType);
            MyDebugTool.LogDiagnostic("SENSOR", "capture-stop-" + cameraType, $"capture={cameraType} stop requested", 0f);
            switch (cameraType)
            {
                case XvCameraStreamType.WebCameraStream:
                    XvWebCameraManager.GetXvWebCameraManager().StopCapture();

                    break;
                case XvCameraStreamType.ARCameraStream:
                    XvARCameraManager.GetXvARCameraManager().StopCapture();
                    break;
                case XvCameraStreamType.TofDepthCameraStream:
                    XvTofManager.GetXvTofManager().StopCapture(TofStreamType.DeapthStream);
                    break;

                case XvCameraStreamType.TofIRCameraStream:
                    XvTofManager.GetXvTofManager().StopCapture(TofStreamType.IRStream);
                    break;

                case XvCameraStreamType.LeftStereoCameraStream:
                    XvStereoCameraManager.GetXvStereoCameraManager(true).StopCapture();

                    break;
                case XvCameraStreamType.RightStereoCameraStream:
                    XvStereoCameraManager.GetXvStereoCameraManager(false).StopCapture();
                    break;
                default:
                    break;
            }
        }

        public bool IsOn(XvCameraStreamType cameraType)
        {
            switch (cameraType)
            {
                case XvCameraStreamType.WebCameraStream:
                    return XvWebCameraManager.GetXvWebCameraManager().IsOn;

                case XvCameraStreamType.ARCameraStream:
                    return XvARCameraManager.GetXvARCameraManager().IsOn;

                case XvCameraStreamType.TofDepthCameraStream:

                    return XvTofManager.GetXvTofManager().IsOn(TofStreamType.DeapthStream);

                case XvCameraStreamType.TofIRCameraStream:

                    return XvTofManager.GetXvTofManager().IsOn(TofStreamType.IRStream);

                case XvCameraStreamType.LeftStereoCameraStream:
                    return XvStereoCameraManager.GetXvStereoCameraManager(true).IsOn;


                case XvCameraStreamType.RightStereoCameraStream:
                    return XvStereoCameraManager.GetXvStereoCameraManager(false).IsOn;

                default:
                    break;
            }

            return false;
        }



        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (pendingCaptures.Count > 0 && API.xslam_ready())
            {
                List<XvCameraStreamType> capturesToStart = new List<XvCameraStreamType>(pendingCaptures);
                pendingCaptures.Clear();
                for (int i = 0; i < capturesToStart.Count; ++i)
                {
                    StartCapture(capturesToStart[i]);
                }
            }
#endif

            if (XvStereoCameraManager.GetXvStereoCameraManager(true).IsOn)
            {
                XvStereoCameraManager.GetXvStereoCameraManager(true).Update();
            }

            if (XvStereoCameraManager.GetXvStereoCameraManager(false).IsOn)
            {
                XvStereoCameraManager.GetXvStereoCameraManager(false).Update();
            }
            XvTofManager.GetXvTofManager().Update();

            if (XvARCameraManager.GetXvARCameraManager().IsOn)
            {
                XvARCameraManager.GetXvARCameraManager().Update();
            }

            if (XvWebCameraManager.GetXvWebCameraManager().IsOn)
            {
                XvWebCameraManager.GetXvWebCameraManager().Update();
            }
            ReportCaptureHealth();
        }

        private void OnDestroy()
        {
            StopTofPointCloud();
            onARCameraStreamFrameArrived.RemoveListener(OnDiagnosticRgbFrame);
            onTofDepthCameraStreamFrameArrived.RemoveListener(OnDiagnosticTofDepthFrame);
            onTofIRCameraStreamFrameArrived.RemoveListener(OnDiagnosticTofIrFrame);

            StopCapture(XvCameraStreamType.WebCameraStream);
            StopCapture(XvCameraStreamType.ARCameraStream);
            StopCapture(XvCameraStreamType.TofDepthCameraStream);
            StopCapture(XvCameraStreamType.TofIRCameraStream);
            StopCapture(XvCameraStreamType.LeftStereoCameraStream);
            StopCapture(XvCameraStreamType.RightStereoCameraStream);

        }

        private void MarkCaptureRequested(XvCameraStreamType cameraType)
        {
            float now = Time.realtimeSinceStartup;
            switch (cameraType)
            {
                case XvCameraStreamType.ARCameraStream:
                    rgbCaptureRequestedAt = now;
                    break;
                case XvCameraStreamType.TofDepthCameraStream:
                    tofDepthCaptureRequestedAt = now;
                    break;
                case XvCameraStreamType.TofIRCameraStream:
                    tofIrCaptureRequestedAt = now;
                    break;
            }
        }

        private string DescribeCapture(XvCameraStreamType cameraType)
        {
            switch (cameraType)
            {
                case XvCameraStreamType.ARCameraStream:
                    return $"capture=RGB resolution={XvARCameraParameter.rgbResolution} fps={XvARCameraParameter.fps} " +
                           $"exposureMode={XvARCameraParameter.exposureMode} exposureGain={XvARCameraParameter.exposureGain} " +
                           $"exposureTimeMs={XvARCameraParameter.exposureTimeMs}";
                case XvCameraStreamType.TofDepthCameraStream:
                    return $"capture=TOF_DEPTH streamMode={XvTofCameraParameter.tofStreamMode} " +
                           $"libMode={XvTofCameraParameter.sonyTofLibMode} resolution={XvTofCameraParameter.tofResolution} " +
                           $"fps={XvTofCameraParameter.tofFramerate}";
                case XvCameraStreamType.TofIRCameraStream:
                    return $"capture=TOF_IR streamMode={XvTofCameraParameter.tofStreamMode} " +
                           $"libMode={XvTofCameraParameter.sonyTofLibMode} resolution={XvTofCameraParameter.tofResolution} " +
                           $"fps={XvTofCameraParameter.tofFramerate} gamma={XvTofCameraParameter.enableGamma}";
                default:
                    return $"capture={cameraType}";
            }
        }

        private void OnDiagnosticRgbFrame(cameraData data)
        {
            lastRgbFrameAt = Time.realtimeSinceStartup;
            if (data != null)
            {
                MyDebugTool.LogDiagnostic(
                    "RGB",
                    "frame",
                    $"frame={data.texWidth}x{data.texHeight} timestamp={data.parameter.timeStamp:F6} " +
                    $"position={data.parameter.rgb_position} rotation={data.parameter.rgb_rotation}",
                    2f);
            }
        }

        private void OnDiagnosticTofDepthFrame(cameraData data)
        {
            lastTofDepthFrameAt = Time.realtimeSinceStartup;
            if (data != null)
            {
                MyDebugTool.LogDiagnostic(
                    "TOF",
                    "depth-frame",
                    $"stream=depth frame={data.texWidth}x{data.texHeight} timestamp={data.parameter.timeStamp:F6}",
                    2f);
            }
        }

        private void OnDiagnosticTofIrFrame(cameraData data)
        {
            lastTofIrFrameAt = Time.realtimeSinceStartup;
            if (data != null)
            {
                MyDebugTool.LogDiagnostic(
                    "TOF",
                    "ir-frame",
                    $"stream=ir frame={data.texWidth}x{data.texHeight} timestamp={data.parameter.timeStamp:F6}",
                    2f);
            }
        }

        private void ReportCaptureHealth()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            float now = Time.realtimeSinceStartup;
            ReportMissingFrame("RGB", XvCameraStreamType.ARCameraStream, rgbCaptureRequestedAt, lastRgbFrameAt, now);
            ReportMissingFrame("TOF", XvCameraStreamType.TofDepthCameraStream, tofDepthCaptureRequestedAt, lastTofDepthFrameAt, now);
            ReportMissingFrame("TOF", XvCameraStreamType.TofIRCameraStream, tofIrCaptureRequestedAt, lastTofIrFrameAt, now);
#endif
        }

        private void ReportMissingFrame(
            string category,
            XvCameraStreamType cameraType,
            float requestedAt,
            float lastFrameAt,
            float now)
        {
            if (requestedAt < 0f || !IsOn(cameraType))
            {
                return;
            }

            float referenceTime = lastFrameAt >= requestedAt ? lastFrameAt : requestedAt;
            if (now - referenceTime >= 5f)
            {
                MyDebugTool.LogDiagnosticWarning(
                    category,
                    "frame-timeout-" + cameraType,
                    $"capture={cameraType} has produced no frame for {now - referenceTime:F1}s; sdkReady={API.xslam_ready()}",
                    5f);
            }
        }

        private void OnApplicationQuit()
        {
            StopCapture(XvCameraStreamType.WebCameraStream);
            StopCapture(XvCameraStreamType.ARCameraStream);
            StopCapture(XvCameraStreamType.TofDepthCameraStream);
            StopCapture(XvCameraStreamType.TofIRCameraStream);
            StopCapture(XvCameraStreamType.LeftStereoCameraStream);
            StopCapture(XvCameraStreamType.RightStereoCameraStream);
        }



        #region ToF camera
        private int width;
        private int height;
        private bool isGetTofData;
        private Vector3[] vecGroup;
        private bool startPointCloud;
        private Coroutine pointCloudStartRoutine;

        /// <summary>
        /// Set ToF exposure time
        /// </summary>
        /// <param name="exposureTimeMs">0~255</param>
        public void SetTofExposure(int exposureTimeMs)
        {
            if (exposureTimeMs >= 0 && exposureTimeMs <= 255)
            {
                // Set ToF exposure parameters; the final argument is the exposure value
                byte[] hid = new byte[] { 0x02, 0xae, 0xF5, 0x02, Convert.ToByte(exposureTimeMs) };
                byte[] response = API.HidWriteAndRead(hid, hid.Length);
                MyDebugTool.LogDiagnostic(
                    "TOF",
                    "exposure-hid",
                    $"exposure={exposureTimeMs} responseBytes={(response == null ? 0 : response.Length)}",
                    0f);
            }
            else {
                MyDebugTool.LogError("Exception:SetTofExposure()"+ exposureTimeMs);
            }
            
        }



        /// <summary>
        /// Start ToF point-cloud acquisition
        /// </summary>
        public void StartTofPointCloud()
        {
            if (startPointCloud) return;
            startPointCloud = true;
#if !UNITY_ANDROID || UNITY_EDITOR
            MyDebugTool.LogDiagnosticWarning("TOF", "point-cloud-editor", "point cloud requires the Android SDK and a connected device", 0f);
            return;
#else
            pointCloudStartRoutine = StartCoroutine(StartTofPointCloudRoutine());
#endif
        }

        private IEnumerator StartTofPointCloudRoutine()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!API.xslam_ready())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    startPointCloud = false;
                    pointCloudStartRoutine = null;
                    Debug.LogWarning("ToF point cloud: SDK readiness timeout.");
                    yield break;
                }
                yield return null;
            }

            if (IsOn(XvCameraStreamType.TofDepthCameraStream))
            {
                StopCapture(XvCameraStreamType.TofDepthCameraStream);
            }

            if (!startPointCloud) yield break;
            pointCloudStartRoutine = null;
            // These values are the known working point-cloud configuration used
            // by the 4.1.1 branch. DepthOnly/IQMIX_SF does not produce cloud data.
            XvTofCameraParameter.tofStreamMode = TofStreamMode.CloudOnLeftHandSlam;
            XvTofCameraParameter.sonyTofLibMode = SonyTofLibMode.M2MIX_DF;
            XvTofManager.GetXvTofManager().SetTofStreamMode((int)XvTofCameraParameter.tofStreamMode);
            XvTofManager.GetXvTofManager().StartTofStream(XvTofCameraParameter);
            MyDebugTool.LogDiagnostic(
                "TOF",
                "point-cloud-start",
                $"streamMode={XvTofCameraParameter.tofStreamMode} libMode={XvTofCameraParameter.sonyTofLibMode} " +
                $"resolution={XvTofCameraParameter.tofResolution} fps={XvTofCameraParameter.tofFramerate}",
                0f);
        }

        /// <summary>
        /// Get point-cloud data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool GetPointCloudData(out Vector3[] data)
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            data = null;
            return false;
#endif
            if (!startPointCloud)
            {
                data = null;
                return false;
            }
            if (!API.xslam_ready())
            {
                MyDebugTool.LogDiagnosticWarning("TOF", "point-cloud-not-ready", "point cloud requested while SDK is not ready", 2f);
                data = null;
                return false;
            }
            if (!isGetTofData)
            {
                width = API.xslam_get_tof_width();
                height = API.xslam_get_tof_height();
                MyDebugTool.LogDiagnostic("TOF", "point-cloud-size", $"reportedSize={width}x{height}", 2f);
                if (width > 0 && height > 0)
                {
                    vecGroup = new Vector3[width * height];
                    isGetTofData = true;
                }
            }

            if (isGetTofData && vecGroup.Length > 0)
            {
                bool b = API.xslam_get_cloud_data_ex(vecGroup);
                data = vecGroup;
                if (b)
                {
                    MyDebugTool.LogDiagnostic("TOF", "point-cloud-frame", $"points={vecGroup.Length} size={width}x{height}", 2f);
                }
                else
                {
                    MyDebugTool.LogDiagnosticWarning("TOF", "point-cloud-empty", "xslam_get_cloud_data_ex returned no new cloud frame", 2f);
                }

                return b;
            }
            data = null;

            return false;
        }

       /// <summary>
       /// Stop point-cloud acquisition
       /// </summary>
        public void StopTofPointCloud()
        {
            if (pointCloudStartRoutine != null) StopCoroutine(pointCloudStartRoutine);
            pointCloudStartRoutine = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (startPointCloud && API.xslam_ready()) XvTofManager.GetXvTofManager().StopTofStream();
#endif
            startPointCloud = false;
            isGetTofData = false;
            vecGroup = null;
            width = 0;
            height = 0;
            MyDebugTool.LogDiagnostic("TOF", "point-cloud-stop", "point cloud stopped and buffers released", 0f);

        }


        /// <summary>
        /// Set ToF parameters
        /// </summary>
        /// <param name="libmode"></param>
        /// <param name="resulution">Resolution</param>
        /// <param name="fps">Frame rate</param>
        /// <param name="exposureTimeMs">Exposure duration</param>
        public void SetTofExposure(int libmode, int resulution, int fps, float exposureTimeMs)
        {
            XvTofManager.GetXvTofManager().StopTofStream();

            bool v1 = API.xslam_start_sony_tof_stream(libmode, resulution, fps);
            bool v2 = API.xslam_tof_set_exposure(1, 0, exposureTimeMs);
            MyDebugTool.LogDiagnostic(
                "TOF",
                "exposure",
                $"libMode={libmode} resolution={resulution} fps={fps} exposureTimeMs={exposureTimeMs} " +
                $"streamStartResult={v1} exposureResult={v2}",
                0f);
        }

        #endregion


    }

    public enum XvCameraStreamType
    {

        WebCameraStream,//Compute unit rear camera
        ARCameraStream,//MR glasses RGB camera
        TofDepthCameraStream,//ToF depth camera
        TofIRCameraStream,//ToF IR camera

        LeftStereoCameraStream,//Left fisheye camera
        RightStereoCameraStream,//Right fisheye camera
    }

    public class cameraData
    {
        public int texWidth;
        public int texHeight;
        public Texture tex;

        //Camera pose
        public CameraParameter parameter;
    }



    public struct CameraParameter
    {
        //AR camera pose
        public Vector3 position;
        public Quaternion rotation;

        public Vector3 rgb_position;
        public Quaternion rgb_rotation;

        public Vector3 rgb_extrinsic_pos;
        public Quaternion rgb_extrinsic_rot;

        //Timestamp
        public double timeStamp;

        //Camera intrinsics
        public float focal;
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public float k1;
        public float k2;
        public float p1;
        public float p2;
        public float k3;



        //Texture width and height
        public float width;
        public float height;
    }

    // RGB_1920x1080 = 0, ///< RGB 1080p
    // RGB_1280x720  = 1, ///< RGB 720p
    // RGB_640x480   = 2, ///< RGB 480p
    // RGB_320x240   = 3, ///< RGB QVGA
    // RGB_2560x1920 = 4, ///< RGB 5m
    public enum RgbResolution
    {
        RGB_1920x1080 = 0,
        RGB_1280x720 = 1,
        RGB_640x480 = 2,
        RGB_320x240 = 3,
        RGB_2560x1920 = 4,
        RGB_3840x2160 = 5

    }
}
