using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Singray.Engine;
using Singray.SystemEvents;
namespace Singray.Foundation.SampleScenes
{
    public class WifiControlDemo : MonoBehaviour
    {
        public GameObject wifiContain;
        public List<string> wifiList = new List<string>();
        public List<string> wifiListState = new List<string>();
        public GameObject wifiBtn;
        public Text wifiInfoState;
        public Text ipTxt;
        public Text ssidTxt;



        public Text wifi;
        public Text pwd;
        public GameObject wifiOnBtn;
        public GameObject wifiOffBtn;

        private bool isTest=false;

        private void Awake()
        {
            XvDeviceManager.Manager.OnWifiConnectState.AddListener(updateWifiStateInfo);

            if (isTest) { 
            AndroidConnection.startBackService();
            
            }
        }
        private void Start()
        {
            getWifiState();
        }


        /// <summary>
        /// Enable Wi-Fi
        /// </summary>

        public void TurnOnWifi()
        {
#if UNITY_EDITOR
            return;
#endif

            AndroidConnection.openWifi();
            Invoke("getWifiState", 1f);
        }

        /// <summary>
        /// Disable Wi-Fi
        /// </summary>
        public void TurnOffWifi()
        {
#if UNITY_EDITOR
            return;
#endif

            AndroidConnection.closeWifi();
            Invoke("getWifiState", 0.2f);
        }

        /// <summary>
        /// Get the current Wi-Fi state
        /// </summary>
        public void getWifiState()
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("getWifiState:");

            if (AndroidConnection.getWifiState() == true)
            {

                MyDebugTool.Log("getWifiState:true");


                wifiOffBtn.SetActive(false);
                wifiOnBtn.SetActive(true);
            }
            else
            {
                MyDebugTool.Log("getWifiState:false");

                wifiOffBtn.SetActive(true);
                wifiOnBtn.SetActive(false);
                while (wifiContain.transform.childCount > 0)
                {
                    DestroyImmediate(wifiContain.transform.GetChild(0).gameObject);
                }
            }

            string staticip = "";
            string mask = "";
            string getway = "";
            string dns = "";
            if (PlayerPrefs.HasKey("staticip"))
            {
                staticip = PlayerPrefs.GetString("staticip");
            }
            if (PlayerPrefs.HasKey("mask"))
            {
                mask = PlayerPrefs.GetString("mask");
            }
            if (PlayerPrefs.HasKey("getway"))
            {
                getway = PlayerPrefs.GetString("getway");
            }
            if (PlayerPrefs.HasKey("dns"))
            {
                dns = PlayerPrefs.GetString("dns");
            }
            if (staticip != "")
            {
                AndroidConnection.setStaticIp(staticip, mask, getway, dns);
            }
        }

        private int nowWifiID = -1;
        public void wifiBtnClick(Button btn)
        {
            int i = int.Parse(btn.name.Split('_')[1]);
            wifi.text = wifiList[i];
            pwd.text = "";
            nowWifiID = i;
        }

        /// <summary>
        /// Connect to Wi-Fi
        /// </summary>
        public void ConnectWifi()
        {
#if UNITY_EDITOR
            return;
#endif
            if (pwd.text.Length < 8)
            {
                wifiInfoState.text = "Incorrect password";
            }
            else
            {
                wifiInfoState.text = "Connecting...";
                MyDebugTool.Log("ConnectWifi start:" + wifi.text + "  " + pwd.text);
                try
                {

                    bool isHasPwd=!string.IsNullOrEmpty(pwd.text);

                    AndroidConnection.connectWifi(wifi.text, pwd.text, isHasPwd);
                    MyDebugTool.Log("ConnectWifi:" + wifi.text + "  " + pwd.text + "  " + isHasPwd);
                }
                catch (Exception ex)
                {
                    MyDebugTool.Log("ConnectWifi Exception:" + ex.Message);

                }

                
            }
        }

        /// <summary>
        /// Called when the Wi-Fi connection state changes
        /// </summary>
        /// <param name="str"></param>
        private void updateWifiStateInfo(string str)
        {

            MyDebugTool.Log("updateWifiStateInfo:" + str);

            wifiInfoState.text = str;
            Invoke("getWifiList", 3);
            Invoke("getIP", 1);
            Invoke("getIP", 3);
            Invoke("getIP", 5);
            Invoke("getIP", 8);
            Invoke("getIP", 10);

            Invoke("getSSID", 3);
            Invoke("getSSID", 1);
            Invoke("getSSID", 3);
            Invoke("getSSID", 5);
            Invoke("getSSID", 8);
            Invoke("getSSID", 10);

           
        }


        /// <summary>
        /// Get the Wi-Fi network list
        /// </summary>
        private void getWifiList()
        {
#if UNITY_EDITOR
            return;
#endif
            MyDebugTool.Log("getWifiList:enter");

            string wifiStr = AndroidConnection.getWifiList();

            MyDebugTool.Log("getWifiList:" + wifiStr);

            wifiList = new List<string>();
            wifiListState = new List<string>();


            string[] strArray = wifiStr.Split('|');

            for (int i = 0; i < strArray.Length; i++)
            {
                if (strArray[i] == "")
                {
                    continue;
                }
                string name = strArray[i].Split(':')[0];
                string state = strArray[i].Split(':')[1];
                if (name == "")
                {
                    continue;
                }
                //TouchScreenKeyboard.active = false;
                if (state == "")
                {
                    continue;
                }
                wifiList.Add(name);
                wifiListState.Add(state);
            }

            while (wifiContain.transform.childCount > 0)
            {
                DestroyImmediate(wifiContain.transform.GetChild(0).gameObject);
            }

           // int itemNum = 0;
            for (int i = 0; i < wifiList.Count; i++)
            {
                GameObject wifibtn = Instantiate(wifiBtn);
                wifibtn.SetActive(true);
                wifibtn.transform.parent = wifiContain.transform;
                wifibtn.transform.localPosition = new Vector3(0, 0, 0);
                wifibtn.transform.localScale = new Vector3(1, 1, 1);
                wifibtn.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                wifibtn.name = "wifibtn_" + i;
                wifibtn.transform.Find("Text").GetComponent<Text>().text = wifiList[i];
                //itemNum++;
                //if (itemNum == 9)
                //{
                //    break;
                //}
            }

            string ip = AndroidConnection.getWifiIpAddress();
            ipTxt.text = ip;
        }

        /// <summary>
        /// Get the IP address after connecting
        /// </summary>
        private void getIP()
        {
#if UNITY_EDITOR
            return;
#endif
            string ip = AndroidConnection.getWifiIpAddress();
            ipTxt.text = ip;

        }

        /// <summary>
        /// Get the IP address after connecting
        /// </summary>
        private void getSSID()
        {
#if UNITY_EDITOR
            return;
#endif
            string ssid = AndroidConnection.getCurrWifiSsid();
            ssidTxt.text = ssid;

        }

    }
}