
using System;
using UnityEngine;

namespace Singray.Foundation
{
    [Serializable]

    public class XvCameraParameterSetting
    { 
    
     }

    public class XvCameraBase
    {

        protected cameraData cameraData = new cameraData();
        protected bool isOpen;
        protected XvCameraParameterSetting xvCameraParameterSetting;
    
        public delegate void FrameArrived(cameraData cameraData);
        public FrameArrived frameArrived;


        public XvCameraBase(XvCameraParameterSetting xvCameraParameterSetting, FrameArrived frameArrived)
        {
            this.xvCameraParameterSetting = xvCameraParameterSetting;
            this.frameArrived = frameArrived;
        }
        public virtual void StartCapture()
        {
            isOpen = true;
        }

        public virtual void StopCapture()
        {
            isOpen = false;
        }

        public virtual void Update()
        {

        }

        public bool IsOpen
        {
            get
            {
                return isOpen;
            }

        }
    }

}