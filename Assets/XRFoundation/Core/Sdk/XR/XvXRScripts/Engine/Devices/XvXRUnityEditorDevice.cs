
using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

using System.Text;
using Singray.utils;
using System.Collections.Generic;

namespace Singray.Engine
{

    public class XvXRUnityEditorDevice : XvXRBaseDevice
    {
        protected const int initRenderTextureId = 0x766667;
        protected IntPtr lastLeftId = IntPtr.Zero;

        protected IntPtr lastRightId = IntPtr.Zero;

        double[] mPose = new double[7];

        private float[] datasAll = {6.2f,4f,3.525f,50f,50f,50f,50f,
                 0.05637f,0.02742f,0.05637f,0.02742f,0.05637f,0.02742f};


        float[] quaternionData = new float[4] { 0,0,0,0};


        protected Texture2D leftRenderTexture2D;
        protected Texture2D rightRenderTexture2D;

        public override void OnApplicationQuit()
        {
#if !UNITY_EDITOR
            TerminatePlugin();
#endif
        }
        public override void Init()
        {
            //QualitySettings.vSyncCount = 0;
            //Input.gyro.enabled = true;
           
            Info = new XvXRConfigInfo();
         
            if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE&& XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
            {
                KeepFrameRate();
            }
            

            //Screen.SetResolution(640, 480, false);
           // Application.runInBackground = true;
            callbackDelegate = PluginCallbackFunc;
            try
            {
                RegisterPluginCallbackDelegate(callbackDelegate);
            }
            catch
            {

            }
           
            
            onSdkSwitchVrMode(true);
        }
    
        public override DisplayMetrics GetDisplayMetrics()
        {
            int[] size = new int[2] { Screen.width,Screen.height};
          
            int width = size[0];
            int height = size[1];

            return new DisplayMetrics { width = width, height = height, xdpi = Screen.dpi, ydpi = Screen.dpi };
        }

        public override XvXRConfigInfo.Lenses GetEyeCenter()
        {
            return Info.GetEyeCenter();
        }


        public void ReadConfigInfo()
        {

           
        }

        public override void SetStereoScreen(RenderTexture leftRenderTexture, RenderTexture rightRenderTexture) {

            lastLeftId = leftRenderTexture != null ? leftRenderTexture.GetNativeTexturePtr() : IntPtr.Zero;
            lastRightId = rightRenderTexture != null ? rightRenderTexture.GetNativeTexturePtr() : IntPtr.Zero;
            
            try
            {
                SetTextureId(lastLeftId, lastRightId, renderTextureWidth, renderTextureHeight);
                if (leftRenderTexture2D != null && rightRenderTexture2D != null)
                {
                    SetCopyTextureId(leftRenderTexture2D.GetNativeTexturePtr(), rightRenderTexture2D.GetNativeTexturePtr());
                }
                //GL.IssuePluginEvent(RenderEventFunc(), initRenderTextureId);
                //GL.InvalidateState();

            }
            catch
            {

            }

           

        }
        public override void SetDistortionCorrectionEnabled(bool enabled) { }


        private Quaternion initialRotation = Quaternion.identity;

        public override RenderTexture[] CreateStereoScreen()
        {
#if UNITY_EDITOR_WIN
            return null;
#endif
            float scale = XvXRManager.SDK.StereoScreenScale;
            int width = Mathf.RoundToInt(recommendedTextureSize.x * scale) / 2;
            int height = Mathf.RoundToInt(recommendedTextureSize.y * scale);


          

            XvXRLog.LogInfo("Creating new 2 screen texture " + width + "x" + height + "." + scale);


            leftRenderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
            rightRenderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);

            leftRenderTexture.filterMode = FilterMode.Point;
            rightRenderTexture.filterMode = FilterMode.Point;

  

            leftRenderTexture.Create();
            rightRenderTexture.Create();

            rt[0] = leftRenderTexture;
            rt[1] = rightRenderTexture;

            leftRenderTexture2D = new Texture2D(width, height, TextureFormat.ARGB32,false);
            rightRenderTexture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);

            leftRenderTexture2D.filterMode = FilterMode.Point;
            rightRenderTexture2D.filterMode = FilterMode.Point;

            leftRenderTexture2D.Apply();
            rightRenderTexture2D.Apply();

            RenderTexture src = RenderTexture.active;
            RenderTexture.active = leftRenderTexture;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = rightRenderTexture;
            GL.Clear(false, true, Color.black);
            
            RenderTexture.active = src;
            return rt;
        }

        private bool remoteCommunicating = false;
        private bool RemoteCommunicating
        {
            get
            {
                if (!remoteCommunicating)
                {
                    remoteCommunicating = Vector3.Dot(Input.gyro.rotationRate, Input.gyro.rotationRate) > 0.05;
                }
                return remoteCommunicating;
            }
        }

        //bool isReady = false;

        //bool lastReady = false;
        void KeepFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        public override void UpdateState()
        {
            KeepFrameRate();

            try
            {
                GetCurrentPose(mPose);

                headPose.Set(new Vector3((float)mPose[4], (float)mPose[5], (float)mPose[6]), new Quaternion((float)mPose[0], (float)mPose[1], (float)mPose[2], (float)mPose[3]));

            }
            catch
            {

            }

            ProcessEvents();

        }

        private void SetProjectionMatrixUnity(Matrix4x4 leftProjection, Matrix4x4 rightProjection)
        {
            float[] leftMatrix = new float[16];
            float[] rightMatrix = new float[16];
            for (int i = 0; i < 16; i++)
            {
                leftMatrix[i] = leftProjection[i];
                rightMatrix[i] = rightProjection[i];
            }
            try
            {
                SetProjectionMatrix(leftMatrix, rightMatrix);
            }
            catch
            {

            }
            
        }

        private void SetSrcQuaternionUnity(Quaternion quaternion, long timestamp)
        {
            float[] destQuaternion = new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
            try
            {
                SetSrcQuaternion(destQuaternion, timestamp);
            }
            catch
            {

            }
           

        }

        public override bool GetDeviceState()
        {
            return true;
        }

        internal void ChangeStatus()
        {
            SetSrcQuaternionUnity(headPose.Orientation, posetimestamp);
        }
        public override void PostRender()
        {
            if (openVrRender)
            {
                GL.IssuePluginEvent(RenderEventFunc(), renderEventId);
               // GL.InvalidateState();
            }
        }

        
        


        public override void UpdateScreenData()
        {
            ReadConfigInfo();
            ComputeEyesFromProfile();
            SetProjectionMatrixUnity(leftEyeProjection, rightEyeProjection);
        }


        protected override void ComputeEyesFromProfile()
        {
            GameObject obj = GameObject.Find("XvXRCamera");

            float near = 0.3f;
            float far = 1000f;


            if (obj != null)
            {
                near = obj.GetComponent<Camera>().nearClipPlane;
                far = obj.GetComponent<Camera>().farClipPlane;
            }

            if (isReadFed)
            {
                left_intrinsic = new double[6] { fed.calibrations[0].intrinsic.K[0], fed.calibrations[0].intrinsic.K[1], fed.calibrations[0].intrinsic.K[2], fed.calibrations[0].intrinsic.K[3], fed.calibrations[0].intrinsic.K[9], fed.calibrations[0].intrinsic.K[10] };

                right_intrinsic = new double[6] { fed.calibrations[1].intrinsic.K[0], fed.calibrations[1].intrinsic.K[1], fed.calibrations[1].intrinsic.K[2], fed.calibrations[1].intrinsic.K[3], fed.calibrations[1].intrinsic.K[9], fed.calibrations[1].intrinsic.K[10] };

                leftEyeProjection = PerspectiveOffCenter((float)left_intrinsic[0], (float)left_intrinsic[1], (float)left_intrinsic[2], (float)left_intrinsic[3], (float)left_intrinsic[4], (float)left_intrinsic[5], near, far);

                rightEyeProjection = PerspectiveOffCenter((float)right_intrinsic[0], (float)right_intrinsic[1], (float)right_intrinsic[2], (float)right_intrinsic[3], (float)right_intrinsic[4], (float)right_intrinsic[5], near, far);

                recommendedTextureSize = new Vector2((float)left_intrinsic[4] * 2, (float)left_intrinsic[5]);//new Vector2(Info.parameter.screen_width_pixels, Info.parameter.screen_height_pixels);
            }
            else
            {

                leftEyeProjection = PerspectiveOffCenter(2050.307362166624f, 2049.907613612145f, 667.9349320781658f, 402.2790669535445f, 1920f, 1080f, near, far);

                rightEyeProjection = PerspectiveOffCenter(2058.227514657356f, 2055.713629861365f, 680.6451850614396f, 289.3983815255807f, 1920f, 1080f, near, far);

                recommendedTextureSize = new Vector2(0, 0);//new Vector2(Info.parameter.screen_width_pixels, Info.parameter.screen_height_pixels);
            }
            XvXRLog.LogInfo("compute from profile width:" + recommendedTextureSize.x + ",height:" + recommendedTextureSize.y);
        }

        public override void Recenter()
        {

        }


        //

        public const string dllName = "XvXRRenderPlugin";


        internal  IntPtr RenderEventFunc()
        {
            return GetRenderEventFunc();
        }

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetRenderEventFunc();

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetTextureId(IntPtr leftTextureId, IntPtr rightTextureId, int width, int height);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetCopyTextureId(IntPtr leftTextureId, IntPtr rightTextureId);



        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCurrentPose([In, Out] double[] pose);


        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetSrcQuaternion(float[] quaternion, long timestamp);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetProjectionMatrix(float[] leftMatrix, float[] rightMatrix);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ReadStereoDisplayCalibration(ref API.stereo_pdm_calibration calib);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterPluginCallbackDelegate(PluginCallbackDelegate callbackDelegate);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TerminatePlugin();

        public delegate void PluginCallbackDelegate(int eventId, bool param);

        private static Queue<int> eventQueue = new Queue<int>();

        public static Queue<int> EventQueue { get { return eventQueue; } }

        public const int EventDeviceConnected = 0;
        public const int EventDeviceDisconnected = 1;
        static void PluginCallbackFunc(int eventId,bool param)
        {
            switch (eventId)
            {
                case 0:
                    if (param)
                    {
                        EventQueue.Enqueue(EventDeviceConnected);
                    }
                    else
                    {
                        EventQueue.Enqueue(EventDeviceDisconnected);
                    }
                    break;
            }
        }


        static void onSdkDeviceStatusChanged(bool isConnect)
        {
            if (isConnect)
            {
                XvDeviceManager.Manager.onDeviceConnectChanged(true);
            }
            else
            {
                XvDeviceManager.Manager.onDeviceConnectChanged(false);
            }
        }

        static void onSdkSwitchVrMode(bool isVrMode)
        {

            if (isVrMode)
            {
                XvXRManager.SDK.EnterVrMode();
            }
            else
            {
                XvXRManager.SDK.LeaveVrMode();
            }

        }

        static void onSdkReCenter()
        {
            Singray.utils.XvXRLog.InternalXvXRLog("onSdkReCenter");
            XvXRManager.SDK.OnReCenterClick();
        }

        static void onSdkConfigParamterChange()
        {
            Singray.utils.XvXRLog.InternalXvXRLog("onSdkConfigParamterChange");

            XvXRManager.SDK.onSdkConfigParamterChange();
        }

        static PluginCallbackDelegate callbackDelegate;



       
       

        private int[] events = new int[4];

        protected virtual void ProcessEvents()
        {

            int num = 0;
            lock (eventQueue)
            {
                num = eventQueue.Count;
                if (num == 0)
                {
                    return;
                }
                if (num > events.Length)
                {
                    events = new int[num];
                }
                eventQueue.CopyTo(events, 0);
                eventQueue.Clear();
            }
            for (int i = 0; i < num; i++)
            {
                XvXRLog.LogInfo("code ****" + events[i]);
                switch (events[i])
                {
                    case EventDeviceConnected:
                        onSdkDeviceStatusChanged(true); 
                        break;
                    case EventDeviceDisconnected:
                        onSdkDeviceStatusChanged(false);
                        break;
                    default:
                        break;
                }
            }
        }



    }





}
