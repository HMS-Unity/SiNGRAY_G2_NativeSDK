using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Singray.Engine;

namespace Singray.Foundation.SampleScenes
{
    public class XvPointCloudDemo : MonoBehaviour
    {
        public XvParticlesCloudPoint particlesCloudPoint;
        Vector3[] vecGroup;

        public Text info;
        public Slider slider;


        public Text info_0;
        public Slider slider_0;

        public Text info_1;
        public Slider slider_1;

        public Text info_2;
        public Slider slider_2;
        public Text vvv;

        private int v_0 = 4;
        private int v_1 = 1;
        private int v_2 = 5;
        private float v_3 = 0.2f;

        [SerializeField]
        private XvCameraManager cameraManager;


        public XvCameraManager XvCameraManager
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

        private void OnEnable()
        {
            slider_0.onValueChanged.AddListener(changeIpd0);
            slider_1.onValueChanged.AddListener(changeIpd1);
            slider_2.onValueChanged.AddListener(changeIpd2);
            slider.onValueChanged.AddListener(changeIpd);
        }

        private void OnDisable()
        {
            slider_0.onValueChanged.RemoveListener(changeIpd0);
            slider_1.onValueChanged.RemoveListener(changeIpd1);
            slider_2.onValueChanged.RemoveListener(changeIpd2);
            slider.onValueChanged.RemoveListener(changeIpd);

            CancelInvoke();
            StopTofPointCloud();
        }

        private void OnDestroy()
        {
            if (pixelHandle.IsAllocated)
            {
                pixelHandle.Free();
            }
        }


        private void Start()
        {

        }

        private int countTime = 0;

        private bool ifSave = false;

        // Update is called once per frame
        void Update()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            return;
#endif
            if (!API.xslam_ready())
            {
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode.ToString() == "Return")
                    {
                        API.xslam_reset_slam();
                    }

                    if (keyCode.ToString() == "UpArrow")
                    {
                        Invoke("StartTofPointCloud", 1);

                        CancelInvoke("saveAsc");
                        ifSave = true;
                        particlesCloudPoint.ifCreatFold = true;
                        InvokeRepeating("saveAsc", 1, 1);
                    }

                    if (keyCode.ToString() == "DownArrow")
                    {
                        CancelInvoke("saveAsc");
                        StopTofPointCloud();
                        particlesCloudPoint.ClearPointCloud();
                    }

                    if (keyCode.ToString() == "LeftArrow")
                    {
                        InvokeRepeating("screenShot", 1, 1);
                    }
                    if (keyCode.ToString() == "RightArrow")
                    {
                        CancelInvoke("screenShot");
                    }
                }
            }

            if (ifSave == false)
            {
                return;
            }

            countTime++;
            if (countTime == 10)
            {
                countTime = 0;

                if (XvCameraManager.GetPointCloudData(out vecGroup))
                {
                    particlesCloudPoint.gameObject.SetActive(true);
                    particlesCloudPoint.StartDraw(vecGroup);
                }
            }
        }


        private int lastWidth = 0;
        private int lastHeight = 0;
        private Texture2D tex = null;
        private GCHandle pixelHandle;
        private Color32[] pixel32;
        private IntPtr pixelPtr;
        private double rgbTimestamp = 0;

        public void screenShot()
        {
            int width = API.xslam_get_rgb_width();
            int height = API.xslam_get_rgb_height();
            if (width > 0 && height > 0)
            {
                if (lastWidth != width || lastHeight != height)
                {
                    try
                    {
                        double r = 1.0;
                        int w = (int)(width * r);
                        int h = (int)(height * r);
                        TextureFormat format = TextureFormat.RGBA32;
                        if (pixelHandle.IsAllocated)
                        {
                            pixelHandle.Free();
                        }
                        if (tex != null)
                        {
                            Destroy(tex);
                        }
                        tex = new Texture2D(w, h, format, false);
                        tex.Apply();
                        pixel32 = tex.GetPixels32();
                        pixelHandle = GCHandle.Alloc(pixel32, GCHandleType.Pinned);
                        pixelPtr = pixelHandle.AddrOfPinnedObject();
                    }
                    catch (Exception e)
                    {
                        return;
                    }

                    lastWidth = width;
                    lastHeight = height;
                }
            }

            if (tex == null)
            {
                return;
            }

            if (API.xslam_get_rgb_image_RGBA_flip(pixelPtr, tex.width, tex.height, ref rgbTimestamp))
            {
                tex.SetPixels32(pixel32);
                tex.Apply();
                if (particlesCloudPoint.folderName == "")
                {
                    particlesCloudPoint.folderName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                }
                string path = "/storage/emulated/0/CollectData/RGB/" + particlesCloudPoint.folderName;
                bool a = CreateFolderIfNotExists(path);
                long t = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                File.WriteAllBytes(path + "/" + t + "_" + XvXRManager.SDK.HeadPose.Position.x*100 + "," + XvXRManager.SDK.HeadPose.Position.y * 100 + "," + XvXRManager.SDK.HeadPose.Position.z * 100 + "_" + XvXRManager.SDK.HeadPose.Orientation.x + "," + XvXRManager.SDK.HeadPose.Orientation.y + "," + XvXRManager.SDK.HeadPose.Orientation.z + "," + XvXRManager.SDK.HeadPose.Orientation.w + ".png", tex.EncodeToPNG());
            }
        }

        /// <summary>
        /// Create the directory if it does not exist.
        /// </summary>
        /// <param name="folderPath">Full directory path</param>
        /// <returns>True if the directory exists or was created; otherwise false</returns>
        public bool CreateFolderIfNotExists(string folderPath)
        {
            try
            {
                // Check for an empty path
                if (string.IsNullOrEmpty(folderPath))
                {
                    Debug.LogError("Directory path is empty.");
                    return false;
                }

                // Check whether the directory exists
                if (!Directory.Exists(folderPath))
                {
                    // Create the directory and any missing parent directories
                    Directory.CreateDirectory(folderPath);
                    Debug.Log($"Directory created: {folderPath}");
                    return true;
                }
                else
                {
                    Debug.Log($"Directory already exists: {folderPath}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create directory: {folderPath}\nError: {e.Message}");
                return false;
            }
        }

        private void saveAsc()
        {
            particlesCloudPoint.savePointCloud();
        }

        public void StartTofPointCloud()
        {
            ifSave = true;
            countTime = 0;
            XvCameraManager.StartTofPointCloud();
        }

        public void StopTofPointCloud()
        {
            bool wasRunning = ifSave;
            ifSave = false;
            if (particlesCloudPoint != null) particlesCloudPoint.gameObject.SetActive(false);
            if (wasRunning && cameraManager != null) cameraManager.StopTofPointCloud();
        }

        public void SetUp()
        {
            XvCameraManager.SetTofExposure(v_0, v_1, v_2, v_3);
        }

        private void changeIpd0(float value)
        {
            v_0 = int.Parse(slider_0.value.ToString());

            info_0.text = v_0 + " ";
        }

        private void changeIpd1(float value)
        {
            v_1 = int.Parse(slider_1.value.ToString());

            info_1.text = v_1 + " ";
        }

        private void changeIpd2(float value)
        {
            v_2 = int.Parse(slider_2.value.ToString());

            info_2.text = v_2 + " ";
        }

        private void changeIpd(float value)
        {
            v_3 = slider.value;

            info.text = v_3 + " ";
        }
    }
}
