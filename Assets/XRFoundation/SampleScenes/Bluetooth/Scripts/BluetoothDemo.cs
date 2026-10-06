using UnityEngine;
using UnityEngine.UI;
using Singray.SystemEvents;

namespace Singray.Foundation.SampleScenes
{
   

    public partial class BluetoothDemo : SingletonMonoBehaviour<BluetoothDemo>
    {

        public static BluetoothDemo GetInstance()
        {
            return SingletonMonoBehaviour<BluetoothDemo>.Instance;
        }


        public GameObject blueToothOpenBtn;
        public GameObject blueToothCloseBtn;

        public GameObject blueTeechContent;
        public GameObject blueTeechBtn;

      

        public BluetoothManager blueToothManager;
        public Text bluetoothNameText;

        private void OnEnable()
        {
            nowPage = 0;
        }

        // Start is called before the first frame update
        void Start()
        {

            if (blueToothManager==null) {
                blueToothManager = GetComponent<BluetoothManager>();
                if (blueToothManager==null) {
                    blueToothManager = new GameObject("BlueToothManager").AddComponent<BluetoothManager>();
                }
            }
#if  UNITY_EDITOR

            return;

#endif

            blueToothManager.onScan.AddListener(updateBlueTooch);
            blueToothManager.onStateChange.AddListener(getBloothToothState);


            if (AndroidConnection.IsTurnOnBluetooth())
            {

                blueToothManager. StartBle();
                blueToothOpenBtn.SetActive(false);
                blueToothCloseBtn.SetActive(true);
            }
            else
            {
              
                clearContain();
                blueToothOpenBtn.SetActive(true);
                blueToothCloseBtn.SetActive(false);
            }

            bluetoothNameText.text ="Device Bluetooth: "+ AndroidConnection.getDeviceBtName();

        }


        AndroidJavaObject GetUnityActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("top.xv.xrlib.unity.XvMainActivity");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return unityActivity;
        }

        public void getBloothToothState()
        {
            if (AndroidConnection.IsTurnOnBluetooth() == true)
            {
                blueToothOpenBtn.SetActive(false);
                blueToothCloseBtn.SetActive(true);
            }
            else
            {
                clearContain();
                blueToothOpenBtn.SetActive(true);
                blueToothCloseBtn.SetActive(false);
            }

            updateBlueTooch();
        }

        private void clearContain()
        {

            MyDebugTool.Log(" clearContain");

            if (blueTeechContent.transform.childCount > 1)
            {
                for (int i = 0; i < blueTeechContent.transform.childCount; i++)
                {
                    if (blueTeechContent.transform.GetChild(i).gameObject.name == "wifi")
                    {
                        continue;
                    }
                    Destroy(blueTeechContent.transform.GetChild(i).gameObject);
                }
            }
        }

        public void clickBlueBtn(Button btn)
        {
            MyDebugTool.Log("connectBle:btn.name:" + btn.name);

            int index = int.Parse(btn.name.Split('_')[1]);
            blueToothManager.connectBle(GetInstance().blueToothManager.BluetoolthList[index].info/*.Split('#')[1]*/);
        }

        public void clickDisconnectBtn(Button btn)
        {
            int index = int.Parse(btn.name.Split('_')[1]);
            blueToothManager.disconnect(GetInstance().blueToothManager.BluetoolthList[index].info/*.Split('#')[1]*/);
        }

        public void clickUnpairDevice(Button btn)
        {
            int index = int.Parse(btn.name.Split('_')[1]);
            blueToothManager.unpairDevice(GetInstance().blueToothManager.BluetoolthList[index].info/*.Split('#')[1]*/);
            blueToothManager.BluetoolthList.RemoveAt(index);
            updateBlueTooch();
        }

        public void btnClick(Button btn)
        {

            MyDebugTool.Log("btnClick" + btn.name);
            switch (btn.name)
            {
                case "BlueToothOpenBtn":
                    MyDebugTool.Log(" BlueToothOpenBtn" + blueToothManager.Ble_Bond_Status);

                    if (blueToothManager.Ble_Bond_Status == Ble_Bond_Status.STATE_OFF)
                    {
                        blueToothManager.openBluetooth();

                    }

                    break;
                case "BlueToothCloseBtn":
                    MyDebugTool.Log(" BlueToothCloseBtn" + blueToothManager.Ble_Bond_Status);

                    if (blueToothManager.Ble_Bond_Status == Ble_Bond_Status.STATE_ON)
                    {
                        blueToothManager.BluetoolthList.Clear();
                        clearContain();
                        blueToothManager.closeBluetooth();
                    }

                    break;

                case "Refresh":

                    if (blueToothManager.Ble_Bond_Status == Ble_Bond_Status.STATE_ON)
                    {
                        MyDebugTool.Log(" Refresh");
                        blueToothManager.BluetoolthList.Clear();
                        clearContain();
                        blueToothManager.Scan();
                    }
                    break;
            }
        }


        public void updateBlueTooch()
        {
            clearContain();
            int itemNum = 0;


            for (int i = 0; i < GetInstance().blueToothManager.BluetoolthList.Count; i++)
            {
                GameObject btn = Instantiate(blueTeechBtn);
                btn.SetActive(true);

                btn.transform.Find("id").GetComponent<Text>().text = GetInstance().blueToothManager.BluetoolthList[i].info;/*.Split('#')[0]*/;
                string status = "";

                btn.transform.Find("Disconnect").gameObject.SetActive(false);
                btn.transform.Find("UnPairing").gameObject.SetActive(false);
                switch (GetInstance().blueToothManager.BluetoolthList[i].status)
                {
                    case Ble_Bond_Status.BOND_NONE:
                        status = "Disconnected";
                        break;
                    case Ble_Bond_Status.BOND_BOND:
                        status = "Connecting";
                        break;
                    case Ble_Bond_Status.BOND_BOND_COMPLETE:
                        status = "Paired";
                        btn.transform.Find("UnPairing").gameObject.SetActive(true);

                        break;

                    case Ble_Bond_Status.BOND_BOND_CONNECTED:
                        status = "Connected";
                        btn.transform.Find("Disconnect").gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }



                btn.transform.Find("status").GetComponent<Text>().text = status;/*.Split('#')[0]*/;


                btn.transform.parent = blueTeechContent.transform;
                btn.transform.localPosition = new Vector3(0, 0, 0);
                btn.transform.localScale = new Vector3(1, 1, 1);
                btn.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                btn.transform.Find("bleName").name = "Bt_" + itemNum;
                itemNum++;

                if (itemNum == 8)
                {
                    break;
                }

            }


        }



        private int nowPage = 0;
        public void nextBtPage()
        {
            nowPage++;
            int pageNum = (GetInstance().blueToothManager.BluetoolthList.Count / 8) + 1;
            if (nowPage > pageNum)
            {
                nowPage = pageNum;
            }
            if (nowPage * 9 > GetInstance().blueToothManager.BluetoolthList.Count)
            {
                return;
            }
            clearContain();
            int itemNum = 0;
            for (int i = nowPage * 9; i < GetInstance().blueToothManager.BluetoolthList.Count; i++)
            {
                GameObject btn = Instantiate(blueTeechBtn);
                btn.SetActive(true);

                btn.transform.Find("id").GetComponent<Text>().text = GetInstance().blueToothManager.BluetoolthList[i].info;/*.Split('#')[0]*/;
                string status = "";

                btn.transform.Find("Disconnect").gameObject.SetActive(false);
                btn.transform.Find("UnPairing").gameObject.SetActive(false);
                switch (GetInstance().blueToothManager.BluetoolthList[i].status)
                {
                    case Ble_Bond_Status.BOND_NONE:
                        status = "Disconnected";
                        break;
                    case Ble_Bond_Status.BOND_BOND:
                        status = "Connecting";
                        break;
                    case Ble_Bond_Status.BOND_BOND_COMPLETE:
                        status = "Paired";
                        btn.transform.Find("UnPairing").gameObject.SetActive(true);

                        break;

                    case Ble_Bond_Status.BOND_BOND_CONNECTED:
                        status = "Connected";
                        btn.transform.Find("Disconnect").gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }

                btn.transform.Find("status").GetComponent<Text>().text = status;/*.Split('#')[0]*/;


                btn.transform.parent = blueTeechContent.transform;
                btn.transform.localPosition = new Vector3(0, 0, 0);
                btn.transform.localScale = new Vector3(1, 1, 1);
                btn.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                btn.transform.Find("bleName").name = "Bt_" + itemNum;
                itemNum++;

                if (itemNum == 8)
                {
                    break;
                }
            }
        }



        public void preBtPage()
        {
            nowPage--;
            if (nowPage < 0)
            {
                nowPage = 0;
            }
            clearContain();
            int itemNum = 0;
            for (int i = 0; i < GetInstance().blueToothManager.BluetoolthList.Count; i++)
            {
                GameObject btn = Instantiate(blueTeechBtn);
                btn.SetActive(true);

                btn.transform.Find("id").GetComponent<Text>().text = GetInstance().blueToothManager.BluetoolthList[i].info;/*.Split('#')[0]*/;
                string status = "";

                btn.transform.Find("Disconnect").gameObject.SetActive(false);
                btn.transform.Find("UnPairing").gameObject.SetActive(false);
                switch (GetInstance().blueToothManager.BluetoolthList[i].status)
                {
                    case Ble_Bond_Status.BOND_NONE:
                        status = "Disconnected";
                        break;
                    case Ble_Bond_Status.BOND_BOND:
                        status = "Connecting";
                        break;
                    case Ble_Bond_Status.BOND_BOND_COMPLETE:
                        status = "Paired";
                        btn.transform.Find("UnPairing").gameObject.SetActive(true);

                        break;

                    case Ble_Bond_Status.BOND_BOND_CONNECTED:
                        status = "Connected";
                        btn.transform.Find("Disconnect").gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }



                btn.transform.Find("status").GetComponent<Text>().text = status;/*.Split('#')[0]*/;


                btn.transform.parent = blueTeechContent.transform;
                btn.transform.localPosition = new Vector3(0, 0, 0);
                btn.transform.localScale = new Vector3(1, 1, 1);
                btn.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                btn.transform.Find("bleName").name = "Bt_" + itemNum;
                itemNum++;

                if (itemNum == 8)
                {
                    break;
                }
            }
        }




        //#region Bluetooth API
        //public class bleInfo
        //{
        //    public string info;
        //    public Ble_Bond_Status status;
        //}

        //private AndroidJavaObject mAndroidBle;
        //private BlePoseListener mBlePoseListener;

        //public List<bleInfo> mBlueToolthInfo = new List<bleInfo>();
        //class BlePoseListener : AndroidJavaProxy
        //{
        //    public BlePoseListener() : base("top.xv.xrlib.common.ble.IPoseListener") { }


        //    /// <summary>
        //    /// Device-discovery callback
        //    /// </summary>
        //    /// <param name="bleInfo"></param>
        //    /// <param name="status"></param>
        //    /// <param name="isconnected"></param>
        //    public void onScan(string bleInfo, int status, bool isconnected)
        //    {
        //        WorkQueue.Instance.InvokeOnAppThread(() =>
        //        {
        //            bleInfo info = null;

        //            for (int i = 0; i < GetInstance().mBlueToolthInfo.Count; i++)
        //            {
        //                if (GetInstance().mBlueToolthInfo[i].info == bleInfo)
        //                {
        //                    info = GetInstance().mBlueToolthInfo[i];
        //                    break;
        //                }
        //            }


        //            if (info == null)
        //            {
        //                info = new bleInfo();
        //                GetInstance().mBlueToolthInfo.Add(info);
        //            }

        //            info.info = bleInfo;

        //            if (status == 10)
        //            {
        //                // Disconnected
        //                info.status = Ble_Bond_Status.BOND_NONE;
        //            }
        //            else if (status == 11)
        //            {
        //                // Connecting
        //                info.status = Ble_Bond_Status.BOND_BOND;

        //            }
        //            else if (status == 12)
        //            {
        //                // Paired
        //                info.status = Ble_Bond_Status.BOND_BOND_COMPLETE;

        //            }

        //            if (isconnected)
        //            {
        //                info.status = Ble_Bond_Status.BOND_BOND_CONNECTED;
        //            }

        //            GetInstance().updateBlueTeech();

        //        });

        //    }


        //    void onCallback(AndroidJavaObject obj)
        //    {

        //    }


        //    /// <summary>
        //    /// Connection-state change callback
        //    /// </summary>
        //    /// <param name="status"></param>
        //    void onStateChange(int status)
        //    {

        //        WorkQueue.Instance.InvokeOnAppThread(() =>
        //        {
        //            if (status == 10)
        //            {
        //                GetInstance().ble_Bond_Status = Ble_Bond_Status.STATE_OFF;
        //            }
        //            else if (status == 11)
        //            {

        //                GetInstance().ble_Bond_Status = Ble_Bond_Status.STATE_TURNING_ON;
        //            }
        //            else if (status == 12)
        //            {
        //                GetInstance().ble_Bond_Status = Ble_Bond_Status.STATE_ON;
        //            }
        //            else if (status == 13)
        //            {
        //                GetInstance().ble_Bond_Status = Ble_Bond_Status.STATE_TURNING_OFF;
        //            }

        //            GetInstance().getBloothTeechState();
        //        }
        //            );
        //        MyDebugTool.Log("  onStateChange ： " + status + "    ");
        //    }
        //}


        ///// <summary>
        ///// Enable Bluetooth
        ///// </summary>
        //private void openBluetooth()
        //{
        //    MyDebugTool.Log(" openBluetooth");
        //    AndroidJavaObject unityActivity = GetUnityActivity();
        //    //AndroidJavaClass clazz = new AndroidJavaClass("org.xv.xvsdk.ext.ble.BleController");
        //    AndroidJavaClass clazz = new AndroidJavaClass("top.xv.xrlib.common.ble.BleController");
        //    mAndroidBle = clazz.CallStatic<AndroidJavaObject>("getInstance", unityActivity);

        //    mBlePoseListener = new BlePoseListener();

        //    //  mAndroidBle.Call<bool>("grantPermission");

        //    mAndroidBle.Call<bool>("openBluetooth", mBlePoseListener);

        //    MyDebugTool.Log(" openBluetooth2");

        //}

        ///// <summary>
        ///// Disable Bluetooth
        ///// </summary>
        //private void closeBluetooth()
        //{
        //    // Disable Bluetooth
        //    MyDebugTool.Log(" closeBluetooth");

        //    mAndroidBle.Call<bool>("closeBluetooth");
        //}

        ///// <summary>
        ///// Connect Bluetooth
        ///// </summary>
        ///// <param name="bleInfo"></param>
        //private void connectBle(string bleInfo)
        //{

        //    MyDebugTool.Log("connectBle:" + bleInfo);
        //    mAndroidBle.Call<bool>("connect", bleInfo);
        //}

        ///// <summary>
        ///// Disconnect Bluetooth
        ///// </summary>
        ///// <param name="bleInfo"></param>
        //private void disconnect(string bleInfo)
        //{
        //    MyDebugTool.Log("disconnect:" + bleInfo);
        //    mAndroidBle.Call<bool>("disconnect", bleInfo);
        //}

        ///// <summary>
        ///// Unpair Bluetooth device
        ///// </summary>
        ///// <param name="bleInfo"></param>
        //private void unpairDevice(string bleInfo)
        //{

        //    MyDebugTool.Log("unpairDevice:" + bleInfo);
        //    mAndroidBle.Call<bool>("unpairDevice", bleInfo);
        //}


        ///// <summary>
        ///// Refresh the Bluetooth device list
        ///// </summary>
        //private void Scan()
        //{
        //    MyDebugTool.Log("Scan:");

        //    mAndroidBle.Call("scan");
        //}



        //// BLE send HID command
        //// Send HID command bytes; example controller SLAM reset command: 021a9601
        //public void writeHid(string cmd)
        //{
        //    MyDebugTool.Log("writeHid:" + cmd);
        //    mAndroidBle.Call<bool>("write", cmd);
        //}


        //#endregion


    }
}