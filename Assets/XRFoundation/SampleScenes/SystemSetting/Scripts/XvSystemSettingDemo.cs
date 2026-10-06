using AOT;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Singray.SystemEvents;
using static Singray.Foundation.XvSystemSetting;

namespace Singray.Foundation.SampleScenes
{
    public class XvSystemSettingDemo : MonoBehaviour
    {
        public XvSystemSettingManager settingManager;
        private Text brightnessValue;
        private Text ipdValue;
        private Text battery;

        private Text cpuInfo;
        private Text gpuInfo;
        private Text meInfo;
        private Text tempInfo;



        public static Text wearText;
        public static Text lightPerceptiontText;
        public static Text keyTxt;
        public static Text keyStateTxt;
        public  Text volumnText;
        public AudioSource audioSourceOfVolumn;


        private float ipd;

        private void Awake()
        {
            if (settingManager==null) {
                settingManager=FindObjectOfType<XvSystemSettingManager>();

                if (settingManager==null) {
                    settingManager = new GameObject("XvSystemSettingManager").AddComponent<XvSystemSettingManager>();
                }
            }
            brightnessValue = transform.Find("UI/Canvas/Brightness/brightnessValue").GetComponent<Text>();
            ipdValue = transform.Find("UI/Canvas/Ipd/IpdValue").GetComponent<Text>();

            wearText = transform.Find("UI/Canvas/Wear/Wear").GetComponent<Text>();
            lightPerceptiontText = transform.Find("UI/Canvas/LightPerceptiont/LightPerceptiont").GetComponent<Text>();
            keyTxt = transform.Find("UI/Canvas/Key/KeyTxt").GetComponent<Text>();
            keyStateTxt = transform.Find("UI/Canvas/Key/KeyStateTxt").GetComponent<Text>();
            battery = transform.Find("UI/Canvas/Battery/Value").GetComponent<Text>();

            cpuInfo= transform.Find("UI/Canvas/CPUInfo/Value").GetComponent<Text>();
            gpuInfo = transform.Find("UI/Canvas/GPUInfo/Value").GetComponent<Text>();
            meInfo = transform.Find("UI/Canvas/MemoryInfo/Value").GetComponent<Text>();
            tempInfo = transform.Find("UI/Canvas/TempInfo/Value").GetComponent<Text>();
        }

        private void Start()
        {
            AndroidConnection.startBackService();
            Invoke("Initialized", 2);
        }
        private void Update()
        {


#if UNITY_EDITOR
            return;
#endif


            if (Time.frameCount%120==0) {
                battery.text = SystemInfo.batteryLevel * 100 + "%";

                MyDebugTool.Log("getCpuInfo 1");

                string cpuStr = AndroidConnection.getCpuInfo();

                MyDebugTool.Log("cpuStr" + cpuStr);
                MyDebugTool.Log("getCpuInfo 2");


                string[] arr = cpuStr.Split('|');


                cpuInfo.text = arr[0];
                tempInfo.text= arr[1];
                meInfo.text = arr[2];

                MyDebugTool.Log("getGpuLoad 1");

                gpuInfo.text = AndroidConnection.getGpuLoad() + "%";
                MyDebugTool.Log("getGpuLoad 2" + gpuInfo.text);
            }
           


        }




        private void Initialized()
        {
            ipd =  (float)Math.Round(settingManager.GetIPD(), 1);
            SetIpd(ipd);

            brightnessValue.text = string.Format("{0}", settingManager.GetBrightnessLevel());
            ipdValue.text = string.Format("{0}mm", ipd);

            settingManager.XSlamStartEventStream(OnDevice_stream_callback);
            volumnText.text=settingManager.GetVolumeCurrent().ToString();
        }
        


        public void BrightnessUp() {
           int level= settingManager.GetBrightnessLevel();
            level++;
            SetBrightness(level);
        }
        public void BrightnessDown()
        {
            int level = settingManager.GetBrightnessLevel();
            level--;
            SetBrightness(level);
        }
        public void SetBrightness(float value)
        {
            int level = (int)value;
            level = Mathf.Clamp(level, 0, 9);
           
            brightnessValue.text = string.Format("{0}", level);

           
            settingManager.SetBrightnessLevel(level);
        }
        public void IpdUp()
        {
            ipd += 1;
           
            SetIpd(ipd);
        }
        public void IpdDown()
        {
            ipd -= 1;
            SetIpd(ipd);
        }
        public void SetIpd(float value)
        {
            ipd = value;
          
            ipd = Mathf.Clamp(ipd, 55, 75);
            settingManager.SetIPD(ipd);
            ipdValue.text = string.Format("{0}mm", ipd);
          
            // ipdSlider.value = value;

        }


        private static bool isWear=false;
        [MonoPInvokeCallback(typeof(device_stream_callback))]
        public static void OnDevice_stream_callback(XvEvent xvEvent)
        {
            //key = 2, state = 0: glasses removed
            //key = 2, state = 1: glasses worn

            //key = 6, state = 0: ambient light

            //key = 14, 1, 13, 3; state = 254: pressed, 255: released
            //key = 17, 18; state = 101: positive rotation, 99: negative rotation
            switch (xvEvent.type)
            {
                case 14:
                case 1:
                case 13:
                case 3:
                case 17:
                case 18:
                    keyTxt.text = xvEvent.type.ToString();
                    keyStateTxt.text = xvEvent.state.ToString();
                    break;
                case 2:
                    if (xvEvent.state == 1)
                    {
                        wearText.text = "Worn";
                    }
                    else
                    {
                        wearText.text = "Not worn";
                    }
                    break;
                case 6:
                    lightPerceptiontText.text = xvEvent.state.ToString();
                    break;
            }
        }


        public void VolumnUp() {
            AdjustVolume(1);
        }

        public void VolumnDown() { 
            AdjustVolume(-1);

        }
        internal  void AdjustVolume(int direction)
        {
            if (audioSourceOfVolumn!=null) {
                if (audioSourceOfVolumn.isPlaying)
                {
                    audioSourceOfVolumn.Stop();
                }

                audioSourceOfVolumn.Play();
            }
            
            volumnText.text = settingManager.AdjustVolume(direction).ToString();
        }

    }
}
