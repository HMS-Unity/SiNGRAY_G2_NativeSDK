using System.Collections.Generic;
using UnityEngine;

namespace Singray.Foundation
{
    public class XvJoystickManager : MonoBehaviour
    {
        private XvJoystickManager() { }
        public static XvJoystickManager Instance;

        [SerializeField]
        private XvHandleController leftController;
        [SerializeField]

        private XvHandleController rightController;

        [SerializeField]

        private XvHandleController seerPadController;

        public bool useSeerPad;


        private XvXRJoystickKeyState left;
        private XvXRJoystickKeyState right;
        private XvXRJoystickKeyState seerPad;


        private void Awake()
        {
            Instance = this;

            if (useSeerPad)
            {
                MyDebugTool.Log("XvJoystickManager.start() 1");

                XvSeerPadJoystick.Instance.StartSensor();
                MyDebugTool.Log("XvJoystickManager.start() 2");

                if (seerPadController == null)
                {
                    seerPadController = transform.Find("SeerPadController").GetComponent<XvHandleController>();
                }
                if (seerPadController != null)
                {

                    seerPad = new XvXRJoystickKeyState(seerPadController);
                }
                MyDebugTool.Log("XvJoystickManager.start() 3");

            }
            else {
                XvXRJoystick.Instance.StartBle();
                if (leftController==null) {
                   
                    leftController = transform.Find("LeftController").GetComponent<XvHandleController>();
                   
                }
                if (rightController == null)
                {
                    
                    rightController = transform.Find("RightController").GetComponent<XvHandleController>();
                  
                }



                if (leftController != null)
                {
                    
                    left = new XvXRJoystickKeyState(leftController);
                   
                }
                else
                {
                   

                    MyDebugTool.Log("leftController == null");

                }
                if (rightController != null)
                {
                   
                    MyDebugTool.Log("new XvXRJoystickKeyState(rightController)");
                    right = new XvXRJoystickKeyState(rightController);
                

                }
                else { 
                    MyDebugTool.Log("rightController == null");

                }

               

            }
        }
       



        /// <summary>
        /// Get raw controller data
        /// </summary>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public JoystickData GetJoystickData(TrackerType trackerType)
        {

            switch (trackerType)
            {

                case TrackerType.Left:


                    return leftController == null ? null : leftController.GetJoystickData();

                case TrackerType.Right:
                    return rightController == null ? null : rightController.GetJoystickData();

                case TrackerType.SeerPad:
                    return rightController == null ? null : seerPadController.GetJoystickData();

                default:
                    break;
            }
            return null;

        }
        /// <summary>
        /// Controller button pressed event
        /// </summary>
        /// <param name="button"></param>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public bool GetKeyDown(JoystickButton button, TrackerType trackerType)
        {

            switch (trackerType)
            {

                case TrackerType.Left:
                    return left == null ? false : left.GetKeyDown(button);

                case TrackerType.Right:
                    return right == null ? false : right.GetKeyDown(button);
                case TrackerType.SeerPad:
                    return seerPad == null ? false : seerPad.GetKeyDown(button);

                default:
                    break;
            }
            return false;

        }
        /// <summary>
        /// Controller button released event
        /// </summary>
        /// <param name="button"></param>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public bool GetKeyUp(JoystickButton button, TrackerType trackerType)
        {
            switch (trackerType)
            {

                case TrackerType.Left:
                    return left == null ? false : left.GetKeyUp(button);

                case TrackerType.Right:
                    return right == null ? false : right.GetKeyUp(button);
                case TrackerType.SeerPad:
                    return seerPad == null ? false : seerPad.GetKeyUp(button);
                default:
                    break;
            }
            return false;

        }
        /// <summary>
        /// Controller button long press
        /// </summary>
        /// <param name="button"></param>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public bool GetKey(JoystickButton button, TrackerType trackerType)
        {

            switch (trackerType)
            {

                case TrackerType.Left:
                    return left == null ? false : left.GetKey(button);

                case TrackerType.Right:

                    if (right == null) {
                        Debug.LogError("right == null");
                    }
                    return right == null ? false : right.GetKey(button);

                case TrackerType.SeerPad:
                    return seerPad == null ? false : seerPad.GetKey(button);
                default:
                    break;
            }
            return false;


        }
        /// <summary>
        /// Controller button double click
        /// </summary>
        /// <param name="button"></param>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public bool GetDoubleClick(JoystickButton button, TrackerType trackerType)
        {
            switch (trackerType)
            {

                case TrackerType.Left:
                    return left == null ? false : left.GetDoubleClick(button);

                case TrackerType.Right:
                    return right == null ? false : right.GetDoubleClick(button);


                case TrackerType.SeerPad:
                    return seerPad == null ? false : seerPad.GetDoubleClick(button);
                default:
                    break;
            }
            return false;

        }




        /// <summary>
        /// Get controller thumbstick input
        /// </summary>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public Vector2 GetRockerVector2(TrackerType trackerType)
        {

            JoystickData joystickData = null;
            switch (trackerType)
            {

                case TrackerType.Left:
                    joystickData = leftController?.GetJoystickData();
                    break;
                case TrackerType.Right:
                    joystickData = rightController?.GetJoystickData();
                    break;

                default:
                    break;
            }
            if (joystickData != null)
            {
                Vector2 RockerVec = new Vector2(joystickData.keyRockerX, -joystickData.keyRockerY) / 32767f;


                return RockerVec;
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Get the current controller position
        /// </summary>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public Vector3 GetPosition(TrackerType trackerType)
        {
            switch (trackerType)
            {

                case TrackerType.Left:
                    return leftController == null ? Vector3.zero : leftController.GetPosition();

                case TrackerType.Right:
                    return rightController == null ? Vector3.zero : rightController.GetPosition();

                case TrackerType.SeerPad:
                    return seerPadController == null ? Vector3.zero : seerPadController.GetPosition();

                default:
                    break;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Get the current controller rotation
        /// </summary>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public Quaternion GetRotation(TrackerType trackerType)
        {
            switch (trackerType)
            {

                case TrackerType.Left:
                    return leftController == null ? Quaternion.identity : leftController.GetRotation();

                case TrackerType.Right:
                    return rightController == null ? Quaternion.identity : rightController.GetRotation();


                case TrackerType.SeerPad:
                    return seerPadController == null ? Quaternion.identity : seerPadController.GetRotation();
                default:
                    break;
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Get the current controller tracking confidence
        /// </summary>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public int GetConfidence(TrackerType trackerType)
        {
            switch (trackerType)
            {

                case TrackerType.Left:
                    return leftController == null ? 0 : leftController.GetJoystickData().confidence;

                case TrackerType.Right:
                    return rightController == null ? 0 : rightController.GetJoystickData().confidence;
                default:
                    break;
            }

            return 0;
        }

        public string GetSerialNumber(TrackerType trackerType) {
            switch (trackerType)
            {
                case TrackerType.Left:
                    return leftController == null ? null : leftController.SerialNumber;

                case TrackerType.Right:
                    return rightController == null ? null : rightController.SerialNumber;
                default:
                    break;
            }
            return null;
        }


        public int GetBattery(TrackerType trackerType)
        {

            switch (trackerType)
            {
                case TrackerType.Left:
                    return leftController == null ? -1 : leftController.GetBattery();

                case TrackerType.Right:
                    return rightController == null ? -1 : rightController.GetBattery();
                default:
                    break;
            }
            return -1;
        }

        public int GetTemperature(TrackerType trackerType)
        {
            switch (trackerType)
            {
                case TrackerType.Left:
                    return leftController == null ? -1 : leftController.GetTemperature();

                case TrackerType.Right:
                    return rightController == null ? -1 : rightController.GetTemperature();
                default:
                    break;
            }
            return -1;
        }
        public virtual API.WirelessControllerDeviceInfo GetDeviceInfo(TrackerType trackerType)
        {

            switch (trackerType)
            {
                case TrackerType.Left:
                    return leftController == null ? default : leftController.GetDeviceInfo();

                case TrackerType.Right:
                    return rightController == null ? default : rightController.GetDeviceInfo();
                default:
                    break;
            }
            return default;
        }


        #region Bluetooth controller

        /// <summary>
        /// Get the Bluetooth device list
        /// </summary>
        /// <param name="trackerType"></param>
        /// <returns></returns>
        public List<bleInfo> GetBleInfo(TrackerType trackerType)
        {
            if (useSeerPad) {
                return null;
            }
            return XvXRJoystick.Instance.GetBleInfo();

           
        }

        /// <summary>
        /// Connect to a Bluetooth device
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mac"></param>
        public void ConnectXvBle(TrackerType trackerType, string name, string mac)
        {
            if (useSeerPad)
            {
                return ;
            }
            XvXRJoystick.Instance.ConnectXvBle(name, mac);

          
        }

        /// <summary>
        /// Disconnect Bluetooth
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mac"></param>
        public void DisConnectXvBle(TrackerType trackerType, string name, string mac)
        {
            if (useSeerPad)
            {
                return ;
            }
            XvXRJoystick.Instance.DisConnectXvBle(name, mac);

           
        }

        public bool IsConnected(TrackerType trackerType)
        {
            switch (trackerType)
            {
                case TrackerType.Left:
                    if (leftController==null) { 
                    return false;
                    }

                   return leftController.IsConnected();

                
                case TrackerType.Right:
                    if (rightController == null)
                    {
                        return false;
                    }
                    return  rightController.IsConnected();


                case TrackerType.SeerPad:
                    if (seerPad == null )
                    {
                        return false;
                    }
                    
                    return seerPadController.IsConnected();
            }
            return false;
}

        #endregion


        private void Update()
        {

            if (left != null&&IsConnected(TrackerType.Left))
            {

                left.LateUpdate();
            }
            if (right != null && IsConnected(TrackerType.Right))
            {

                right.LateUpdate();
            }

            if (seerPad != null && IsConnected(TrackerType.SeerPad))
            {

                seerPad.LateUpdate();
            }
        }


    }

    #region
    public enum TrackerType
    {
        //None,
        Left,
        Right,
        SeerPad,
        //Tracker
    }

    public enum JoystickButton
    {
     
        Button_Trigger,//Trigger button
        Button_A,
        Button_B,
        Button_Grip,//Grip button
        Button_Thumbstick,//Thumbstick
    }
    public enum DataSource
    {
        ANDROID_BLE = 1,
        ANDROID_2_4G = 2,
        WINDOWS_2_4G = 3
    }
    public class KeyState
    {

        public DataSource dataSource;
        private JoystickButton button;
        private bool IsPress;
        private KeyCode keyCode;
        private XvHandleController xRJoystick;
        public KeyState(JoystickButton buttonKey, XvHandleController xRJoystick)
        {
            this.button = buttonKey;
            this.xRJoystick = xRJoystick;
        }



        private bool IsTriggerPress;
        private float timer;
        private bool isKeyDown;


        private bool keyDown;
        private bool keyUp;
        private bool key;
        private bool doubleClick;



        public void Update()
        {
            keyDown = false;
            keyUp = false;

            doubleClick = false;



            switch (button)
            {
                case JoystickButton.Button_Trigger:
                    IsPress = xRJoystick.GetJoystickData().keyTrigger;
                    keyCode = KeyCode.Space;
                    break;
                case JoystickButton.Button_A:
                    IsPress = xRJoystick.GetJoystickData().keyA;
                    keyCode = KeyCode.A;

                    break;
                case JoystickButton.Button_B:
                    keyCode = KeyCode.B;

                    IsPress = xRJoystick.GetJoystickData().keyB;

                    break;
                case JoystickButton.Button_Grip:
                    keyCode = KeyCode.G;

                    IsPress = xRJoystick.GetJoystickData().keySlide;

                    break;
                case JoystickButton.Button_Thumbstick:
                    keyCode = KeyCode.H;

                    IsPress = xRJoystick.GetJoystickData().keyRocker;

                    break;
                default:
                    break;
            }
            if (IsPress || Input.GetKey(keyCode))
            {
                if (!IsTriggerPress)
                {
                    keyDown = true;
                    //MyDebugTool.Log("OnTriggerKeyDown:   " + buttonKey);
                    IsTriggerPress = true;

                    if (isKeyDown)
                    {

                        if (timer < 0.2f)
                        {
                            doubleClick = true;
                            //MyDebugTool.Log("OnTriggerDoubleClick:" + buttonKey);

                        }
                        isKeyDown = false;
                        timer = 0;
                    }
                    else
                    {

                        isKeyDown = true;
                    }
                }
                else
                {


                    key = true;
                    // MyDebugTool.Log("OnTriggerKey");

                }

            }
            else
            {
                if (isKeyDown)
                {
                    timer += Time.deltaTime;
                    if (timer > 0.3f)
                    {
                        timer = 0;
                        isKeyDown = false;
                    }
                }
                if (IsTriggerPress)
                {
                    keyUp = true;

                    //MyDebugTool.Log("OnTriggerKeyUp:" + buttonKey);

                    key = false;
                    IsTriggerPress = false;
                }
            }

        }




        public bool GetKeyDown()
        {
            return keyDown;

        }
        public bool GetKeyUp()
        {
            return keyUp;

        }

        public bool GetKey()
        {
            return key;

        }
        public bool GetDoubleClick()
        {
            return doubleClick;

        }


    }
    public class XvXRJoystickKeyState
    {
        public Dictionary<JoystickButton, KeyState> buttonKeyDic = new Dictionary<JoystickButton, KeyState>();
        public XvXRJoystickKeyState(XvHandleController xvXRJoystick)
        {

            buttonKeyDic.Add(JoystickButton.Button_Trigger, new KeyState(JoystickButton.Button_Trigger, xvXRJoystick));
            buttonKeyDic.Add(JoystickButton.Button_A, new KeyState(JoystickButton.Button_A, xvXRJoystick));
            buttonKeyDic.Add(JoystickButton.Button_B, new KeyState(JoystickButton.Button_B, xvXRJoystick));
            buttonKeyDic.Add(JoystickButton.Button_Grip, new KeyState(JoystickButton.Button_Grip, xvXRJoystick));
            buttonKeyDic.Add(JoystickButton.Button_Thumbstick, new KeyState(JoystickButton.Button_Thumbstick, xvXRJoystick));
        }



        public void LateUpdate()
        {
            foreach (var item in buttonKeyDic.Values)
            {
                item.Update();
            }
        }


        public bool GetKeyDown(JoystickButton buttonKey)
        {
            buttonKeyDic.TryGetValue(buttonKey, out KeyState keyState);
            return keyState.GetKeyDown();


        }
        public bool GetKeyUp(JoystickButton buttonKey)
        {
            buttonKeyDic.TryGetValue(buttonKey, out KeyState keyState);
            return keyState.GetKeyUp();

        }

        public bool GetKey(JoystickButton buttonKey)
        {
            buttonKeyDic.TryGetValue(buttonKey, out KeyState keyState);
            return keyState.GetKey();

        }
        public bool GetDoubleClick(JoystickButton buttonKey)
        {
            buttonKeyDic.TryGetValue(buttonKey, out KeyState keyState);
            return keyState.GetDoubleClick();

        }
    }
    #endregion

}
