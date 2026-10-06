using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Runtime.InteropServices;
namespace Singray.Engine
{
    [RequireComponent(typeof(Camera))]
    public class XvXRStereoController : MonoBehaviour
    {


        private bool renderedStereo = false;
        private bool isVrMode = true;


        private XvXREye[] eyes;
        private XvXRHeadTracking head;


        public XvXREye[] Eyes
        {
            get
            {

                if (eyes == null)
                {
                    eyes = GetComponentsInChildren<XvXREye>();
                }
                return eyes;
            }
        }

        public XvXRHeadTracking Head
        {
            get
            {

                if (head == null)
                {
                    head = GetComponentInParent<XvXRHeadTracking>();
                }
                return head;
            }
        }


        public void InvalidateEyes()
        {

            eyes = null;
            head = null;

        }


        public void UpdateStereoValues()
        {
            XvXREye[] eyes = Eyes;
            foreach (XvXREye eye in eyes)
            {
                eye.UpdateStereoValues();
            }
        }

        new public Camera camera { get; private set; }

        void Awake()
        {
            camera = GetComponent<Camera>();

        }
        void Start()
        {

            //if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
            //{
            //    GameObject centerObject = new GameObject();
            //    centerObject.name = "CenterCamera";
            //    centerObject.transform.parent = this.gameObject.transform;
            //    Camera centerCamera = centerObject.AddComponent<Camera>();
            //    centerObject.AddComponent<FlareLayer>();
            //    centerCamera.CopyFrom(camera);
            //    XvXREyeMirrorPrivew centereye = centerObject.AddComponent<XvXREyeMirrorPrivew>();
                
            //    if (centereye != null)
            //    {
            //        centereye.eye = XvXRManager.Eye.Center;
            //    }
            //}

        }


        void OnEnable()
        {
            if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
            {

            }
            else
            {
                StartCoroutine("EndOfFrame");
            }
        }

        void OnDisable()
        {
            if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
            {

            }
            else
            {
                StopCoroutine("EndOfFrame");
            }
        }


        void OnPreCull()
        {
            
            foreach (XvXREye eye in Eyes)
            {
                if (isVrMode)
                {
                    eye.camera.enabled = true;
                    eye.SetupStereo();
                }
                else
                {
                    if(eye.eye == XvXRManager.Eye.Left)
                    {
                        eye.camera.enabled = true;
                        eye.CloseVrMode();
                    }
                    else
                    {
                        eye.camera.enabled = false;
                    }
                   
                }
               
            }
            if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
            {

            }
            else
            {
                camera.enabled = false;
                renderedStereo = true;
            }


        }


        IEnumerator EndOfFrame()
        {
            while (true)
            {
                if (renderedStereo)
                {
                    camera.enabled = true;
                    renderedStereo = false;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        internal void OpenVrMode()
        {
            isVrMode = true;
        }

        internal void CloseVrMode()
        {
            isVrMode = false;
        }
    }

}


