using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation
{ 

public class ScreenCanvas : MonoBehaviour
{
        [SerializeField]
        private XvMRVideoCaptureManager captureManager;

        public RawImage videoTexture;

        private void Awake()
        {

            if (videoTexture!=null) {
                videoTexture.gameObject.SetActive(true);
            }
            if (captureManager == null)
            {
                captureManager = FindObjectOfType<XvMRVideoCaptureManager>();

                if (captureManager == null)
                {
                    GameObject newObj = Instantiate(Resources.Load<GameObject>("XvMRVideoCaptureManager"));

                    newObj.name = "XvMRVideoCaptureManager";
                    captureManager = newObj.GetComponent<XvMRVideoCaptureManager>();
                }
            }

        }
        void Start()
        {
            Show();
        }

        public void Show() {

            if (videoTexture!=null) {
                captureManager.StartCapture();
                videoTexture.gameObject.SetActive(true);
                videoTexture.texture = captureManager.CameraRenderTexture;
            }
          
        }

        public void Hide() {
            if (videoTexture != null)
            {
                captureManager.StopCapture(true);
                videoTexture.gameObject.SetActive(false);
            }
        }
    }

}