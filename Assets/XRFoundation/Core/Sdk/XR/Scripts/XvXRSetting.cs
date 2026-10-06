
#if !UNITY_EDITOR
#if UNITY_ANDROID
#define ANDROID_DEVICE
#elif UNITY_IPHONE
#define IPHONE_DEVICE
#endif
#endif
using UnityEngine;
using System.Collections;
using Singray.Engine;
using Singray.utils;

public class XvXRSetting :MonoBehaviour{
	 static XvXRSetting(){
	
        XvXRLog.LogEnable = false;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        XvXRSdkConfig.XvXR_PLATFORM=XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR;
#elif ANDROID_DEVICE
		XvXRSdkConfig.XvXR_PLATFORM=XvXRSdkConfig.PLATFORM.XvXR_UNITY_ANDROID;
#elif  IPHONE_DEVICE
		XvXRSdkConfig.XvXR_PLATFORM=XvXRSdkConfig.PLATFORM.XvXR_UNITY_IOS; 
#else
    XvXRSdkConfig.XvXR_PLATFORM = XvXRSdkConfig.PLATFORM.XvXR_UNKOWN;
#endif
	}

    void Start() {
       
    }

    void Update()
    {
     
    }

}
