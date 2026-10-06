using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Singray.Foundation
{
   /// <summary>
   /// Demonstrates camera start/stop controls and video-stream retrieval.
   /// </summary>
    public class XvCameraDemo : MonoBehaviour
    {
        [SerializeField]
        private XvCameraManager cameManager;

        [SerializeField]
        private XvMRVideoCaptureManager captureManager;

        public RawImage arCameraImage;
        public RawImage webCameraImage;
        public RawImage leftStereoCameraImage;
        public RawImage rightStereoCameraImage;
        public RawImage tofCameraImage;

        public RawImage mrVideoImage;

        public RawImage tofIRCameraImage;


        private void Start()
        {
            //Invoke("StartARCamera", 2);
        }


        private void Awake()
        {
            if (cameManager==null) {
                cameManager = FindObjectOfType<XvCameraManager>();

                if (cameManager == null)
                {
                    cameManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
                }
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

        #region Camera lens modes

        private int hidNum = 68;
        /// <summary>
        /// Select the zoom lens mode.
        /// </summary>
        public void setZoom()
        {
            if (API.xslam_ready())
            {
                MyDebugTool.Log("xslam_zoom_lens 1");
                API.xslam_zoom_lens();
                MyDebugTool.Log("xslam_zoom_lens 2");

            }
        }

        /// <summary>
        /// Select the prime lens mode.
        /// </summary>
        public void setPrime()
        {
            if (API.xslam_ready())
            {
                MyDebugTool.Log("xslam_prime_lens 1");

                API.xslam_prime_lens();
                MyDebugTool.Log("xslam_prime_lens 2");

            }
        }



        private void setCameraRgb()
        {
            string s = Convert.ToString((int)hidNum, 16);
            string s0 = "02abdd" + s;

            byte[] hid;// HID command buffer
            hid = new byte[4];
            for (int i = 0; i < hid.Length; i++)
            {
                hid[i] = Convert.ToByte(s0.Substring(i * 2, 2), 16);
            }
            // Send the HID command
            byte[] result;
            result = API.HidWriteAndRead(hid, 4);
            for (int i = 0; i < result.Length; i++)
            {
                Debug.Log($"SendHid result:{result[i]}");
            }

            // txt.text = s.ToString();
        }

        #endregion

        // Update is called once per frame
        void Update()
        {

         
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                setZoom();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                setPrime();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                hidNum += 5;
                setCameraRgb();
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {

                hidNum -= 5;
                setCameraRgb();

            }

            //Vector3 position = Vector3.zero;
            // Vector3 orientation = Vector3.zero;
            //Vector4 quaternion = Vector4.zero;
            //long edgeTimestamp = 0;
            //double hostTimestamp = 0;
            //double confidence = 0;
            //double prediction = 0;

            //if (API.xv_get_6dof_prediction(ref position, ref orientation, ref quaternion, ref edgeTimestamp, ref hostTimestamp, ref confidence, ref prediction))
            //{
            //    MyDebugTool.Log("xv_get_6dof_prediction succeeded " + confidence);

            //}
            //else {
            //    MyDebugTool.Log("xv_get_6dof_prediction failed");
            //}


        }
         
        public void StartARCamera() {
            XvCameraManager.onARCameraStreamFrameArrived.AddListener(onARCameraFrameArrived);
            cameManager.StartCapture(XvCameraStreamType.ARCameraStream);
        }
        public void StopARCamera()
        {
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onARCameraFrameArrived);

            cameManager.StopCapture(XvCameraStreamType.ARCameraStream);

            arCameraImage.texture = null;
        }

        public void StartTofCamera() {
            XvCameraManager.onTofDepthCameraStreamFrameArrived.AddListener(onTofCameraFrameArrived);
            cameManager.StartCapture(XvCameraStreamType.TofDepthCameraStream);

        }
        public void StopTofCamera()
        {
            XvCameraManager.onTofDepthCameraStreamFrameArrived.RemoveListener(onTofCameraFrameArrived);
            cameManager.StopCapture(XvCameraStreamType.TofDepthCameraStream);
            tofCameraImage.texture = null;
        }

        public void StartTofIRCamera()
        {
            XvCameraManager.onTofIRCameraStreamFrameArrived.AddListener(onTofIRCameraFrameArrived);
            cameManager.StartCapture(XvCameraStreamType.TofIRCameraStream);

        }
        public void StopTofIRCamera()
        {
            XvCameraManager.onTofIRCameraStreamFrameArrived.RemoveListener(onTofIRCameraFrameArrived);
            cameManager.StopCapture(XvCameraStreamType.TofIRCameraStream);
            tofIRCameraImage.texture = null;
        }



        public void StartWebCamera()
        {
            XvCameraManager.onWebCameraStreamFrameArrived.AddListener(onWebCameraFrameArrived);

            cameManager.StartCapture(XvCameraStreamType.WebCameraStream);

        }

        public void StopWebCamera() {
            XvCameraManager.onWebCameraStreamFrameArrived.RemoveListener(onWebCameraFrameArrived);

            cameManager.StopCapture(XvCameraStreamType.WebCameraStream);
            webCameraImage.texture = null;

        }

        public void StartLeftStereoCamera() {
            XvCameraManager.onLeftStereoStreamFrameArrived.AddListener(onLeftStereoCameraFrameArrived);

            cameManager.StartCapture(XvCameraStreamType.LeftStereoCameraStream);

        }

        public void StopLeftStereoCamera()
        {
            XvCameraManager.onLeftStereoStreamFrameArrived.RemoveListener(onLeftStereoCameraFrameArrived);

            cameManager.StopCapture(XvCameraStreamType.LeftStereoCameraStream);
            leftStereoCameraImage.texture = null;

        }

        public void StartRightStereoCamera()
        {
            XvCameraManager.onRightStereoStreamFrameArrived.AddListener(onRightStereoCameraFrameArrived);

            cameManager.StartCapture(XvCameraStreamType.RightStereoCameraStream);

        }

        public void StopRightStereoCamera()
        {
            XvCameraManager.onRightStereoStreamFrameArrived.RemoveListener(onRightStereoCameraFrameArrived);

            cameManager.StopCapture(XvCameraStreamType.RightStereoCameraStream);
            rightStereoCameraImage.texture = null;

        }


        public void StartMRCaptureCamera()
        {
            mrVideoImage.texture = captureManager.CameraRenderTexture;
            captureManager.StartCapture();
        }

        public void StopMRCaptureCamera()
        {
            mrVideoImage.texture = null;
            captureManager.StopCapture();
        }
        private void onARCameraFrameArrived(cameraData cameraData)
        {

            if (arCameraImage != null)
            {
                arCameraImage.texture = cameraData.tex;
                //MyDebugTool.Log(string.Format("cameraData.parameter:fx:{0} fy:{1} cx:{2} cy:{3}  k1:{4}  k2:{5}  p1:{6}  p2:{7}  k3:{8}  width:{9} height:{10}", cameraData.parameter. fx, cameraData.parameter.fy, cameraData.parameter.cx, cameraData.parameter.cy, cameraData.parameter.k1,
                //    cameraData.parameter.k2, cameraData.parameter.p1,
                //    cameraData.parameter.p2,cameraData.parameter.k3, cameraData.parameter.width, cameraData.parameter.height));
            }
        }

        private void onWebCameraFrameArrived(cameraData cameraData)
        {

            if (webCameraImage != null)
            {
                webCameraImage.texture = cameraData.tex;
            }
        }
        private void onLeftStereoCameraFrameArrived(cameraData cameraData)
        {

            if (leftStereoCameraImage != null)
            {
                leftStereoCameraImage.texture = cameraData.tex;
            }
        }
        private void onRightStereoCameraFrameArrived(cameraData cameraData)
        {

            if (rightStereoCameraImage != null)
            {
                rightStereoCameraImage.texture = cameraData.tex;
            }
        }
        //ushort[] depthData = new ushort[640 * 480];

        float[] depthData = new float[640 * 480];
        private void onTofCameraFrameArrived(cameraData cameraData)
        {

            if (tofCameraImage != null)
            {
                tofCameraImage.texture = cameraData.tex;

                //MyDebugTool.Log("depthData width height " + cameraData.tex.width + "  " + cameraData.tex.height);

                //if (API.xv_get_tof_image_depth(depthData))
                //{
                //    for (int i = 100; i < 120; i++)
                //    {
                //        for (int j = 100; j < 105; j++)
                //        {
                //            MyDebugTool.Log("Depth data retrieved successfully: " + depthData[j * 640 + i]);
                //        }

                //    }
                //}
                //else
                //{
                //    MyDebugTool.Log("Failed to retrieve depth data");

                //}


            }
        }

        private void onTofIRCameraFrameArrived(cameraData cameraData)
        {

            if (tofIRCameraImage != null)
            {
                tofIRCameraImage.texture = cameraData.tex;
            }
        }
    }
}
