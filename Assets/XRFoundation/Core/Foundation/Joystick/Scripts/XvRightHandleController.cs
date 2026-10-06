using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singray.MixedReality.Toolkit.XvXR.Input;

namespace Singray.Foundation
{

    public class XvRightHandleController : XvHandleController
    {

        //Controller origin in Unity coordinates
        private Vector3 originPosition = Vector3.zero;
        private Quaternion originRotation = Quaternion.identity;


        //Current controller position in Unity
        private Vector3 currentPosition = Vector3.zero;
        private Quaternion currentRotation = Quaternion.identity;
        public Transform virHandle;
        private MixedRealityControllerVisualizer mixedRealityControllerVisualizer;



        private Matrix4x4 realOrigin;

        private Matrix4x4 virOrigin;
        [SerializeField]
        private JoystickButton joystickButton = JoystickButton.Button_A;

        private bool isLeft=false;
        private void Start()
        {
            cameraTran = Camera.main.transform;
            ResetCenter();
        }

        private void Update()
        {
            if (XvJoystickManager.Instance.useSeerPad)
            {
                return;
            }
            if (XvJoystickManager.Instance.GetKeyDown(joystickButton, TrackerType.Right))
            {
                ResetCenter();
                MyDebugTool.Log("GetKeyDown:B");
            }

            if (XvJoystickManager.Instance.GetKeyDown(JoystickButton.Button_B, TrackerType.Right))
            {

                if (mixedRealityControllerVisualizer==null) {

                    MixedRealityControllerVisualizer[] mixedRealityControllerVisualizers= FindObjectsOfType<MixedRealityControllerVisualizer>();


                    for (int i = 0; i < mixedRealityControllerVisualizers.Length; i++)
                    {
                        if (mixedRealityControllerVisualizers[i].gameObject.name.Contains("joystick R"))
                        {
                            mixedRealityControllerVisualizer = mixedRealityControllerVisualizers[i];

                        }
                        
                    }            

                   
                }

                if (mixedRealityControllerVisualizer!=null) {
                    mixedRealityControllerVisualizer.gameObject.SetActive(false);
                }


                if (XvJoystickManager.Instance.useSeerPad)
                {
                    return;
                }

                Vector3 dir = Vector3.ProjectOnPlane(cameraTran.forward, Vector3.up).normalized;


                Vector3 pos = cameraTran.position + dir * 0.5f - Vector3.up * 0.2f;
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

                virHandle.gameObject.SetActive(true);
                virHandle.position = pos;

                virHandle.rotation = rot;

                MyDebugTool.Log("GetKeyDown:B");
            }
        }

        protected override void UpdatePose()
        {
            if (XvJoystickManager.Instance.useSeerPad)
            {
                return;
            }

          
            JoystickData joystickData = GetJoystickData();

            //Physical controller origin quaternion
            Quaternion realOringinQua = GetQuaternionByMatrix(realOrigin);

            //Physical controller quaternion
            Quaternion realCurrentQua = Quaternion.Euler(joystickData.rotation);


            //Unity origin quaternion
            Quaternion virOringinQua = GetQuaternionByMatrix(virOrigin);


            currentRotation = virOringinQua * (Quaternion.Inverse(realOringinQua) * realCurrentQua);

            currentPosition = virOrigin.GetColumn(3) + virOrigin * (realOrigin.inverse.MultiplyPoint(joystickData.position));
        }


        private Quaternion GetQuaternionByMatrix(Matrix4x4 matrix)
        {
            return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        }
        private Vector3 GetScale(Matrix4x4 matrix)
        {
            return new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude);
        }
        private Vector3 GetPosition(Matrix4x4 matrix)
        {
            return matrix.GetColumn(3);
        }

        private Transform cameraTran;



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
            return XvXRJoystick.Instance.GetJoystickData(isLeft);
        }

        public override bool IsConnected()
        {
            return XvXRJoystick.Instance.IsReady(isLeft);

        }
        public void ResetCenter()
        {
            if (XvJoystickManager.Instance.useSeerPad)
            {
                return;
            }

            Vector3 dir = Vector3.ProjectOnPlane(cameraTran.forward, Vector3.up).normalized;


            Vector3 pos = cameraTran.position + dir * 0.5f - Vector3.up * 0.2f;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);


            SetOrigin(pos, rot);
            JoystickData joystickData = GetJoystickData();


            //Physical controller origin matrix
            realOrigin = Matrix4x4.TRS(joystickData.position, Quaternion.Euler(joystickData.rotation), Vector3.one);


            //Virtual controller origin matrix
            virOrigin = Matrix4x4.TRS(originPosition, originRotation, Vector3.one);

            virHandle.gameObject.SetActive(false);
            if (mixedRealityControllerVisualizer != null)
            {
                mixedRealityControllerVisualizer.gameObject.SetActive(true);
            }
        }



        private void SetOrigin(Vector3 position, Quaternion rotation)
        {
            originPosition = position;
            originRotation = rotation;

        }

        public override int GetBattery()
        {
            return XvXRJoystick.Instance.GetBattery(isLeft);
        }

        public override int GetTemperature()
        {
            return XvXRJoystick.Instance.GetTemperature(isLeft);
        }

        public override API.WirelessControllerDeviceInfo GetDeviceInfo()
        {
            return XvXRJoystick.Instance.GetDeviceInfo(isLeft);
        }

    }
}
