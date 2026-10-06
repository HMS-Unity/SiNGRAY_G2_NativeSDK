using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Singray.SystemEvents;
using System;
using Singray.utils;
using Assets.XvXRScripts.Engine;

namespace Singray.Engine
{
    public abstract class XvXRMobileDevice:XvXRBaseDevice
    {

        protected const int jniEnvInitEventId = 0x66660;//call java init the jni evn for android

        protected const int renderEventId = 0x666666;//render draw event

        protected const int initRenderEventId = 0x666667;//init render

        protected const int changeRenderDataEventId = 0x666668;//change render data

        protected const int copyRederEventId = 0x7666666;

        protected int lastLeftId = 0;

        protected int lastRightId = 0;


        public abstract void  ReadConfigInfo();
        /// <summary>
        /// Update parameters and send the calculated projection matrices to the Android Java library
        /// </summary>
        public override void UpdateScreenData()
        {
            XvXRLog.InternalXvXRLog("mobile test UpdateScreenData");
            //Copy device.mParameter, obtained from the glasses at device attachment, into XvXRConfigInfo.parmeter
            ReadConfigInfo();
            //Calculate default eye projection matrices and set leftEyeProjection, rightEyeProjection and recommendedTextureSize
            //See the overload in XvXRAndroidDevice.cs
            ComputeEyesFromProfile();
            //Send Info.parameter and the calculated projection matrices to the glasses, then issue changeRenderDataEventId
            ChangeRenderData();


        }

        public override void SetStereoScreen(RenderTexture leftRenderTexture, RenderTexture rightRenderTexture)
        {


            lastLeftId = leftRenderTexture != null ? (int)leftRenderTexture.GetNativeTexturePtr() : 0;
            lastRightId = rightRenderTexture != null ? (int)rightRenderTexture.GetNativeTexturePtr() : 0;


            SetTextureIdMobile();
        }

        protected void CheckTextureId()//has the problem 
        {
            if (XvXRManager.SDK.StereoScreen != null)
            {
                if (lastLeftId != (int)XvXRManager.SDK.StereoScreen[0].GetNativeTexturePtr() || lastRightId != (int)XvXRManager.SDK.StereoScreen[1].GetNativeTexturePtr())
                {
                    //XvXRLog.InternalXvXRLog(" CheckTextureId:lastleft:" + lastLeftId + ",lastright:" + lastRightId + ",stereoscreenleftid:" + (int)XvXRManager.SDK.StereoScreen[0].GetNativeTexturePtr() + ",steroscreenrightid:" + (int)XvXRManager.SDK.StereoScreen[1].GetNativeTexturePtr());
                    SetStereoScreen(XvXRManager.SDK.StereoScreen[0], XvXRManager.SDK.StereoScreen[1]);
                }
            }
        }
        public override void SetDistortionCorrectionEnabled(bool enabled)
        {

        }

        public override void Recenter()
        {

        }


        public void ChangeRenderData()
        {
            XvXRLog.InternalXvXRLog("ChangeRenderData");
            //Send Info.parameter to the glasses
            SetRenderDataMobile();
            //Send the event to the glasses
            GL.IssuePluginEvent(RenderEventFunc(), changeRenderDataEventId);

        }

        public override void PostRender()
        {
            GL.IssuePluginEvent(RenderEventFunc(), renderEventId);
        }

        public void InitRenderEvent()
        {
            InitRenderMobile(1);//0: backbuffer ; 1 singlebuffer
            GL.IssuePluginEvent(RenderEventFunc(), initRenderEventId);
        }


        internal abstract void InitRenderMobile(int bufferMode);

        internal abstract void SetRenderDataMobile();

        internal abstract void SetTextureIdMobile();

        internal abstract IntPtr RenderEventFunc();




    }
}
