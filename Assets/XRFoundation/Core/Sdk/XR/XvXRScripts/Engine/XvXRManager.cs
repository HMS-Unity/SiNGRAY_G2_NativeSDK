
﻿using UnityEngine;
using System.Collections;
using System;
using Singray.Foundation;
using Singray.utils;
using Assets.XvXRScripts.Engine;

namespace Singray.Engine
{

public class XvXRManager : MonoBehaviour {

	public static XvXRManager SDK {
	
		get {
			if (sdk == null) {
				sdk = UnityEngine.Object.FindObjectOfType<XvXRManager> ();
			}
			if (sdk == null) {
				XvXRLog.LogInfo("Creating vrmanager object");
				var go = new GameObject ("XvXRManager");
				sdk = go.AddComponent<XvXRManager> ();
				go.transform.localPosition = Vector3.zero;
			}
			return sdk;
		}
	}

      

    private static XvXRManager sdk = null ;



	private static Camera currentMainCamera;
	private static XvXRStereoController currentController;

	private static XvXRBaseDevice device;

  

	public  XvXRBaseDevice GetDevice(){
		return device;
	}

	public bool NativeDistortionCorrectionSupported { get; private set; }

	internal void OnCameraPreCull()
	{
		
	}

		public static XvXRStereoController Controler{
		get{
			Camera camera=Camera.main;
			XvXRLog.LogInfo("maincamera:"+camera);
			if ((camera!=currentMainCamera||currentController==null)&&camera!=null){
				XvXRLog.LogInfo("GetComponent");
				currentMainCamera=camera;
				currentController=camera.GetComponent<XvXRStereoController>();
			}
			return currentController;
		}
	}

	public enum DistortionCorrectionMethod{
		None,
		Native,
	}

	

	public enum Eye{
		Left,Right,Center,
	}

	public enum Distortion {
		Distorted,   // Viewing through the lenses
		Undistorted  // No lenses
	}



	void Awake() {

		XvXRLog.InternalXvXRLog("~~~~~~~~~~~~~~~~~~~~~~~~Awake()");
		
		if (sdk == null) {
		sdk = this;
		}
		if (sdk != this) {

			enabled = false;
			return;
		}
		StereoScreen = null;
		// Prevent the screen from dimming / sleeping
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
        InitDevice();		
      
        ZeroQuaternion = Quaternion.identity;
	}
		/// <summary>
		/// Read glasses optical parameters into device.mParameter via the Java SDK GetXvXROpticalParameter() call
		/// Android callback chain: xv -> onDeviceAttach() -> cpp::setXvXROpticalParameterChange() -> Unity -> onSdkConfigParamterChange
		/// --->updateopticalparameter()---->updatescreendata()
		/// </summary>
		internal void onSdkConfigParamterChange()
        {
			//get the params from java sdk
			float[] configParams = device.GetXvXROpticalParameter();
            if (configParams != null&&configParams.Length>=14)
            {
				XvXROpticalParameter_t parameter = new XvXROpticalParameter_t();
			
				
				parameter.fov_left = configParams[0];
				parameter.fov_right = configParams[1];
				parameter.fov_top = configParams[2];
				parameter.fov_bottom = configParams[3];

				parameter.screen_width_physics = configParams[4];
				parameter.screen_height_physics = configParams[5];
				parameter.screen_width_pixels = configParams[6];
				parameter.screen_height_pixels = configParams[7];

				parameter.bottomOffset = configParams[8];
				parameter.separation = configParams[9];
				parameter.screenDistance = configParams[10];
				parameter.renderType = (int)configParams[11];

				parameter.red_coff = new float[16];
				parameter.blue_coff = new float[16];
				parameter.green_coff = new float[16];
				parameter.red_coff[0] = configParams[12];
				parameter.red_coff[1] = configParams[13];
				parameter.blue_coff[0] = configParams[12];
				parameter.blue_coff[1] = configParams[13];
				parameter.green_coff[0] = configParams[12];
				parameter.green_coff[1] = configParams[13];
				//Update device.mParameter
				UpdateOpticalParameter(parameter, false);


            }
            else
            {
				XvXRLog.InternalXvXRLog("configParams is not contect:"+((configParams == null)?"null":configParams.Length.ToString()));

			}
        }

        internal void OnReCenterClick()
        {
			ReCenterOnClick?.Invoke();

		}

		void Start()
		{
			XvXRLog.InternalXvXRLog("~~~~~~~~~~~~~~~~~~~~~~~~Start()");
			if (device == null)
			{
				XvXRLog.InternalXvXRLog("~~~~~~~~~~~~~~~~~~~~~~~~Start(),InitDevice()");
				InitDevice();
			}
			ReCenter();
			VersionInfo.Print();
		}


    [HideInInspector]
	public bool autoUntiltHead = true;

	[HideInInspector]
	public bool UseUnityRemoteInput = false;
 
	public DistortionCorrectionMethod DistortionCorrection {
		get {
			return distortionCorrection;
		}
		set {
			if (value != distortionCorrection && device != null) {
				device.SetDistortionCorrectionEnabled(value == DistortionCorrectionMethod.Native
				                                      && NativeDistortionCorrectionSupported);
				device.UpdateScreenData();
			}
			distortionCorrection = value;
		}
	}
	[SerializeField]
	private DistortionCorrectionMethod distortionCorrection = DistortionCorrectionMethod.Native;

	public float StereoScreenScale {
		get {
			return stereoScreenScale;
		}
		set {
			value = Mathf.Clamp(value, 0.1f, 1.0f);  // Sanity.
			if (stereoScreenScale != value) {
				stereoScreenScale = value;
				StereoScreen = null;
			}
		}
	}
	[SerializeField]
	[Range(0,1)]
	private float stereoScreenScale = 1;
	

	public RenderTexture[] StereoScreen {
		get {
				
				// Don't need it except for distortion correction.
			if (distortionCorrection == DistortionCorrectionMethod.None) {
				return null;
			}
				
				if (stereoScreen == null) {
					// Create on demand.
					//XvXRLog.InternalXvXRLog("getStereoScreen .");
					stereoScreen = device.CreateStereoScreen();  // Note: uses set{}

					if (device != null && stereoScreen !=null)
					{
					    if( Singray.Engine.XvXRManager.SDK.IsSingleTexture){
							device.SetStereoScreen(stereoScreen[0], stereoScreen[0]);
							device.UpdateAtwPoseEnable(false);
							device.UpdateIsSingleTexture(true);
						
						}else{
							device.SetStereoScreen(stereoScreen[0], stereoScreen[1]);
						}
						

					}

				}
				return stereoScreen;
		}
		set {
				if (value == null)
				{
					return;
				}
				if (value == stereoScreen) {
                XvXRLog.InternalXvXRLog("set the same steroscreen.");
				return;
			}
			
               
			stereoScreen = value;
            XvXRLog.InternalXvXRLog("set the steroscreen.");
			if (device != null) {
                XvXRLog.InternalXvXRLog("set device steroscreen.");
			  if( Singray.Engine.XvXRManager.SDK.IsSingleTexture){
					device.SetStereoScreen(stereoScreen[0], stereoScreen[0]);
					device.UpdateAtwPoseEnable(false);
					device.UpdateIsSingleTexture(true);
				
				}else{
					device.SetStereoScreen(stereoScreen[0], stereoScreen[1]);
				}
						
			}
			if (OnStereoScreenChanged != null) {
				OnStereoScreenChanged(stereoScreen);
			}
         
		}
	}


	private static RenderTexture[] stereoScreen = null;
	
	/// A callback for notifications that the StereoScreen property has changed.
	public delegate void StereoScreenChangeDelegate(RenderTexture[] newStereoScreen);
	
	/// Occurs when StereoScreen has changed.
	public event StereoScreenChangeDelegate OnStereoScreenChanged;

	public bool ConfiginfoChanged { get; private set; }

	public XvXRConfigInfo Info{
		get{
			return device.Info;
		}
	}

	/// The transformation of head from origin in the tracking system.
	public Pose3D HeadPose {
		get {
			return device.GetHeadPose();
		}
	}


	
	/// The transformation from head to eye.
	public Pose3D EyePose(Eye eye) {
		return device.GetEyePose(eye);
	}


	public Matrix4x4 Projection(Eye eye) {
		return device.GetProjection(eye);
	}


	private bool updated = false;

	public void UpdateState() {
      if (!updated) {
			updated = true;
         //   XvXRLog.InternalXvXRLog("UpdateState vrmanager");
			device.UpdateState();
			
		}
	}




	public void PostRender(){
		if (device != null) {
			device.PostRender();
		}
	}


		private void InitDevice()
		{

			if (device != null)
			{
				device.Destroy();
			}

			
			device = XvXRBaseDevice.GetDevice();

			//if (device.GetDeviceState() == false)
			//{
			//    yield return new WaitForSeconds(5);
			//}


            {
				device.Init();

				NativeDistortionCorrectionSupported = device.SupportsNativeDistortionCorrection();
				//XvXRLog.LogInfo("surport" + NativeDistortionCorrectionSupported);
				XvXRLog.InternalXvXRLog("InitDevice init log");
				device.UpdateScreenData();
			}
			//else
			//{
			//	device.Destroy();
			//	device = null;
			//}

		}

    public static float zeroy = 0.0f;

    public void ReCenter() {
        zeroy = -XvXRManager.SDK.HeadPose.Orientation.eulerAngles.y;
    }
    void Update() {
			if (device == null)
			{
				XvXRLog.InternalXvXRLog("~~~~~~~~~~~~~~~~~~~~~~~~Update(),InitDevice()");
				InitDevice();
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
			
			}

        /*z recenter*/
        if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR) {
            if (Input.GetKeyDown(KeyCode.Z)&& XvXRSdkConfig.isTurnOnZForReCenter)
            {
                    ReCenter();
            }
        }

        Vector3 axisY = Quaternion.Inverse(XvXRManager.SDK.HeadPose.Orientation) * new Vector3(0, 1, 0);
        Quaternion zero = Quaternion.AngleAxis(zeroy, axisY);
        XvXRManager.SDK.ZeroQuaternion = zero;
    }


	public Quaternion ZeroQuaternion{ get; set;}




		void OnEnable()
		{
			//  XvXRLog.InternalXvXRLog("vrmanager onenable");
			if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
			{
				XvXRLog.InternalXvXRLog("~~~~~~~~~~~~~~~~~~~~~~~~OnEnable()");
				if (device == null)
				{
					InitDevice();
				}
			}
            if (device == null)
            {
                XvXRLog.InternalXvXRLog("~~~~~~~~~~~~~~~~~~~~~~~~OnEnable(),InitDevice()");
                InitDevice();
            }
            StartCoroutine("EndOfFrame");

		}
	
	void OnDisable() {
		StopCoroutine("EndOfFrame");
      
        }

	IEnumerator EndOfFrame() {
		while (true) {
			yield return new WaitForEndOfFrame();
			UpdateState();  // Just in case it hasn't happened by now.
			updated = false;
				if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_ANDROID && !IsUseUserPose)
				{
					((XvXRAndroidDevice)GetDevice()).ChangeStatus();
				}
				else if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
				{
					((XvXRUnityEditorDevice)GetDevice()).ChangeStatus();
				}
           // XvXRLog.InternalXvXRLog("vrmanager endofframe");
		}
	}

 

    public void UpdateConfigInfo() {
        device.UpdateScreenData();
    }

		
		public delegate void VrModeChangeDelegate(bool isVrMode);

		///  when vrmode has changed.
		public event VrModeChangeDelegate OnVrModeChangeDelegate;


		public delegate void OnReCenterClickDelegate();

		///  when vrmode has changed.
		public event OnReCenterClickDelegate ReCenterOnClick;


		public XvXRConfigInfo.Lenses GetEyeCenter()
    {
        return device.GetEyeCenter();
    }

	private bool isVrMode = true;

    public bool IsVRMode { get { return isVrMode; } }
	public void EnterVrMode()
	{
		
			XvXRLog.LogInfo("EnterVrMode");
			isVrMode = true;
			Controler.OpenVrMode();
			device.OpenVrRender();
			OnVrModeChangeDelegate?.Invoke(true);
	}
	public void LeaveVrMode()
	{
			
			XvXRLog.LogInfo("LeaveVrMode");
			isVrMode = false;
			Controler.CloseVrMode();
			device.CloseVrRender();
			OnVrModeChangeDelegate?.Invoke(false);
		}
		double lastChangeTime = 0;
	internal void SwitchVRMode()
		{
			double currentTime = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
			if (currentTime - lastChangeTime > 1.5)
			{
				if (isVrMode)
				{
					LeaveVrMode();
				}
				else
				{
					EnterVrMode();
				}
				lastChangeTime = currentTime;
			}
			
		}

		public void UpdateOpticalParameter(XvXROpticalParameter_t param, bool isUseDefaultScreen)
	{
			XvXRLog.InternalXvXRLog("UpdateOpticalParameter");
			if (param.red_coff != null && param.blue_coff != null && param.green_coff != null && param.red_coff.Length == 16 && param.green_coff.Length == 16 && param.blue_coff.Length == 16)
			{
				//Update device parameters: mParameter, isUseDefaultScreen, userDefined
				device.SetOpticalParameter(param, isUseDefaultScreen);
				device.UpdateScreenData();

			}
		
	}

		public void UpdateDevicePose(Quaternion quaternion, Vector3 postion)
        {
			device.UpdateDevicePose(quaternion, postion);
        }

		void OnApplicationQuit()
		{
			XvXRLog.InternalXvXRLog("OnApplicationQuit");
			device.OnApplicationQuit();
		}

		public static void SetUpdateTexture(float sumDis, int tCount)
		{
			device.UpdateTexture(sumDis, tCount);
		}

		public static RenderTexture GetTexture(bool IsLeftEye)
		{
			return device.GetTexture(IsLeftEye);
		}

        internal static RenderTexture GetDepthTexture(RenderTexture colorRenderTexture)
        {
            return device.GetDepthTexture(colorRenderTexture);
        }

        public bool IsSingleTexture = false;

		public bool IsUseUserPose = false;

	}

}
