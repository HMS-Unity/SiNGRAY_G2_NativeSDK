using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Microsoft.MixedReality.Toolkit.Utilities;
using Singray.Engine;
using TMPro;
using Singray.Foundation;


public class GetEyeImage : MonoBehaviour
{
    public XvEyeFlowManage xvEyeFlowManage;

    public static bool isReady;
    private Texture2D lefttex = null;
    private Texture2D righttex = null;
    private Color32[] pixel32_Left;
    private Color32[] pixel32_Right;
    private GCHandle pixelHandle_Left;
    private GCHandle pixelHandle_Right;
    private IntPtr pixelPtr_Left;
    private IntPtr pixelPtr_Right;
    [HideInInspector]
    public static bool isInit;
    [HideInInspector]
    public static bool isGetEyeImage = false;

    bool createLeftSprite;
    bool createRightSprite;

    Sprite ETleftSprite;
    Sprite ETrightSprite;

    int width;
    int height;


    public void OpenEyeImage()
    {
        bool b = XvEyeTracking.xv_eyetracking_start();
        MyDebugTool.Log($"xv_eyetracking_start {b}");

        isGetEyeImage = true;
    }

    public void CloseEyeImage()
    {
        isGetEyeImage = false;
        bool b = XvEyeTracking.xv_eyetracking_stop();
        MyDebugTool.Log($"xv_eyetracking_stop:{b}");
    }

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {       


        Invoke("OpenEyeImage", 2f);
    }

    public void SaveEyeImage()
    {

        File.WriteAllBytes($"/sdcard/{SceneManager.GetActiveScene().name}lefttex.png", lefttex.EncodeToPNG());
        File.WriteAllBytes($"/sdcard/{SceneManager.GetActiveScene().name}righttex.png", righttex.EncodeToPNG());
    }
    


    // Update is called once per frame
    void Update()
    {
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                //if (keyCode.ToString() == "LeftArrow")
                //{
                //    Debug.Log("Left");
                //    OpenEyeImage();
                //}
                //if (keyCode.ToString() == "RightArrow")
                //{
                //    Debug.Log("Right");
                //    CloseEyeImage();

                //}

            }
        }


        //Render eye-tracking images
        if (API.xslam_ready() && isGetEyeImage)
        {


            if (XvEyeTracking.xv_eyetracking_get_rgba(pixelPtr_Left, pixelPtr_Right,ref width,ref height))
            {
                

                if (width != 0 && height != 0)
                {
                    TextureFormat format = TextureFormat.RGBA32;

                    if (!lefttex)
                    {
                        lefttex = new Texture2D(width, height, format, false);
                        pixel32_Left = lefttex.GetPixels32();
                        pixelHandle_Left = GCHandle.Alloc(pixel32_Left, GCHandleType.Pinned);
                        pixelPtr_Left = pixelHandle_Left.AddrOfPinnedObject();
                    }
                    if (!righttex)
                    {
                        righttex = new Texture2D(width, height, format, false);
                        pixel32_Right = righttex.GetPixels32();
                        pixelHandle_Right = GCHandle.Alloc(pixel32_Right, GCHandleType.Pinned);
                        pixelPtr_Right = pixelHandle_Right.AddrOfPinnedObject();
                    }

                    //Left-eye image
                    lefttex.SetPixels32(pixel32_Left);
                    lefttex.Apply();
                    if (!createLeftSprite)
                    {
                        ETleftSprite = Sprite.Create(lefttex, new Rect(0.0f, 0.0f, lefttex.width, lefttex.height), new Vector2(0.5f, 0.5f));
                        createLeftSprite = true;
                    }

                    xvEyeFlowManage.LefteyeImage.GetComponent<Image>().sprite = ETleftSprite;

                    //Right-eye image
                    righttex.SetPixels32(pixel32_Right);
                    righttex.Apply();
                    if (!createRightSprite)
                    {
                        ETrightSprite = Sprite.Create(righttex, new Rect(0.0f, 0.0f, righttex.width, righttex.height), new Vector2(0.5f, 0.5f));
                        createRightSprite = true;
                    }

                    xvEyeFlowManage.RighteyeImage.GetComponent<Image>().sprite = ETrightSprite;
                }
                else
                {
                    MyDebugTool.Log("GetEyeImage Invalid texture");
                }


                
            }
            else
            {

                MyDebugTool.Log("GetEyeImage xv_eyetracking_get_rgba Invalid texture");
            }

        }
    }



    private void OnApplicationPause(bool isPause)
    {
        //Triggered when returning to the desktop
        if (isPause)
        {

            bool b = XvEyeTracking.xv_eyetracking_stop();
            MyDebugTool.Log($"OnApplicationPause xv_eyetracking_stop:{b}");
        }
    }


}
