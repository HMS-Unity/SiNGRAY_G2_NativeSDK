using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singray.Engine;
using Singray.MixedReality.Toolkit.XvXR.Input;

namespace Singray.Foundation
{

    public class XvSeerPadController : XvHandleController
    {

      
        private Quaternion originRotation = Quaternion.identity;


        //Current controller position in Unity
        private Vector3 currentPosition = Vector3.zero;
        private Quaternion currentRotation = Quaternion.identity;



        [SerializeField]
        private JoystickButton resetKeyButton = JoystickButton.Button_A;
        API.stereo_pdm_calibration fed;
        [HideInInspector]
        public float offsetX=0;


        protected  void Awake()
        {

            Invoke("ResetCenter",2);
            //ResetCenter();
        }

        private void Update()
        {
            if (!XvJoystickManager.Instance.useSeerPad)
            {
                return;
            }
            if (XvJoystickManager.Instance.GetKeyDown(resetKeyButton, TrackerType.SeerPad))
            {
                ResetCenter();
                MyDebugTool.Log("GetKeyDown:B");
            }
        }

        protected override void UpdatePose()
        {
            if (!XvJoystickManager.Instance.useSeerPad)
            {
                return;
            }
            JoystickData joystickData = GetJoystickData();


            currentRotation = originRotation * joystickData.quaternion;


            currentPosition = new Vector3(XvXRManager.SDK.HeadPose.Position.x + offsetX, XvXRManager.SDK.HeadPose.Position.y - 0.25f, XvXRManager.SDK.HeadPose.Position.z);


          
        }

        public override Vector3 GetPosition()
        {
            return currentPosition;

        }
        public override Quaternion GetRotation()
        {
            return currentRotation;
        }

        public override JoystickData GetJoystickData()
        {
            return XvSeerPadJoystick.Instance.GetJoystickData();
        }

        public override bool IsConnected()
        {
            return XvSeerPadJoystick.Instance.IsConnected();

        }
        public void ResetCenter()
        {
          
            JoystickData joystickData = GetJoystickData();

            Quaternion rot = XvXRManager.SDK.HeadPose.Orientation* Quaternion.Inverse(joystickData.quaternion);
            rot = Quaternion.Euler(new Vector3(0, rot.eulerAngles.y, 0));

           

            SetOrigin( rot);
#if UNITY_EDITOR
            return;
#endif
            if (API.xslam_ready() && XvXRManager.SDK.GetDevice().isConnected)
            {
                bool b = XvXRAndroidDevice.readStereoDisplayCalibration(ref fed);
                //Debug.Log($"readStereoDisplayCalibration:{b}");
                offsetX = (float)fed.calibrations[0].extrinsic.translation[0] + (Mathf.Abs((float)fed.calibrations[0].extrinsic.translation[0]) + Mathf.Abs((float)fed.calibrations[1].extrinsic.translation[0])) / 2;

            }

        }



        private void SetOrigin( Quaternion rotation)
        {
            originRotation = rotation;

        }

      

    }
}




