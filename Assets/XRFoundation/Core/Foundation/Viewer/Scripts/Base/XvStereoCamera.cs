using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Singray.Foundation
{
    public enum StereoCameraIndex { 
      LeftEye,

      RightEye,
    }
    [Serializable]
    public class XvStereoCameraParameter : XvCameraParameterSetting
    {
      
        public StereoCameraIndex cameraIndex;//0==left       1=right
    }
    public class XvStereoCamera : XvCameraBase
    {
        public XvStereoCamera(XvStereoCameraParameter cameraParameter, FrameArrived frameArrived) : base(cameraParameter, frameArrived)
        {
           
            this.cameraParameter = cameraParameter;

        }
        private Color32[] pixel32;
        private GCHandle pixelHandle;
        private IntPtr pixelPtr;
        private Texture2D tex = null;
       
        private XvStereoCameraParameter cameraParameter;
        public override void StartCapture()
        {
            base.StartCapture();
            ChangeStereoStatus(true);
        }

        public override void StopCapture()
        {
            base.StopCapture();
            pixelHandle.Free();
            tex = null;
            ChangeStereoStatus(false);

        }

        public override void Update()
        {
            if (isOpen) {
                if (API.xslam_ready() && isStartStereo)
                {

                    int width = API.xslam_get_stereo_width();
                    int height = API.xslam_get_stereo_height();
                    int size = width * height;

                    if (width > 0 && height > 0 && size > 0)
                    {

                        if (!tex)
                        {
                            MyDebugTool.Log("Create STEREO texture " + width + "x" + height);
                            TextureFormat format = TextureFormat.RGBA32;
                            tex = new Texture2D(width, height, format, false);


                            pixel32 = tex.GetPixels32();
                            pixelHandle = GCHandle.Alloc(pixel32, GCHandleType.Pinned);
                            pixelPtr = pixelHandle.AddrOfPinnedObject();


                        }
                        double ts = 0;
                        switch (cameraParameter.cameraIndex)
                        {
                            case StereoCameraIndex.LeftEye:
                                
                                if (API.xslam_get_left_image(pixelPtr, tex.width, tex.height, ref ts))
                                {
                                    //Update the Texture2D with array updated in C++
                                    tex.SetPixels32(pixel32);
                                    tex.Apply();

                                    cameraData.tex = tex;
                                    cameraData.texWidth = tex.width;

                                    cameraData.texHeight = tex.height;

                                    frameArrived?.Invoke(cameraData);
                                }
                                else
                                {
                                    MyDebugTool.Log("Invalid Stereo texture");
                                }
                                break;
                            case StereoCameraIndex.RightEye:
                               
                                if (API.xslam_get_right_image(pixelPtr, tex.width, tex.height, ref ts))
                                {
                                    //Update the Texture2D with array updated in C++
                                    tex.SetPixels32(pixel32);
                                    tex.Apply();

                                    cameraData.tex = tex;
                                    cameraData.texWidth = tex.width;

                                    cameraData.texHeight = tex.height;

                                    frameArrived?.Invoke(cameraData);
                                }
                                else
                                {
                                    MyDebugTool.Log("Invalid Stereo texture");
                                }
                                break;
                            default:
                                break;
                        }
                     

                        /*int leftPointsCount = 0;
                        if( API.xslam_get_left_points( leftPoints, ref leftPointsCount) ){
                            Debug.Log("leftPointsCount " + leftPointsCount);
                        }*/
                    }
                }
            }
          
        }
        private bool isStartStereo;
        private bool ChangeStereoStatus(bool isOn)
        {
            if (API.xslam_ready())
            {
                try
                {


                    if (isOn)
                    {
                        API.xslam_stop_stereo_stream();
                        API.xslam_start_stereo_stream();
                        isStartStereo = true;
                        return true;
                    }
                    else
                    {
                        API.xslam_start_stereo_stream();
                        isStartStereo = false;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }


}
