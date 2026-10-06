using UnityEngine;
using UnityEngine.UI;
using static Singray.Foundation.XvEyeTrackingManager;
namespace Singray.Foundation.SampleScenes
{
    public class EyeImageDemo : MonoBehaviour
    {
        public RawImage ETleftTex;
        public RawImage ETrightTex;
        public bool autoCapture;

        //[SerializeField]
        private XvEyeTrackingManager xvEyeTrackingManager;

        private void Awake()
        {
            if (xvEyeTrackingManager == null)
            {
                xvEyeTrackingManager = FindObjectOfType<XvEyeTrackingManager>();

                if (xvEyeTrackingManager == null)
                {
                    xvEyeTrackingManager = new GameObject("XvEyeTrackingManager").AddComponent<XvEyeTrackingManager>();
                }
            }
        }

        private void OnEnable()
        {
            if (autoCapture)
            {
                StartCapture();
            }

            XvEyeTrackingManager.onEyeCameraStreamFrameArrived.AddListener(onEyeCameraStreamFrameArrived);
        }
        private void OnDisable()
        {
            XvEyeTrackingManager.onEyeCameraStreamFrameArrived.RemoveListener(onEyeCameraStreamFrameArrived);

        }

        private void onEyeCameraStreamFrameArrived(EyeCameraData eyeCameraData)
        {
            ETleftTex.texture = eyeCameraData.leftTex;
            ETrightTex.texture = eyeCameraData.rightTex;
        }




        private void OnApplicationPause(bool isPause)
        {
            //Triggered when returning to the desktop
            if (isPause)
            {
                bool b = xvEyeTrackingManager.StopCapture();

            }
        }


        public void StartCapture()
        {
            xvEyeTrackingManager.StartGaze();
            xvEyeTrackingManager.StartCapture();
        }

        public void StopCapture()
        {
            xvEyeTrackingManager.StopCapture();
        }
    }
}


