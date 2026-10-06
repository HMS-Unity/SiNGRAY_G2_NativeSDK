using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Singray.Engine
{
    public class XvXRSdkPlugin
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ddQuaternionResultData
        {
            public float x;
            public float y;
            public float z;
            public float w;
            public int error;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ddPlatformData
        {
            public int platform;
            public int batch;
            public int mcuVersion;
            public int mcuMode;
            public byte[] icNumber;
            public int icNumberLength;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ddRotationInfo
        {
            public float x;
            public float y;
            public float z;
            public float w;
            public int dx;
            public int dy;
            public int dz;
            public int time;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ddPrimaryData
        {
            public float mx;
            public float my;
            public float mz;
            public float gx;
            public float gy;
            public float gz;
            public float ax;
            public float ay;
            public float az;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ddRotationInfoAndPrimaryData
        {
            public ddRotationInfo ddRotationInfo;
            public ddPrimaryData ddPrimaryData;
        };


        [DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern ddQuaternionResultData dd_ReadRotation();


        //[DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        //static extern void dd_ReleaseRotationDevice();

        [DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool dd_IsReady();

        [DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern ddPlatformData dd_ReadPlatformData();

        [DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern ddRotationInfoAndPrimaryData dd_ReadRotationAndPrimaryData();

        [DllImport("XvXRPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void dd_GetMcuSlotBuffer(char type, byte[] pOutMcuSlotBuffer, ref int length);

        [DllImport("XvXRPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void dd_UpdateMcuSlotBuffer(byte[] pInMcuSlotBuffer, int inLength, byte[] pOutMcuSlotBuffer, ref int outLength);

        [DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool dd_IsLightOn();

        [DllImport("XvXRPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern void dd_ChangeMcuDebugMode(char mode);



        [DllImport("XvXRPlugin")]
        public static extern void XvXR_SetTimeFromUnity(float t);
        [DllImport("XvXRPlugin")]
        public static extern void XvXR_GetTextureDesc(ref int leftTextureWidth
            , ref int leftTextureHeight
            , ref int rightTextureWidth
            , ref int rightTextureHeight);


        [DllImport("XvXRPlugin")]
        public static extern void XvXR_SetRenderTextureFromUnity(System.IntPtr leftRenderTexturePtr, System.IntPtr rightRenderTexturePtr, int w, int h);


        [DllImport("XvXRPlugin")]
        public static extern IntPtr XvXR_GetRenderEventFunc();

    }
}
