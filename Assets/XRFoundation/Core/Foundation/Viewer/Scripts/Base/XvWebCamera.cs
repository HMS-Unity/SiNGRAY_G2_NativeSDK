
using System;
using UnityEngine;
namespace Singray.Foundation
{
    [Serializable]
    public class XvWebCameraParameter : XvCameraParameterSetting
    {
        public int width;
        public int height;
        public int fps;
    }

    public class XvWebCamera : XvCameraBase
    {
        private WebCamTexture webCamTexture;
        private Material material;
        private RenderTexture renderTexture;
        public XvWebCamera(XvWebCameraParameter cameraParameter, FrameArrived frameArrived) : base(cameraParameter, frameArrived)
        {

            this.cameraParameter = cameraParameter;

            Shader shader = Shader.Find("MyShader/RgbImage");

            if (shader != null)
            {
                material = new Material(shader);

            }
            else
            {
                Debug.LogError("Material MyShader/RgbImage was not found");
            }
        }

        private XvWebCameraParameter cameraParameter;

        public override void StartCapture()
        {
            WebCamDevice[] webCamDevices = WebCamTexture.devices;

            if (webCamDevices == null || webCamDevices.Length < 0)
            {
                isOpen = false;
                MyDebugTool.Log("No camera detected");

                return;
            }
            else
            {
                MyDebugTool.Log("Detected number of cameras：" + webCamDevices.Length);
            }
            webCamTexture = new WebCamTexture(webCamDevices[0].name, cameraParameter.width, cameraParameter.height);
            webCamTexture.Play();
            cameraData.tex = webCamTexture;
            cameraData.texWidth = cameraParameter.width;
            cameraData.texHeight = cameraParameter.height;
            renderTexture = new RenderTexture(cameraParameter.width, cameraParameter.height, 0, RenderTextureFormat.ARGB32);

            MyDebugTool.Log("Turn on the camera" + webCamTexture.isPlaying);
            isOpen = true;

        }

        public override void Update()
        {
            if (webCamTexture != null)
            {
                cameraData.tex = webCamTexture;
                cameraData.texHeight = cameraParameter.width;
                cameraData.texHeight = cameraParameter.height;

                Graphics.Blit(webCamTexture, renderTexture, material);

                cameraData.tex = renderTexture;
               
                frameArrived.Invoke(cameraData);
            }
            else
            {
                MyDebugTool.Log(123);

            }

        }

        public override void StopCapture()
        {

            if (isOpen && webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
                renderTexture = null;



                MyDebugTool.Log("Turn off the camera");
            }
            isOpen = false;


        }


    }
}
