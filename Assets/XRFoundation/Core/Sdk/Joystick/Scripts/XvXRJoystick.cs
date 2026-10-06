using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Singray.MixedReality.Toolkit.XvXR.Input;

namespace Singray.Foundation
{
    /// <summary>
    /// status:STATE_DISCONNECTED = 0;  STATE_CONNECTING = 1; STATE_CONNECTED = 2;STATE_DISCONNECTING = 3;STATE_DISCOVERD = 4;
    /// </summary>
    public class bleInfo
    {
        public string id;
        public string mac;
        public int status;//
        public string serialNumber;
    }
    public class JoystickData
    {
        public Vector3 position;
        public Vector3 rotation;
        public Quaternion quaternion;
        public int confidence;
        public bool keyTrigger;

        public int keyMenu;
        public bool keyRocker;
        public bool keySlide;
        public bool keyA;
        public bool keyB;
        public int keyRockerX;
        public int keyRockerY;

        public void Copy(JoystickData data)
        {
            position = data.position;
            rotation = data.rotation;
            quaternion = data.quaternion;
            confidence = data.confidence;
            keyTrigger = data.keyTrigger;

            keyMenu = data.keyMenu;
            keyRocker = data.keyRocker;
            keySlide = data.keySlide;
            keyA = data.keyA;
            keyB = data.keyB;
            keyRockerX = data.keyRockerX;
            keyRockerY = data.keyRockerY;
        }
    }


    public partial class XvXRJoystick : SingletonMonoBehaviour<XvXRJoystick>
    {

        const string TAG = "XvXRJoystick";
        public enum DataSource
        {
            BLE_TYPE_AC = 1,
            BLE_TYPE_B = 2,
            ANDROID_SENSOR = 3,
            BLE_TYPE_I500 = 4,
        }

        private AndroidJavaObject mAndroidBle;

        private static JoystickData leftJoystickData = new JoystickData();
        private static JoystickData rightJoystickData = new JoystickData();

        private bool mLeftConnect = false;
        private bool mRightConnect = false;


        //private List<string> bleList = new List<string>();

        //private static List<string> connectBlList = new List<string>();





        bool mXvBleInit = false;
        private BlePoseListener mBlePoseListener;

        private static List<bleInfo> bleInfos = new List<bleInfo>();
        public List<bleInfo> GetBleInfo()
        {
            //bleInfos.Clear();
            //for (int i = 0; i < bleList.Count; i++)
            //{
            //    if (bleList[i].Trim().Contains("xv"))
            //    {
            //        bleInfo bleInfo = new bleInfo();
            //        bleInfo.id = bleList[i].Split('#')[0];
            //        bleInfo.mac = bleList[i].Split('#')[1];
            //        bleInfo.serialNumber = bleList[i];
            //        for (int j = 0; j < connectBlList.Count; j++)
            //        {
            //            if (connectBlList[j] == bleList[i].Split('#')[1])
            //            {
            //                bleInfo.status = 1;
            //            }
            //        }
            //        bleInfos.Add(bleInfo);


            //    }
            //}
            return bleInfos;
        }









        AndroidJavaObject GetUnityActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return unityActivity;
        }

        class BlePoseListener : AndroidJavaProxy
        {
            public BlePoseListener() : base("org.xv.xvsdk.ext.ble.IPoseListener") { }

            void onScan(string ble)
            {
                Utility.Log(TAG, "onScan bleInfo:" + ble);

                //if (ble.Contains("xv")) {
                //    bool isExist=false;
                //    for (int i = 0; i < bleInfos.Count; i++)
                //    {
                //        if (bleInfos[i].serialNumber== ble)
                //        {
                //            isExist = true;
                //            break;
                //        }
                //    }

                //    if (!isExist) {

                //        bleInfo bleInfo = new bleInfo();
                //        bleInfo.id = ble.Split('#')[0];
                //        bleInfo.mac = ble.Split('#')[1];
                //        bleInfo.serialNumber = ble;

                //        bleInfos.Add(bleInfo);
                //    }
                //}

            }
        }
        private API.WirelessControllerDeviceInfo wirelessControllerDeviceInfo = default(API.WirelessControllerDeviceInfo);
        public API.WirelessControllerDeviceInfo GetDeviceInfo(bool isLeft) {
            if ((mLeftConnect && isLeft) || (mRightConnect && !isLeft))
            {
                int type = isLeft ? 1 : 2;
                API.xv_wireless_get_device_info(ref wirelessControllerDeviceInfo, type);
                return wirelessControllerDeviceInfo;

            }
            return default;
        }

        public int GetBattery(bool isLeft)
        {
            if ((mLeftConnect && isLeft)|| (mRightConnect && !isLeft)) {
                int type = isLeft ? 1 : 2;
                MyDebugTool.Log("xv_wireless_get_device_info 1");

                API.xv_wireless_get_device_info(ref wirelessControllerDeviceInfo, type);
                MyDebugTool.Log("xv_wireless_get_device_info 2" + wirelessControllerDeviceInfo.battery + "  " + wirelessControllerDeviceInfo.temp);

                return wirelessControllerDeviceInfo.battery;
            }

            return -1;
           
        }

        public int GetTemperature(bool isLeft)
        {
            if ((mLeftConnect && isLeft) || (mRightConnect && !isLeft))
            {

                int type = isLeft ? 1 : 2;

                MyDebugTool.Log("xv_wireless_get_device_info 1");
                API.xv_wireless_get_device_info(ref wirelessControllerDeviceInfo, type);

                MyDebugTool.Log("xv_wireless_get_device_info 2"+ wirelessControllerDeviceInfo.battery+"  "+ wirelessControllerDeviceInfo.temp);

                return wirelessControllerDeviceInfo.temp;
            }
            return -1;

        }


        public JoystickData GetJoystickData(bool isLeft)
        {

            if (isLeft)
            {
                //MyDebugTool.Log("GetJoystickData() " + isLeft + "  " + leftJoystickData.position);
                return leftJoystickData;
            }
            else
            {
               // MyDebugTool.Log("GetJoystickData() " + isLeft + "  " + rightJoystickData.position);


                return rightJoystickData;

            }


        }
        public bool IsReady(bool isLeft)
        {

            if (isLeft)
            {
                return mLeftConnect;
            }
            else
            {
                return mRightConnect;
            }



        }

        public void SetReady(bool isLeft)
        {
            if (isLeft)
            {
                mLeftConnect = true;

            }
            else
            {
                mRightConnect = true;
            }


        }



        /// <summary>
        /// Initialize the Bluetooth Java environment
        /// </summary>
       public void StartBle()
        {
#if UNITY_EDITOR
            return;
#endif

            AndroidJavaObject unityActivity = GetUnityActivity();
            AndroidJavaClass clazz = new AndroidJavaClass("org.xv.xvsdk.ext.ble.BleController");
            mAndroidBle = clazz.CallStatic<AndroidJavaObject>("getInstance", unityActivity);

          //  mBlePoseListener = new BlePoseListener();
            // mBlePoseListener.SetJoystickData(leftJoystickData);
           // mAndroidBle.Call<bool>("start", (int)DataSource.BLE_TYPE_I500, mBlePoseListener);
        }

        /// <summary>
        /// Get Bluetooth device-list status
        /// </summary>
        /// <param name="blName"></param>
        /// <param name="blMac"></param>
        /// <param name="state"></param>
        [MonoPInvokeCallback(typeof(API.WirelessStateCallback))]
        static void OnWirelessStateCallback(IntPtr blName, IntPtr blMac, int state)
        {
            string sName = Marshal.PtrToStringAnsi(blName);
            string sMac = Marshal.PtrToStringAnsi(blMac);


            MyDebugTool.Log("OnWirelessStateCallback1:sName=" + sName + "  sMac " + sMac + "   state:" + state);


            for (int i = 0; i < bleInfos.Count; i++)
            {
                if (bleInfos[i].id == sName&& bleInfos[i].mac== sMac)
                {
                    MyDebugTool.Log("OnWirelessStateCallback2:sName=" + sName + "  sMac " + sMac + "   state:" + state);

                    bleInfos[i].status = state;
                    break;
                }
            }
        }


        /// <summary>
        /// Get Bluetooth controller data
        /// </summary>
        /// <param name="data"></param>
        [MonoPInvokeCallback(typeof(API.WirelessPoseCallback))]
        static void OnWirelessPoseCallback(ref API.WirelessPos data)
        {

            if (data.type == 196)
            {
                Quaternion q = new Quaternion((float)data.quaternion.x, (float)data.quaternion.y, (float)data.quaternion.z, (float)data.quaternion.w);
                leftJoystickData.position = new Vector3(data.position.x, -data.position.y, data.position.z);
                Vector3 rotation = new Vector3(q.eulerAngles.x * -1.0f, q.eulerAngles.y, q.eulerAngles.z * -1.0f);
                leftJoystickData.rotation = rotation;
                leftJoystickData.quaternion = q;

                leftJoystickData.keyRockerX = data.rocker_x;
                leftJoystickData.keyRockerY = data.rocker_y;

                leftJoystickData.keyA = data.keyA == 1;
                leftJoystickData.keyB = data.keyB == 1;
                leftJoystickData.keySlide = data.keySide > 50;
                leftJoystickData.keyTrigger = data.keyTrigger > 50;

                Instance.SetReady(true);
                MyDebugTool.Log("OnWirelessPoseCallback:" + data.type + "  " + leftJoystickData.position+"  "+ data.keyTrigger);

            }
            else
            {


                Quaternion q = new Quaternion((float)data.quaternion.x, (float)data.quaternion.y, (float)data.quaternion.z, (float)data.quaternion.w);
                rightJoystickData.position = new Vector3(data.position.x, -data.position.y, data.position.z);
                Vector3 rotation = new Vector3(q.eulerAngles.x * -1.0f, q.eulerAngles.y, q.eulerAngles.z * -1.0f);
                rightJoystickData.rotation = rotation;
                rightJoystickData.quaternion = q;

                rightJoystickData.keyRockerX = data.rocker_x;
                rightJoystickData.keyRockerY = data.rocker_y;

                rightJoystickData.keyA = data.keyA == 1;
                rightJoystickData.keyB = data.keyB == 1;
                rightJoystickData.keySlide = data.keySide > 50;
                rightJoystickData.keyTrigger = data.keyTrigger > 50;

                Instance.SetReady(false);
                MyDebugTool.Log("OnWirelessPoseCallback:" + data.type + "  " + leftJoystickData.position + "  " + data.keyTrigger);

            }



        }

        /// <summary>
        /// Get Bluetooth device-list information
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mac"></param>
        [MonoPInvokeCallback(typeof(API.wirelessScanCallback))]
        static void OnWirelessScanCallback(IntPtr name, IntPtr mac)
        {
            string sName = Marshal.PtrToStringAnsi(name);
            string sMac = Marshal.PtrToStringAnsi(mac);
            string ble= sName + "#" + sMac;
            MyDebugTool.Log( "OnWirelessScanCallback :" + ble);


            if (ble.Contains("xv"))
            {
                bool isExist = false;
                for (int i = 0; i < bleInfos.Count; i++)
                {
                    if (bleInfos[i].serialNumber == ble)
                    {
                        isExist = true;
                        break;
                    }
                }

                if (!isExist)
                {

                    bleInfo bleInfo = new bleInfo();
                    bleInfo.id = ble.Split('#')[0];
                    bleInfo.mac = ble.Split('#')[1];
                    bleInfo.serialNumber = ble;

                    bleInfos.Add(bleInfo);
                }
            }

           
        }

        /// <summary>
        /// Connect to the specified Bluetooth device
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mac"></param>
        public void ConnectXvBle(string name, string mac)
        {
            MyDebugTool.Log( "wirelessConnect name:" + name + " mac:" + mac);
            API.xv_wireless_connect(name, mac);
        }

        /// <summary>
        /// Disconnect the specified Bluetooth device
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mac"></param>
        public void DisConnectXvBle(string name, string mac)
        {
            MyDebugTool.Log( "DisConnectXvBle1 name:" + name + " mac:" + mac);
            API.xv_wireless_disconnect(name, mac);
            MyDebugTool.Log( "DisConnectXvBle2 name:" + name + " mac:" + mac);

            for (int i = 0; i < bleInfos.Count; i++)
            {
                if (bleInfos[i].id == name && bleInfos[i].mac == mac)
                {
                   
                    bleInfos[i].status = 0;
                    mRightConnect = false;
                    mLeftConnect = false;

                    leftJoystickData.position = Vector3.up * 5000;
                    rightJoystickData.position = Vector3.up * 5000;



                    break;
                }
            }


        }

        /// <summary>
        /// Register Bluetooth callbacks
        /// </summary>
        void StartXvBle()
        {
            if (mXvBleInit)
            {
                return;
            }

            API.xv_wireless_start();//Start receiving messages
            API.xv_wireless_scan(OnWirelessScanCallback);//Get the Bluetooth device list
            API.xv_wireless_register(OnWirelessPoseCallback);//Controller pose state
            API.xv_wireless_register_state(OnWirelessStateCallback);//Bluetooth connection state

            MyDebugTool.Log( "StartXvBle");
            mXvBleInit = true;
        }



        // Update is called once per frame
        void Update()
        {
#if !UNITY_EDITOR
            if (API.xslam_ready())
            {
                StartXvBle();
            }
      
#endif
        }
    }

}