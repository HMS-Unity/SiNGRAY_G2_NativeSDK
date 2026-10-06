
namespace Singray.Foundation
{
    #region AR glasses camera

    public sealed class XvARCameraManager
    {
        private static XvARCameraManager xvARCameraManager;

        public static XvARCameraManager GetXvARCameraManager() {
            if (xvARCameraManager==null) {
                xvARCameraManager=new XvARCameraManager();
            }
            return xvARCameraManager;
        }

        private XvARCameraManager () { }

        

        private XvCameraBase frameBase;
        private bool isOn;
        public bool IsOn { get { return isOn; } }
        public void StartCapture(XvARCameraParameter xvARCameraParameter)
        {
#if UNITY_EDITOR
            return;
#endif
            if (IsOn)
            {
                return;
            }

            //StopCapture();
            frameBase = new XvARCamera(xvARCameraParameter, FrameArrived);
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
            XvCameraManager.onARCameraStreamFrameArrived?.Invoke(cameraData);
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