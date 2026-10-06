using UnityEngine;
using System.Collections;
using Singray.utils;
using System;
using System.Collections.Generic;

namespace Singray.Engine
{
    [RequireComponent(typeof(Camera))]
    public class XvXREye : MonoBehaviour
    {

        public XvXRManager.Eye eye;
        public LayerMask toggleCullingMask = 0;

        public static int EDI = 2;
        public static double EyeDistance = 0;
        public bool ChangeModel = false;
        //Left-eye rotation and translation arrays
        // eye rotation matrix (array) and transform matrix (array)
        private double[] _R;
        private double[] _T;
        //Left-eye Euler angles
        private double[] _EulerAngles;

        private XvXRStereoController mController;
        private static List<Transform> mTransformsList = new List<Transform>();
        private static List<Transform> mTransformsListAnchor = new List<Transform>();
        private bool forcedAnchor = false;

        
        private Shader depthOnlyShader;
        private RenderTexture configuredDepthTexture;
        private RenderTexture depthRenderScratchTexture;
        private Camera depthCamera;
        private Canvas[] depthCanvasCache;
        private int nextDepthCanvasCacheFrame;
        private readonly List<Canvas> depthTemporarilyDisabledCanvases = new List<Canvas>();

        public XvXRStereoController Controller
        {

            get
            {
                if (transform.parent == null) { return null; }
                if ((XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR == XvXRSdkConfig.XvXR_PLATFORM && !Application.isPlaying) || mController == null)
                {
                    mController = transform.parent.GetComponentInParent<XvXRStereoController>();
                }
                return mController;
            }

        }

        public XvXRHeadTracking Head
        {
            get
            {
                return GetComponentInParent<XvXRHeadTracking>();
            }
        }
        private Camera monoCamera;

        new public Camera camera { get; private set; }

        private void getModeList()
        {
            camera = GetComponent<Camera>();
            camera.backgroundColor = new Color(0F, 0F, 0F, 1F);
            camera.clearFlags = CameraClearFlags.Skybox;
            UnityEngine.Object[] mRenderObject;
            UnityEngine.Object[] mCanvasRenderObject;
            mRenderObject = UnityEngine.Object.FindObjectsOfType(typeof(MeshRenderer));
            float IndividualDist = 0.05f;
            mCanvasRenderObject = UnityEngine.Object.FindObjectsOfType(typeof(CanvasRenderer));
            float canIndDist = 0.5f;

            mTransformsList.Clear();


            for (int i = 0; i < mRenderObject.Length; i++)
            {
                GameObject tGO = GameObject.Find(mRenderObject[i].name);
                Vector3 tV3 = tGO.transform.position;

                bool IsOverlay = false;
                for (int j = 0; j < mTransformsList.Count; j++)
                {
                    if ((mTransformsList[j].position - tV3).magnitude < IndividualDist)
                    {
                        IsOverlay = true;
                        break;
                    }
                }
                if (IsOverlay == false)
                {
                    mTransformsList.Add(tGO.transform);
                }
            }

            if (mTransformsList.Count == 0)
            {
                for (int i = 0; i < mCanvasRenderObject.Length; i++)
                {
                    GameObject tGO = GameObject.Find(mCanvasRenderObject[i].name);
                    Vector3 tV3 = tGO.transform.position;

                    bool IsOverlay = false;
                    for (int j = 0; j < mTransformsList.Count; j++)
                    {
                        if ((mTransformsList[j].position - tV3).magnitude < canIndDist)
                        {
                            IsOverlay = true;
                            break;
                        }
                    }
                    if (IsOverlay == false)
                    {
                        mTransformsList.Add(tGO.transform);
                    }
                }
            }
        }
        void Awake()
        {
            getModeList();

            if (XvXRSdkConfig.UseDepth && depthOnlyShader == null)
            {
                depthOnlyShader = Resources.Load<Shader>("XvXRDepthOnly");
            }

            if (XvXRSdkConfig.UseDepth && depthOnlyShader == null)
            {
                UnityEngine.Debug.LogError(
                    "XvXR depth output is enabled, but Resources/XvXRDepthOnly.shader is missing.");
            }

        }

        private void ConfigureDepthOutput(RenderTexture colorRT)
        {
            camera.targetTexture = colorRT;

            if (!XvXRSdkConfig.UseDepth)
            {
                if (configuredDepthTexture != null ||
                    depthRenderScratchTexture != null ||
                    depthCamera != null ||
                    depthCanvasCache != null ||
                    depthTemporarilyDisabledCanvases.Count != 0)
                {
                    camera.ResetReplacementShader();
                    ReleaseDepthOutputResources();
                }
                return;
            }

            camera.ResetReplacementShader();

            if (depthOnlyShader == null)
            {
                depthOnlyShader = Resources.Load<Shader>("XvXRDepthOnly");
            }

            if (colorRT == null || depthOnlyShader == null)
            {
                configuredDepthTexture = null;
                return;
            }

            RenderTexture depthRT = XvXRManager.GetDepthTexture(colorRT);
            if (depthRT == null || !depthRT.IsCreated())
            {
                configuredDepthTexture = null;
                return;
            }

            configuredDepthTexture = depthRT;
            EnsureDepthRenderScratchTexture(depthRT);
        }

        private void EnsureDepthRenderScratchTexture(RenderTexture destinationDepthTexture)
        {
            if (depthRenderScratchTexture != null &&
                depthRenderScratchTexture.width == destinationDepthTexture.width &&
                depthRenderScratchTexture.height == destinationDepthTexture.height &&
                depthRenderScratchTexture.IsCreated())
            {
                return;
            }

            ReleaseDepthTexture(ref depthRenderScratchTexture);

            depthRenderScratchTexture = new RenderTexture(
                destinationDepthTexture.width,
                destinationDepthTexture.height,
                24,
                RenderTextureFormat.RFloat,
                RenderTextureReadWrite.Linear)
            {
                name = eye + "EyeLinearDepthScratch",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                hideFlags = HideFlags.HideAndDontSave
            };
            depthRenderScratchTexture.Create();
        }

        private void EnsureDepthCamera()
        {
            if (depthCamera != null)
            {
                return;
            }

            GameObject depthCameraObject = new GameObject(eye + "EyeDepthCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            depthCameraObject.transform.SetParent(transform, false);
            depthCamera = depthCameraObject.AddComponent<Camera>();
            depthCamera.enabled = false;
        }

        private void LateUpdate()
        {
            if (XvXRSdkConfig.UseDepth)
            {
                RenderDepthOutput();
            }
        }

        private void RenderDepthOutput()
        {
            if (!XvXRSdkConfig.UseDepth ||
                configuredDepthTexture == null ||
                !configuredDepthTexture.IsCreated() ||
                depthRenderScratchTexture == null ||
                !depthRenderScratchTexture.IsCreated() ||
                depthOnlyShader == null)
            {
                return;
            }

            EnsureDepthCamera();
            ConfigureDepthCamera(depthRenderScratchTexture);

            depthCamera.SetReplacementShader(depthOnlyShader, "RenderType");
            try
            {
                RenderDepthCameraWithoutCanvases();
            }
            finally
            {
                depthCamera.ResetReplacementShader();
            }

            // RFloat sampling through Graphics.Blit returns a constant value on
            // the target GLES device. Copy the completed R32F surface bit-for-bit.
            Graphics.CopyTexture(depthRenderScratchTexture, configuredDepthTexture);

        }

        private void ConfigureDepthCamera(RenderTexture targetTexture)
        {
            depthCamera.CopyFrom(camera);
            depthCamera.enabled = false;
            depthCamera.targetTexture = targetTexture;
            depthCamera.clearFlags = CameraClearFlags.SolidColor;
            // Zero means "no opaque source geometry". The native compositor
            // can then use its established rotation-only warp for skybox/UI/
            // disocclusion pixels without assuming Unity and native far planes
            // are identical.
            depthCamera.backgroundColor = Color.clear;
            depthCamera.depthTextureMode = DepthTextureMode.None;
            depthCamera.allowHDR = false;
            depthCamera.allowMSAA = false;
            depthCamera.stereoTargetEye = StereoTargetEyeMask.None;
            depthCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            depthCamera.useOcclusionCulling = false;
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                depthCamera.cullingMask &= ~(1 << uiLayer);
            }

            depthCamera.transform.SetPositionAndRotation(transform.position, transform.rotation);
            // XvXR eyes use device-calibrated matrices; copy them explicitly.
            depthCamera.worldToCameraMatrix = camera.worldToCameraMatrix;
            depthCamera.projectionMatrix = camera.projectionMatrix;
            depthCamera.cullingMatrix = camera.cullingMatrix;
        }

        private void RenderDepthCameraWithoutCanvases()
        {
            if (depthCanvasCache == null || Time.frameCount >= nextDepthCanvasCacheFrame)
            {
                depthCanvasCache = UnityEngine.Object.FindObjectsOfType<Canvas>();
                nextDepthCanvasCacheFrame = Time.frameCount + 300;
            }

            depthTemporarilyDisabledCanvases.Clear();
            if (depthCanvasCache != null)
            {
                for (int i = 0; i < depthCanvasCache.Length; ++i)
                {
                    Canvas canvas = depthCanvasCache[i];
                    if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    // Overlay canvases ignore camera culling masks. World-space
                    // canvases may also live on Default and must not write fake depth.
                    canvas.enabled = false;
                    depthTemporarilyDisabledCanvases.Add(canvas);
                }
            }

            try
            {
                // The native compositor changes GLES state outside Unity.
                GL.InvalidateState();
                depthCamera.Render();
            }
            finally
            {
                GL.InvalidateState();
                for (int i = 0; i < depthTemporarilyDisabledCanvases.Count; ++i)
                {
                    Canvas canvas = depthTemporarilyDisabledCanvases[i];
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }

                depthTemporarilyDisabledCanvases.Clear();
            }
        }

        private void OnDestroy()
        {
            ReleaseDepthOutputResources();
        }

        private void ReleaseDepthOutputResources()
        {
            configuredDepthTexture = null;

            if (depthCamera != null)
            {
                Destroy(depthCamera.gameObject);
                depthCamera = null;
            }

            ReleaseDepthTexture(ref depthRenderScratchTexture);
            depthCanvasCache = null;
            depthTemporarilyDisabledCanvases.Clear();
        }

        private void ReleaseDepthTexture(ref RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.Release();
            Destroy(texture);
            texture = null;
        }


        void Start()
        {
#if UNITY_EDITOR_WIN
            this.gameObject.SetActive(false);
            return;
#endif
            var ctlr = Controller;
            if (ctlr == null)
            {
                XvXRLog.InternalXvXRLog("vreye must be child of a stereocontroller.");
                enabled = false;
            }
            //XvXRLog.LogError("this game object name is :" + this.gameObject.name);


            monoCamera = Controller.GetComponent<Camera>();

            UpdateStereoValues();

        }


        //void OnPreCull()
        //{
        //    SetupStereo();
        //}

        /// <summary>
        /// Read display calibration from the glasses and update intrinsics and extrinsics for both Unity cameras
        /// Called by Camera.onPreCull and Start() to run custom code before camera culling
        /// </summary>
        public void UpdateStereoValues()//Original VR implementation
        {
#if UNITY_EDITOR_WIN
            // return;
#endif
            if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR || XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_IOS)
            {
                camera = GetComponent<Camera>();
                monoCamera = Controller.GetComponent<Camera>();
            }

            if (closeVrMode)
            {
                camera.CopyFrom(monoCamera);
                camera.targetTexture = null;

            }
            else
            {

                if (XvXRManager.SDK.GetDevice() == null)
                {
                    XvXRLog.LogInfo("XvXRManager.SDK.GetDevice()==null");
                }

                if (XvXRManager.SDK.GetDevice().isConnected && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_ANDROID)
                {


                    API.stereo_pdm_calibration fed_ = default(API.stereo_pdm_calibration);

                    //     XvXRLog.InternalXvXRLog("camera is :" + (camera == null) + ",monocamera is:" + (monoCamera == null));
                    if (XvXRManager.SDK.GetDevice().isReadFed == false)
                    {
                        //Read the display calibration parameter fed from the glasses; ComputeEyesFromProfile() uses it to calculate projection matrices
                        if (XvXRAndroidDevice.readStereoDisplayCalibration(ref fed_))
                        {
                            XvXRLog.LogInfo("readStereoDisplayCalibration:" + fed_);
                            XvXRManager.SDK.GetDevice().SetFed(fed_);
                            XvXRManager.SDK.GetDevice().isReadFed = true;
                            // Update parameters and send the calculated projection matrices to the Android Java library
                            XvXRManager.SDK.GetDevice().UpdateScreenData();
                        }
                        else
                        {
                            XvXRLog.LogInfo("readStereoDisplayCalibration faild");
                        }
                    }
                }
                else if (XvXRManager.SDK.GetDevice().isConnected && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
                {

                    API.stereo_pdm_calibration fed_ = default(API.stereo_pdm_calibration);

                    if (XvXRManager.SDK.GetDevice().isReadFed == false)
                    {

                        if (XvXRUnityEditorDevice.ReadStereoDisplayCalibration(ref fed_))
                        {
                            XvXRLog.LogInfo("ReadStereoDisplayCalibration:" + fed_);
                            XvXRManager.SDK.GetDevice().SetFed(fed_);
                            XvXRManager.SDK.GetDevice().isReadFed = true;
                            XvXRManager.SDK.GetDevice().UpdateScreenData();
                        }
                        else
                        {
                            XvXRLog.LogInfo("ReadStereoDisplayCalibration faild");
                        }
                    }
                }

                Matrix4x4 proj = XvXRManager.SDK.Projection(eye);


                camera.CopyFrom(monoCamera);


                if (XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR == XvXRSdkConfig.XvXR_PLATFORM)
                {
                    camera.fieldOfView = 2 * Mathf.Atan(1 / proj[1, 1]) * Mathf.Rad2Deg;
                }


                //camera.cullingMask ^= toggleCullingMask.value;
                camera.depth = monoCamera.depth;

                camera.projectionMatrix = proj;



                API.stereo_pdm_calibration fed = XvXRManager.SDK.GetDevice().GetFed();

                //Apply glasses calibration data in left-eye/right-eye order
                if (eye == XvXRManager.Eye.Left)
                {

                    if (XvXRManager.SDK.GetDevice().isReadFed)
                    {
                        _T = new double[3] { fed.calibrations[0].extrinsic.translation[0], -fed.calibrations[0].extrinsic.translation[1], fed.calibrations[0].extrinsic.translation[2] };
                        //Convert the left-eye calibration rotation matrix to Euler angles
                        _R = new double[9] { fed.calibrations[0].extrinsic.rotation[0], -fed.calibrations[0].extrinsic.rotation[1], fed.calibrations[0].extrinsic.rotation[2], -fed.calibrations[0].extrinsic.rotation[3], fed.calibrations[0].extrinsic.rotation[4],
                                     -fed.calibrations[0].extrinsic.rotation[5],fed.calibrations[0].extrinsic.rotation[6],-fed.calibrations[0].extrinsic.rotation[7],fed.calibrations[0].extrinsic.rotation[8]};
                        RotationMatrixToEulerAngles(ref _EulerAngles, _R);

                        //Set glasses extrinsics for the left camera
                        transform.localPosition = new Vector3((float)_T[0], (float)_T[1], (float)_T[2]);

                        transform.localEulerAngles = new Vector3((float)_EulerAngles[0], (float)_EulerAngles[1], (float)_EulerAngles[2]);

                    }
                    else
                    {

                    }

                    if (XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR == XvXRSdkConfig.XvXR_PLATFORM)
                    {
                        camera.fieldOfView = 2 * Mathf.Atan(1 / proj[1, 1]) * Mathf.Rad2Deg;
                        monoCamera.fieldOfView = 2 * Mathf.Atan(1 / proj[1, 1]) * Mathf.Rad2Deg;
                        monoCamera.projectionMatrix = proj;

                    }



                }
                else if (eye == XvXRManager.Eye.Right)
                {

                    if (XvXRManager.SDK.GetDevice().isReadFed)
                    {
                        //Right-eye camera
                        _T = new double[3] { fed.calibrations[1].extrinsic.translation[0], -fed.calibrations[1].extrinsic.translation[1], fed.calibrations[1].extrinsic.translation[2] };
                        _R = new double[9] { fed.calibrations[1].extrinsic.rotation[0], -fed.calibrations[1].extrinsic.rotation[1], fed.calibrations[1].extrinsic.rotation[2], -fed.calibrations[1].extrinsic.rotation[3], fed.calibrations[1].extrinsic.rotation[4],
                                     -fed.calibrations[1].extrinsic.rotation[5],fed.calibrations[1].extrinsic.rotation[6],-fed.calibrations[1].extrinsic.rotation[7],fed.calibrations[1].extrinsic.rotation[8]};

                        RotationMatrixToEulerAngles(ref _EulerAngles, _R);


                        //Set glasses extrinsics for the right camera
                        transform.localPosition = new Vector3((float)_T[0], (float)_T[1], (float)_T[2]);

                        transform.localEulerAngles = new Vector3((float)_EulerAngles[0], (float)_EulerAngles[1], (float)_EulerAngles[2]);

                    }
                    else
                    {

                    }


                }


                transform.localScale = Vector3.one;


                if (XvXRSdkConfig.sdkUseMode == XvXRSdkConfig.SDK_MODE.XvXR_UNITY_CLIENT_MODE && XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
                {

                    //Rect rect = camera.rect;
                    //Vector2 center = rect.center;
                    //center.x = Mathf.Lerp(center.x, 0.5f, 0);
                    //center.y = Mathf.Lerp(center.y, 0.5f, 0);
                    //rect.center = center;

                    //float width = Mathf.SmoothStep(-0.5f, 0.5f, (rect.width + 1) / 2);
                    //rect.x += (rect.width - width) / 2;
                    //rect.width = width;
                    //rect.x *= (0.5f - rect.width) / (1 - rect.width);

                    //if (eye == BvrManager.Eye.Right)
                    //{
                    //    rect.x += 0.5f; // Move to right half of the screen.
                    //}

                    camera.targetTexture = monoCamera.targetTexture;
                    //camera.rect = rect;
                    camera.targetTexture = (eye == XvXRManager.Eye.Left ? XvXRManager.SDK.StereoScreen[0] : XvXRManager.SDK.StereoScreen[1]);

                }
                else
                {

                    if (XvXRManager.DistortionCorrectionMethod.None == XvXRManager.SDK.DistortionCorrection || XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR)
                    {


                        Rect rect = camera.rect;
                        Vector2 center = rect.center;
                        center.x = Mathf.Lerp(center.x, 0.5f, 0);
                        center.y = Mathf.Lerp(center.y, 0.5f, 0);
                        rect.center = center;

                        float width = Mathf.SmoothStep(-0.5f, 0.5f, (rect.width + 1) / 2);
                        rect.x += (rect.width - width) / 2;
                        rect.width = width;
                        rect.x *= (0.5f - rect.width) / (1 - rect.width);

                        if (eye == XvXRManager.Eye.Right)
                        {
                            rect.x += 0.5f; // Move to right half of the screen.
                        }

                        camera.targetTexture = monoCamera.targetTexture;
                        camera.rect = rect;
                    }
                    else
                    {
                        RenderTexture colorRT = (eye == XvXRManager.Eye.Left ? XvXRManager.SDK.StereoScreen[0] : XvXRManager.SDK.StereoScreen[1]);
                        ConfigureDepthOutput(colorRT);
                      

                    }

                }


            }

        }
        bool closeVrMode = false;
        internal void CloseVrMode()
        {
            closeVrMode = true;
            XvXRManager.SDK.UpdateState();
            UpdateStereoValues();
        }

        public void SetupStereo()
        {
            closeVrMode = false;
            XvXRManager.SDK.UpdateState();
            UpdateStereoValues();
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
        {


            if (null != destTexture)
            {
                Graphics.Blit(sourceTexture, destTexture);
            }

        }
#endif
        public bool InitModel(GameObject obj, bool anchor = false)
        {
            if(anchor)
            {
                mTransformsListAnchor.Add(obj.transform);
            }
            else
            {
                mTransformsList.Add(obj.transform);
            }

            return true;
        }

        

        private void Update()
        {
            //if (ChangeModel == true)
            //{
            //    getModeList();
            //    ChangeModel = false;
            //}
            /*Camera myCamera = GetComponent<Camera>();
            
            if (eye == XvXRManager.Eye.Left)
            {
                myCamera.cullingMask = 1 << 8;
            }
            else
            {
                myCamera.cullingMask = 1 << 9;
            }
            */


            //if (eye == XvXRManager.Eye.Left) { return; }

            //XvXRManager.setTexture();

            //camera.targetTexture = (eye == XvXRManager.Eye.Left ? XvXRManager.SDK.StereoScreen[0] : XvXRManager.SDK.StereoScreen[1]);
            bool isLeft = eye == XvXRManager.Eye.Left ? true : false;

            RenderTexture colorRT = XvXRManager.GetTexture(isLeft);
            ConfigureDepthOutput(colorRT);


            // Debug.LogError("shudan Object count:" + mTransformsList.Count);
            if (isLeft)
            {
                int tCount = 0;
                float sumDis = 0.0f;
                if (mTransformsListAnchor.Count > 0)
                {
                    for (int i = 0; i < mTransformsListAnchor.Count; i++)
                    {
                        Vector3 dir = (mTransformsListAnchor[i].position - this.transform.position).normalized;
                        float dot = Vector3.Dot(this.transform.forward, dir);
                        //float dotX = Vector2.Dot(new Vector2(this.transform.forward.x, this.transform.forward.z), new Vector2(dir.x, dir.z));
                        //float dotY = Vector2.Dot(new Vector2(this.transform.forward.y, this.transform.forward.z), new Vector2(dir.y, dir.z));

                        //if (dotX >= XvXRManager.SDK.GetDevice().CosX - 0.01 && dotY >= XvXRManager.SDK.GetDevice().CosY - 0.01)
                        //{

                        //    tCount++;
                        //    sumDis += (this.transform.position - mTransformsList[i].position).magnitude;
                        //}
                        if (dot > 0.1f)
                        {
                            Vector2 viewPos = this.GetComponent<Camera>().WorldToViewportPoint(mTransformsListAnchor[i].position);
                            if (viewPos.x >= 0.0f && viewPos.x <= 1.0f && viewPos.y >= 0.0f && viewPos.y <= 1.0f)
                            {
                                tCount++;
                                sumDis += (this.transform.position - mTransformsListAnchor[i].position).magnitude;
                            }
                        }

                    }
                }
                if (tCount == 0)
                {
                    for (int i = 0; i < mTransformsList.Count; i++)
                    {
                        Vector3 dir = (mTransformsList[i].position - this.transform.position).normalized;
                        float dot = Vector3.Dot(this.transform.forward, dir);
                        //float dotX = Vector2.Dot(new Vector2(this.transform.forward.x, this.transform.forward.z), new Vector2(dir.x, dir.z));
                        //float dotY = Vector2.Dot(new Vector2(this.transform.forward.y, this.transform.forward.z), new Vector2(dir.y, dir.z));

                        //if (dotX >= XvXRManager.SDK.GetDevice().CosX - 0.01 && dotY >= XvXRManager.SDK.GetDevice().CosY - 0.01)
                        //{

                        //    tCount++;
                        //    sumDis += (this.transform.position - mTransformsList[i].position).magnitude;
                        //}
                        if (dot > 0.1f)
                        {
                            Vector2 viewPos = this.GetComponent<Camera>().WorldToViewportPoint(mTransformsList[i].position);
                            if (viewPos.x >= 0.0f && viewPos.x <= 1.0f && viewPos.y >= 0.0f && viewPos.y <= 1.0f)
                            {
                                tCount++;
                                sumDis += (this.transform.position - mTransformsList[i].position).magnitude;
                            }
                        }

                    }
                }
                XvXRManager.SetUpdateTexture(sumDis, tCount);
            }
            if (EDI < 2)
            {
                //Debug.LogError("set localPosition:" + transform.localPosition[0] + transform.localPosition[1] + transform.localPosition[2]);
                API.stereo_pdm_calibration fed = XvXRManager.SDK.GetDevice().GetFed();
                //double oldDis = (fed.calibrations[1].extrinsic.translation[0] - fed.calibrations[0].extrinsic.translation[0] - EyeDistance) / 2.0;
                //double leftT = fed.calibrations[0].extrinsic.translation[0] - oldDis;
                //double ringhtT = fed.calibrations[1].extrinsic.translation[0] + oldDis;

                if (eye == XvXRManager.Eye.Left)
                {

                    //Set glasses extrinsics for the left camera
                    transform.localPosition = new Vector3((float)fed.calibrations[0].extrinsic.translation[0], (float)-fed.calibrations[0].extrinsic.translation[1], (float)fed.calibrations[0].extrinsic.translation[2]);

                    EDI++;
                    //Debug.LogError("set localPosition left:" + transform.localPosition[0] + transform.localPosition[1] + transform.localPosition[2]);
                }
                else if (eye == XvXRManager.Eye.Right)
                {
                    transform.localPosition = new Vector3((float)fed.calibrations[1].extrinsic.translation[0], (float)-fed.calibrations[1].extrinsic.translation[1], (float)fed.calibrations[1].extrinsic.translation[2]);

                    EDI++;
                    //Debug.LogError("set localPosition Right:" + transform.localPosition[0] + transform.localPosition[1] + transform.localPosition[2]);
                }
            }
        }


        //Convert the rotation matrix to Euler angles
        internal static void RotationMatrixToEulerAngles(ref double[] eulerAngle, double[] rm)
        {

            double sy = Math.Sqrt(rm[0] * rm[0] + rm[3] * rm[3]);

            bool singular = sy < 1e-6; // If

            double x, y, z;
            if (!singular)
            {
                x = Math.Atan2(rm[7], rm[8]);
                y = Math.Atan2(-rm[6], sy);
                z = Math.Atan2(rm[3], rm[0]);
            }
            else
            {
                x = Math.Atan2(-rm[5], rm[4]);
                y = Math.Atan2(-rm[6], sy);
                z = 0;
            }
            x = x * 180.0f / Math.PI;
            y = y * 180.0f / Math.PI;
            z = z * 180.0f / Math.PI;
            eulerAngle = new double[3] { x, y, z };

        }


    }

}


