using Singray.utils;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using AOT;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Threading;
using Singray.SystemEvents;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using UnityEngine.Events;

namespace Singray.Engine
{

    class XvDeviceManager : MonoBehaviour
    {
        public static XvDeviceManager Manager
        {

            get
            {
                if (manager == null)
                {
                    manager = UnityEngine.Object.FindObjectOfType<XvDeviceManager>();
                }
                if (manager == null)
                {
                    var go = new GameObject("XvDeviceManager");
                    manager = go.AddComponent<XvDeviceManager>();
                    go.transform.localPosition = Vector3.zero;
                }
                return manager;
            }
        }

        private static XvDeviceManager manager = null;

        [Tooltip("Enable gestures")]

        public bool needStartGesture = false;

        [Tooltip("Enable debug logging")]

        public bool logEnable = false;

        [Tooltip("Allow Back or Home to exit the application")]
        public bool backHome=true;//

        internal UnityAction onApplicationQuit;


        private bool isStartGesture = false;
        private int skeletonId = -1;
        private const int skeletonType = 1;
        private bool isFocus = false;

        public float reportInterval = 1f;
        private float _accumTime;
        private int _frameCount;

        private void Awake()
        {
            XvXRLog.LogEnable = logEnable;
            MyDebugTool.logEnable = logEnable;
            //AndroidConnection.setHomeKeyEnable(backHome);
            transform.Find("Head").localRotation = Quaternion.identity;
            transform.Find("Head").localPosition = Vector3.zero;

            transform.Find("Head/XvXRCamera").localRotation = Quaternion.identity;
            transform.Find("Head/XvXRCamera").localPosition = Vector3.zero;

        }
        void OnEnable()
        {
            isFocus = false;
        }



        void Start()
        {
            Application.targetFrameRate = 120;
            QualitySettings.SetQualityLevel(0, true);
#if UNITY_EDITOR
            return;
#endif
           
            Invoke("getConfig", 0.5f);

        }
        public static event Action OnBeforeQuit;
        void Update()
        {
            /*
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    Debug.LogError("wayland ==== keyCode " + keyCode.ToString());
                }
            }
            */
            if (Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Home))
            {
                if (backHome)
                {
                    OnBeforeQuit?.Invoke();
                    ApplicationQuit();
                }
            }


#if UNITY_EDITOR
            return;
#endif
            if (API.xslam_ready())
            {
                if (needStartGesture)
                {
                    if (!isStartGesture)
                    {

                        if (skeletonId >= 0)
                        {
                            API.xslam_stop_slam_skeleton_with_cb(skeletonType, skeletonId);
                            skeletonId = -1;
                        }


                        // API.xslam_set_gesture_filter(level);

                     string deviceName=   AndroidConnection.getBoxName();
                        MyDebugTool.Log("getBoxName:" + deviceName);
                        if (deviceName.Contains("Kalama")) {
                          API.xslam_set_gesture_platform(2);

                        }

                        skeletonId = API.xslam_start_skeleton_ex_with_cb(XvXRInput.OnSkeletonCallback);

                        isStartGesture = true;
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

            _frameCount++;
            _accumTime += Time.unscaledDeltaTime;
            if (_accumTime < reportInterval) return;

            float fps = _frameCount / _accumTime;
            float frameTimeMs = _accumTime / _frameCount * 1000f;
            Debug.Log($"[FPS] fps={fps:F1} frameTimeMs={frameTimeMs:F1}");

            _frameCount = 0;
            _accumTime = 0f;
        }

        private void OnApplicationPause()
        {
            quit();
        }


        public void ApplicationQuit() {
            Debug.LogError("wayland Back  Home");
            API.xslam_stop_rgb_stream();
            API.xslam_stop_imu();
            API.xslam_stop_tof_stream();
            API.xslam_stop_stereo_stream();
            API.xslam_stop_play();
            API.xslam_unset_mic_callback();
            //onApplicationQuit?.Invoke();
            AndroidConnection.startCustomPackage("com.xv.launcher");
            Application.Quit();
        }
        private void getConfig()
        {
            int ipdVal = API.xslam_get_glass_ipd2();
            if (ipdVal != 65000)
            {
                XvXRAndroidDevice.updateCalibra((float)(ipdVal / 10000.0));
                XvXRManager.SDK.GetDevice().setFedDis((float)(ipdVal / 10000.0));
                XvXREye.EDI = 0;
            }

            API.xslam_set_device_status_callback(OnDeviceStatusCallback);
        }



        [MonoPInvokeCallback(typeof(API.device_status_callback_ex))]
        public static void OnDeviceStatusCallback(API.deviceStatus_package devicesStatus)
        {
            Debug.LogError("devicesStatus.status === " + devicesStatus.status[0]);

        }


        public void OnDestory()
        {

        }


        public void onDeviceConnectChanged(bool isConnected)
        {
            Singray.Engine.XvXRManager.SDK.GetDevice().isConnected = isConnected;
            Singray.Engine.XvXRManager.SDK.GetDevice().isReadFed = false;
        }



        public void init()
        {

            needStartGesture = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            API.xslam_stop_stereo_stream();
            API.xslam_stop_rgb_stream();
            API.xslam_stop_tof_stream();
#endif
        }


        public bool ChangeGetureStatus(bool isOn)
        {
            if (API.xslam_ready())
            {
                try
                {
                    if (isOn)
                    {
                        isStartGesture = false;
                        needStartGesture = true;
                        return true;
                    }
                    else
                    {

                        needStartGesture = false;
                        if (skeletonId >= 0)
                        {
                            bool close = API.xslam_stop_slam_skeleton_with_cb(skeletonType, skeletonId);
                            MyDebugTool.Log("Stop gesture recognition:" + close);
                            skeletonId = -1;
                        }


                        return true;
                    }
                }
                catch (Exception e)
                {

                    MyDebugTool.Log("ChangeGetureStatusException" + e.Message);

                    return false;
                }
            }
            else
            {
                return false;
            }
        }




        // Update is called once per frame
        public void ReadStereoFisheyesCalibration()
        {
            XvXRLog.LogInfo("readStereoFisheyesCalibration: start....");
            API.stereo_fisheyes stereo_params = default(API.stereo_fisheyes);
            API.stereo_pdm_calibration stereo_pdm = default(API.stereo_pdm_calibration);
            int imu_fisheye_shift_us = 0;
            if (API.xslam_ready())
            {

                if (API.readStereoFisheyesCalibration(ref stereo_params, ref imu_fisheye_shift_us))
                {
                    XvXRLog.LogInfo("readStereoFisheyesCalibration:" + stereo_params);
                    for (int i = 0; i < 2; i++)
                    {
                        XvXRLog.LogInfo("readStereoFisheyesCalibration camera" + i + " Fx,Fy: " +
                        stereo_params.calibrations[i].intrinsic.K[0] + "," + stereo_params.calibrations[i].intrinsic.K[1] +
                        ",Cx,Cy:" + stereo_params.calibrations[i].intrinsic.K[2] + "," + stereo_params.calibrations[i].intrinsic.K[3] +
                        "rotation[0]:" + stereo_params.calibrations[i].extrinsic.rotation[0]);

                    }

                }
                else
                {
                    XvXRLog.LogInfo("readStereoFisheyesCalibration faild");
                }
            }

        }


        public void onAppInstallState(string str)
        {
            
            }

        public void onDisplayState(string str)
        {

        }
        public void onPressBoxHome(string str)
        {
            ApplicationQuit();
        }



        [HideInInspector]
        public UnityEvent<string> OnWifiConnectState;
        [HideInInspector]

        public UnityEvent<string> OnWifiApStateChange;


        public void onWifiApStateChange(string str)
        {

            MyDebugTool.Log("onWifiApStateChange");
            OnWifiApStateChange?.Invoke(str);
        }

        public void onWifiConnectState(string str)
        {
            MyDebugTool.Log("onWifiConnectState");

            OnWifiConnectState?.Invoke(str);
        }


       

        public void quit()
        {
            if (isFocus == false)
            {
                isFocus = true;
            }
            else
            {
                isFocus = false;
            }
        }

        private string GetText(string file_path, string file_name)
        {
            string str_info = "62;4";
            StreamWriter sw;
            FileInfo file_info = new FileInfo(file_path + "//" + file_name);
            if (!file_info.Exists)
            {
                sw = file_info.CreateText();//Create a UTF-8 text writer
                MyDebugTool.Log("File created successfully！");
                sw.Write(str_info);
                sw.Close();
                sw.Dispose();//Dispose the file stream
            }

            string result = string.Empty;
            try
            {
                FileInfo file = new FileInfo(file_path + "//" + file_name);
                result = file.OpenText().ReadToEnd();
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        void StartFeThread(){
            MyDebugTool.Log("Fisheye FE startThread...........");
            API.xslam_start_fisheyes_rectification_thread();
        }



    }
}
