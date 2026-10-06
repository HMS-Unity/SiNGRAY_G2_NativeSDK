using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;
using Singray.utils;
using Assets.XvXRScripts.Engine;
using System.Collections.Generic;

namespace Singray.Engine
{

    public abstract class XvXRBaseDevice  {

		protected const int renderEventId = 0x666666;//render draw event 
		public struct DisplayMetrics{
		    public int width,height;
		    public float xdpi,ydpi;
	    }


		public static int leftCurId = 0;
		public static int rightCurId = 0;
		protected static int distance_mm = 1000;
		protected static int renderEndId = 0;
		protected static int[] LRRId = new int[4];
		protected static int[] CURId = new int[8];
		private static XvXRBaseDevice device=null;

	    protected XvXRBaseDevice(){

	    }

	    public XvXRConfigInfo Info{ get; protected set;}

	    public abstract void Init();
        public abstract void SetStereoScreen(RenderTexture leftRenderTexture, RenderTexture rightRenderTexture);
	    public abstract void SetDistortionCorrectionEnabled(bool enabled);
	    public abstract void UpdateState();
	    public abstract void UpdateScreenData();
	    public abstract void Recenter();
	    public abstract void PostRender();

		public abstract bool GetDeviceState();


		public abstract XvXRConfigInfo.Lenses GetEyeCenter();
         

        public RenderTexture[] rt = new RenderTexture[2];

		protected XvXROpticalParameter_t mParameter;
		protected bool isUseDefaultScreen = true;
		protected bool userDefined = false;

		protected bool openVrRender = true;

		protected RenderTexture leftRenderTexture;
		protected RenderTexture rightRenderTexture;


		protected RenderTexture leftRenderTextureCopy;
		protected RenderTexture rightRenderTextureCopy;


		protected RenderTexture leftRenderTextureCopyAtw;
		protected RenderTexture rightRenderTextureCopyAtw;

        protected RenderTexture leftRenderTextureCopyAtw1;
        protected RenderTexture rightRenderTextureCopyAtw1;

        protected RenderTexture leftRenderTextureCopyAtw2;
        protected RenderTexture rightRenderTextureCopyAtw2;

        internal int renderTextureWidth = 0;

        internal int renderTextureHeight = 0;


		protected Dictionary<RenderTexture, RenderTexture> renderTextureDepthMap = new Dictionary<RenderTexture, RenderTexture>();

		protected RenderTexture[] depthRenderTextures = new RenderTexture[10];

        //Returns landscape orientation display metrics.
        public virtual DisplayMetrics GetDisplayMetrics(){
		    int width = Mathf.Max (Screen.width, Screen.height);
		    int height = Mathf.Min (Screen.width, Screen.height);
		    return new DisplayMetrics{width=width,height=height,xdpi=Screen.dpi,ydpi=Screen.dpi};
	    }

	    public virtual bool SupportsNativeDistortionCorrection() {
		    bool support = true;
		    
		    if (!SupportsUnityRenderEvent()) {
			    support = false;
		    }
		    return support;
	    }

	    public bool SupportsUnityRenderEvent() {
		    bool support = true;
		    if (Application.isMobilePlatform) {
			    try {
				    string version = new Regex(@"(\d+\.\d+)\..*").Replace(Application.unityVersion, "$1");
				    if (new Version(version) < new Version("4.5")) {
					    support = false;
				    }
			    } catch {
				    XvXRLog.InternalXvXRLog("Unable to determine Unity version from: " + Application.unityVersion);
			    }
		    }
		    return support;
	    }


	    public Pose3D GetHeadPose() {
		    return this.headPose;
	    }
	    protected MutablePose3D headPose = new MutablePose3D();
		protected MutablePose3D tempHeadPose = new MutablePose3D();
		protected long posetimestamp = 0;
		protected API.stereo_pdm_calibration fed = default(API.stereo_pdm_calibration);
		protected double[] left_intrinsic;
		protected double[] right_intrinsic;
		public volatile bool isReadFed = false;
		public volatile bool isConnected = false;
		public volatile float CosX = 0.0f;
		public volatile float CosY = 0.0f;

		public void SetFed(API.stereo_pdm_calibration fed_)
        {
			this.fed = fed_;
            this.mParameter.screen_width_pixels = (float)fed.calibrations[0].intrinsic.K[9] * 2;
            this.mParameter.screen_height_pixels = (float)fed.calibrations[0].intrinsic.K[10];
            XvXRLog.LogInfo("mParameter width = " + mParameter.screen_width_pixels + "height" + mParameter.screen_height_pixels);
			CosX = (float)(fed.calibrations[0].intrinsic.K[0] / Math.Sqrt(fed.calibrations[0].intrinsic.K[0]* fed.calibrations[0].intrinsic.K[0] + fed.calibrations[0].intrinsic.K[9]* fed.calibrations[0].intrinsic.K[9] * 0.25));
			CosY = (float)(fed.calibrations[0].intrinsic.K[1] / Math.Sqrt(fed.calibrations[0].intrinsic.K[1] * fed.calibrations[0].intrinsic.K[1] + fed.calibrations[0].intrinsic.K[10] * fed.calibrations[0].intrinsic.K[10] * 0.25));

		}

		public API.stereo_pdm_calibration GetFed()
        {
			return this.fed;
        }

		public void setFedDis(double val)
		{
			double oldDis = (fed.calibrations[1].extrinsic.translation[0] - fed.calibrations[0].extrinsic.translation[0] - val*0.01) *0.5;
			fed.calibrations[0].extrinsic.translation[0] = fed.calibrations[0].extrinsic.translation[0] + oldDis;
			fed.calibrations[1].extrinsic.translation[0] = fed.calibrations[1].extrinsic.translation[0] - oldDis;
		}

		public Pose3D GetEyePose(XvXRManager.Eye eye) {
		    switch(eye) {
		    case XvXRManager.Eye.Left:
			    return leftEyePose;
		    case XvXRManager.Eye.Right:
			    return rightEyePose;
		    default:
			    return null;
		    }
	    }

        internal void UpdateCameraData()
        {
			ComputeEyesFromProfile();

		}

        protected MutablePose3D leftEyePose = new MutablePose3D();
	    protected MutablePose3D rightEyePose = new MutablePose3D();

	    public Matrix4x4 GetProjection(XvXRManager.Eye eye) {
		    switch(eye) {
		    case XvXRManager.Eye.Left:
            case XvXRManager.Eye.Center:
                    return leftEyeProjection;
		    case XvXRManager.Eye.Right:
			    return rightEyeProjection;
		    default:
			    return Matrix4x4.identity;
		    }
	    }

	    protected Matrix4x4 leftEyeProjection;
	    protected Matrix4x4 rightEyeProjection;


	    protected Vector2 recommendedTextureSize;


	    public virtual RenderTexture[] CreateStereoScreen() {
			if(recommendedTextureSize.x == 0)
            {
				return null;
            }
		    float scale = XvXRManager.SDK.StereoScreenScale;
		    int width = Mathf.RoundToInt(recommendedTextureSize.x * scale)/2;
			int height = Mathf.RoundToInt(recommendedTextureSize.y * scale);

			if (width > XvXRSdkConfig.MaxWidthChoose)
			{
				int tempW = XvXRSdkConfig.MaxWidthChoose;
				int tempH = (tempW * height) / width;
				width = tempW;
				height = tempH;
			}else if(width< XvXRSdkConfig.MinWidthChoose){
				int tempW = XvXRSdkConfig.MinWidthChoose;
				int tempH = (tempW * height) / width;
				width = tempW;
				height = tempH;
			}
			height = (int)Math.Min(height, recommendedTextureSize.y);

			//width = 960;
			//height = 960;
			XvXRLog.LogInfo("Creating new 2 screen texture " + width + "x" + height + "." + scale);

			const int colorDepthBits = 24;
			const RenderTextureFormat colorFormat = RenderTextureFormat.RGB565;
			leftRenderTexture = new RenderTexture(width, height, colorDepthBits, colorFormat);
            rightRenderTexture = new RenderTexture(width, height, colorDepthBits, colorFormat);

			leftRenderTextureCopy = new RenderTexture(width, height, colorDepthBits, colorFormat);
			rightRenderTextureCopy = new RenderTexture(width, height, colorDepthBits, colorFormat);

			leftRenderTextureCopyAtw = new RenderTexture(width, height, colorDepthBits, colorFormat);
			rightRenderTextureCopyAtw = new RenderTexture(width, height, colorDepthBits, colorFormat);

			leftRenderTextureCopyAtw1 = new RenderTexture(width, height, colorDepthBits, colorFormat);
			rightRenderTextureCopyAtw1 = new RenderTexture(width, height, colorDepthBits, colorFormat);

			leftRenderTextureCopyAtw2 = new RenderTexture(width, height, colorDepthBits, colorFormat);
			rightRenderTextureCopyAtw2 = new RenderTexture(width, height, colorDepthBits, colorFormat);


            renderTextureHeight = height;
            renderTextureWidth = width;
			// A multisampled GLES depth attachment is commonly a renderbuffer and
			// cannot be sampled as a regular texture. Disable MSAA only while
			// exporting depth so the camera depth attachment can be copied into
			// RFloat; otherwise keep the previous MSAA behavior.
			int aliasingI = XvXRSdkConfig.UseDepth ? 1 : Mathf.Max(QualitySettings.antiAliasing, 8);
            leftRenderTexture.anisoLevel = 2;
		    leftRenderTexture.antiAliasing = aliasingI;
			leftRenderTexture.filterMode = FilterMode.Trilinear;

            rightRenderTexture.anisoLevel = 2;
            rightRenderTexture.antiAliasing = aliasingI;
			rightRenderTexture.filterMode = FilterMode.Trilinear;

			leftRenderTextureCopy.anisoLevel = 2;
			leftRenderTextureCopy.antiAliasing = aliasingI;
			leftRenderTextureCopy.filterMode = FilterMode.Trilinear;

			rightRenderTextureCopy.anisoLevel = 2;
			rightRenderTextureCopy.antiAliasing = aliasingI;
			rightRenderTextureCopy.filterMode = FilterMode.Trilinear;

			leftRenderTextureCopyAtw.anisoLevel = 2;
			leftRenderTextureCopyAtw.antiAliasing = aliasingI;
			leftRenderTextureCopyAtw.filterMode = FilterMode.Trilinear;

			rightRenderTextureCopyAtw.anisoLevel = 2;
			rightRenderTextureCopyAtw.antiAliasing = aliasingI;
			rightRenderTextureCopyAtw.filterMode = FilterMode.Trilinear;

			leftRenderTextureCopyAtw.anisoLevel = 2;
			leftRenderTextureCopyAtw.antiAliasing = aliasingI;
			leftRenderTextureCopyAtw.filterMode = FilterMode.Trilinear;

			rightRenderTextureCopyAtw.anisoLevel = 2;
			rightRenderTextureCopyAtw.antiAliasing = aliasingI;
			rightRenderTextureCopyAtw.filterMode = FilterMode.Trilinear;
			rt[0]=leftRenderTexture;
            rt[1]=rightRenderTexture;

			leftRenderTexture.Create();
			rightRenderTexture.Create();
			leftRenderTextureCopy.Create();
			rightRenderTextureCopy.Create();
			leftRenderTextureCopyAtw.Create();
			rightRenderTextureCopyAtw.Create();
			leftRenderTextureCopyAtw1.Create();
			rightRenderTextureCopyAtw1.Create();
			leftRenderTextureCopyAtw2.Create();
			rightRenderTextureCopyAtw2.Create();

            RenderTexture src1 = RenderTexture.active;
			RenderTexture.active = leftRenderTexture;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = rightRenderTexture;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = leftRenderTextureCopy;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = rightRenderTextureCopy;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = leftRenderTextureCopyAtw;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = rightRenderTextureCopyAtw;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = leftRenderTextureCopyAtw1;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = rightRenderTextureCopyAtw1;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = leftRenderTextureCopyAtw2;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = rightRenderTextureCopyAtw2;
            GL.Clear(false, true, Color.black);
			RenderTexture.active = src1;
			renderTextureDepthMap.Clear();

			for(int i = 0;i<depthRenderTextures.Length; i++)
            {
				if (depthRenderTextures[i] != null)
				{
					depthRenderTextures[i].Release();
					UnityEngine.Object.Destroy(depthRenderTextures[i]);
					depthRenderTextures[i] = null;
				}
			}

			if (XvXRSdkConfig.UseDepth)
			{
				RenderTexture previousActiveTexture = RenderTexture.active;
				for(int i = 0;i<depthRenderTextures.Length; i++)
				{
				// Linear eye-space depth in metres, stored in the R color channel.
				depthRenderTextures[i] = new RenderTexture(
					width,
					height,
					24,
					RenderTextureFormat.RFloat,
					RenderTextureReadWrite.Linear)
				{
					name = "XvXRSharedDepth" + i,
					filterMode = FilterMode.Point,
					wrapMode = TextureWrapMode.Clamp,
					useMipMap = false,
					autoGenerateMips = false,
					antiAliasing = 1
				};
				depthRenderTextures[i].Create();
                RenderTexture.active = depthRenderTextures[i];
                GL.Clear(false, true, Color.black);
				}
				RenderTexture.active = previousActiveTexture;
				renderTextureDepthMap.Add(leftRenderTexture, depthRenderTextures[0]);
				renderTextureDepthMap.Add(rightRenderTexture, depthRenderTextures[1]);
				renderTextureDepthMap.Add(leftRenderTextureCopy, depthRenderTextures[2]);
				renderTextureDepthMap.Add(rightRenderTextureCopy, depthRenderTextures[3]);
				renderTextureDepthMap.Add(leftRenderTextureCopyAtw, depthRenderTextures[4]);
				renderTextureDepthMap.Add(rightRenderTextureCopyAtw, depthRenderTextures[5]);
				renderTextureDepthMap.Add(leftRenderTextureCopyAtw1, depthRenderTextures[6]);
				renderTextureDepthMap.Add(rightRenderTextureCopyAtw1, depthRenderTextures[7]);
				renderTextureDepthMap.Add(leftRenderTextureCopyAtw2, depthRenderTextures[8]);
				renderTextureDepthMap.Add(rightRenderTextureCopyAtw2, depthRenderTextures[9]);
			}

            return rt;
	    }



		// Helper functions.
		/// <summary>
		/// Calculate both eye projection matrices and set leftEyeProjection, rightEyeProjection and recommendedTextureSize
		/// </summary>
		protected virtual void ComputeEyesFromProfile() {

            GameObject obj = GameObject.Find("XvXRCamera");

            float near = 0.3f;
            float far = 1000f;


            if (obj != null) {
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

			//leftEyeProjection = PerspectiveOffCenter(2050.307362166624f, 2049.907613612145f, 667.9349320781658f, 402.2790669535445f, 1920f, 1080f, near, far);

			//rightEyeProjection = PerspectiveOffCenter(2058.227514657356f, 2055.713629861365f, 680.6451850614396f, 289.3983815255807f, 1920f, 1080f, near, far);

			//recommendedTextureSize = new Vector2(3840, 1080);//new Vector2(Info.parameter.screen_width_pixels, Info.parameter.screen_height_pixels);
		 //   XvXRLog.LogInfo("compute from profile width:" + recommendedTextureSize.x + ",height:" + recommendedTextureSize.y);
	    }

		internal static Matrix4x4 PerspectiveOffCenter(float fx, float fy, float u0, float v0,
												  float w, float h, float near, float far)
		{
			float x = 2.0f * fx / w;
			float y = 2.0f * fy / h;
			float a = 1.0f - 2.0f * u0 / w;
			float b = -1.0f + 2.0f * v0 / h;
			float c = -(far + near) / (far - near);
			float d = -(2.0f * far * near) / (far - near);
			float e = -1.0f;
			Matrix4x4 m = new Matrix4x4();
			m[0, 0] = x;
			m[0, 1] = 0;
			m[0, 2] = a;
			m[0, 3] = 0;
			m[1, 0] = 0;
			m[1, 1] = y;
			m[1, 2] = b;
			m[1, 3] = 0;
			m[2, 0] = 0;
			m[2, 1] = 0;
			m[2, 2] = c;
			m[2, 3] = d;
			m[3, 0] = 0;
			m[3, 1] = 0;
			m[3, 2] = e;
			m[3, 3] = 0;
			return m;
		}


		private static Matrix4x4 MakeProjection(float l, float t, float r, float b, float n, float f)
		{
			Matrix4x4 m = Matrix4x4.zero;
			m[0, 0] = 2 * n / (r - l);
			m[1, 1] = 2 * n / (t - b);
			m[0, 2] = (r + l) / (r - l);
			m[1, 2] = (t + b) / (t - b);
			m[2, 2] = (n + f) / (n - f);
			m[2, 3] = 2 * n * f / (n - f);
			m[3, 2] = -1;
			return m;
		}


		public virtual void Destroy() {
		    if (device == this) {
			    device = null;
		    }
	    }
	
	    public static XvXRBaseDevice GetDevice() {
		    if (device == null) {
			    if(XvXRSdkConfig.XvXR_PLATFORM==XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR){
				    device = new XvXRUnityEditorDevice ();
					XvXRLog.InternalXvXRLog("XvXR_UNITY_EDITOR");
			    }else if(XvXRSdkConfig.XvXR_PLATFORM==XvXRSdkConfig.PLATFORM.XvXR_UNITY_ANDROID){
				    device = new XvXRAndroidDevice();
                    XvXRLog.InternalXvXRLog("XvXR_UNITY_ANDROID");
                }
                else
                {
			       throw new InvalidOperationException("Unsupported device.");
			    }
                XvXRLog.LogInfo("sdk version:0.0.8_p1");
		    }
		    return device;
	    }

		public virtual void CloseVrRender()
		{
			openVrRender = false;
		}

		public virtual void OpenVrRender()
		{
			openVrRender = true;
		}

		public virtual float[] GetXvXROpticalParameter()
        {
			return null;
        }

		/// <summary>
		/// Update device parameters: mParameter, isUseDefaultScreen, userDefined
		/// </summary>
		/// <param name="param"></param>
		/// <param name="isUseDefaultScreen"></param>
		internal void SetOpticalParameter(XvXROpticalParameter_t param, bool isUseDefaultScreen)
		{
			
			this.isUseDefaultScreen = isUseDefaultScreen;
			this.mParameter = param;
			this.userDefined = true;
		}

		internal virtual void UpdateDevicePose(Quaternion quaternion, Vector3 postion)
        {
			this.tempHeadPose.Set(postion, quaternion);
;			
        }
		public virtual void UpdateAtwPoseEnable(bool isEnable){
			
		}

		public virtual void UpdateIsSingleTexture(bool isSingleTexture){
			
		}



		public virtual void OnApplicationQuit()
        {

        }

		protected int iR = 0;

		RenderTexture textureId = null;
		public void UpdateTexture(float sumDis, int tCount)
        {
			float dis = 0;
			if(tCount != 0)
			{
				dis = (sumDis / tCount) * 1000.0f;
			}
			CURId[3] = (int)dis;
		}


		public RenderTexture GetTexture(bool isLeftRender)
		{

			iR++;
			//float dis = distance * 1000.0f;
			switch ((iR-1) / 2 % 4)
			{
				case 0:
					if (isLeftRender)
					{
						textureId =  leftRenderTexture;
					}
					else
					{
						textureId =  rightRenderTexture;
					}
					break;
				case 1:
					if (isLeftRender)
					{
						textureId = leftRenderTextureCopy;
					}
					else
					{
						textureId = rightRenderTextureCopy;
					}
					break;
				case 2:
					if (isLeftRender)
					{
						textureId = leftRenderTextureCopyAtw;
					}
					else
					{
						textureId = rightRenderTextureCopyAtw;
					}
					break;

				case 3:
					if (isLeftRender)
					{
						textureId = leftRenderTextureCopyAtw1;
					}
					else
					{
						textureId = rightRenderTextureCopyAtw1;
					}
					break;
				default:
					iR++;
					if (isLeftRender)
					{
						textureId = rightRenderTextureCopyAtw2;
					}
					else
					{
						textureId = rightRenderTextureCopyAtw2;
					}
					break;
			}
			if (isLeftRender)
			{
				CURId[0] = textureId != null ? (int)textureId.GetNativeTexturePtr() : 0;
				CURId[6] = GetNativeDepthTextureId(textureId);
            }
			else
			{
				CURId[1] = textureId != null ? (int)textureId.GetNativeTexturePtr() : 0;
				CURId[7] = GetNativeDepthTextureId(textureId);
            }

			return textureId;
		}

		private int GetNativeDepthTextureId(RenderTexture colorTexture)
		{
			if (!XvXRSdkConfig.UseDepth)
			{
				return 0;
			}

			RenderTexture depthTexture = GetDepthTexture(colorTexture);
			return depthTexture != null && depthTexture.IsCreated()
				? (int)depthTexture.GetNativeTexturePtr()
				: 0;
		}

        public RenderTexture GetDepthTexture(RenderTexture colorTexture)
		{
			if (!XvXRSdkConfig.UseDepth)
			{
				return null;
			}

			RenderTexture depthTexture;
			return colorTexture != null && renderTextureDepthMap.TryGetValue(colorTexture, out depthTexture)
				? depthTexture
				: null;
        }

    }
}
