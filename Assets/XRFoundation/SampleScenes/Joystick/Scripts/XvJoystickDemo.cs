using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Singray.Engine;
using Singray.MixedReality.Toolkit.XvXR.Input;
using Singray.UI.Input;
using static Singray.Foundation.BluetoothManager;


namespace Singray.Foundation.SampleScenes
{

    public class XvJoystickDemo : MonoBehaviour
    {
        public Text headsixdof;
        public Text headsixdof_rot;

        public Text realhandle;
        public Text realhandle_rot;

        public Text keyA;
        public Text keyB;
        public Text keyTrigger;
        public Text keySlide;
        public Text keyRockerValue;

        public Text battery;
        public Text temperature;


        public Text sleep;
        public Text charging;

        public Text confidence;


      
        public GameObject blueTeechBtn;
        public GameObject blueTeechContent;


        private bool gazeShow = true;
        private bool handRayShow = true;
        private bool joystickRayShow = true;




        // Start is called before the first frame update
        void Start()
        {
          

            for (int i = 0; i < 20; i++)
            {
                GameObject btn = Instantiate(blueTeechBtn, Vector3.zero, Quaternion.identity);
                btn.transform.parent = blueTeechContent.transform;
                btn.transform.localPosition = new Vector3(0, 0, 0);
                btn.transform.localScale = new Vector3(1, 1, 1);
                btn.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                btn.transform.Find("id").GetComponent<Text>().text = "";
                btn.transform.Find("mac").GetComponent<Text>().text = "";
                btn.name = "Bt_" + i;
                btn.SetActive(false);
            }
        }



        public void btnClick(GameObject btn)
        {
            switch (btn.name)
            {
                case "HandleRestartBtn":
                    API.xslam_reset_slam();
                    break;

                case "ShellBtn":
                   

                    joystickRayShow = !joystickRayShow;

                    if (joystickRayShow)
                    {
                        btn.transform.GetComponentInChildren<Text>().text = "Disable controller";
                        PointerUtils.SetMotionControllerRayPointerBehavior(PointerBehavior.AlwaysOn);
                    }
                    else { 
                        PointerUtils.SetMotionControllerRayPointerBehavior(PointerBehavior.AlwaysOff);
                        btn.transform.GetComponentInChildren<Text>().text = "Enable controller";
                    }

                    MixedRealityControllerVisualizer[] mixedRealityControllerVisualizers = GameObject.FindObjectsOfType<MixedRealityControllerVisualizer>(true);

                    foreach (var item in mixedRealityControllerVisualizers)
                    {
                        item.gameObject.SetActive(joystickRayShow);
                    }

                    break;

                case "HideGaze":
                    gazeShow = !gazeShow;
                    if (XvHeadGazeInputController.Instance) {

                        if (gazeShow)
                        {
                            btn.transform.GetComponentInChildren<Text>().text = "Disable head tracking";

                        }
                        else { 
                            btn.transform.GetComponentInChildren<Text>().text = "Enable Head Tracking";

                        }


                        XvHeadGazeInputController.Instance.ShowOrHidePointer(gazeShow);
                    }
                    break;

                case "HandsRay":
                    handRayShow = !handRayShow;

                    if (handRayShow)
                    {
                        btn.transform.GetComponentInChildren<Text>().text= "Disable gestures";
                        PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOn);
                    }
                    else {
                        btn.transform.GetComponentInChildren<Text>().text = "Enable gestures";

                        PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);
                    }

                    MixedRealityInputSystemProfile inputSystemProfile = CoreServices.InputSystem?.InputSystemProfile;
                    if (inputSystemProfile == null)
                    {
                        return;
                    }

                    MixedRealityHandTrackingProfile handTrackingProfile = inputSystemProfile.HandTrackingProfile;
                    if (handTrackingProfile != null)
                    {
                        handTrackingProfile.EnableHandMeshVisualization = handRayShow;
                    }

                    XvDeviceManager.Manager.ChangeGetureStatus(handRayShow);

                    break;

                    

            }
        }

     

        private void clearContain()
        {
            int childCount = blueTeechContent.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                blueTeechContent.transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        private void updateBlueTeech()
        {
            if (XvJoystickManager.Instance.useSeerPad) {
                return;
            }
           
            clearContain();
            int index = 0;
            List<bleInfo> bleInfoList = XvJoystickManager.Instance.GetBleInfo(TrackerType.Right);


            if (bleInfoList == null)
            {
                MyDebugTool.Log("updateBlueTeech   bleInfoList == null");
                return;
            }
            string rightSerialNumber= XvJoystickManager.Instance.GetSerialNumber(TrackerType.Right);
            string leftSerialNumber = XvJoystickManager.Instance.GetSerialNumber(TrackerType.Left);

            for (int i = 0; i < bleInfoList.Count; i++)
            {

                GameObject btn = blueTeechContent.transform.GetChild(index).gameObject;
                btn.SetActive(true);
                btn.transform.Find("id").GetComponent<Text>().text = bleInfoList[i].id;
                btn.transform.Find("mac").GetComponent<Text>().text = bleInfoList[i].mac;
                btn.GetComponent<BlueTeethControl>().bleInfo = bleInfoList[i];
             
                btn.name = "Bt_" + i;


                //Connect automatically if a default serial number is configured
                if (leftSerialNumber == bleInfoList[i].serialNumber) {
                    if (bleInfoList[i].status == 0)
                    {
                        MyDebugTool.Log("Auto-connect：" + rightSerialNumber);
                        XvJoystickManager.Instance.ConnectXvBle(TrackerType.Right, bleInfoList[i].id, bleInfoList[i].mac);
                    }
                }

                //Connect automatically if a default serial number is configured
                if (rightSerialNumber== bleInfoList[i].serialNumber) {
                    if (bleInfoList[i].status == 0)
                    {
                        MyDebugTool.Log("Auto-connect：" + rightSerialNumber);
                        XvJoystickManager.Instance.ConnectXvBle(TrackerType.Right, bleInfoList[i].id, bleInfoList[i].mac);
                    }
                }
                //xv_left_23006970#D3:D3:50:4D:32:68
                ///Update the status display
                ///
                switch (bleInfoList[i].status)
                {
                    case 0:
                        btn.transform.Find("mac").GetComponent<Text>().text = "<color=white>Not connected</color>";
                        break;
                    case 1:
                        btn.transform.Find("mac").GetComponent<Text>().text = "<color=white>Connecting...</color>";

                        break;
                    case 2:
                        btn.transform.Find("mac").GetComponent<Text>().text = "<color=white>Connected</color>";

                        break;
                    case 3:
                        btn.transform.Find("mac").GetComponent<Text>().text = "<color=white>Disconnecting...</color>";

                        break;
                    case 4:
                        btn.transform.Find("mac").GetComponent<Text>().text = "<color=white>Acquiring Pose</color>";

                        break;
                    default:
                        break;
                }

               
                index++;
            }

           

        }

        public void blueTeechConnect(GameObject btn)
        {

            bleInfo bleInfo = btn.GetComponent<BlueTeethControl>().bleInfo;

            MyDebugTool.Log("blueTeechConnect" + bleInfo.id + "   " + bleInfo.mac+"  "+ bleInfo.status);

            if (bleInfo.status == 0)
            {
                XvJoystickManager.Instance.ConnectXvBle(TrackerType.Right, bleInfo.id, bleInfo.mac);
            }
            else {

                if (bleInfo.status==4) {
                    XvJoystickManager.Instance.DisConnectXvBle(TrackerType.Right, bleInfo.id, bleInfo.mac);

                    btn.transform.Find("mac").GetComponent<Text>().text = "<color=white>Disconnecting...</color>";
                }
            }
        }

        

        // Update is called once per frame
        void Update()
        {
            if (XvJoystickManager.Instance.useSeerPad)
            {
                return;
            }


            if (headsixdof != null)
            {
                headsixdof.text = $"glass pos: {Math.Round(XvXRManager.SDK.HeadPose.Position.x, 5)} , {Math.Round(XvXRManager.SDK.HeadPose.Position.y, 5)} , {Math.Round(XvXRManager.SDK.HeadPose.Position.z, 5)}";
            }
            if (headsixdof_rot != null)
            {
                headsixdof_rot.text = $"glass rot: {XvXRManager.SDK.HeadPose.Orientation.eulerAngles}";
            }

            Vector3 pos = XvJoystickManager.Instance.GetPosition(TrackerType.Right);
            Quaternion rot = XvJoystickManager.Instance.GetRotation(TrackerType.Right);


            if (realhandle != null)
            {
                realhandle.text = $"controller pos: {Math.Round(pos.x, 5)} , {Math.Round(pos.y, 5)} , {Math.Round(pos.z, 5)}";
            }
            if (realhandle_rot != null)
            {
                realhandle_rot.text = $"controller rot: {rot}";
            }


            if (keyTrigger != null)
            {
                keyTrigger.text = $"keyTrigger: {XvJoystickManager.Instance.GetKey(JoystickButton.Button_Trigger,TrackerType.Right)}";
            }
            if (keyA != null)
            {
                keyA.text = $"keyA: {XvJoystickManager.Instance.GetKey(JoystickButton.Button_A,TrackerType.Right)}";
            }
            if (keyB != null)
            {
                keyB.text = $"keyB: {XvJoystickManager.Instance.GetKey(JoystickButton.Button_B,TrackerType.Right)}";
            }
            if (keySlide != null)
            {
                keySlide.text = $"keySlide: {XvJoystickManager.Instance.GetKey(JoystickButton.Button_Grip,TrackerType.Right)}";
            }

            //if (keyRocker != null)
            //{
            //    keyRocker.text = $"keyRocker: {XvJoystickManager.Instance.GetJoystickData(TrackerType.Right).keyRocker}";
            //}

            if (keyRockerValue != null)
            {
                keyRockerValue.text = $"keyRockerValue: {XvJoystickManager.Instance.GetRockerVector2(TrackerType.Right)}";
            }
            if (confidence != null)
            {
                confidence.text = $"confidence: {XvJoystickManager.Instance.GetConfidence(TrackerType.Right)}";
            }

            API.WirelessControllerDeviceInfo info= XvJoystickManager.Instance.GetDeviceInfo(TrackerType.Right);
            if (battery!=null) {
                battery.text = $"battery: {info.battery}%";
            }
            if (temperature != null)
            {
                temperature.text = $"temperature: {info.temp}°C";
            }

            if (sleep) {
                sleep.text = $"sleep: {info.sleep==1}";

            }
            if (charging)
            {
                charging.text = $"charging: {info.charging==1}";

            }
            if (Time.frameCount%120==0) {
                updateBlueTeech();
            }

          
        }

    }

}