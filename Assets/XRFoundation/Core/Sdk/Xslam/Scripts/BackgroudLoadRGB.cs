using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

public class BackgroudLoadRGB : MonoBehaviour
{
    private Texture2D tex = null;
    private Color32[] pixel32;
    private GCHandle pixelHandle;
    private IntPtr pixelPtr;    
    private double rgbTimestamp = 0;
    private int lastWidth = 0;
    private int lastHeight = 0;
    private Dictionary<string, Texture2D> gesTextures = new Dictionary<string, Texture2D>();
    private Texture2D texGes;
    private int handtype = -1;

     public GameObject backgroundGameObjects;
    
    void Start()
    {
        // use uvc rgb
        //API.xslam_set_rgb_source( 0 );

        // set to 720p
        //API.xslam_set_rgb_resolution( 1 );
    }
    
    void Update()
    {
    	if( API.xslam_ready() ){
    	
    		int width = API.xslam_get_rgb_width();
    		int height = API.xslam_get_rgb_height();
    		
    		if( width > 0 && height > 0 ){

				if( lastWidth != width || lastHeight != height ){
                    try{
                        double r = 0.25;
                        if (width <=1280 && height <=720) {
                            r = 1.0;
                        }
                        int w = (int)(width * r);
                        int h = (int)(height * r);
                        MyDebugTool.Log("Create RGB texture " + w + "x" + h);
                        TextureFormat format = TextureFormat.RGBA32;
                        tex = new Texture2D(w, h, format, false);
                        //tex.filterMode = FilterMode.Point;
                        tex.Apply();
                        try {
                            pixelHandle.Free();
                        } catch {}
                        pixel32 = tex.GetPixels32();
                        pixelHandle = GCHandle.Alloc(pixel32, GCHandleType.Pinned);
                        pixelPtr = pixelHandle.AddrOfPinnedObject();
                        if(backgroundGameObjects!=null){
                            backgroundGameObjects.GetComponent<Image>().material.mainTexture = tex;
                        }
                       
                    }catch (Exception e)
                    {
                        Debug.LogException(e, this);
                        return;
                    }

                    lastWidth = width;
                    lastHeight = height;
				}
				
           
                try{
                    if( API.xslam_get_rgb_image_RGBA(pixelPtr, tex.width, tex.height, ref rgbTimestamp) ){
                       
                        tex.SetPixels32(pixel32);
                        tex.Apply();
                    }else{
                        MyDebugTool.Log("Invalid texture");
                    }
                }catch (Exception e)
                {
                    MyDebugTool.LogError(e);
                    return;
                }
                
			}
	    }
    }
    void OnGUI()
    {
       
    }
    void OnApplicationQuit()
    {
        //Free handle
        pixelHandle.Free();
    }
}
