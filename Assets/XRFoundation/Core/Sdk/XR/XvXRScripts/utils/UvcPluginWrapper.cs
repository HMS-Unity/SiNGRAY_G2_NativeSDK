using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using Singray.UI.Keyboard;
using Singray.utils;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static UnityEngine.UI.InputField;

public class UvcPluginWrapper : MonoBehaviour, ICustomInputField
{

    private static int renderInit = 0x22;
    private static int renderDraw = 0x24;



    protected const string dllName = "TwoDShowPlugin";


    [DllImport(dllName)]
    private static extern IntPtr GetRenderEventFunc();



    private AndroidJavaObject interfaceObject;

    private Texture2D playPlaneTexture = null;

    public GameObject playGameObject = null;

    public int videoWidth = 1280;
    public int videoHeight = 1080;



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
                    AndroidJavaClass interfaceClass = AndroidHelper.GetClass("com.xv.twodshowplugin.UnityInterface");

                    interfaceObject = interfaceClass.CallStatic<AndroidJavaObject>("getInstance", new object[] { activityObject });
                }
            }
            return interfaceObject;
        }
    }

  

    private void Awake()
    {
        Invoke("startThird2DApp", 6);
    }


    IEnumerator Start()
    {
        Debug.Log($"UvcPluginWrapper start");

        if (Application.platform == RuntimePlatform.Android)
        {

            AndroidHelper.CallObjectMethod(InterfaceObject, "nativeInit", new object[] { });
            CreateTextureToPlayStream();
            GL.IssuePluginEvent(GetRenderEventFunc(), renderInit);
            yield return StartCoroutine("CallPluginAtEndOfFrames");
        }
        else
        {
            CreateTextureToPlayStream();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            scrollScreen(0);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            scrollScreen(1);
        }



        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            stopThird2DApp();
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            stopThird2DApp();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            stopThird2DApp();
        }


        if (Input.GetKeyDown(KeyCode.C))
        {
            onSoftInputMethodStatus("true");
        }


        if (Input.GetKeyDown(KeyCode.V))
        {
            onSoftInputMethodStatus("false");
        }

    }

    private AndroidJavaObject InitActivityObject()
    {
        AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        return activityClass.GetStatic<AndroidJavaObject>("currentActivity");
    }

   
    public void startThird2DApp()
    {
        MyDebugTool.Log("startThird2DApp:call Start Douyin");
      //  AndroidHelper.CallObjectMethod(InitActivityObject(), "startThird2DApp", new object[] { "com.xv.hwar", "com.xv.hwar.ui.login.LoginActivity" });
        AndroidHelper.CallObjectMethod(InitActivityObject(), "startThird2DApp", new object[] { "com.ss.android.ugc.aweme", "com.ss.android.ugc.aweme.splash.SplashActivity" });

        MyDebugTool.Log("startThird2DApp:call complete");

    }

    public void stopThird2DApp()
    {
        try
        {
            MyDebugTool.Log("stopThird2DApp:call Close Douyin");
        //    AndroidHelper.CallObjectMethod(InitActivityObject(), "stopThird2DApp", new object[] { "com.xv.hwar" });
             AndroidHelper.CallObjectMethod(InitActivityObject(), "stopThird2DApp", new object[] { "com.ss.android.ugc.aweme" });
        }
        catch (Exception ex)
        {
            MyDebugTool.LogError("stopThird2DApp:call Error closing Douyin: " + ex.Message);
        }
    }

    public void sendRayCastPoint(int type, float x, float y)
    {
        MyDebugTool.Log("stopThird2DApp:call sendRayCastPoint" + type + " " + x + " " + y);
        AndroidHelper.CallObjectMethod(InitActivityObject(), "sendRayCastPoint", new object[] { type, x, y });
    }

    public void scrollScreen(int dir)
    {
        try
        {
            MyDebugTool.Log("scrollScreen:" + dir);
            AndroidHelper.CallObjectMethod(InitActivityObject(), "scrollScreen", new object[] { dir });
        }
        catch (Exception ex)
        {
            MyDebugTool.LogError("stopThird2DApp:call Error closing Douyin: " + ex.Message);
        }
    }
    public string getThirdInputText()

    {
        string str = "";

        try
        {
           
            AndroidHelper.CallObjectMethod<string>(ref  str,InitActivityObject(), "getThirdInputText");
        }
        catch (Exception ex)
        {
            MyDebugTool.LogError("stopThird2DApp:call Error closing Douyin: " + ex.Message);
        }

        return str;
    }
    public void scrollScreen(int type, float startX, float startY, float endX, float endY)
    {
        
        try
        {
           
            AndroidHelper.CallObjectMethod(InitActivityObject(), "scrollScreen", new object[] { type, startX, startY , endX , endY });
        }
        catch (Exception ex)
        {
            MyDebugTool.LogError("stopThird2DApp:call Error closing Douyin: " + ex.Message);
        }
    }

   
    private void OnDisable()
    {
        stopThird2DApp();
    }

    private void CreateTextureToPlayStream()
    {
        if (playPlaneTexture == null)
        {
            // playPlaneTexture = new Texture2D(videoWidth, videoHeight);


            playPlaneTexture = new Texture2D(videoWidth, videoHeight, TextureFormat.RGBA32, false);


            playPlaneTexture.Apply();

        }
        if (playGameObject != null)
        {
            playGameObject.GetComponent<MeshRenderer>().material.mainTexture = playPlaneTexture;
        }
        SetCameraTexture();

    }

    private IEnumerator CallPluginAtEndOfFrames()
    {
        while (true)
        {
            // Wait until all frame rendering is done
            yield return new WaitForEndOfFrame();
            GL.IssuePluginEvent(GetRenderEventFunc(), renderDraw);

            Debug.Log($"UvcPluginWrapper CallPluginAtEndOfFrames");
        }
    }



    private void SetCameraTexture()
    {

        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidHelper.CallObjectMethod(InterfaceObject, "setPlayStreamTexture", new object[] { (int)playPlaneTexture.GetNativeTexturePtr(), videoWidth, videoHeight });
        }
        Debug.Log($"UvcPluginWrapper SetCameraTexture");
    }


    #region Keyboard input API

    public void sendKeyCode(string content)
    {
      
        if (Application.platform == RuntimePlatform.Android)
        {
            MyDebugTool.Log("Input field content sendContent:" + content);

            AndroidHelper.CallObjectMethod(InitActivityObject(), "sendThirdAppKeyCode", new object[] { content });
        }
    }

  
    public void onSoftInputMethodStatus(string showStatus)
    {
        MyDebugTool.Log("showStatus：" + showStatus);

        if (showStatus == "true")
        {
            MRKeyboard.Instance.Show();
            MRKeyboard.Instance.SetInputField(this, Pose.identity, ContentType.Standard);
        }
        else
        {
       
            MRKeyboard.Instance.Hide();
        }
    }
    public void SetText(string v)
    {
       
       sendKeyCode(v);

    }

    public void SetCaretActive(bool isActive)
    {
       
    }

    public void OnHideEvent()
    {

    }

    public void InputKeyCode(string keycode)
    {
    }

   
    public string text
    {
        get
        {
            string str = getThirdInputText();

            MyDebugTool.Log("Input field content: "+ str);

            return str;
        }
       
    }

    #endregion
}
