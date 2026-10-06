using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
namespace Singray.Engine
{
    public class XvXREyeMirrorPrivew : MonoBehaviour
    {

        internal XvXRManager.Eye eye;
        new public Camera camera { get; private set; }

        void Awake()
        {
            camera = GetComponent<Camera>();


        }

        
        void Update()
        {
            
        }
        void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
        {
            if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
            {

                if (null != destTexture)
                {
                    Graphics.Blit(sourceTexture, destTexture);
                }
            }
        }
    }
}