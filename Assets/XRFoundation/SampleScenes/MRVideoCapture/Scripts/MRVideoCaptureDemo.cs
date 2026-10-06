using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{
    public class MRVideoCaptureDemo : MonoBehaviour
    {
       // [SerializeField]
        private XvMRVideoCaptureManager captureManager;

        public RawImage videoRender;
        void Start()
        {
            if (captureManager==null) {
                captureManager = FindObjectOfType<XvMRVideoCaptureManager>();

                if (captureManager==null) {
                    GameObject newObj = Instantiate(Resources.Load<GameObject>("XvMRVideoCaptureManager"));

                    newObj.name = "XvMRVideoCaptureManager";
                    captureManager = newObj.GetComponent<XvMRVideoCaptureManager>();
                }
            }
        }

        public void StartMRCaptureCamera() {
           
            videoRender.texture = captureManager.CameraRenderTexture;
            captureManager.StartCapture();
        }

        public void StopMRCaptureCamera()
        {
            videoRender.texture = null;
            captureManager.StopCapture();
        }
    }
}
