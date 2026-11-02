using System;
using UnityEngine;

namespace Singray.Foundation
{
    public enum TofStreamType { Unknown = 0, DeapthStream, IRStream }

    public class XvTofDepth
    {
        private XvCameraBase frameBase;
        public bool IsOn { get; private set; }
        public void StartCapture(XvTofCameraParameter xvTofCameraParameter)
        {
            var parameter = xvTofCameraParameter;
            if (IsOn) return;
            var manager = XvTofManager.GetXvTofManager();
            manager.StartTofStream(parameter);
            if (!manager.LastStartSubmitted) return;
            frameBase = new XvTofCamera(parameter, FrameArrived);
            frameBase.StartCapture();
            IsOn = true;
        }
        internal void ReleaseFrame()
        {
            IsOn = false;
            frameBase?.StopCapture();
            frameBase = null;
        }
        public void StopCapture()
        {
            bool running = IsOn;
            ReleaseFrame();
            if (running) XvTofManager.GetXvTofManager().StopIfUnused();
        }
        private void FrameArrived(cameraData frame) => XvCameraManager.onTofDepthCameraStreamFrameArrived?.Invoke(frame);
        public void Update()
        {
            frameBase?.Update();
            if (frameBase != null && !frameBase.IsOpen) XvTofManager.GetXvTofManager().StopTofStream();
        }
    }

    public class XvTofIR
    {
        private XvCameraBase frameBase;
        public bool IsOn { get; private set; }
        public void StartCapture(XvTofCameraParameter xvTofIRCameraParameter)
        {
            var parameter = xvTofIRCameraParameter;
            if (IsOn) return;
            var manager = XvTofManager.GetXvTofManager();
            manager.StartTofIRStream(parameter);
            if (!manager.LastStartSubmitted) return;
            frameBase = new XvTofIRCamera(parameter, FrameArrived);
            frameBase.StartCapture();
            IsOn = true;
        }
        internal void ReleaseFrame()
        {
            IsOn = false;
            frameBase?.StopCapture();
            frameBase = null;
        }
        public void StopCapture()
        {
            bool running = IsOn;
            ReleaseFrame();
            if (running) XvTofManager.GetXvTofManager().StopIfUnused();
        }
        private void FrameArrived(cameraData frame) => XvCameraManager.onTofIRCameraStreamFrameArrived?.Invoke(frame);
        public void Update() => frameBase?.Update();
    }

    public sealed class XvTofManager
    {
        private static XvTofManager xvTofManager;
        public static XvTofManager GetXvTofManager() => xvTofManager ?? (xvTofManager = new XvTofManager());
        private XvTofManager() { }
        public bool modelSet = false;
        private XvTofDepth xvTofDepth;
        private XvTofIR xvTofIR;
        private int configuredMode = -1;
        private XvTofCameraParameter activeParameter;
        private bool streamRequested, irRequested, depthRequested;
        private bool stopSubmitted;
        private int generation;
        internal bool LastStartSubmitted { get; private set; }

        public void StartCapture(XvTofCameraParameter xvTofCameraParameter)
        {
            var parameter = xvTofCameraParameter;
            if (parameter == null) return;
            if (parameter.streamType == TofStreamType.DeapthStream)
            {
                if (xvTofDepth == null) xvTofDepth = new XvTofDepth();
                xvTofDepth.StartCapture(parameter);
            }
            else if (parameter.streamType == TofStreamType.IRStream)
            {
                if (xvTofIR == null) xvTofIR = new XvTofIR();
                xvTofIR.StartCapture(parameter);
            }
        }
        public void StopCapture(TofStreamType streamType)
        {
            var type = streamType;
            if (type == TofStreamType.DeapthStream) xvTofDepth?.StopCapture();
            else if (type == TofStreamType.IRStream) xvTofIR?.StopCapture();
        }
        public bool IsOn(TofStreamType streamType) => streamType == TofStreamType.DeapthStream ? xvTofDepth != null && xvTofDepth.IsOn
            : streamType == TofStreamType.IRStream && xvTofIR != null && xvTofIR.IsOn;

        internal void StopIfUnused()
        {
            if (!IsOn(TofStreamType.DeapthStream) && !IsOn(TofStreamType.IRStream)) StopTofStream();
        }
        public void StartTofStream(XvTofCameraParameter xvTofIRCameraParameter) => TryStart(xvTofIRCameraParameter, false);
        public void StartTofIRStream(XvTofCameraParameter xvTofIRCameraParameter) => TryStart(xvTofIRCameraParameter, true);

        private bool Matches(XvTofCameraParameter p) => activeParameter != null
            && activeParameter.tofStreamMode == p.tofStreamMode && activeParameter.sonyTofLibMode == p.sonyTofLibMode
            && activeParameter.tofResolution == p.tofResolution && activeParameter.tofFramerate == p.tofFramerate;

        private void TryStart(XvTofCameraParameter p, bool ir)
        {
            LastStartSubmitted = false;
            if (p == null || !XvTofNative.Ready())
            {
                Debug.LogWarning("[TOF-LIFE] phase=start-rejected SDK not ready or parameters missing.");
                return;
            }
            if ((int)p.tofStreamMode < 0 || (int)p.tofStreamMode > 4 || (int)p.tofResolution < 0 || (int)p.tofResolution > 2 || (int)p.sonyTofLibMode < 0
                || (int)p.sonyTofLibMode > 5 || (int)p.tofFramerate < 0 || (int)p.tofFramerate > 5)
            {
                Debug.LogError("[TOF-LIFE] phase=start-rejected invalid ToF configuration.");
                return;
            }
            try
            {
                if (streamRequested && !Matches(p)) StopTofStream();
                SetTofStreamMode((int)p.tofStreamMode);
                if (!modelSet) return;
                XvTofNative.Log("start-before", $"generation={generation} mode={p.tofStreamMode} sony={p.sonyTofLibMode} requested={p.tofResolution} fps={p.tofFramerate} ir={ir}");
                if (ir)
                {
                    if (!irRequested) XvTofNative.Ir((int)p.sonyTofLibMode, (int)p.tofResolution, (int)p.tofFramerate);
#if UNITY_ANDROID && !UNITY_EDITOR
                    API.xslam_tof_enbale_ir_gramma(p.enableGamma);
#endif
                    irRequested = true;
                }
                else if (!depthRequested)
                {
                    // Preserve the supplier's two-step startup, using the actual void ABI.
                    XvTofNative.Start();
                    XvTofNative.Sony((int)p.sonyTofLibMode, (int)p.tofResolution, (int)p.tofFramerate);
                    depthRequested = true;
                }
                activeParameter = new XvTofCameraParameter { tofStreamMode = p.tofStreamMode, sonyTofLibMode = p.sonyTofLibMode,
                    tofResolution = p.tofResolution, tofFramerate = p.tofFramerate, enableGamma = p.enableGamma };
                streamRequested = LastStartSubmitted = true;
                XvTofNative.Log("start-submitted", "generation=" + generation + " nativeReturn=void confirmation=pending-fresh-frame");
            }
            catch (Exception error)
            {
                StopTofStream();
                Debug.LogError("[TOF-LIFE] phase=start-failed " + error);
            }
        }

        public void SetTofStreamMode(int mode)
        {
            if (mode < 0 || mode > 4 || !XvTofNative.Ready()) return;
            if (modelSet && configuredMode == mode) return;
            StopTofStream();
            if (!stopSubmitted) return;
            try
            {
                int result = XvTofNative.Mode(mode);
                // The bundled function returns its underlying setMode result masked with 1.
                if (result != 1)
                {
                    Debug.LogError($"[TOF-LIFE] phase=mode-rejected generation={generation} mode={mode} rawResult={result}; startup cancelled.");
                    return;
                }
                configuredMode = mode;
                modelSet = true;
                XvTofNative.Log("mode-submitted", $"generation={generation} mode={mode} rawResult={result}");
            }
            catch (Exception error) { Debug.LogError("[TOF-LIFE] phase=mode-failed " + error); }
        }

        public void StopTofStream()
        {
            generation++;
            stopSubmitted = false;
            LastStartSubmitted = streamRequested = irRequested = depthRequested = false;
            modelSet = false;
            configuredMode = -1;
            activeParameter = null;
            xvTofDepth?.ReleaseFrame();
            xvTofIR?.ReleaseFrame();
            if (!XvTofNative.Ready()) return;
            try
            {
                XvTofNative.TimestampFloor = Math.Max(XvTofNative.TimestampFloor, XvTofNative.CurrentTimestamp());
                XvTofNative.Log("stop-before", "generation=" + generation);
                XvTofNative.Stop();
                stopSubmitted = true;
                XvTofNative.Log("stop-returned", "generation=" + generation + " nativeReturn=void timestampFloor=" + XvTofNative.TimestampFloor.ToString("F6"));
            }
            catch (Exception error) { Debug.LogError("[TOF-LIFE] phase=stop-failed " + error); }
        }
        public void Update()
        {
            xvTofDepth?.Update();
            xvTofIR?.Update();
        }
    }
}
