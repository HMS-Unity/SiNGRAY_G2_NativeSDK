using System;
using System.Collections.Generic;
using UnityEngine;

namespace Singray.Foundation
{
    public class TagDetection
    {
        public int id;
        public Vector3 translation;
        public Vector3 rotation;
        public Vector4 quaternion;
        public float confidence;
        public byte[] qrcode;
    }

    public class XvAprilTag
    {
        public delegate void TagArrayCallback(IntPtr tagData, int count);
        internal delegate int ReadTags(string family, double size, ref API.TagData tags, int capacity);
        internal delegate int ReadRgbTags(ref API.TagData tags, int capacity);
        private static Func<bool> readyNative = PlatformReady;
        private static ReadTags readFishNative = API.xslam_detect_tags;
        private static ReadRgbTags readRgbNative = API.xslam_get_rgb_detect_tags;
        // Keep the managed delegate alive for the lifetime of the native registration.
        private static TagArrayCallback retainedRgbCallback;
        private static bool PlatformReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }
        public static TagDetection[] StartFishEyeDetector(string tagFamily, double size)
        {
            if (!readyNative()) return null;
            API.TagData tags = default;
            int count = readFishNative(tagFamily, size, ref tags, 64);
            return ConvertTags(tags, count);
        }
        public static void StopFishEyeDetector()
        {
            if (readyNative()) API.xslam_stop_detect_tags();
        }
        public static void StartRgbDetector(string tagFamily, double size, TagArrayCallback tagArrayCallback)
        {
            if (!readyNative()) { Debug.LogWarning("[TAG-LIFE] RGB recognition not started: SDK is not ready."); return; }
            retainedRgbCallback = tagArrayCallback;
            API.xslam_getTagDetectionrgbImage(tagFamily, size, retainedRgbCallback);
        }
        public static TagDetection[] GetRgbDetectTags()
        {
            if (!readyNative()) return null;
            API.TagData tags = default;
            int count = readRgbNative(ref tags, 64);
            return ConvertTags(tags, count);
        }
        public static void StopRgbDetector()
        {
            if (readyNative()) API.xslam_stop_rgb_detect_tags();
            retainedRgbCallback = null;
        }
        internal static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        internal static bool ValidSize(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
        internal static TagDetection[] ConvertTags(API.TagData tags, int count)
        {
            if (count <= 0) return null;
            if (count > 64 || tags.detect == null || count > tags.detect.Length) return null;
            var output = new List<TagDetection>(count);
            for (int i = 0; i < count; i++)
            {
                var t = tags.detect[i];
                if (!Finite(t.position.x) || !Finite(t.position.y) || !Finite(t.position.z) ||
                    !Finite(t.orientation.x) || !Finite(t.orientation.y) || !Finite(t.orientation.z) ||
                    !Finite(t.quaternion.x) || !Finite(t.quaternion.y) || !Finite(t.quaternion.z) || !Finite(t.quaternion.w) || !Finite(t.confidence)) continue;
                output.Add(new TagDetection
                {
                    id = t.tagID, translation = new Vector3(t.position.x, t.position.y, t.position.z),
                    rotation = new Vector3(t.orientation.x, t.orientation.y, t.orientation.z),
                    quaternion = new Vector4(t.quaternion.x, t.quaternion.y, t.quaternion.z, t.quaternion.w),
                    confidence = t.confidence, qrcode = t.qrcode == null ? Array.Empty<byte>() : (byte[])t.qrcode.Clone()
                });
            }
            return output.Count == 0 ? null : output.ToArray();
        }
        internal static string QrText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            int length = Array.IndexOf(bytes, (byte)0);
            return System.Text.Encoding.UTF8.GetString(bytes, 0, length < 0 ? bytes.Length : length).Trim();
        }
    }
}
