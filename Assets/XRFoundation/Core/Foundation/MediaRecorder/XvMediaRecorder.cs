using System.Collections;
using UnityEngine;


namespace Singray.Foundation
{
    /// <summary>
    /// Depends on XvMediaRecorderManager
    /// Provides menu controls for local video recording and screenshots
    /// </summary>
    public class XvMediaRecorder : MonoBehaviour
    {
        [SerializeField]
        private XvMediaRecorderManager xvMediaRecorderManager;

        public XvMediaRecorderManager XvMediaRecorderManager { 
        get {
                if (xvMediaRecorderManager == null)
                {
                    xvMediaRecorderManager = FindFirstObjectByType<XvMediaRecorderManager>();
                    if (xvMediaRecorderManager == null)
                    {

                        GameObject newObj = new GameObject("XvMediaRecorderManager");
                        xvMediaRecorderManager = newObj.AddComponent<XvMediaRecorderManager>();

                    }
                }
                return xvMediaRecorderManager;
            }
        }


        [SerializeField]
        private XvCameraManager xvCameraManager;

        public XvCameraManager XvCameraManager
        {
            get
            {
                if (xvCameraManager == null)
                {
                    xvCameraManager = FindFirstObjectByType<XvCameraManager>();
                    if (xvCameraManager == null)
                    {
                        GameObject newObj = new GameObject("XvCameraManager");
                        xvCameraManager = newObj.AddComponent<XvCameraManager>();
                    }
                }
                return xvCameraManager;
            }
        }

        [SerializeField]
        private XvRTSPStreamerManager xvRTSPStreamerManager;

        public XvRTSPStreamerManager XvRTSPStreamerManager
        {
            get
            {
                if (xvRTSPStreamerManager == null)
                {
                    xvRTSPStreamerManager = FindFirstObjectByType<XvRTSPStreamerManager>();
                    if (xvRTSPStreamerManager == null)
                    {

                        GameObject newObj = new GameObject("XvRTSPStreamerManager");
                        xvRTSPStreamerManager = newObj.AddComponent<XvRTSPStreamerManager>();

                    }
                }
                return xvRTSPStreamerManager;
            }
        }

        //[SerializeField]
        private XvMedioRecordTips tips;

        private TextMesh RtspTips;
        private TextMesh VideoRecordTips;
        private TextMesh ScreenshotTips;
        private TextMesh cameraTips;


        private TextMesh RtspStreamingText;
        private TextMesh VideoRecordingText;
        private TextMesh cameraText;

        private void Awake()
        {
            RtspTips = transform.Find("HandMenu/MenuContent/ButtonCollection/RtspStreaming/IconAndText/RtspTips").GetComponent<TextMesh>();
            VideoRecordTips = transform.Find("HandMenu/MenuContent/ButtonCollection/VideoRecording/IconAndText/VideoRecordTips").GetComponent<TextMesh>();
            ScreenshotTips = transform.Find("HandMenu/MenuContent/ButtonCollection/SaveScreenshot/IconAndText/ScreenshotTips").GetComponent<TextMesh>();
            cameraTips = transform.Find("HandMenu/MenuContent/ButtonCollection/Camera/IconAndText/CameraTips").GetComponent<TextMesh>();
            RtspStreamingText = transform.Find("HandMenu/MenuContent/ButtonCollection/RtspStreaming/IconAndText/RtspStreamingText").GetComponent<TextMesh>();
            VideoRecordingText = transform.Find("HandMenu/MenuContent/ButtonCollection/VideoRecording/IconAndText/VideoRecordingText").GetComponent<TextMesh>();
            cameraText = transform.Find("HandMenu/MenuContent/ButtonCollection/Camera/IconAndText/TextMeshPro").GetComponent<TextMesh>();

            tips = transform.Find("HandMenu/MenuContent/XvMedioRecordTips").GetComponent<XvMedioRecordTips>();
        }


        private void OnEnable()
        {

            ///Enable MR video capture before recording
            //XvMediaRecorderManager.StartCapture();
           
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A)) {
                SaveScreenshot();
            }


            if (Time.frameCount%5==0) {
                if (XvCameraManager.IsOn(XvCameraStreamType.ARCameraStream))
                {
                    cameraText.text = "Stop Camera";
                }
                else
                {
                    cameraText.text = "Start Camera";
                }
                if (XvRTSPStreamerManager.IsStreeaming)
                {
                    RtspStreamingText.text = "Stop Streaming";
                }
                else
                {
                    RtspStreamingText.text = "Start Streaming";
                }

                if (XvMediaRecorderManager.IsVideoRecording())
                {
                    VideoRecordingText.text = "Stop Recording";
                }
                else
                {
                    VideoRecordingText.text = "Start Recording";
                }

                if (XvMediaRecorderManager.IsTakingScreenshots() || isScreenshot)
                {
                    //MyDebugTool.LogError("A photo capture is already in progress...");

                    return;
                }

                ScreenshotTips.gameObject.SetActive(XvMediaRecorderManager.IsTakingScreenshots() || isScreenshot);
                RtspTips.gameObject.SetActive(XvRTSPStreamerManager.IsStreeaming);
                cameraTips.gameObject.SetActive(XvCameraManager.IsOn(XvCameraStreamType.ARCameraStream));
                VideoRecordTips.gameObject.SetActive(XvMediaRecorderManager.IsVideoRecording());
            }
            

        }


        private void OnDisable()
        {
            XvMediaRecorderManager.StopCapture(false);
        }

        public void ARCameraStreaming() {
            if (XvCameraManager.IsOn(XvCameraStreamType.ARCameraStream))
            {
                cameraTips.gameObject.SetActive(false);
                XvCameraManager.StopCapture(XvCameraStreamType.ARCameraStream);
                cameraText.text = "Start Camera";
            }
            else {
                cameraTips.gameObject.SetActive(true);
                cameraText.text = "Stop Camera";

                XvCameraManager.StartCapture(XvCameraStreamType.ARCameraStream);
            }
        }
        public void  RtspStreaming() {

            if (XvRTSPStreamerManager.IsStreeaming)
            {
                RtspTips.gameObject.SetActive(false);
                XvRTSPStreamerManager.StopRtspStreaming();
                RtspStreamingText.text = "Start Streaming";
            }
            else {
                RtspTips.gameObject.SetActive(true);
                XvRTSPStreamerManager.StartRtspStreaming();
                RtspStreamingText.text = "Stop Streaming";
            }
            
        }
       
      

        /// <summary>
        /// Start local video recording
        /// </summary>
        public void VideoRecording()
        {
            if (xvMediaRecorderManager.IsVideoRecording())
            {
                VideoRecordTips.gameObject.SetActive(false);

                xvMediaRecorderManager.StopRecording((filePath) =>
                {
                    tips.ShowTips("Saved to: " + filePath, 2);
                   
                });

                VideoRecordingText.text = "Start Recording";
            }
            else {
                
                VideoRecordTips.gameObject.SetActive(true);
                XvMediaRecorderManager.StartRecording();
                VideoRecordingText.text = "Stop Recording";
            }


        }
        //private IEnumerator RelayRecording()
        //{

        //    tips.ShowTips("Preparing to record");

        //    yield return new WaitForSeconds(1);

        //    for (int i = 3; i >= 0; i--)
        //    {
        //        tips.ShowTips(i.ToString());
        //        yield return new WaitForSeconds(0.5f);
        //    }


        //    tips.ShowTips("Recording",1);
        // XvMediaRecorderManager.StartRecording();


        //}

        private bool isScreenshot;
        /// <summary>
        /// Save screenshot
        /// </summary>
        public void SaveScreenshot()
        {
          
            if (XvMediaRecorderManager.IsTakingScreenshots()||isScreenshot)
            {
                MyDebugTool.LogError("A photo capture is already in progress...");
               
                return;
            }
            isScreenshot = true;
            ScreenshotTips.gameObject.SetActive(true);
         
            StartCoroutine(RelayScreenshot());
        }

       
        private IEnumerator RelayScreenshot()
        {

            yield return new WaitForSeconds(1);

            for (int i = 3; i >= 0; i--)
            {
                tips.ShowTips(i.ToString(),200);
                yield return new WaitForSeconds(1f);
            }
            tips.HideTips();
            XvMediaRecorderManager.SaveScreenshot((filePath) => {
                tips.ShowTips("Saved to: " + filePath,1);
                ScreenshotTips.gameObject.SetActive(false);
                isScreenshot = false;
            });
        }

    }
}
