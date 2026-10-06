using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Singray.SystemEvents;

namespace Singray.Foundation
{
    public enum Ble_Bond_Status
    {

        //State of discovered Bluetooth devices
        BOND_NONE,//Disconnected
        BOND_BOND,//Pairing
        BOND_BOND_COMPLETE,//Paired
        BOND_BOND_CONNECTED,//Connected


        //Local Bluetooth adapter state
        STATE_OFF,//Off
        STATE_TURNING_ON,//Starting
        STATE_ON,//On
        STATE_TURNING_OFF,//Stopping
    }
    public class BluetoothManager : MonoBehaviour
    {

        public class bleInfo
        {
            public string info;
            public Ble_Bond_Status status;
        }

        private AndroidJavaObject mAndroidBle;
        private BlePoseListener mBlePoseListener;

        private List<bleInfo> mBluetoolthInfo = new List<bleInfo>();
        public List<bleInfo> BluetoolthList { 
          get { return mBluetoolthInfo; }
        }

        private Ble_Bond_Status ble_Bond_Status = Ble_Bond_Status.STATE_OFF;
        public Ble_Bond_Status Ble_Bond_Status
        {
            get
            {
                return ble_Bond_Status;
            }
        }


        public UnityEvent onStateChange;
        public UnityEvent onScan;

        private void Start()
        {
#if UNITY_EDITOR
            return;
#endif
            if (AndroidConnection.IsTurnOnBluetooth())
            {
                ble_Bond_Status = Ble_Bond_Status.STATE_ON;
               
            }
            else
            {
                ble_Bond_Status = Ble_Bond_Status.STATE_OFF;

              
            }

        }


        /// <summary>
        /// Start listening for Bluetooth devices
        /// </summary>
        public void StartBle()
        {

#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("StartBle");
            AndroidJavaObject unityActivity = GetUnityActivity();
            AndroidJavaClass clazz = new AndroidJavaClass("top.xv.xrlib.common.ble.BleController");
            mAndroidBle = clazz.CallStatic<AndroidJavaObject>("getInstance", unityActivity);

            mBlePoseListener = new BlePoseListener(this);

            // mAndroidBle.Call<bool>("grantPermission");
            mAndroidBle.Call<bool>("start", mBlePoseListener);


            MyDebugTool.Log("StartBle  finish");


        }



        /// <summary>
        /// Enable Bluetooth
        /// </summary>
        public void openBluetooth()
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log(" openBluetooth");
            AndroidJavaObject unityActivity = GetUnityActivity();
            //AndroidJavaClass clazz = new AndroidJavaClass("org.xv.xvsdk.ext.ble.BleController");
            AndroidJavaClass clazz = new AndroidJavaClass("top.xv.xrlib.common.ble.BleController");
            mAndroidBle = clazz.CallStatic<AndroidJavaObject>("getInstance", unityActivity);

            mBlePoseListener = new BlePoseListener(this);

            //  mAndroidBle.Call<bool>("grantPermission");

            mAndroidBle.Call<bool>("openBluetooth", mBlePoseListener);

            MyDebugTool.Log(" openBluetooth2");

        }

        /// <summary>
        /// Disable Bluetooth
        /// </summary>
        public void closeBluetooth()
        {
#if UNITY_EDITOR
            return;
#endif
            //Disable Bluetooth
            MyDebugTool.Log(" closeBluetooth");

            mAndroidBle.Call<bool>("closeBluetooth");
        }

        /// <summary>
        /// Connect Bluetooth
        /// </summary>
        /// <param name="bleInfo"></param>
        public void connectBle(string bleInfo)
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("connectBle:" + bleInfo);
            mAndroidBle.Call<bool>("connect", bleInfo);
        }

        /// <summary>
        /// Disconnect Bluetooth
        /// </summary>
        /// <param name="bleInfo"></param>
        public void disconnect(string bleInfo)
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("disconnect:" + bleInfo);
            mAndroidBle.Call<bool>("disconnect", bleInfo);
        }

        /// <summary>
        /// Unpair Bluetooth device
        /// </summary>
        /// <param name="bleInfo"></param>
        public void unpairDevice(string bleInfo)
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("unpairDevice:" + bleInfo);
            mAndroidBle.Call<bool>("unpairDevice", bleInfo);
        }


        /// <summary>
        /// Refresh the Bluetooth device list
        /// </summary>
        public void Scan()
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("Scan:");

            mAndroidBle.Call("scan");
        }



        // BLE send HID command
        // Send HID command bytes; example controller SLAM reset command: 021a9601
        public void writeHid(string cmd)
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("writeHid:" + cmd);
            mAndroidBle.Call<bool>("write", cmd);
        }


        AndroidJavaObject GetUnityActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("top.xv.xrlib.unity.XvMainActivity");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return unityActivity;
        }
        class BlePoseListener : AndroidJavaProxy
        {
            public BlePoseListener(BluetoothManager blueToothManager) : base("top.xv.xrlib.common.ble.IPoseListener")
            {

                this.toothManager = blueToothManager;
            }
            private BluetoothManager toothManager;

            /// <summary>
            /// Device-discovery callback
            /// </summary>
            /// <param name="bleInfo"></param>
            /// <param name="status"></param>
            /// <param name="isconnected"></param>
            public void onScan(string bleInfo, int status, bool isconnected)
            {
                WorkQueue.Instance.InvokeOnAppThread(() =>
                {
                    bleInfo info = null;

                    for (int i = 0; i < toothManager.mBluetoolthInfo.Count; i++)
                    {
                        if (toothManager.mBluetoolthInfo[i].info == bleInfo)
                        {
                            info = toothManager.mBluetoolthInfo[i];
                            break;
                        }
                    }


                    if (info == null)
                    {
                        info = new bleInfo();
                        toothManager.mBluetoolthInfo.Add(info);
                    }

                    info.info = bleInfo;

                    if (status == 10)
                    {
                        //Disconnected
                        info.status = Ble_Bond_Status.BOND_NONE;
                    }
                    else if (status == 11)
                    {
                        //Connecting
                        info.status = Ble_Bond_Status.BOND_BOND;

                    }
                    else if (status == 12)
                    {
                        //Paired
                        info.status = Ble_Bond_Status.BOND_BOND_COMPLETE;

                    }

                    if (isconnected)
                    {
                        info.status = Ble_Bond_Status.BOND_BOND_CONNECTED;
                    }

                    toothManager.onScan?.Invoke();

                });

            }


            void onCallback(AndroidJavaObject obj)
            {

            }


            /// <summary>
            /// Connection-state change callback
            /// </summary>
            /// <param name="status"></param>
            void onStateChange(int status)
            {

                WorkQueue.Instance.InvokeOnAppThread(() =>
                {
                    if (status == 10)
                    {
                        toothManager.ble_Bond_Status = Ble_Bond_Status.STATE_OFF;
                    }
                    else if (status == 11)
                    {

                        toothManager.ble_Bond_Status = Ble_Bond_Status.STATE_TURNING_ON;
                    }
                    else if (status == 12)
                    {
                        toothManager.ble_Bond_Status = Ble_Bond_Status.STATE_ON;
                    }
                    else if (status == 13)
                    {
                        toothManager.ble_Bond_Status = Ble_Bond_Status.STATE_TURNING_OFF;
                    }

                    toothManager.onStateChange?.Invoke();

                }
                    );
                MyDebugTool.Log("  onStateChange ： " + status + "    ");
            }
        }

     
    }
}
