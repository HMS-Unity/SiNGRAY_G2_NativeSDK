using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Singray.Engine;
using static API;
using Quaternion = UnityEngine.Quaternion;

namespace Singray.Foundation
{
    public enum ExposureMode
    { 
      AutoExposure=0,
      ManualExposure=1,
    }

    [Serializable]
    public class XvARCameraParameter: XvCameraParameterSetting {
        public RgbResolution rgbResolution;
        public ExposureMode exposureMode;//0:auto exposure 1:manual exposure
        [Range(100,1550)]
        public int exposureGain=1500;//exposureGain Only valid in manual exposure mode, [100,1550]
        public float exposureTimeMs=30;//exposureTimeMs Only valid in manual exposure mode    milliseconds

        public int fps;
    }
    public  class XvARCamera : XvCameraBase
    {
        public XvARCamera(XvARCameraParameter cameraParameter, FrameArrived frameArrived) : base(cameraParameter, frameArrived)
        {
            this.cameraParameter = cameraParameter;
        }
        
        private Texture2D tex = null;
       
        private byte[] pixelBytes;

        private double rgbTimestamp = 0;
        private int lastWidth = 0;
        private int lastHeight = 0;
        private int countTime = 0;

        private XvARCameraParameter cameraParameter;


        public bool needOpenCamera;

        int count = 0;

        public override void StartCapture()
        {

            MyDebugTool.Log("Create RGB texture rgbResolution：" + cameraParameter.rgbResolution);

            switch (cameraParameter.rgbResolution)
            {
                case RgbResolution.RGB_1920x1080:
                    API.xslam_set_rgb_resolution(0);
                    MyDebugTool.Log("Create RGB texture set_rgb_resolution：0");

                    break;
                case RgbResolution.RGB_1280x720:
                    API.xslam_set_rgb_resolution(1);
                    MyDebugTool.Log("Create RGB texture set_rgb_resolution：1");

                    break;
                case RgbResolution.RGB_640x480:
                    API.xslam_set_rgb_resolution(2);
                    MyDebugTool.Log("Create RGB texture set_rgb_resolution：2");

                    break;
                case RgbResolution.RGB_320x240:
                    API.xslam_set_rgb_resolution(3);
                    MyDebugTool.Log("Create RGB texture set_rgb_resolution：3");

                    break;
                case RgbResolution.RGB_2560x1920:
                    API.xslam_set_rgb_resolution(4);
                    MyDebugTool.Log("Create RGB texture set_rgb_resolution：4");


                    break;
                case RgbResolution.RGB_3840x2160:
                    API.xslam_set_rgb_resolution(5);
                    MyDebugTool.Log("Create RGB texture set_rgb_resolution：5");

                    break;
                default:
                    break;
            }

          

            needOpenCamera = true;

            //API.xslam_rgb_set_exposure(1, 0, 5);

        }
        public override void StopCapture()
        {

            if (IsOpen)
            {
                needOpenCamera = false;

                bool stopped = API.xslam_stop_rgb_stream();
                MyDebugTool.LogDiagnostic("RGB", "stop", $"stopResult={stopped}", 0f);

                isOpen = false;

               
            }
            isOpen = false;


        }
        public override void Update()
        {

#if UNITY_EDITOR
            return;
#endif
            if (API.xslam_ready())
            {
                count++;
                if (count < 100)
                {
                    return;
                }


                if (needOpenCamera)
                {
                    if (!isOpen)
                    {
                        //// Stop streams due to firmware not stable
                        MyDebugTool.Log("XvisioDeviceManager stop streams");
                        bool preStartStopResult = API.xslam_stop_rgb_stream();

                        //if (!API.xslam_set_rgb_source(1))
                        //{
                        //    MyDebugTool.Log("XvisioDeviceManager set rgb source faild");
                        //}
                        //else
                        //{
                        //    MyDebugTool.Log("XvisioDeviceManager set rgb source success");
                        //}

                        //if (!API.xslam_set_rgb_resolution(0))
                        //{
                        //    MyDebugTool.Log("XvisioDeviceManager set rgb resolustion faild");
                        //}
                        //else
                        //{
                        //    MyDebugTool.Log("XvisioDeviceManager set rgb resolustion success");
                        //}
                        //// Start image streams
                        MyDebugTool.Log("XvisioDeviceManager start xslam_start_rgb_stream");
                        bool started = API.xslam_start_rgb_stream();
                        MyDebugTool.LogDiagnostic(
                            "RGB",
                            "native-start",
                            $"preStartStopResult={preStartStopResult} startResult={started}",
                            0f);

                        isOpen = started;
                    }
                }

            }
            else
            {
                if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
                {
                    API.xslam_init();

                }
            }

            if (isOpen)
            {
                if (API.xslam_ready())
                {

                    if (!readRgbCalibrationFlag)
                    {
                        ReadRgbCalibration();
                    }

                    int width = API.xslam_get_rgb_width();
                    int height = API.xslam_get_rgb_height();

                    if (width > 0 && height > 0)
                    {

                        if (lastWidth != width || lastHeight != height)
                        {
                            try
                            {
                                double r = 1.0;
                                /*if (width < 1280 && height < 720) {
                                    r = 1.0;
                                }*/
                                int w = (int)(width * r);
                                int h = (int)(height * r);
                                MyDebugTool.Log("Create RGB texture " + w + "x" + h);
                                TextureFormat format = TextureFormat.RGBA32;
                                tex = new Texture2D(w, h, format, false);


                                cameraData.texWidth = w;
                                cameraData.texHeight = h;


                                //tex.filterMode = FilterMode.Point;
                                tex.Apply();


                                pixelBytes = new byte[w * h * 4];

                            }
                            catch (Exception e)
                            {

                                return;
                            }

                            lastWidth = width;
                            lastHeight = height;
                        }

                        countTime++;

                        if (countTime == 2)
                        {
                            countTime = 0;
                            return;
                        }
                        try
                        {
                            if (API.xslam_get_rgb_image_RGBA_Byte(pixelBytes, tex.width, tex.height, ref rgbTimestamp))
                            {
                                tex.SetPixelData(pixelBytes, 0, 0);
                                tex.Apply();

                                cameraData.tex = tex;

                                cameraData.parameter.timeStamp = rgbTimestamp;


                                if (rgbTimestamp > 0)
                                {
                                    if (API.xslam_get_pose_at(_poseData, rgbTimestamp))
                                    {
                                        //cameraData.parameter.rotation = new Quaternion(-(float)_poseData[0], (float)_poseData[1], -(float)_poseData[2], (float)_poseData[3])*offsetRotation;
                                        //cameraData.parameter.position = new Vector3((float)_poseData[4], -(float)_poseData[5], (float)_poseData[6])+ offsetPosition;

                                        cameraData.parameter.rgb_rotation = new Quaternion(-(float)_poseData[0], (float)_poseData[1], -(float)_poseData[2], (float)_poseData[3]);
                                        cameraData.parameter.rgb_position = new Vector3((float)_poseData[4], -(float)_poseData[5], (float)_poseData[6]);

                                        // Preserve the 4.1.1 public fields for existing clients while
                                        // also exposing the new raw RGB pose fields.
                                        cameraData.parameter.rotation = cameraData.parameter.rgb_rotation * offsetRotation;
                                        cameraData.parameter.position = cameraData.parameter.rgb_position + offsetPosition;

                                        if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_ANDROID && XvXRManager.SDK.IsUseUserPose)
                                        {
                                            Quaternion quaternion = cameraData.parameter.rotation;
                                            ((XvXRAndroidDevice)XvXRManager.SDK.GetDevice()).SetSrcQuaternionUnity(quaternion, (long)rgbTimestamp * 1000000);
                                            //Debug.Log("IsUseUserPose");
                                        }
                                    }
                                    else
                                    {
                                        MyDebugTool.Log("RGBRecord xslam_get_pose_at faild");
                                    }
                                }



                                frameArrived?.Invoke(cameraData);

                            }
                            else
                            {
                                MyDebugTool.Log("Invalid texture");
                            }
                        }
                        catch (Exception e)
                        {
                            MyDebugTool.LogError(e);
                            return;
                        }
                        // MyDebugTool.Log("====================================");
                    }
                }
            }
        }

        private bool readRgbCalibrationFlag = false;
        private double[] _R;
        private double[] _T;
        //Left-eye Euler angles
        private double[] _EulerAngles;

        private double[] _poseData = new double[7];

        private Vector3 offsetPosition;
        private Quaternion offsetRotation;

        void ReadRgbCalibration()
        {
            API.rgb_calibration rgb_Calibration = default(API.rgb_calibration);
            if (API.readRGBCalibration(ref rgb_Calibration))
            {
                float near = 0.3f;
                float far = 1000f;
                MyDebugTool.Log("readRGBCalibration");
                pdm pdm = rgb_Calibration.intrinsic720;



                switch (cameraParameter.rgbResolution)
                {
                    case RgbResolution.RGB_1920x1080:
                        pdm = rgb_Calibration.intrinsic1080;

                        break;
                    case RgbResolution.RGB_1280x720:
                        pdm = rgb_Calibration.intrinsic720;

                        break;
                    case RgbResolution.RGB_640x480:
                        pdm = rgb_Calibration.intrinsic480;

                        break;
                    case RgbResolution.RGB_320x240:
                        break;
                    case RgbResolution.RGB_2560x1920:
                        break;
                    default:
                        break;
                }


                //intrinsic720
                //Matrix4x4 proj = Singray.Engine.XvXRBaseDevice.PerspectiveOffCenter((float)pdm.K[0], (float)pdm.K[1],
                //     (float)pdm.K[2], (float)pdm.K[3], (float)pdm.K[9], (float)pdm.K[10], near, far);

                //camera.fieldOfView = 2 * Mathf.Atan(1 / proj[1, 1]) * Mathf.Rad2Deg;
                //camera.projectionMatrix = proj;

                _T = new double[3] { rgb_Calibration.extrinsic.translation[0], -rgb_Calibration.extrinsic.translation[1], rgb_Calibration.extrinsic.translation[2] };

                //Convert the left-eye calibration rotation matrix to Euler angles
                _R = new double[9] { rgb_Calibration.extrinsic.rotation[0], -rgb_Calibration.extrinsic.rotation[1], rgb_Calibration.extrinsic.rotation[2], -rgb_Calibration.extrinsic.rotation[3], rgb_Calibration.extrinsic.rotation[4],
                            -rgb_Calibration.extrinsic.rotation[5],rgb_Calibration.extrinsic.rotation[6],-rgb_Calibration.extrinsic.rotation[7],rgb_Calibration.extrinsic.rotation[8]};
                Singray.Engine.XvXREye.RotationMatrixToEulerAngles(ref _EulerAngles, _R);



                offsetPosition = new Vector3((float)_T[0], (float)_T[1], (float)_T[2]);
                Vector3 localEuler = new Vector3((float)_EulerAngles[0], (float)_EulerAngles[1], (float)_EulerAngles[2]);

                offsetRotation = Quaternion.Euler(localEuler);

                //Configure the physical camera
                cameraData.parameter.focal = 3.519f;//RGB camera focal length in millimeters
                cameraData.parameter.fx = (float)pdm.K[0];
                cameraData.parameter.fy = (float)pdm.K[1];
                cameraData.parameter.cx = (float)pdm.K[2];
                cameraData.parameter.cy = (float)pdm.K[3];
                cameraData.parameter.k1 = (float)pdm.K[4];
                cameraData.parameter.k2 = (float)pdm.K[5];
                cameraData.parameter.p1 = (float)pdm.K[6];
                cameraData.parameter.p2 = (float)pdm.K[7];
                cameraData.parameter.k3 = (float)pdm.K[8];
                cameraData.parameter.width = (float)pdm.K[9];
                cameraData.parameter.height = (float)pdm.K[10];

                cameraData.parameter.rgb_extrinsic_pos = offsetPosition;
                cameraData.parameter.rgb_extrinsic_rot = offsetRotation;

                MyDebugTool.Log("RGBRecord ReadRgbCalibration:" + rgb_Calibration);

                MyDebugTool.Log("RGBRecord ReadRgbCalibration camera Fx,Fy: " +
                    pdm.K[0] + "," + pdm.K[1]);

                readRgbCalibrationFlag = true;



            }
            else
            {
                MyDebugTool.Log("RGBRecord readStereoFisheyesCalibration faild");
            }
        }
    }
}
