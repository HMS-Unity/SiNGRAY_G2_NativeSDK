
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Singray.SystemEvents;
using System;
using Singray.utils;
using System.Threading;
using Assets.XvXRScripts.Engine;

namespace Singray.Engine
{
    public class XvXRAndroidDevice :XvXRMobileDevice {


    

        Quaternion srcQuaternion = Quaternion.identity;

        double[] mPose = new double[7];
        double[] mPredPose = new double[7];

        // Pose actually consumed by the Unity cameras for the current frame.
        // This snapshot must travel with that frame's RGB/depth textures.  Do
        // not replace it with a newer tracking sample in PostRender.
        readonly double[] renderSourcePose = new double[7];
        long currentPoseTimestamp = 0;
        long renderSourcePoseTimestamp = 0;
        int renderSourceUnityFrame = -1;
        bool currentPoseValid = false;
        bool renderSourcePoseValid = false;

        public override void Init()
	   {

		    Info = new XvXRConfigInfo ();
		    AndroidEvent.Init();
		
            InitRenderEvent();

            if (AndroidConnection.GetVrMode())
            {
                XvXRManager.SDK.EnterVrMode();
            }
            else
            {
                XvXRManager.SDK.LeaveVrMode();
            }

       }

      
     

        void KeepFrameRate()
        {
            //QualitySettings.vSyncCount = 0;
        //    Application.targetFrameRate = 30;

        }

        public override XvXRConfigInfo.Lenses GetEyeCenter()
        {
            return Info.GetEyeCenter();
        }

        /// <summary>
        /// Read display parameters, recalculate FOV and update XvXRConfigInfo.parmeter
        /// mParameter
        /// </summary>
        public override void ReadConfigInfo()
        {
            XvXROpticalParameter_t parameter = new XvXROpticalParameter_t();
            //Use default parameters if userDefined is not set
            //Java onSdkConfigParamterChange() -> SetOpticalParameter() assigns userDefined
            if (!userDefined)
            {
                //iqy xyy
                parameter.fov_left = 40f;
                parameter.fov_right = 40f;
                parameter.fov_top = 40f;
                parameter.fov_bottom = 40f;
                parameter.bottomOffset = 0;
                parameter.separation = 0.063f;
                parameter.screenDistance = 0.045f;
                parameter.renderType = 0;
                parameter.red_coff = new float[16];
                parameter.blue_coff = new float[16];
                parameter.green_coff = new float[16];
                parameter.red_coff[0] = 0f;
                parameter.red_coff[1] = 0f;
                parameter.blue_coff[0] = 0f;
                parameter.blue_coff[1] = 0f;
                parameter.green_coff[0] = 0f;
                parameter.green_coff[1] = 0f;
            }
            else
            {
                parameter = mParameter;
            }


            //Java onSdkConfigParamterChange() -> SetOpticalParameter() assigns isUseDefaultScreen
            if (isUseDefaultScreen)
            {
                //Read display parameters such as physicalWidth, physicalHeight, pixelWidth and pixelHeight
                float[] datas = AndroidEvent.GetXvXRConfigInfo();
                float physicalWidth = datas[0];
                float physicalHeight = datas[1];
                float pixelWidth = datas[2];
                float pixelHeight = datas[3];

             
                parameter.screen_width_physics = physicalWidth;
                parameter.screen_height_physics = physicalHeight;
                parameter.screen_width_pixels = pixelWidth;
                parameter.screen_height_pixels = pixelHeight;
              
            }
            float[] realFov =new float[] { 0f,0f,0f,0f};
            CalculateRealFov(parameter,realFov);
            Info.setParamter(parameter,realFov);
            XvXRLog.LogInfo("data ************ get data:" + parameter.screen_width_physics + "," + parameter.screen_height_physics + "," 
                + parameter.screen_width_pixels + "," + parameter.screen_height_pixels
                +","+realFov[0]+","+realFov[1]+","+realFov[2]+","+realFov[3]);


        } 

	
	public override void UpdateState()
	{

            // headPose.Set(tempHeadPose.Position, tempHeadPose.Orientation);

            // headPose.Orientation = AndroidEvent.GetSensorQuaternion();

            // float [] pose = AndroidEvent.GetPose();
            //Read the latest glasses pose via Java getCurrentPose()

            long currTimestamp = 0;
            long predTimestamp = 0;
            bool poseOk = getCurrentTwoPoseWithTimesResult(
                mPose, mPredPose, ref currTimestamp, ref predTimestamp);

            currentPoseValid = poseOk && IsFinitePose(mPose, mPredPose);
            if (currentPoseValid)
            {
                currentPoseTimestamp = currTimestamp;
                posetimestamp = currTimestamp;
                headPose.Set(
                    new Vector3((float)mPredPose[4], (float)mPredPose[5], (float)mPredPose[6]),
                    new Quaternion((float)mPose[0], (float)mPose[1], (float)mPose[2], (float)mPose[3]));
            }
           // XvXRLog.LogInfo("tss,UpdateState:"+headPose.Orientation);
            ProcessEvents();
  
      
	}

        private static bool IsFinitePose(double[] rotationPose, double[] positionPose)
        {
            if (rotationPose == null || rotationPose.Length < 4 ||
                positionPose == null || positionPose.Length < 7)
            {
                return false;
            }

            double normSquared = 0.0;
            for (int i = 0; i < 4; ++i)
            {
                double value = rotationPose[i];
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return false;
                }
                normSquared += value * value;
            }

            for (int i = 4; i < 7; ++i)
            {
                double value = positionPose[i];
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return false;
                }
            }

            return normSquared > 0.25 && normSquared < 2.25;
        }

        /// <summary>
        /// Freezes the tracking pose that was just applied to the Unity camera
        /// hierarchy. XvXRHeadTracking calls this exactly once per rendered
        /// frame after updating its Transform.
        /// </summary>
        internal void CaptureRenderSourcePose()
        {
            if (!currentPoseValid)
            {
                renderSourcePoseValid = false;
                return;
            }

            renderSourcePose[0] = mPose[0];
            renderSourcePose[1] = mPose[1];
            renderSourcePose[2] = mPose[2];
            renderSourcePose[3] = mPose[3];
            renderSourcePose[4] = mPredPose[4];
            renderSourcePose[5] = mPredPose[5];
            renderSourcePose[6] = mPredPose[6];
            renderSourcePoseTimestamp = currentPoseTimestamp;
            renderSourceUnityFrame = Time.frameCount;
            renderSourcePoseValid = true;
        }

        public override bool GetDeviceState()
        {
            return  getDeviceState();
        }

        public override float[] GetXvXROpticalParameter()
        {
            return AndroidEvent.GetXvXROpticalParameter();
        }


        internal override void InitRenderMobile(int bufferMode)
        {
            AndroidConnection.NativeJniEnvInit(bufferMode);
            GL.IssuePluginEvent(RenderEventFunc(), jniEnvInitEventId);
            InitRender(XvXRSdkConfig.isAberration, XvXRSdkConfig.isReverse, XvXRSdkConfig.vignette);

        }


        // Helper functions.
        /// <summary>
        /// Calculate both eye projection matrices and set leftEyeProjection, rightEyeProjection and recommendedTextureSize
        /// </summary>
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

                recommendedTextureSize = new Vector2((float)left_intrinsic[4]*2, (float)left_intrinsic[5]);//new Vector2(Info.parameter.screen_width_pixels, Info.parameter.screen_height_pixels);
            }
            else
            {

                leftEyeProjection = PerspectiveOffCenter(2050.307362166624f, 2049.907613612145f, 667.9349320781658f, 402.2790669535445f, 1920f, 1080f, near, far);

                rightEyeProjection = PerspectiveOffCenter(2058.227514657356f, 2055.713629861365f, 680.6451850614396f, 289.3983815255807f, 1920f, 1080f, near, far);

                recommendedTextureSize = new Vector2(0, 0);//new Vector2(Info.parameter.screen_width_pixels, Info.parameter.screen_height_pixels);
            }

            MyDebugTool.Log("compute from profile width:" + recommendedTextureSize.x + ",height:" + recommendedTextureSize.y);
        }

        internal override void SetRenderDataMobile()
        {
          
            SetRenderData(Info.parameter);
            SetProjectionMatrixUnity(leftEyeProjection,rightEyeProjection);
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
            //Set the glasses projection matrices through the Java API
            SetProjectionMatrix(leftMatrix,rightMatrix);
        }

        public void SetSrcQuaternionUnity(Quaternion quaternion,long timestamp)
        {
            float[] destQuaternion = new float[] { quaternion .x, quaternion .y, quaternion .z,quaternion.w};
            //XvXRLog.InternalXvXRLogError("SetSrcQuaternionUnity:" + timestamp);
            SetSrcQuaternion(destQuaternion, timestamp);

        }


        public override void CloseVrRender()
        {
            openVrRender = false;
            SetVrMode(false);
            AndroidConnection.SetVrMode(false);
        }

        public override void OpenVrRender()
        {
            openVrRender = true;
            SetVrMode(true);
            AndroidConnection.SetVrMode(true);
        }

        internal override void SetTextureIdMobile()
        {
            SetTextureId(lastLeftId, lastRightId, renderTextureWidth, renderTextureHeight);
            LRRId[0] = 0;
            LRRId[1] = 0;
            LRRId[2] = 0;
            CURId[0] = 0;
            CURId[1] = 0;
            CURId[2] = 0;
            CURId[3] = 0;
            CURId[4] = 0;
            CURId[5] = 0;
            CURId[6] = 0;
            CURId[7] = 0;

            int lastLeftCopyId = leftRenderTextureCopy != null ? (int)leftRenderTextureCopy.GetNativeTexturePtr() : 0;
            int lastRightCopyId = rightRenderTextureCopy != null ? (int)rightRenderTextureCopy.GetNativeTexturePtr() : 0;
            int lastLeftCopyIdAtw = leftRenderTextureCopyAtw != null ? (int)leftRenderTextureCopyAtw.GetNativeTexturePtr() : 0;
            int lastRightCopyIdAtw = rightRenderTextureCopyAtw != null ? (int)rightRenderTextureCopyAtw.GetNativeTexturePtr() : 0;
            int lastLeftCopyIdAtw1 = leftRenderTextureCopyAtw1 != null ? (int)leftRenderTextureCopyAtw1.GetNativeTexturePtr() : 0;
            int lastRightCopyIdAtw1 = rightRenderTextureCopyAtw1 != null ? (int)rightRenderTextureCopyAtw1.GetNativeTexturePtr() : 0;
            int lastLeftCopyIdAtw2 = leftRenderTextureCopyAtw2 != null ? (int)leftRenderTextureCopyAtw2.GetNativeTexturePtr() : 0;
            int lastRightCopyIdAtw2 = rightRenderTextureCopyAtw2 != null ? (int)rightRenderTextureCopyAtw2.GetNativeTexturePtr() : 0;
            //     SetCopyTextureId(lastLeftCopyId, lastRightCopyId, lastLeftCopyIdAtw, lastRightCopyIdAtw);
            SetCopyTextureIds(lastLeftCopyId, lastRightCopyId, lastLeftCopyIdAtw, lastRightCopyIdAtw, lastLeftCopyIdAtw1, lastRightCopyIdAtw1, lastLeftCopyIdAtw2, lastRightCopyIdAtw2);

        }

        internal override IntPtr RenderEventFunc()
        {
            return GetRenderEventFunc();
        }
        internal override void UpdateDevicePose(Quaternion quaternion, Vector3 postion)
        {
            this.tempHeadPose.Set(postion, quaternion);
            AndroidJNI.AttachCurrentThread();
            AndroidEvent.UpdateDevicePose(quaternion, postion);
        }

        public override void  UpdateAtwPoseEnable(bool isEnable){
            SetAtwPoseEnable(isEnable);
        }

        public override void UpdateIsSingleTexture(bool isSingleTexture){
			SetIsSingleTexture(isSingleTexture);
		}


      

        protected const string dllName = "XvXRRenderPlugin";

        [DllImport(dllName)]
        private static extern void InitRender(int isAberration, int isReverse, int isVignette);

        [DllImport(dllName)]
        private static extern void SetRenderData(XvXROpticalParameter_t renderParams);

        [DllImport(dllName)]
        private static extern void CalculateRealFov(XvXROpticalParameter_t renderParams, float[] realFov);

        [DllImport(dllName)]
        private static extern void SetProjectionMatrix(float[] leftMatrix, float[] rightMatrix);

        [DllImport(dllName)]
        private static extern void SetSrcQuaternion(float[] quaternion, long timestamp);


        [DllImport(dllName)]
        private static extern void SetTextureId(int leftTextureId, int rightTextureId, int width, int height);

        [DllImport(dllName)]
        private static extern void SetCopyTextureId(int leftTextureId, int rightTextureId, int leftTextureIdAtw, int rightTextureIdAtw);

        [DllImport(dllName)]
        private static extern void SetCopyTextureIds(int leftTextureId, int rightTextureId, int leftTextureIdAtw, int rightTextureIdAtw, int leftTextureIdAtw1, int rightTextureIdAtw1, int leftTextureIdAtw2, int rightTextureIdAtw2);
        [DllImport(dllName)]
        private static extern void SetVrMode(bool isVrMode);

        [DllImport(dllName)]
        private static extern IntPtr GetRenderEventFunc();


        [DllImport(dllName)]
        public static extern bool readStereoDisplayCalibration(ref API.stereo_pdm_calibration calib);

        [DllImport(dllName)]
        public static extern bool updateCalibra(double distance);

        [DllImport(dllName)]
        public static extern bool getCurrentPose([In, Out] double[] pose);


        [DllImport(dllName)]
        public static extern bool getCurrentTwoPose([In, Out] double[] currPose, [In, Out] double[] predPose);

        [DllImport(dllName)]
        public static extern bool getCurrentTwoPoseWithTimesResult([In, Out] double[] currPose, [In, Out] double[] predPose, ref long currPoseTimestamp, ref long predPoseTimestamp);

        [DllImport(dllName)]
        public static extern bool getDeviceState();

        [DllImport(dllName)]
        public static extern void SetFrame(int Frame);

        [DllImport(dllName)]
        public static extern void SetUForecast(float Forecast);


        [DllImport(dllName)]
        private static extern void SubmitFrame(
            int unityFrameNumber,
            int leftColorTexture,
            int rightColorTexture,
            int leftDepthTexture,
            int rightDepthTexture,
            int auxTexture,
            int canvasDistanceMm,
            [In] double[] sourcePose,
            long sourcePoseTimestamp,
            int sourcePoseValid);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private delegate void RenderCallback(int code);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public delegate void CallGetQuaternion();

        [DllImport(dllName)]
        private static extern void SetAtwPoseEnable(bool isEnable);

        [DllImport(dllName)]
        private static extern void SetIsSingleTexture(bool isSingleTexture);

        [DllImport(dllName)]
        private static extern void SetUseHalfFrameRender(bool useHalfFrameRender);


        public static void RenderPluginCallback(int eventId)
        {
            XvXRLog.InternalXvXRLog("RenderPluginCallback:" + eventId);
        


        }

        internal void ChangeStatus()
        {
            //Call Java SetSrcQuaternion() to provide the source pose for reprojection
            SetSrcQuaternionUnity(headPose.Orientation, posetimestamp);
        }

        public override void PostRender()
        {
            if (openVrRender)
            {
                int canvasDistance = CURId[3] > 0 ? CURId[3] : distance_mm;
                int leftDepthTexture = XvXRSdkConfig.UseDepth ? CURId[6] : 0;
                int rightDepthTexture = XvXRSdkConfig.UseDepth ? CURId[7] : 0;
                bool poseMatchesFrame = renderSourcePoseValid &&
                                        renderSourceUnityFrame == Time.frameCount;
                SubmitFrame(
                    Time.frameCount,
                    CURId[0], CURId[1],
                    leftDepthTexture, rightDepthTexture,
                    CURId[5], canvasDistance,
                    renderSourcePose,
                    renderSourcePoseTimestamp,
                    poseMatchesFrame ? 1 : 0);
                GL.IssuePluginEvent(RenderEventFunc(), renderEventId);
                GL.InvalidateState();
            }
          
        }
     
    
	
	public override void Destroy() {
		
		base.Destroy();
	}
	



	//private Queue<int> eventQueue = new Queue<int>();

	//public Queue<int> EventQueue{get {return eventQueue;} }
	
	//private int[] events = new int[4];
	
	protected virtual void ProcessEvents() {
	
		//int num = 0;
		//lock (eventQueue) {
		//	num = eventQueue.Count;
		//	if (num == 0) {
		//		return;
		//	}
		//	if (num > events.Length) {
		//		events = new int[num];
		//	}
		//	eventQueue.CopyTo(events, 0);
		//	eventQueue.Clear();
		//}
		
	}


       
    }

}
