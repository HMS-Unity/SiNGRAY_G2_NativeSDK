using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;


namespace Singray.Foundation
{
    public partial class XvSeerPadJoystick : SingletonMonoBehaviour<XvSeerPadJoystick>
    {
        private static JoystickData mJoystickData = new JoystickData();


        private AndroidJavaObject mAndroidSensor;

        private SensorPoseListener mSensorPoseListener;

        public KeyCode resetKeyCode = KeyCode.UpArrow;

        class SensorPoseListener : AndroidJavaProxy
        {

          
            //Glasses quaternion (updated only during 3588 ray calibration)
            public static Quaternion headQua = Quaternion.identity;
            //3588 quaternion in Unity world coordinates, converted from the raw Android quaternion
            public static Quaternion finalQua = Quaternion.identity;
            //3588 quaternion in Unity world coordinates (updated only during ray calibration)
            public static Quaternion finalQuaCaliMoment = Quaternion.identity;

            public static bool calibration = false;
            //Conversion quaternion (updated only during 3588 ray calibration)
            private static Quaternion Qtransfer = Quaternion.identity;
            //private int n;
            public SensorPoseListener() : base("org.xv.xvsdk.ext.ble.ISensorPhoneListener") { }


            bool isGameVector()
            {
                bool ret = false;
                if (SystemInfo.deviceModel.Contains("SeerPad ONE"))
                {
                    ret = true;
                    //Debug.Log("Device is SeerPad ONE");
                }

                //Debug.Log($"isGameVector deviceModel:{SystemInfo.deviceModel} ret:{ret}");
                return ret;
            }



            void onCallback(float w, float x, float y, float z)
            {

                MyDebugTool.Log("XvSeerPadJoystick.onCallback()1");

                //float[] rotation = AndroidJNIHelper.ConvertFromJNIArray<float[]>(jo.GetRawObject());

                //jo.Dispose();

                ////The Android rotation input is a quaternion expressed in Android device coordinates
                //float w = rotation[0];
                //float x = rotation[1];
                //float y = rotation[2];
                //float z = rotation[3];


                // MyDebugTool.Log("XvSeerPadJoystick.onCallback()"+x+"  "+y+"  "+z+"  "+w);
                MyDebugTool.Log("XvSeerPadJoystick.currentRotation onCallback" + x + "  " + y + "  " + z + "  " + w);

                


                if (isGameVector())
                    {
                        MyDebugTool.Log("XvSeerPadJoystick.onCallback()2");


                        //Raw Android quaternion; do not use directly
                        Quaternion qt = new Quaternion(x, y, z, w);
                        //Debug.Log($"qt.eulerAngles:{qt.eulerAngles}");

                        //Convert the Android quaternion to Unity coordinates, producing finalQua
                        Quaternion unityQuaternion = new Quaternion(y, z, x, w) * Quaternion.Euler(0, -90, 180);
                        Vector3 unityEuler = new Vector3(-unityQuaternion.eulerAngles.x, -unityQuaternion.eulerAngles.y, unityQuaternion.eulerAngles.z);

                        finalQua = Quaternion.Euler(unityEuler);


                        //Pass the calibrated quaternion to mData
                        mJoystickData.quaternion = finalQua;
                        mJoystickData.rotation = (finalQua).eulerAngles;
                        MyDebugTool.Log("XvSeerPadJoystick.onCallback()3");


                        MyDebugTool.Log($"Phone3dof after calibrate" + mJoystickData.quaternion);

                    }
                    else
                    {
                        MyDebugTool.Log("XvSeerPadJoystick.Sensor device is not SeerPad ONE!!!");

                        Debug.LogError($"Sensor device is not SeerPad ONE!!!");
                    }

              //  AndroidJNI.PopLocalFrame(IntPtr.Zero);
            }


        }

        AndroidJavaObject GetUnityActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("top.xv.xrlib.unity.XvMainActivity");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return unityActivity;
        }

        protected  void Update()
        {

            mJoystickData.keyTrigger = Input.GetKey(KeyCode.Return);
            mJoystickData.keyA = Input.GetKey(resetKeyCode);
           

#if UNITY_EDITOR


            mJoystickData.position = transform.position;
            mJoystickData.rotation = transform.eulerAngles;

#endif
        }


        private bool isStart;
       public void StartSensor()
        {
           
#if UNITY_EDITOR
            return;
#endif

            if (isStart) {
                return;
            }
            MyDebugTool.Log($"Phone3dof StartSensor");
            AndroidJavaObject unityActivity = GetUnityActivity();
            AndroidJavaClass clazz = new AndroidJavaClass("org.xv.xvsdk.ext.ble.AndroidSensorPhone");
            mAndroidSensor = clazz.CallStatic<AndroidJavaObject>("getInstance", unityActivity);

            mSensorPoseListener = new SensorPoseListener();
           // mSensorPoseListener =null;

            // mSensorPoseListener.SetJoystickData(mJoystickData);
            bool b = mAndroidSensor.Call<bool>("start", mSensorPoseListener);
            MyDebugTool.Log($"Phone3dof StartSensor result:{b}");
            isStart = true;
        }



        public  JoystickData GetJoystickData()
        {
            return mJoystickData;
        }

        public  bool IsConnected()
        {
            //MyDebugTool.Log("IsConnected");
            return true;
        }

    }
}
