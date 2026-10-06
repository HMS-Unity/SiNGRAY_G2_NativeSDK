using AOT;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Singray.Engine;
namespace Singray.Foundation
{
    using static XvEyeTracking;

    /// <summary>
    /// Controls eye tracking and retrieves eye-tracking data
    /// </summary>

    public sealed class XvEyeTrackingManager : MonoBehaviour
    {
        const string TAG = "XvEyeTrackingManager";
        private XvEyeTrackingManager() { }
        private static string config_path = "/data/misc/xr/";
      

        //Glasses head 6DoF matrix
        Matrix4x4 MatrixHead = Matrix4x4.identity;
        //Binocular-center pose matrix relative to the IMU
        Matrix4x4 MatrixMiddleOfEyes = Matrix4x4.identity;
        //Binocular-center transform in world coordinates
        private Matrix4x4 middleOfEyeToHeadMatrix;

        public Matrix4x4 MiddleOfEyeToHeadMatrix
        {
            get { return middleOfEyeToHeadMatrix; }
        }

        /// <summary>
        /// Whether eye tracking is active
        /// </summary>
        private static bool tracking;
        public bool Tracking
        {
            get
            {
                return tracking;
            }
        }

        public XV_ET_EYE_DATA_EX EyeData
        {
            get
            {
                return eyeData;
            }
        }

        /// <summary>
        /// Binocular origin
        /// </summary>
        public Vector3 GazeOrigin
        {
            get
            {

                return GetGazePoint(eyeData.recomGaze.gazeOrigin);
            }
        }

        /// <summary>
        /// Binocular gaze direction
        /// </summary>

        public Vector3 GazeDirection
        {
            get
            {
                return GetDirection(eyeData.recomGaze.gazeOrigin, eyeData.recomGaze.gazeDirection);
            }
        }



        /// <summary>
        /// Left-eye gaze origin
        /// </summary>
        public Vector3 LeftGazeOrigin
        {
            get
            {
                return GetGazePoint(eyeData.leftGaze.gazeOrigin);
            }
        }

        /// <summary>
        /// Left-eye gaze direction
        /// </summary>
        public Vector3 LeftGazeDirection
        {
            get
            {
                return GetDirection(eyeData.leftGaze.gazeOrigin, eyeData.leftGaze.gazeDirection); ;
            }
        }


        //Right-eye origin
        public Vector3 RightGazeOrigin
        {
            get
            {
                return GetGazePoint(eyeData.rightGaze.gazeOrigin);

            }
        }

        /// <summary>
        /// Right-eye gaze direction
        /// </summary>
        public Vector3 RightGazeDirection
        {
            get
            {
                return GetDirection(eyeData.rightGaze.gazeOrigin, eyeData.rightGaze.gazeDirection); ;
            }
        }

        public float Ipd {
            get
            {
                return eyeData.ipd;
            }

        }

        private void OnEnable()
        {
            XvDeviceManager.OnBeforeQuit += StopGaze;
        }

        private void OnDisable()
        {
            XvDeviceManager.OnBeforeQuit -= StopGaze;
        }



        /// <summary>
        /// Start eye tracking
        /// </summary>
        public void StartGaze()
        {

#if UNITY_EDITOR
            return;
#endif
            if (!tracking)
            {
                 //Set display resolution
               //xslam_set_gaze_configs(1920, 1080);
                //Set the configuration file path
                xslam_gaze_set_config_path(config_path);
                MyDebugTool.Log($"{TAG} config_path:{config_path}");

                bool b_start_gaze = xslam_start_gaze();
                MyDebugTool.Log($"{TAG} b_start_gaze:{b_start_gaze}");

                string path = "/data/misc/xr/XVETcaliData_" + Singray.SystemEvents.AndroidConnection.getLoginUser() + ".dat";
                //string path = "/sdcard/XVETcaliData.dat";
                int apply = xslam_gaze_calibration_apply(path);
                MyDebugTool.Log($"{TAG} xslam_gaze_calibration_apply:{apply},path:{path}");



                StartCoroutine(InitializeGazeAfterDelay(b_start_gaze));
            }
        }

        private IEnumerator InitializeGazeAfterDelay(bool gazeStarted)
        {
            yield return new WaitForSeconds(1f);

            if (!gazeStarted)
            {
                MyDebugTool.Log($"{TAG} xslam_start_gaze failed!!!");
                yield break;
            }

            if (GetETparams())
            {
                bool exposureSet = xslam_set_exposure(
                    eyeExposureBright.exposure_LeftGain,
                    eyeExposureBright.exposure_LeftTimeMs,
                    eyeExposureBright.exposure_RightGain,
                    eyeExposureBright.exposure_RightTimeMs);
                MyDebugTool.Log($"{TAG} xslam_set_exposure:{exposureSet},exposure_LeftGain:{eyeExposureBright.exposure_LeftGain},exposure_LeftTimeMs:{eyeExposureBright.exposure_LeftTimeMs},exposure_RightGain:{eyeExposureBright.exposure_RightGain},exposure_RightTimeMs:{eyeExposureBright.exposure_RightTimeMs}");

                bool leftBrightnessSet = XvEyeTracking.xslam_set_bright(
                    0,
                    eyeExposureBright.eye_LeftLed,
                    eyeExposureBright.eye_LeftBrightness);
                MyDebugTool.Log($"{TAG} xslam_set_bright left:{leftBrightnessSet},eye_LeftLed:{eyeExposureBright.eye_LeftLed},eye_LeftBrightness:{eyeExposureBright.eye_LeftBrightness}");

                bool rightBrightnessSet = XvEyeTracking.xslam_set_bright(
                    1,
                    eyeExposureBright.eye_RightLed,
                    eyeExposureBright.eye_RightBrightness);
                MyDebugTool.Log($"{TAG} xslam_set_bright right:{rightBrightnessSet},eye_RightLed:{eyeExposureBright.eye_RightLed},eye_RightBrightness:{eyeExposureBright.eye_RightBrightness}");
            }
            else
            {
                bool exposureSet = xslam_set_exposure(12, 6, 12, 6);
                MyDebugTool.Log($"{TAG} b_set_exposure:{exposureSet}");
                bool brightnessSet = xslam_set_bright(2, 8, 27);
                MyDebugTool.Log($"{TAG} b_set_bright:{brightnessSet}");
            }

            int callbackResult = xslam_set_gaze_callback(OnStartSkeletonCallback);
            MyDebugTool.Log($"{TAG} b_set_gaze_callback:{callbackResult}");
        }



        //Define EyeExposureBright
        public struct EyeExposureBright
        {
            public int exposure_LeftGain;
            public int exposure_RightGain;
            public int exposure_LeftTimeMs;
            public int exposure_RightTimeMs;
            public int eye_Index;
            public int eye_LeftLed;
            public int eye_RightLed;
            public int eye_LeftBrightness;
            public int eye_RightBrightness;
        }
        EyeExposureBright eyeExposureBright = new EyeExposureBright();

        GazeParams gazeParams = new GazeParams();
        bool GetETparams()
        {
            try
            {
                if (XvEyeTracking.xslam_get_eyetracking_params(ref gazeParams))
                {


                    MyDebugTool.Log($"{TAG} leftGain:{gazeParams.leftGain}\nleftTime:{gazeParams.leftTime}\nleftLed:{gazeParams.leftLed}\nleftBrightness:{gazeParams.leftBrightness}\n" +
                        $"rightGain:{gazeParams.rightGain}\nrightTime:{gazeParams.rightTime}\nrightLed:{gazeParams.rightLed}\nrightBrightness:{gazeParams.rightBrightness}");

                    if (gazeParams.leftBrightness == 0 || gazeParams.rightBrightness == 0)
                    {
                        MyDebugTool.LogError($"{TAG} leftBrightness or rightBrightness is 0!");
                        return false;
                    }
                    else
                    {
                        eyeExposureBright.exposure_LeftGain = gazeParams.leftGain;
                        eyeExposureBright.exposure_RightGain = gazeParams.rightGain;
                        eyeExposureBright.exposure_LeftTimeMs = gazeParams.leftTime;
                        eyeExposureBright.exposure_RightTimeMs = gazeParams.rightTime;
                        eyeExposureBright.eye_Index = 2;
                        eyeExposureBright.eye_LeftLed = gazeParams.leftLed;
                        eyeExposureBright.eye_RightLed = gazeParams.rightLed;
                        eyeExposureBright.eye_LeftBrightness = gazeParams.leftBrightness;
                        eyeExposureBright.eye_RightBrightness = gazeParams.rightBrightness;

                        return true;
                    }




                }
                else
                {
                    MyDebugTool.LogError($"xslam_get_eyetracking_params false!!!");
                    return false;
                }

            }
            catch (Exception e)
            {
                MyDebugTool.LogError($"Glass Device version may not support eyetracking params!!!\n{e}");
                return false;
            }
        }

        /// <summary>
        /// Stop eye tracking
        /// </summary>
        public void StopGaze()
        {
#if UNITY_EDITOR
            return;
#endif

            if (Tracking)
            {


                bool b_set_bright = XvEyeTracking.xslam_set_bright(2, 8, 0);
                MyDebugTool.Log($"{TAG} xslam_set_bright:{b_set_bright}");
                bool unset = xslam_unset_gaze_callback();
                MyDebugTool.Log($"{TAG} xslam_unset_gaze_callback:{unset}");
                bool b = xslam_stop_gaze();
                MyDebugTool.Log($"{TAG} xslam_stop_gaze:{b}");

                tracking = false;
            }
        }

        public static XV_ET_EYE_DATA_EX eyeData = new XV_ET_EYE_DATA_EX();

        [MonoPInvokeCallback(typeof(fn_gaze_callback))]
        private static void OnStartSkeletonCallback(XV_ET_EYE_DATA_EX gazedata)
        {
            tracking = true;
            //MyDebugTool.Log($"OnStartSkeletonCallback");
            eyeData = gazedata;

            //MyDebugTool.Log($"{TAG} eyeData ipd:{gazedata.ipd}");
            //MyDebugTool.Log($"{TAG} eyeData leftEyeMove:{eyeData.leftEyeMove}");
            //MyDebugTool.Log($"{TAG} eyeData rightEyeMove:{eyeData.rightEyeMove}");
        }


       

        private void UpdateMatrix()
        {
            MatrixHead.SetTRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);

            API.stereo_pdm_calibration fed = XvXRManager.SDK.GetDevice().GetFed();

            Vector3 LeftdisplayPos = new Vector3((float)fed.calibrations[0].extrinsic.translation[0], -(float)fed.calibrations[0].extrinsic.translation[1], (float)fed.calibrations[0].extrinsic.translation[2]);
            Vector3 RightdisplayPos = new Vector3((float)fed.calibrations[1].extrinsic.translation[0], -(float)fed.calibrations[1].extrinsic.translation[1], (float)fed.calibrations[1].extrinsic.translation[2]);

            Vector3 normal = (RightdisplayPos - LeftdisplayPos).normalized;
            float distance = Vector3.Distance(LeftdisplayPos, RightdisplayPos);
            Vector3 middleOfEyes_pos = normal * (distance * 0.5f) + LeftdisplayPos;


            DebugLog($"middleOfEyes_pos:" + middleOfEyes_pos);

            Quaternion middleQua = RotationMatrixToQuaternion(fed.calibrations[0].extrinsic.rotation);

            MatrixMiddleOfEyes.SetTRS(middleOfEyes_pos, new Quaternion(-middleQua.x, middleQua.y, -middleQua.z, middleQua.w), Vector3.one);

            middleOfEyeToHeadMatrix = MatrixHead * MatrixMiddleOfEyes;
        }

        private Vector3 GetGazePoint(XV_ETPoint3D oGazeOrigin)
        {
            Vector3 gazeOrigin = new Vector3(oGazeOrigin.x, -oGazeOrigin.y, oGazeOrigin.z) / 1000;



            Matrix4x4 Matrix_gazeOrigin = Matrix4x4.identity;
            Matrix4x4 Matrix_XVgazeOrigin = Matrix4x4.identity;
            Matrix_gazeOrigin.SetTRS(gazeOrigin, Quaternion.identity, Vector3.one);
            Matrix_XVgazeOrigin = middleOfEyeToHeadMatrix * Matrix_gazeOrigin;

            ///Get position
            gazeOrigin = Matrix_XVgazeOrigin.GetColumn(3);

            return gazeOrigin;

        }
        private Vector3 GetDirection(XV_ETPoint3D oGazeOrigin, XV_ETPoint3D oGazeDirection)
        {
            Vector3 gazeOrigin = new Vector3(oGazeOrigin.x, -oGazeOrigin.y, oGazeOrigin.z) / 1000;
            Vector3 gazeDirection = gazeOrigin + new Vector3(oGazeDirection.x, -oGazeDirection.y, oGazeDirection.z) * 15;
            Matrix4x4 Matrix_target = Matrix4x4.identity;
            Matrix4x4 Matrix_XVtarget = Matrix4x4.identity;

            Matrix_target.SetTRS(gazeDirection, Quaternion.identity, Vector3.one);

            Matrix_XVtarget = middleOfEyeToHeadMatrix * Matrix_target;
            //Get the new direction vector
            gazeDirection = (Vector3)Matrix_XVtarget.GetColumn(3) - GetGazePoint(oGazeOrigin);

            return gazeDirection;

        }

      

        //Convert the rotation matrix to a quaternion
        private Quaternion RotationMatrixToQuaternion(double[] rm)
        {
            double w, x, y, z;
            //if (1 + rm[0] + rm[4] + rm[8]>0&& (Math.Sqrt(1 + rm[0] + rm[4] + rm[8]) / 2)!=0)
            {
                w = Math.Sqrt(1 + rm[0] + rm[4] + rm[8]) / 2;
                x = (rm[7] - rm[5]) / (4 * w);
                y = (rm[2] - rm[6]) / (4 * w);
                z = (rm[3] - rm[1]) / (4 * w);
            }
            Quaternion qua = new Quaternion((float)x, (float)y, (float)z, (float)w);
            return qua;
        }

        private void DebugLog(object msg)
        {
             MyDebugTool.Log(msg);
        }



        #region Eye-tracking calibration

        /// <summary>
        /// Enter calibration mode
        /// </summary>
        public void GazeCalibrationEnter()
        {
            XvEyeTracking.xslam_gaze_calibration_enter();
        }

        /// <summary>
        /// Collect a calibration point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GazeCalibrationCollect(Vector3 point, int index)
        {
            return XvEyeTracking.xslam_gaze_calibration_collect(point.x, point.y, point.z, index);
        }

        /// <summary>
        /// Reset calibration data
        /// </summary>
        /// <returns></returns>
        public bool UnsetGazeCallback()
        {
            return XvEyeTracking.xslam_unset_gaze_callback();
        }
        /// <summary>
        /// Calibration completed
        /// </summary>
        public int CalibrationComplete()
        {
            int setup = xslam_gaze_calibration_setup();
            MyDebugTool.Log($" xslam_gaze_calibration_setup:{setup}");

            int compute_apply = xslam_gaze_calibration_compute_apply();
            MyDebugTool.Log($" xslam_gaze_calibration_compute_apply:{compute_apply}");

            int leave = xslam_gaze_calibration_leave();
            MyDebugTool.Log($" xslam_gaze_calibration_leave:{leave}");

            string path = "/data/misc/xr/XVETcaliData_" + Singray.SystemEvents.AndroidConnection.getLoginUser() + ".dat";//Output path for the completed eye-tracking calibration
                                                                                                                      //string path = "/sdcard/XVETcaliData.dat";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            int retrieve = xslam_gaze_calibration_retrieve(path);
            MyDebugTool.Log($" xslam_gaze_calibration_retrieve:{retrieve}");

            //Make the saved calibration file readable so third-party apps can use it
            sys_chmod(path, _0755);
            return retrieve;

        }

        // user permissions
        const int S_IRUSR = 0x100;
        const int S_IWUSR = 0x80;
        const int S_IXUSR = 0x40;

        // group permission
        const int S_IRGRP = 0x20;
        const int S_IWGRP = 0x10;
        const int S_IXGRP = 0x8;

        // other permissions
        const int S_IROTH = 0x4;
        const int S_IWOTH = 0x2;
        const int S_IXOTH = 0x1;

        const int _0755 = S_IRUSR | S_IXUSR | S_IWUSR | S_IRGRP | S_IXGRP | S_IROTH | S_IXOTH;
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int sys_chmod(string path, int mode);
        #endregion

        #region Get eye-tracking images

        private Texture2D lefttex = null;
        private Texture2D righttex = null;
        private Color32[] pixel32_Left;
        private Color32[] pixel32_Right;
        private GCHandle pixelHandle_Left;
        private GCHandle pixelHandle_Right;
        private IntPtr pixelPtr_Left;
        private IntPtr pixelPtr_Right;
        private  bool isGetEyeImage = false;
        int width;
        int height;
        private EyeCameraData eyeCameraData = new EyeCameraData();

        public static UnityEvent<EyeCameraData> onEyeCameraStreamFrameArrived = new UnityEvent<EyeCameraData>();

        public class EyeCameraData
        {
            public int texWidth;
            public int texHeight;
            public Texture leftTex;
            public Texture rightTex;



            //Camera pose


            public CameraParameter parameter;

        }


        public   bool StartCapture() {

            if (isGetEyeImage==false) { 
            isGetEyeImage = true;
             return xv_eyetracking_start();
            }

            return true;

        }


        public  bool StopCapture() {

            if (isGetEyeImage) { 
            
              isGetEyeImage = false;
                return xv_eyetracking_stop();
            }
            return true;

        }


        private  void GetEyetrackingRGBA() {
            if (!isGetEyeImage) {
                return;
            }

            if ( API.xslam_ready())
            {
                Debug.Log($"width:{width},height:{height}");

                if (xv_eyetracking_get_rgba(pixelPtr_Left, pixelPtr_Right, ref width, ref height))
                {

                    if (width != 0 && height != 0)
                    {
                        TextureFormat format = TextureFormat.RGBA32;

                        if (!lefttex)
                        {
                            lefttex = new Texture2D(width, height, format, false);
                            pixel32_Left = lefttex.GetPixels32();
                            pixelHandle_Left = GCHandle.Alloc(pixel32_Left, GCHandleType.Pinned);
                            pixelPtr_Left = pixelHandle_Left.AddrOfPinnedObject();
                        }
                        if (!righttex)
                        {
                            righttex = new Texture2D(width, height, format, false);
                            pixel32_Right = righttex.GetPixels32();
                            pixelHandle_Right = GCHandle.Alloc(pixel32_Right, GCHandleType.Pinned);
                            pixelPtr_Right = pixelHandle_Right.AddrOfPinnedObject();
                        }

                        DebugUtilities.Log($"xv_eyetracking_get_rgba");
                        //Left-eye image
                        lefttex.SetPixels32(pixel32_Left);
                        lefttex.Apply();


                        //Right-eye image
                        righttex.SetPixels32(pixel32_Right);
                        righttex.Apply();

                        eyeCameraData.leftTex = lefttex;
                        eyeCameraData.rightTex = righttex;
                        eyeCameraData.texWidth = width;
                        eyeCameraData.texHeight = height;

                        onEyeCameraStreamFrameArrived?.Invoke(eyeCameraData);

                    }
                    else
                    {
                        DebugUtilities.Log("GetEyeImage Invalid texture");
                    }



                }
                else
                {

                    DebugUtilities.Log("GetEyeImage xv_eyetracking_get_rgba Invalid texture");
                }

            }
            
        }

        #endregion

# region Unity

        private void Update()
        {

            if (!Tracking)
            {
                return;
            }

            UpdateMatrix();

            GetEyetrackingRGBA();

        }
        private void OnDestroy()
        {
            //StopGaze();
        }
       

        //private void OnApplicationPause(bool isPause)
        //{
        //    // Triggered when returning to the desktop
        //    if (isPause)
        //    {

        //        if (Tracking)
        //        {

        //            StopGaze();
        //        }

        //        if (isGetEyeImage) { 
        //          StopCapture();
        //        }
        //    }
        //}

        #endregion
    }
}
