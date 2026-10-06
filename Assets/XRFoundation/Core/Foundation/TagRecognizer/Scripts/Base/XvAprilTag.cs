using System;
using UnityEngine;
using static API;

namespace Singray.Foundation
{
    /// <summary>
    /// Recognition result
    /// </summary>
    public class TagDetection
    {
        public int id;//ID

        public Vector3 translation;//Position

        public Vector3 rotation;

        public Vector4 quaternion;//Rotation

        public float confidence;//Recognition confidence, in the range 0-1
       
        public byte[] qrcode; //QR-code data
    }

    public class XvAprilTag
    {

        public delegate void TagArrayCallback(IntPtr tagData,int count);
        /// <summary>
        /// Start AprilTag detection using the fisheye camera
        /// </summary>
        /// <param name="tagFamily">
        /// Use tagFamily = 36h11 for AprilTag detection
        /// Use tagFamily = qr-code for QR-code detection</param>
        /// <param name="size">Physical tag size in meters</param>
        /// <returns></returns>
        public static TagDetection[] StartFishEyeDetector(string tagFamily, double size)
        {
            if (!API.xslam_ready())
            {
                Debug.LogError("xslam is not ready");
                return null;
            }
            Debug.LogError("wayland =2= fisheye");

            API.TagData tags = default(API.TagData);
            //int len = API.xslam_start_detect_tags(tagFamily, size, ref tags, 64);
            int len = API.xslam_detect_tags(tagFamily, size, ref tags, 64);
            if (len <= 0)
            {
                return null;
            }
            Debug.LogError("wayland =3= fisheye");

            TagDetection[] result = new TagDetection[len];
            MyDebugTool.Log("AprilTag##StartDetector tags size:" + len);
            for (int i = 0; i < len; i++)
            {
                API.DetectData tag = tags.detect[i];
                // Debug.Log("AprilTag##StartDetector tag position:(" + tag.position.x + "," + tag.position.y + "," + tag.position.z + ")");
                // Debug.Log("AprilTag##StartDetector tag orientation:(" + tag.orientation.x + "," + tag.orientation.y + "," + tag.orientation.z + ")");
                // Debug.Log("AprilTag##StartDetector tag quaternion:(" + tag.quaternion.x + "," + tag.quaternion.y + "," + tag.quaternion.z + "," + tag.quaternion.w + ")");

                TagDetection detection = new TagDetection();
                detection.id = tag.tagID;
                detection.translation = new Vector3(tag.position.x, tag.position.y, tag.position.z);
                detection.rotation = new Vector3(tag.orientation.x, tag.orientation.y, tag.orientation.z);
                detection.quaternion = new Vector4(tag.quaternion.x, tag.quaternion.y, tag.quaternion.z, tag.quaternion.w);
                detection.confidence = tag.confidence;
                result[i] = detection;

                MyDebugTool.Log("AprilTag##StartDetector detection translation:(" + detection.translation.x + "," + detection.translation.y + "," + detection.translation.z + ")");
                MyDebugTool.Log("AprilTag##StartDetector detection rotation:(" + detection.rotation.x + "," + detection.rotation.y + "," + detection.rotation.z + ")");
                MyDebugTool.Log("AprilTag##StartDetector detection quaternion:(" + detection.quaternion.x + "," + detection.quaternion.y + "," + detection.quaternion.z + "," + detection.quaternion.w + ")");
            }

            return result;
        }


        /// <summary>
        /// Stop AprilTag detection using the fisheye camera
        /// </summary>
        public static void StopFishEyeDetector()
        {
            if (!API.xslam_ready())
            {
                Debug.LogError("xslam is not ready");
                return;
            }
            MyDebugTool.Log("StopFishEyeDetector");

            API.xslam_stop_detect_tags();
        }


        /// <summary>
        /// Start AprilTag detection using the RGB camera
        /// </summary>
        /// <param name="tagFamily">
        /// Use tagFamily = 36h11 for AprilTag detection
        /// Use tagFamily = qr-code for QR-code detection</param>
        /// <param name="size">Physical tag size in meters</param>
        /// <returns></returns>
        public static  void StartRgbDetector(string tagFamily, double size, TagArrayCallback tagArrayCallback)
        {
            if (!API.xslam_ready())
            {
                Debug.LogError("xslam is not ready");
                return ;
            }

            API.xslam_getTagDetectionrgbImage(tagFamily, size, tagArrayCallback);

      
            //int len = API.xslam_start_rgb_detect_tags(tagFamily, size);

            MyDebugTool.Log("StartRgbDetector"+ tagFamily+"  "+ size);
        }


        public static TagDetection[] GetRgbDetectTags()
        {
            if (!API.xslam_ready())
            {
                Debug.LogError("xslam is not ready");
                return null;
            }

            API.TagData tags = default(API.TagData);
            int len = API.xslam_get_rgb_detect_tags(ref tags, 64);
            if (len <= 0)
            {
                return null;
            }

            TagDetection[] result = new TagDetection[len];
            MyDebugTool.Log("AprilTag##GetRgbDetectTags tags size:" + len);
            for (int i = 0; i < len; i++)
            {
                API.DetectData tag = tags.detect[i];


                TagDetection detection = new TagDetection();

                if (detection.qrcode == null)
                {
                    detection.qrcode = new byte[512];
                }

                detection.id = tag.tagID;
                detection.translation = new Vector3(tag.position.x, tag.position.y, tag.position.z);
                detection.rotation = new Vector3(tag.orientation.x, tag.orientation.y, tag.orientation.z);
                detection.quaternion = new Vector4(tag.quaternion.x, tag.quaternion.y, tag.quaternion.z, tag.quaternion.w);
                detection.confidence = tag.confidence;
                detection.qrcode = tag.qrcode;
                result[i] = detection;

                MyDebugTool.Log("AprilTag##StartRgbDetector detection translation:(" + detection.translation.x + "," + detection.translation.y + "," + detection.translation.z + ")");
                MyDebugTool.Log("AprilTag##StartRgbDetector detection rotation:(" + detection.rotation.x + "," + detection.rotation.y + "," + detection.rotation.z + ")");
                MyDebugTool.Log("AprilTag##StartRgbDetector detection quaternion:(" + detection.quaternion.x + "," + detection.quaternion.y + "," + detection.quaternion.z + "," + detection.quaternion.w + ")");



                try
                {
                    //for (int j = 0; j < detection.qrcode.Length; j++)
                    //{
                    //    Debug.Log($"AprilTag##StartRgbDetector detection qrcode[{j}]:{detection.qrcode[j]}");
                    //}
                    MyDebugTool.Log($"AprilTag##StartRgbDetector detection qrcode string:{System.Text.Encoding.UTF8.GetString(detection.qrcode)}");
                }
                catch (System.Exception e)
                {
                    MyDebugTool.Log($"AprilTag##StartRgbDetector Exception:{e}");
                    MyDebugTool.Log($"AprilTag##StartRgbDetector detection qrcode==null?:{detection.qrcode == null}");
                }


            }

            return result;
        }


        /// <summary>
        /// Stop RGB camera detection
        /// </summary>
        public static void StopRgbDetector()
        {
            if (!API.xslam_ready())
            {
                Debug.LogError("xslam is not ready");
                return;
            }

            MyDebugTool.Log("StopRgbDetector");
            API.xslam_stop_rgb_detect_tags();
        }
    }
}

