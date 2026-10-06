using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Singray.utils;

public class WifiDisplayPluginWrapper : MonoBehaviour
{

    private static int renderInit = 0x2;
    private static int renderDraw = 0x4;

    public  int videoWidth = 1920;
    public  int videoHeight = 1080;

    public GameObject pcStartBtn;
    public GameObject tvStartBtn;

    protected const string dllName = "WifiDisplayPlugin";
    [DllImport (dllName)]
    private static extern void SetCameraTextureFromUnity (System.IntPtr texture,int width,int height);

    [DllImport (dllName)]
    private static extern IntPtr GetRenderEventFunc ();



    	private AndroidJavaObject interfaceObject;

        private Camera wifiCamera;

        private RenderTexture wifiCameraRenderTexture = null;

        /// <summary>
        /// Read-only accessor exposing the Wi-Fi camera RenderTexture for external consumers, such as LiveKit streaming
        /// Null until nativeInit and CreateTextureAndPassToPlugin finish initializing the plugin
        /// </summary>
        public RenderTexture WifiCameraTexture => wifiCameraRenderTexture;

		private AndroidJavaObject InterfaceObject{
			get{
				if(interfaceObject==null){
					AndroidJavaClass activityClass =  AndroidHelper.GetClass("com.unity3d.player.UnityPlayer");
					AndroidJavaObject activityObject= activityClass.GetStatic<AndroidJavaObject>("currentActivity");
					if(activityObject!=null){
                        AndroidJavaClass interfaceClass = AndroidHelper.GetClass("com.xv.wifidisply.UnityInterface");
                        
						interfaceObject=interfaceClass.CallStatic<AndroidJavaObject>("getInstance", new object[]{activityObject});
					}
				}
				return interfaceObject;
			}
		}

    IEnumerator Start () {
        wifiCamera = GetComponent<Camera>();
        if (Application.platform == RuntimePlatform.Android) {
            Singray.utils.XvXRLog.LogError("nativeInit start.....");
            AndroidHelper.CallObjectMethod(InterfaceObject,"nativeInit",new object[]{});
            Singray.utils.XvXRLog.LogError("nativeInit end.....");
            CreateTextureAndPassToPlugin ();
            yield return StartCoroutine ("CallPluginAtEndOfFrames");
        }
    }

    private void CreateTextureAndPassToPlugin () {

       wifiCameraRenderTexture = new  RenderTexture(videoWidth, videoHeight, 24,RenderTextureFormat.RGB565);
        

        if(wifiCamera!=null){
            wifiCamera.targetTexture  = wifiCameraRenderTexture;
        }
        SetCameraTextureFromUnity (wifiCameraRenderTexture.GetNativeTexturePtr (),wifiCameraRenderTexture.width,wifiCameraRenderTexture.height );
        GL.IssuePluginEvent (GetRenderEventFunc (), renderInit);
    }

    private IEnumerator CallPluginAtEndOfFrames () {
        while (true) {
            // Wait until all frame rendering is done
            yield return new WaitForEndOfFrame ();
            GL.IssuePluginEvent (GetRenderEventFunc (), renderDraw);
           // yield return new WaitForEndOfFrame ();

        }
    }

    public void OnPcDisplayClick()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidHelper.CallObjectMethod(InterfaceObject, "setUseDLNA", new object[] { false });// PC streaming mode; supplier comment disagreed with the Boolean value. Confirm against the native API.
            AndroidHelper.CallObjectMethod(InterfaceObject, "tvDisplayClicked", new object[] { });
            
            if (pcStartBtn != null)
                pcStartBtn.SetActive(false);

            if (tvStartBtn != null)
                tvStartBtn.SetActive(true);
        }
    }

    public void OnTvDisplayClick(){
         if (Application.platform == RuntimePlatform.Android) {
            AndroidHelper.CallObjectMethod(InterfaceObject, "setUseDLNA", new object[] { true });// TV streaming mode; supplier comment disagreed with the Boolean value. Confirm against the native API.
            AndroidHelper.CallObjectMethod(InterfaceObject,"tvDisplayClicked",new object[]{});
            if (pcStartBtn != null)
                pcStartBtn.SetActive(false);

            if (tvStartBtn != null)
                tvStartBtn.SetActive(true);
        }
    }

    public void OnTvStopClick(){
        if (Application.platform == RuntimePlatform.Android) {
            AndroidHelper.CallObjectMethod(InterfaceObject,"tvStopClicked",new object[]{});
            if (pcStartBtn != null)
                pcStartBtn.SetActive(true);

            if (tvStartBtn != null)
                tvStartBtn.SetActive(true);
        }
    }

   
}
