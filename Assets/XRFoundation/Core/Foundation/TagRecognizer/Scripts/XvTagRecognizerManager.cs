using AOT;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using static API;

namespace Singray.Foundation
{

    /// <summary>
    /// Provides AprilTag and QR-code recognition
    /// </summary>
    public sealed class XvTagRecognizerManager : MonoBehaviour
    {
        private XvTagRecognizerManager() { }

        [SerializeField]
        private XvCameraManager xvCameraManager;

        public XvCameraManager XvCameraManager
        {
            get
            {
                if (xvCameraManager == null)
                {
                    xvCameraManager = FindFirstObjectByType<XvCameraManager>();
                    if (xvCameraManager == null)
                    {

                        GameObject newObj = new GameObject("XvCameraManager");
                        xvCameraManager = newObj.AddComponent<XvCameraManager>();

                    }
                }
                return xvCameraManager;
            }
        }
       

        /// <summary>
        /// Tag family names: apriltag = "36h11", qrcode = "qr-code"
        /// </summary>
        //[SerializeField]

        private string tagGroupName = "36h11";

        public string TagGroupName
        {
            get
            {
                return tagGroupName;
            }

            set
            {
                tagGroupName = value;
            }
        }


        [SerializeField]
        [Tooltip("Physical tag size")]
        private double size = 0.16f;

        public double Size
        {
            get
            {
                return size;
            }

            set
            {
                size = value;
            }

        }


        [SerializeField]
        [Tooltip("Confidence threshold")]
        private float confidence = 0;
        public float Confidence
        {
            get
            {
                return confidence;
            }

            set
            {
                confidence = value;
            }

        }
        private bool IsDetection=false;


        private UnityEvent<TagDetection[]> onDetectedAprilTagEvent;
        public UnityEvent<TagDetection[]> OnDetectedAprilTagEvent
        {
            get
            {
                if (onDetectedAprilTagEvent == null)
                {
                    onDetectedAprilTagEvent = new UnityEvent<TagDetection[]>();
                }

                return onDetectedAprilTagEvent;
            }

        }
        private static TagDetection[] tagDetection;


        [SerializeField]
        private RecognizerMode currentRecognizerMode;
        public RecognizerMode RecognizerMode { 
        get { return currentRecognizerMode; } 
        }


        private void Awake()
        {
            StartTagDetector(currentRecognizerMode);
        }

        // Update is called once per frame
        void Update()
        {
            StartDetect();

        }

      

        private void OnDestroy()
        {
            StopTagDetector();
        }

      private  void StartDetect()
        {

            switch (currentRecognizerMode)
            {
                case RecognizerMode.None:

                    return;
            
                case RecognizerMode.RGB_QRCode:
                    OnDetectedAprilTagEvent?.Invoke(tagDetection);
                    break;
                case RecognizerMode.RGB_Apriltag:
                    OnDetectedAprilTagEvent?.Invoke(tagDetection);
                    break;
                case RecognizerMode.FishEye_Apriltag:
                    OnDetectedAprilTagEvent?.Invoke(tagDetection);

                    break;
                default:
                    break;
            }

            if (tagDetection == null || tagDetection.Length == 0)
            {
                MyDebugTool.Log($"XVtagDetection.Length:tagDetection == null || tagDetection.Length == 0");

                OnDetectedAprilTagEvent?.Invoke(null);
                return;
            }

          

            try
            {
                if (tagDetection.Length > 0)
                {

                    Debug.LogError("tagDetection.Length ==== " + tagDetection.Length);
                   
                    OnDetectedAprilTagEvent?.Invoke(tagDetection);
                   
                }
                else
                {

                }
            }
            catch (Exception e)
            {
                //Handle exceptions
                MyDebugTool.Log("tagDetection is Null!!!" + e.ToString());
            }
        }


        /// <summary>
        /// Toggle detection state
        /// </summary>
        /// <param name="isDetect"></param>
        public void SetDetectStatus(bool isDetect)
        {
            
        }

        private void StartFishEyeDetector() {
            while (currentRecognizerMode== RecognizerMode.FishEye_Apriltag&&IsDetection)
            {
                MyDebugTool.Log("StartFishEyeDetector1" );

                tagDetection = XvAprilTag.StartFishEyeDetector(TagGroupName, size);//Fisheye mode


                MyDebugTool.Log("StartFishEyeDetector2" );

            }
        }

        private void StopFishEyeDetector()
        {
            XvAprilTag.StopFishEyeDetector();
        }

      


        public void StartTagDetector(RecognizerMode recognizerMode) {
            if (IsDetection && currentRecognizerMode == recognizerMode)
            {
                return;
            }

            if (IsDetection) { 
             StopTagDetector();
            }
            MyDebugTool.Log("StartTagDetector 1" + recognizerMode);
            switch (recognizerMode)
            {
                case RecognizerMode.RGB_QRCode:

                    if (!XvCameraManager.IsOn(XvCameraStreamType.ARCameraStream))
                    {
                       
                        XvCameraManager.StartCapture(XvCameraStreamType.ARCameraStream);
                    }
                    TagGroupName = "qr-code";
                    MyDebugTool.Log("StartTagDetector 11" + recognizerMode);

                    XvAprilTag.StartRgbDetector(TagGroupName, Size, OnDetecterTags);
                    MyDebugTool.Log("StartTagDetector 12" + recognizerMode);

                    break;
                case RecognizerMode.RGB_Apriltag:
                    if (!XvCameraManager.IsOn(XvCameraStreamType.ARCameraStream))
                    {
                        XvCameraManager.StartCapture(XvCameraStreamType.ARCameraStream);
                    }
                    TagGroupName = "36h11";
                    MyDebugTool.Log("StartTagDetector 21" + recognizerMode);

                    XvAprilTag.StartRgbDetector(TagGroupName, Size, OnDetecterTags);
                    MyDebugTool.Log("StartTagDetector 22" + recognizerMode);


                    break;
                case RecognizerMode.FishEye_Apriltag:
                    TagGroupName = "36h11";
                    Thread thread = new Thread(StartFishEyeDetector);
                    thread.Start();

                    break;
                default:
                    break;
            }

            IsDetection = true;
              currentRecognizerMode = recognizerMode;
        }
        public void StopTagDetector()
        {

            MyDebugTool.Log("StopTagDetector" + currentRecognizerMode);

            switch (currentRecognizerMode)
            {
                case RecognizerMode.None:
                    break;
                case RecognizerMode.RGB_QRCode:
                    XvAprilTag.StopRgbDetector();

                    break;
                case RecognizerMode.RGB_Apriltag:
                    XvAprilTag.StopRgbDetector();
                    break;
                case RecognizerMode.FishEye_Apriltag:
                    Invoke("StopFishEyeDetector",0.1f);

                    break;
                default:
                    break;
            }
            currentRecognizerMode = RecognizerMode.None;
            IsDetection = false;

        }

        [MonoPInvokeCallback(typeof(XvAprilTag.TagArrayCallback))]
        private static void OnDetecterTags(IntPtr tagArrayPtr, int count)
        {

            MyDebugTool.Log("OnDetecterTags: count=" + count );

            // Marshal TagArray
            TagData tagArray = Marshal.PtrToStructure<TagData>(tagArrayPtr);
            tagDetection = new TagDetection[count];
            for (int i = 0; i < count; i++)
            {
                API.DetectData tag = tagArray.detect[i];
              
                TagDetection detection = new TagDetection();
                detection.id = tag.tagID;
                detection.translation = new Vector3(tag.position.x, tag.position.y, tag.position.z);
                detection.rotation = new Vector3(tag.orientation.x, tag.orientation.y, tag.orientation.z);
                detection.quaternion = new Vector4(tag.quaternion.x, tag.quaternion.y, tag.quaternion.z, tag.quaternion.w);
                detection.confidence = tag.confidence;

                if (tag.qrcode == null)
                {
                    detection.qrcode = new byte[512];
                }
                else { 
                
                    detection.qrcode = tag.qrcode;
                }
                tagDetection[i] = detection;

                MyDebugTool.Log("AprilTag##StartDetector detection translation:(" + detection.translation.x + "," + detection.translation.y + "," + detection.translation.z + ")");
                MyDebugTool.Log("AprilTag##StartDetector detection rotation:(" + detection.rotation.x + "," + detection.rotation.y + "," + detection.rotation.z + ")");
                MyDebugTool.Log("AprilTag##StartDetector detection quaternion:(" + detection.quaternion.x + "," + detection.quaternion.y + "," + detection.quaternion.z + "," + detection.quaternion.w + ")");
            }
        }


    }
    
    public enum CameraType
    {
        Rgb = 0,//RGB camera
        FishEye = 1,//Fisheye camera
    }

    public enum RecognizerMode
    {
        None,
        RGB_QRCode,
        RGB_Apriltag,
        FishEye_Apriltag,
    }


}

