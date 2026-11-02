using UnityEngine;
using System.Runtime.InteropServices;
using System;
namespace Singray.Foundation
{
    public enum TofFramerate
    {
        FPS_5,
        FPS_10,
        FPS_15,
        FPS_20,
        FPS_25,
        FPS_30
    }
    public enum SonyTofLibMode
    {
        IQMIX_DF,
        IQMIX_SF,
        LABELIZE_DF,
        LABELIZE_SF,
        M2MIX_DF,
        M2MIX_SF
    }
    public enum TofResolution
    {
        Unknown = -1,
        VGA,
        QVGA,
        HQVGA,

    }

    public enum TofStreamMode
    {

        DepthOnly = 0,//0:
        CloudOnly = 1,//1:
        DepthAndCloud = 2,//2:
        None = 3,//3
        CloudOnLeftHandSlam = 4,//4:
    }
    [Serializable]
    public class XvTofCameraParameter : XvCameraParameterSetting
    {


        [HideInInspector]
        public TofStreamType streamType;
        public TofFramerate tofFramerate;
        public SonyTofLibMode sonyTofLibMode;
        public TofResolution tofResolution;
        public TofStreamMode tofStreamMode;



        public bool enableGamma;//IR image available
    }
    public class XvTofCamera : XvCameraBase
    {

        public XvTofCamera(XvTofCameraParameter cameraParameter, FrameArrived frameArrived) : base(cameraParameter, frameArrived)
        {
            this.cameraParameter = cameraParameter;

        }

        private Texture2D tex = null;


        private Color32[] pixel32;
        private GCHandle pixelHandle;
        private IntPtr pixelPtr;
        private XvTofCameraParameter cameraParameter;

        // The bundled RGBA getter allocates before rejecting unsupported/cached frame types.
        // Use its allocation-free depth-copy export and retain the existing Rainbow visualization.
        private const int MaxPixels = 640 * 480; // Largest supported ToF resolution (VGA).
        private float[] depthBuffer;
        private double lastTimestamp;
        private float startedAt, nextDiagnosticAt, nextWarningAt;
        private bool receivedFrame;
        private static readonly Color32[] Rainbow = BuildRainbow();

        public override void StartCapture()
        {
            if (isOpen) return;
            lastTimestamp = Math.Max(XvTofNative.TimestampFloor, XvTofNative.CurrentTimestamp());
            startedAt = Time.realtimeSinceStartup;
            nextDiagnosticAt = nextWarningAt = 0;
            receivedFrame = false;
            isOpen = true;
            XvTofNative.Log("depth-wait", "timestampFloor=" + lastTimestamp.ToString("F6") + " path=raw-depth");
        }

        public override void StopCapture()
        {
            isOpen = false;
            if (pixelHandle.IsAllocated) pixelHandle.Free();
            if (tex != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(tex);
                else UnityEngine.Object.DestroyImmediate(tex);
            }
            tex = null;
            pixel32 = null;
            pixelPtr = IntPtr.Zero;
            depthBuffer = null;
            cameraData.tex = null;
            cameraData.texWidth = cameraData.texHeight = 0;
            cameraData.parameter.timeStamp = 0;
        }

        private bool EnsureTexture(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > 640 || height > 480) return false;
            if (tex != null && tex.width == width && tex.height == height) return true;
            if (tex != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(tex);
                else UnityEngine.Object.DestroyImmediate(tex);
            }
            tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixel32 = new Color32[width * height];
            // Native copy has no capacity argument: reserve the supported maximum, not a stale reported size.
            if (depthBuffer == null) depthBuffer = new float[MaxPixels];
            XvTofNative.Log("depth-buffer", "actual=" + width + "x" + height + " capacity=" + depthBuffer.Length);
            return true;
        }

        public override void Update()
        {
            if (!isOpen || !XvTofNative.Ready()) return;
            int width = 0, height = 0; long edge = 0; double timestamp = 0; uint bytes = 0;
            bool available = XvTofNative.Info(ref width, ref height, ref edge, ref timestamp, ref bytes);
            bool valid = available && width > 0 && height > 0 && width <= 640 && height <= 480
                && !double.IsNaN(timestamp) && !double.IsInfinity(timestamp) && timestamp > lastTimestamp
                && (bytes == (long)width * height * 2 || bytes == (long)width * height * 4);
            if (valid && EnsureTexture(width, height) && XvTofNative.ReadDepth(depthBuffer))
            {
                int afterWidth = 0, afterHeight = 0; long afterEdge = 0; double afterTimestamp = 0; uint afterBytes = 0;
                // Metadata and copy are separate native exports; never publish a frame that changed between them.
                if (XvTofNative.Info(ref afterWidth, ref afterHeight, ref afterEdge, ref afterTimestamp, ref afterBytes)
                    && width == afterWidth && height == afterHeight && timestamp == afterTimestamp && bytes == afterBytes)
                {
                    for (int i = 0; i < pixel32.Length; i++) pixel32[i] = DepthColor(depthBuffer[i]);
                    tex.SetPixels32(pixel32);
                    tex.Apply(false, false);
                    cameraData.tex = tex;
                    cameraData.texWidth = width;
                    cameraData.texHeight = height;
                    cameraData.parameter.timeStamp = timestamp;
                    lastTimestamp = timestamp;
                    bool first = !receivedFrame;
                    receivedFrame = true;
                    if (Time.realtimeSinceStartup >= nextDiagnosticAt)
                    {
                        nextDiagnosticAt = Time.realtimeSinceStartup + (Time.realtimeSinceStartup - startedAt < 10 ? 1 : 5);
                        XvTofNative.Log(first ? "depth-first-frame" : "depth-frame",
                            "actual=" + width + "x" + height + " bytes=" + bytes + " timestamp=" + timestamp.ToString("F6"));
                    }
                    frameArrived?.Invoke(cameraData);
                }
            }
            else if (Time.realtimeSinceStartup >= nextWarningAt)
            {
                nextWarningAt = Time.realtimeSinceStartup + 2;
                Debug.LogWarning($"[TOF-LIFE] phase=depth-frame-rejected actual={width}x{height} bytes={bytes} timestamp={timestamp:F6} last={lastTimestamp:F6}");
            }
            if (!receivedFrame && Time.realtimeSinceStartup - startedAt >= 10)
            {
                Debug.LogError("[TOF-LIFE] phase=depth-timeout no fresh supported depth frame after 10s; stopping capture.");
                StopCapture();
            }
        }

        private static Color32 DepthColor(float metres)
        {
            // Match the original wrapper: threshold (0.2, 10.5] m, 255/4.5 scale, OpenCV COLORMAP_RAINBOW.
            if (float.IsNaN(metres) || metres <= 0.2f || metres > 10.5f) return new Color32(0, 0, 0, 255);
            return Rainbow[Mathf.Clamp(Mathf.RoundToInt(metres * (255f / 4.5f)), 0, 255)];
        }

        private static Color32[] BuildRainbow()
        {
            // Sample the Octave Rainbow function at 64 knots, then interpolate to 256 entries,
            // as OpenCV does: https://github.com/opencv/opencv/blob/4.x/modules/imgproc/src/colormap.cpp
            var knots = new Color[64];
            for (int i = 0; i < knots.Length; i++)
            {
                float x = i / 63f;
                knots[i] = new Color(x < 0.4f ? 1 : x < 0.6f ? 3 - 5*x : x < 0.8f ? 0 : (x - 0.8f)*10/3,
                    x < 0.4f ? x*2.5f : x < 0.6f ? 1 : x < 0.8f ? 4 - 5*x : 0,
                    x < 0.6f ? 0 : x < 0.8f ? 5*x - 3 : 1, 1);
            }
            var colors = new Color32[256];
            for (int i = 0; i < colors.Length; i++)
            {
                float x = i * 63f / 255;
                int index = Mathf.Min((int)x, 62);
                colors[i] = Color.Lerp(knots[index], knots[index + 1], x - index);
            }
            return colors;
        }
    }
}