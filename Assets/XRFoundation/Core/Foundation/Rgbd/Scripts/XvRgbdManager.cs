
using System.Collections;
using UnityEngine;
using static API;

namespace Singray.Foundation
{

    /// <summary>
    /// Provides RGBD start/stop controls and converts RGB pixel coordinates to 3D positions
    /// </summary>
    public sealed class XvRgbdManager : MonoBehaviour
    {
        private XvRgbdManager() { }


        [SerializeField]
        private XvCameraManager cameraManager;


        public XvCameraManager CameraManager
        {
            get
            {

                if (cameraManager == null)
                {
                    cameraManager = FindObjectOfType<XvCameraManager>();
                }

                if (cameraManager == null)
                {
                    cameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
                }
                return cameraManager;

            }
        }


        private double hostTimestamp;
        private Coroutine startRoutine;
        private bool isRunning;


        /// <summary>
        /// Start RGBD
        /// </summary>
        public void StartRgbPose()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            MyDebugTool.LogDiagnosticWarning("RGBD", "editor", "RGBD requires the Android SDK and a connected device", 0f);
            return;
#else
            if (isRunning || startRoutine != null)
            {
                return;
            }
            startRoutine = StartCoroutine(StartRgbPoseRoutine());
#endif
        }

        private IEnumerator StartRgbPoseRoutine()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!API.xslam_ready())
            {
                MyDebugTool.LogDiagnosticWarning("RGBD", "wait-sdk", "waiting for SDK before starting RGBD", 2f);
                if (Time.realtimeSinceStartup >= deadline)
                {
                    startRoutine = null;
                    Debug.LogWarning("RGBD: SDK readiness timeout.");
                    yield break;
                }
                yield return null;
            }

            API.xv_start_rgb_pixel_pose();
            CameraManager.StartCapture(XvCameraStreamType.ARCameraStream);
            CameraManager.StartCapture(XvCameraStreamType.TofDepthCameraStream);

            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
            XvCameraManager.onARCameraStreamFrameArrived.AddListener(onFrameArrived);
            isRunning = true;
            startRoutine = null;
            MyDebugTool.LogDiagnostic("RGBD", "start", "RGB pixel pose, RGB stream and ToF depth stream started", 0f);
        }
        /// <summary>
        /// Stop RGBD
        /// </summary>
        public void StopRgbPose()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            return;

#endif
            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }
            if (!isRunning) return;
            API.xv_stop_rgb_pixel_pose();
            CameraManager.StopCapture(XvCameraStreamType.ARCameraStream);
            CameraManager.StopCapture(XvCameraStreamType.TofDepthCameraStream);
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
            isRunning = false;
            hostTimestamp = 0;
            MyDebugTool.LogDiagnostic("RGBD", "stop", "RGBD and its RGB/ToF streams stopped", 0f);
        }


        /// <summary>
        /// Get a 3D position from RGB pixel coordinates
        /// </summary>
        /// <param name="rgbPoint">RGB pixel coordinates</param>
        /// <param name="spacePoint">3D position in space</param>
        /// <returns></returns>
        public bool GetRgbPixel3DPose(Vector2 rgbPoint,ref Vector3 spacePoint)
        {


#if !UNITY_ANDROID || UNITY_EDITOR
            return false;
 
#endif

            if (!isRunning || hostTimestamp <= 0) return false;
            API.Vector2F rgbPixelPoint = default(API.Vector2F);
            rgbPixelPoint.x = rgbPoint.x;
            rgbPixelPoint.y = rgbPoint.y;

            API.Vector3F pointerPose = default(API.Vector3F);
            if (API.xv_get_rgb_pixel_pose(ref pointerPose, ref rgbPixelPoint, hostTimestamp, 30))
            {
                spacePoint.x = pointerPose.x;
                spacePoint.y = -pointerPose.y;
                spacePoint.z = pointerPose.z;
                return true;
            }

            return false;
        }
        /// <summary>
        /// Convert a list of RGB pixel coordinates to a list of 3D positions
        /// </summary>
        /// <param name="rgbPoint"></param>
        /// <param name="spacePose"></param>
        /// <returns></returns>
        public bool GetRgbPixelPoseList( Vector2[] rgbPoint,ref pointer_3dpose[] spacePose)
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            return false;

#endif

            if (!isRunning || hostTimestamp <= 0 || rgbPoint == null || spacePose == null || spacePose.Length < rgbPoint.Length) return false;
            int size = rgbPoint.Length;

          
            if (API.xslam_start_get_rgb_pixel_buff3d_pose(spacePose, rgbPoint, size, hostTimestamp, 30))
            {

                for (int i = 0; i < spacePose.Length; i++)
                {
                    spacePose[i].pointerPose.y *= -1;
                }
                return true;
            }
            return false;

        }

        private void OnDisable() => StopRgbPose();

        private void onFrameArrived(cameraData cameraData)
        {
            hostTimestamp = cameraData.parameter.timeStamp;
            MyDebugTool.LogDiagnostic("RGBD", "timestamp", $"rgbTimestamp={hostTimestamp:F6}", 2f);
        }

    }
}
