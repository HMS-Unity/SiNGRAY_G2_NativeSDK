using NatSuite.Examples;
using UnityEngine;
using UnityEngine.Events;

namespace Singray.Foundation
{
    /// <summary>
    /// Provides local video recording and screenshots.
    /// </summary>
    [RequireComponent(typeof(ReplayCam))]
    [RequireComponent(typeof(JPG))]

    public sealed class XvMediaRecorderManager : MonoBehaviour
    {
        private XvMediaRecorderManager() { }
        private ReplayCam replayCam;
        private JPG jpg;

        [SerializeField]
        private XvMRVideoCaptureManager xvMRVideoCaptureManager;
        public XvMRVideoCaptureManager XvMRVideoCaptureManager {

            get {

                if (xvMRVideoCaptureManager == null)
                {
                    xvMRVideoCaptureManager = FindObjectOfType<XvMRVideoCaptureManager>();
                }

                if (xvMRVideoCaptureManager == null)
                {
                    GameObject newObj = Instantiate(Resources.Load<GameObject>("XvMRVideoCaptureManager"));
                    xvMRVideoCaptureManager = newObj.GetComponent<XvMRVideoCaptureManager>();
                    newObj.name = "XvMRVideoCaptureManager";
                }

                return xvMRVideoCaptureManager;
            }
        }
     
        public int Width { 
        get { return XvMRVideoCaptureManager.CameraManager.Width; }
        }
       
        public int Height { 
        get { return XvMRVideoCaptureManager.CameraManager.Height; }
        }

        private void Awake()
        {
            replayCam=GetComponent<ReplayCam>();
            jpg=GetComponent<JPG>();
            replayCam.videoWidth = Width;
            replayCam.videoHeight = Height;
            jpg.imageWidth= Width; 
            jpg.imageHeight= Height;
            jpg.cam = XvMRVideoCaptureManager.BgCamera;
            replayCam.cam = XvMRVideoCaptureManager.BgCamera;

        }


        /// <summary>
        /// Start capturing the mixed-reality video stream.
        /// </summary>
        public void StartCapture() {
            XvMRVideoCaptureManager.StartCapture();
        }
        /// <summary>
        /// Stop capturing the mixed-reality video stream.
        /// The camera is shared. Set closeCamera to false if another module still needs it.
        /// </summary>
        /// <param name="closeCamera">True closes the camera; false keeps it open</param>
        public void StopCapture(bool closeCamera=false)
        {
            XvMRVideoCaptureManager.StopCapture(closeCamera);
        }

        /// <summary>
        /// Start recording after StartCapture has started the mixed-reality video stream.
        /// </summary>

        public void StartRecording()
        {
            StartCapture();
            replayCam.StartRecording();
        }

        /// <summary>
        /// True once the RGB camera background has produced at least one real frame
        /// since capture was (re)started. Wait on this before StartRecording() to avoid
        /// recording the placeholder (white) background that shows before the first frame arrives.
        /// </summary>
        public bool IsCameraFrameReady()
        {
            return XvMRVideoCaptureManager.HasFrame;
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        /// <param name="callback"></param>
        public void StopRecording(UnityAction<string> callback)
        {

            replayCam.StopRecording(callback);
        }

        public bool IsVideoRecording() {
            return replayCam.IsRecording;
        }

        /// <summary>
        /// Capture a screenshot after StartCapture has started the mixed-reality video stream.
        /// </summary>
        /// <param name="callback"></param>
        public void SaveScreenshot(UnityAction<string> callback)
        {
            StartCapture();

            jpg.SaveScreenshot(callback);
        }

        public bool IsTakingScreenshots() {
            return jpg.IsRecording;


        }
    }
}
