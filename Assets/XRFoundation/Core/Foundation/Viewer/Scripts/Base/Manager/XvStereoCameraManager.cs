
namespace Singray.Foundation
{
    #region Fisheye camera
    public sealed class XvStereoCameraManager
    {
        private static XvStereoCameraManager xvLeftStereoCameraManager;

        private static XvStereoCameraManager xvRightStereoCameraManager;

        public static XvStereoCameraManager GetXvStereoCameraManager(bool left)
        {
            if (left)
            {
                if (xvLeftStereoCameraManager == null)
                {
                    xvLeftStereoCameraManager = new XvStereoCameraManager();
                }
                return xvLeftStereoCameraManager;
            }
            else {
                if (xvRightStereoCameraManager == null)
                {
                    xvRightStereoCameraManager = new XvStereoCameraManager();
                }
                return xvRightStereoCameraManager;
            }
           
        }
        private XvStereoCameraManager() { }


        private XvCameraBase frameBase;
        private bool isOn;
        public bool IsOn { get { return isOn; } }

        private StereoCameraIndex stereoCameraIndex;
        public void StartCapture(XvStereoCameraParameter xvStereoCameraParameter)
        {
#if UNITY_EDITOR
            return;
#endif
            if (IsOn)
            {
                return;
            }
            this.stereoCameraIndex = xvStereoCameraParameter.cameraIndex;

            StopCapture();
            frameBase = new XvStereoCamera(xvStereoCameraParameter ,FrameArrived);
            isOn = true;
            frameBase.StartCapture();

        }

        public void StopCapture()
        {
#if UNITY_EDITOR
            return;
#endif
            if (frameBase != null && frameBase.IsOpen)
            {
                frameBase.StopCapture();
            }

            frameBase = null;
            // GC.Collect();

            isOn = false;
        }



        private void FrameArrived(cameraData cameraData)
        {
            switch (stereoCameraIndex)
            {
                case StereoCameraIndex.LeftEye:
                    XvCameraManager.onLeftStereoStreamFrameArrived?.Invoke(cameraData);

                    break;
                case StereoCameraIndex.RightEye:
                    XvCameraManager.onRightStereoStreamFrameArrived?.Invoke(cameraData);

                    break;
                default:
                    break;
            }
           
        }

        public void Update()
        {
            if (!IsOn)
            {
                return;
            }
#if UNITY_EDITOR
            return;
#endif
            frameBase?.Update();

        }

    }

    #endregion

}