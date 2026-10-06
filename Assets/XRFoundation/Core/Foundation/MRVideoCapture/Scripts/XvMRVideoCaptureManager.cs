using System;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation
{
    public enum CaptureType
    {
        OnlyCamera,
        OnlyUnityScene,
        MR,
    }
    /// <summary>
    /// Captures mixed-reality video using XvCameraManager.
    /// Ensure the camera is running before enabling mixed-reality capture.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XvMRVideoCaptureManager : MonoBehaviour
    {
        private XvMRVideoCaptureManager() { }
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
        [SerializeField]

        private RawImage rgbBackground;

        [SerializeField]
        private CaptureType captureType = CaptureType.MR;

        private int cullingMask;

        public CaptureType CaptureType
        {
            get { return captureType; }
            set
            {

                captureType = value;

                switch (captureType)
                {
                    case CaptureType.OnlyCamera:
                        rgbBackground.enabled = true;
                        BgCamera.cullingMask = (1 << rgbBackground.gameObject.layer);
                        break;
                    case CaptureType.OnlyUnityScene:
                        rgbBackground.enabled = false;
                        BgCamera.cullingMask = cullingMask;

                        BgCamera.cullingMask |= (1 << rgbBackground.gameObject.layer);
                        break;
                    case CaptureType.MR:
                        rgbBackground.enabled = true;
                        BgCamera.cullingMask = cullingMask;
                        break;
                    default:
                        break;
                }
            }
        }






        private Camera bgCamera;
        public Camera BgCamera
        {
            get
            {
                if (bgCamera == null)
                {
                    bgCamera = transform.Find("BgCamera").GetComponent<Camera>(); ;
                }


                return bgCamera;
            }

        }
        [SerializeField]
        private bool autoCapture = false;
        /// <summary>
        /// Mixed-reality camera
        /// </summary>

        private RenderTexture cameraRenderTexture = null;

        public RenderTexture CameraRenderTexture
        {
            get
            {
                if (cameraRenderTexture == null)
                {
                    cameraRenderTexture = new RenderTexture(CameraManager.Width, CameraManager.Height, 24, RenderTextureFormat.RGB565);
                }
                return cameraRenderTexture;
            }
        }


        private bool isOn = false;
        public bool IsOn
        {
            get { return isOn; }
        }

        // Start is called before the first frame update


        private void Awake()
        {

            //


            rgbBackground.gameObject.layer = LayerMask.NameToLayer("XvBGVideo");

            cullingMask = BgCamera.cullingMask;
            BgCamera.targetTexture = CameraRenderTexture;
            CaptureType = captureType;

            if (autoCapture)
            {
                StartCapture();

            }
        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.LeftArrow))
            //{
            //    CaptureType = CaptureType.MR;
            //}
            //if (Input.GetKeyDown(KeyCode.RightArrow))
            //{
            //    CaptureType = CaptureType.OnlyCamera;
            //}

            //if (Input.GetKeyDown(KeyCode.UpArrow))
            //{
            //    CaptureType = CaptureType.OnlyUnityScene;
            //}
        }

        /// <summary>
        /// Start capturing the mixed-reality video stream.
        /// </summary>

        public void StartCapture()
        {
            //
            if (!CameraManager.IsOn(XvCameraStreamType.ARCameraStream))
            {
                MyDebugTool.Log("StartCapture XvCameraStreamType.ARCameraStream");
                CameraManager.StartCapture(XvCameraStreamType.ARCameraStream);
            }


            if (!isOn)
            {
                isOn = true;
                gameObject.SetActive(true);
                rgbBackground.gameObject.SetActive(true);
                BgCamera.gameObject.SetActive(true);
                XvCameraManager.onARCameraStreamFrameArrived.AddListener(onFrameArrived);
                MyDebugTool.Log("onARCameraStreamFrameArrived onFrameArrived");

            }

        }
        /// <summary>
        /// Stop mixed-reality capture. The camera is shared; consider other consumers before closing it.
        /// </summary>
        /// <param name="closeCamera">True closes the camera; false keeps it open</param>
        public void StopCapture(bool closeCamera = false)
        {
            if (isOn)
            {
                isOn = false;
                if (closeCamera && CameraManager.IsOn(XvCameraStreamType.ARCameraStream))
                {
                    CameraManager.StopCapture(XvCameraStreamType.ARCameraStream);
                    hasFrame = false;
                }
                XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
                gameObject.SetActive(false);
            }
        }

        private bool hasFrame = false;
        // True once at least one real camera frame has been written into rgbBackground
        // after the current camera stream was (re)started. Callers that composite/record
        // BgCamera's output should wait on this instead of a fixed delay, otherwise the
        // first frames show whatever placeholder texture rgbBackground had (usually white)
        // behind the already-rendering MR content.
        public bool HasFrame => hasFrame;

        bool onceSet = false;
        /// <summary>
        /// Map the camera image onto the background image.
        /// Update the camera parameters.
        /// </summary>
        /// <param name="cameraData"></param>
        private void onFrameArrived(cameraData cameraData)
        {
            hasFrame = true;

            if (rgbBackground != null)
            {
                rgbBackground.texture = cameraData.tex;
            }

            //BgCamera.usePhysicalProperties = true;
            //BgCamera.focalLength = cameraData.parameter.focal;
            //BgCamera.sensorSize = new Vector2(cameraData.parameter.focal * cameraData.parameter.width / cameraData.parameter.fx,
            //                              cameraData.parameter.focal * cameraData.parameter.height / cameraData.parameter.fy);

            //BgCamera.lensShift = new Vector2(-(cameraData.parameter.cx - cameraData.parameter.width * 0.5f) / cameraData.parameter.width,
            //                             (cameraData.parameter.cy - cameraData.parameter.height * 0.5f) / cameraData.parameter.height);

            //BgCamera.gateFit = Camera.GateFitMode.Vertical;


            // transform.localPosition = cameraData.parameter.position;
            //transform.localRotation = cameraData.parameter.rotation;


            // Set the RGB camera transform from its extrinsics
            BgCamera.transform.localPosition = cameraData.parameter.rgb_extrinsic_pos;
            BgCamera.transform.localRotation = cameraData.parameter.rgb_extrinsic_rot;
            //Debug.Log($"RGBdata pos:{Math.Round(BgCamera.transform.localPosition.x, 3)},{Math.Round(BgCamera.transform.localPosition.y, 3)},{Math.Round(BgCamera.transform.localPosition.z, 3)}\nrot:{BgCamera.transform.localRotation}");

            // Compute the projection matrix from RGB intrinsics
            //Debug.Log($"RGBdata internal fx:{cameraData.parameter.fx},fy:{cameraData.parameter.fy},cx:{cameraData.parameter.cx},cy:{cameraData.parameter.cy},w:{cameraData.parameter.width},h:{cameraData.parameter.height}");
            BgCamera.projectionMatrix = PerspectiveOffCenter(cameraData.parameter.fx, cameraData.parameter.fy, cameraData.parameter.cx, cameraData.parameter.cy, cameraData.parameter.width, cameraData.parameter.height, 0.03f, 1000f);
            //Debug.Log($"RGBdata projectionMatrix:\n{BgCamera.projectionMatrix}");

            // Apply the pose associated with the RGB frame timestamp
            transform.localPosition = cameraData.parameter.rgb_position;
            transform.localRotation = cameraData.parameter.rgb_rotation;
            //Debug.Log($"RGBdata transform pos:{Math.Round(transform.localPosition.x, 3)},{Math.Round(transform.localPosition.y, 3)},{Math.Round(transform.localPosition.z, 3)}\nrot:{transform.localRotation}");


            if (!onceSet)
            {

                // Adjust RGBBackground position using RGB intrinsics
                Vector3 currentPosition = rgbBackground.transform.localPosition;
                //Debug.Log($"rgbBackground currentPosition:{currentPosition}");
                float cx = cameraData.parameter.cx;
                float cy = cameraData.parameter.cy;
                float halfWidth = cameraData.parameter.width / 2;
                float halfHeight = cameraData.parameter.height / 2;

                float newX = currentPosition.x - (cx - halfWidth);

                float newY = currentPosition.y + (cy - halfHeight);

                rgbBackground.transform.localPosition = new Vector3(newX, newY, currentPosition.z);
                //Debug.Log($"rgbBackground localPosition:{Math.Round(rgbBackground.transform.localPosition.x, 3)},{Math.Round(rgbBackground.transform.localPosition.y, 3)},{Math.Round(rgbBackground.transform.localPosition.z, 3)}");
                onceSet = true;
            }

        }


        private Matrix4x4 PerspectiveOffCenter(float fx, float fy, float u0, float v0,
                                                   float w, float h, float near, float far)
        {
            float x = 2.0f * fx / w;
            float y = 2.0f * fy / h;
            float a = 1.0f - 2.0f * u0 / w;
            float b = -1.0f + 2.0f * v0 / h;
            float c = -(far + near) / (far - near);
            float d = -(2.0f * far * near) / (far - near);
            float e = -1.0f;
            Matrix4x4 m = new Matrix4x4();
            m[0, 0] = x;
            m[0, 1] = 0;
            m[0, 2] = a;
            m[0, 3] = 0;
            m[1, 0] = 0;
            m[1, 1] = y;
            m[1, 2] = b;
            m[1, 3] = 0;
            m[2, 0] = 0;
            m[2, 1] = 0;
            m[2, 2] = c;
            m[2, 3] = d;
            m[3, 0] = 0;
            m[3, 1] = 0;
            m[3, 2] = e;
            m[3, 3] = 0;
            return m;
        }
    }
    }
