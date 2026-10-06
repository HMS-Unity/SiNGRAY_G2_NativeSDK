using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Singray.utils;

namespace Singray.Foundation
{
    /// <summary>
    /// Streams the first-person mixed-reality view over RTSP for viewing in an RTSP client
    /// RTSP URL: rtsp://{ip}:8554/stream/ ; example: rtsp://192.168.32.98:8554/stream/
    ///      
    /// </summary>
    public sealed class XvRTSPStreamerManager : MonoBehaviour
    {
        private XvRTSPStreamerManager() { }


        [SerializeField]
        private XvMRVideoCaptureManager xvMRVideoCaptureManager;
        public XvMRVideoCaptureManager XvMRVideoCaptureManager
        {

            get
            {

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

        public bool autoStreaming;
        private bool audioStreaming=false;
        private bool isStreeaming;
        public bool IsStreeaming { 
        get { return isStreeaming; } 
        }
        private static int renderInit = 0x2;
        private static int renderDraw = 0x4;



        private const string dllName = "WifiDisplayPlugin";
        [DllImport(dllName)]
        private static extern void SetCameraTextureFromUnity(System.IntPtr texture, int width, int height);

        [DllImport(dllName)]
        private static extern IntPtr GetRenderEventFunc();



        private AndroidJavaObject interfaceObject;


        private RenderTexture wifiCameraRenderTexture = null;

        private AndroidJavaObject InterfaceObject
        {
            get
            {
                if (interfaceObject == null)
                {
                    AndroidJavaClass activityClass = AndroidHelper.GetClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activityObject = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                    if (activityObject != null)
                    {
                        AndroidJavaClass interfaceClass = AndroidHelper.GetClass("com.xv.wifidisply.UnityInterface");

                        interfaceObject = interfaceClass.CallStatic<AndroidJavaObject>("getInstance", new object[] { activityObject });
                    }
                }
                return interfaceObject;
            }
        }


        IEnumerator Start()
        {
           
            if (Application.platform == RuntimePlatform.Android)
            {
                MyDebugTool.LogError("nativeInit start.....");
                AndroidHelper.CallObjectMethod(InterfaceObject, "nativeInit", new object[] { });
                MyDebugTool.LogError("nativeInit end.....");
                CreateTextureAndPassToPlugin();
                yield return StartCoroutine("CallPluginAtEndOfFrames");
            }
        }

        private void OnEnable()
        {
            if (autoStreaming) {
                CancelInvoke();
                Invoke("StartRtspStreaming", 5);
            }
        }

        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.LeftArrow)) { 
        //        API.xslam_switch_audio(true);

        //    }
        //    if (Input.GetKeyDown(KeyCode.RightArrow))
        //    {
        //        API.xslam_switch_audio(false);

        //    }
        //}

        private void OnDisable()
        {
            CancelInvoke();
            StopRtspStreaming();
        }
        private void CreateTextureAndPassToPlugin()
        {
            MyDebugTool.Log("GetRenderEventFunc start.... renderInit");
            wifiCameraRenderTexture = XvMRVideoCaptureManager.CameraRenderTexture;
            MyDebugTool.Log(wifiCameraRenderTexture.width + "     height:" + wifiCameraRenderTexture.height);
            SetCameraTextureFromUnity(wifiCameraRenderTexture.GetNativeTexturePtr(), wifiCameraRenderTexture.width, wifiCameraRenderTexture.height);
     

            GL.IssuePluginEvent(GetRenderEventFunc(), renderInit);
            MyDebugTool.Log("GetRenderEventFunc end.... renderInit");

        }

        private IEnumerator CallPluginAtEndOfFrames()
        {
            while (true)
            {

                // Wait until all frame rendering is done
                yield return new WaitForEndOfFrame();
                //MyDebugTool.Log("GetRenderEventFunc start.... renderDraw");

                GL.IssuePluginEvent(GetRenderEventFunc(), renderDraw);
                //MyDebugTool.Log("GetRenderEventFunc end....renderDraw");
                // yield return new WaitForEndOfFrame ();

            }
        }
      


        /// <summary>
        /// Start streaming
        /// </summary>
        public void StartRtspStreaming()
        {
            if (isStreeaming) {
                MyDebugTool.LogWarning("Streaming is already in progress...");
                return;
            }
            isStreeaming = true;
            XvMRVideoCaptureManager.StartCapture();


            if (Application.platform == RuntimePlatform.Android)
            {

                StartAudioCapture();

                MyDebugTool.LogError("OnPcDisplayClick");
                AndroidHelper.CallObjectMethod(InterfaceObject, "setUseDLNA", new object[] { false });
                MyDebugTool.LogError("setUseDLNA");

                AndroidHelper.CallObjectMethod(InterfaceObject, "tvDisplayClicked", new object[] { });
                MyDebugTool.LogError("tvDisplayClicked");
               // Invoke("StartAudioCapture", 3);
            }
        }

        private bool initAudio;
        /// <summary>
        ///Initialize audio once only
        /// </summary>
        private void StartAudioCapture()
        {
            if (audioStreaming&&!initAudio)
            {
                initAudio = true;
                MyDebugTool.LogError("startAudioCapture");

                if (activityObject == null)
                {
                    InitActivityObject();
                }
                AndroidHelper.CallObjectMethod(activityObject, "startAudioCapture", new object[] { });
                MyDebugTool.LogError("startAudioCapture end");
            }
           
        }
        private static AndroidJavaObject activityObject = null;

        private static void InitActivityObject()
        {
            AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activityObject = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
        }

        /// <summary>
        /// Stop streaming
        /// </summary>
        public void StopRtspStreaming()
        {

            
            if (isStreeaming)
            {

                isStreeaming = false;
                if (Application.platform == RuntimePlatform.Android)
                {
                    AndroidHelper.CallObjectMethod(InterfaceObject, "tvStopClicked", new object[] { });

                }
            }
        }
       



      

        private void OnTvDisplayClick()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                AndroidHelper.CallObjectMethod(InterfaceObject, "setUseDLNA", new object[] { true });// TV streaming mode; confirm Boolean semantics against the native API.
                AndroidHelper.CallObjectMethod(InterfaceObject, "tvDisplayClicked", new object[] { });

            }
        }
    }
}
