using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using Singray.Engine;
using Singray.Foundation;
using static Singray.Foundation.XvEyeTracking;


public class XvEyeInit : MonoBehaviour
{

    //Some glasses require UI position adjustments to fit the display
    const float uiOffset_k1 = 2.8f;
    const float uiOffset_zy = -2.3f;

    [Header("Calibration point parent")]
    public Transform calipoint1440x1440;
    public Transform calipoint1920x1200;
    public Transform calipoint1920x1080;


    [Header("Eye image parent")]
    public Transform eyeImage640x480;
    public Transform eyeImage400x400;

    [Header("Gaze point parent")]
    public Transform gazeImage1440x1440;
    public Transform gazeImage1920x1200;
    public Transform gazeImage1920x1080;

    Transform calipoint;

    Transform leftpupil;
    Transform rightpupil;

    Transform leftpupilbox;
    Transform rightpupilbox;

    Transform lefteyeimage;
    Transform righteyeimage;

    Transform recomgaze;
    //public Transform leftgaze;
    //public Transform rightgaze;


    public Transform LefteyeImage
    {
        get
        {
            return lefteyeimage;
        }
    }

    public Transform RighteyeImage
    {
        get
        {
            return righteyeimage;
        }
    }

    public Transform Recomgaze
    {
        get
        {
            return recomgaze;
        }
    }



    //[Header("Left and right 3D gaze points")]
    //public Transform recomSphere;
    //public Transform leftSphere;
    //public Transform rightSphere;

    const string TAG = "XvEyeInit";

    [Header("Eye-tracking data text")]
    public TMP_Text eyeDataText;
    //public TMP_Text ipdText;

    [Header("Eye image script")]
    public GetEyeImage getEyeImage;


    //[Header("Confirmation-point frame number text")]
    //public Text recordframeText;


    [Header("Eye-tracking interaction parent")]
    public GameObject etinteraction;


    private void OnEnable()
    {
        XvDeviceManager.OnBeforeQuit += StopGazeBeforeQuit;
    }

    private void OnDisable()
    {
        XvDeviceManager.OnBeforeQuit -= StopGazeBeforeQuit;
    }


    public static XvEyeTracking.XV_ET_EYE_DATA_EX eyeData = new XvEyeTracking.XV_ET_EYE_DATA_EX();

    static bool gazeCallback = false;

    [MonoPInvokeCallback(typeof(XvEyeTracking.fn_gaze_callback))]
    public static void OnStartGazeCallback(XvEyeTracking.XV_ET_EYE_DATA_EX gazedata)
    {
        //MyDebugTool.Log($"{TAG} OnStartGazeCallback");
        eyeData = gazedata;


        //MyDebugTool.Log($"{TAG} ipd:{gazedata.ipd}");
        gazeCallback = true;
    }


    /// <summary>
    /// Get the device serial number
    /// </summary>
    private static string device_sn;
    public string DeviceSN
    {
        get
        {
            device_sn = API.GetSerialNumber();
            return device_sn;

        }
    }

    /// <summary>
    /// Get the device firmware version
    /// </summary>
    private static string device_version;
    public string DeviceVersion
    {
        get
        {
            byte[] byteArray = new byte[512];
            API.xslam_read_device_version(ref byteArray[0]);
            device_version = Encoding.Default.GetString(byteArray, 0, byteArray.Length); //Convert the byte array to a string

            return device_version;

        }
    }


    //Define EyeExposureBright
    public struct EyeExposureBright
    {
        public int exposure_LeftGain;
        public int exposure_RightGain;
        public int exposure_LeftTimeMs;
        public int exposure_RightTimeMs;
        public int eye_Index;
        public int eye_LeftLed;
        public int eye_RightLed;
        public int eye_LeftBrightness;
        public int eye_RightBrightness;
    }
    EyeExposureBright eyeExposureBright = new EyeExposureBright();

    //Define EyeConfigs
    public struct EyeConfigs
    {
        public string note;
        public string model;
        public int ft_wid;//Display resolution width
        public int ft_hgt;//Display resolution height
        public float ft_foclen;//Display intrinsic parameter fx
        public int et_tot;
        public int et_wid;//Eye-tracking resolution width
        public int et_hgt;//Eye-tracking resolution height
        public int et_fps;
        public float et_foclen;//Eye-tracking camera intrinsic parameter fx
        public float et_occupy;
        public int et_oe_d_max;
        public int et_oe_d_min;
        public int rt_oe;
        public int sr_val;//Slippage compensation: 0 = off, 2 = medium, 3 = high
        public int hi_val;
    };

    EyeConfigs eyeConfigs = new EyeConfigs();
    public static int eyeWidth = 0;
    public static int eyeHeight = 0;

    void SetDefaultConfig()
    {
        //Use the default configuration
        configFilePath = "/data/misc/xr/default.json";
        eyeConfigs = LoadConfig(configFilePath);
        //calibrationFilePath = Path.Combine(Application.persistentDataPath, "CaliData_default.dat");
        calibrationFilePath = "/data/misc/xr/CaliData_default.dat";

        //Set exposure gain and LED brightness parameters
        eyeExposureBright.exposure_LeftGain = 10;
        eyeExposureBright.exposure_RightGain = 10;
        eyeExposureBright.exposure_LeftTimeMs = 5;
        eyeExposureBright.exposure_RightTimeMs = 5;
        eyeExposureBright.eye_Index = 2;
        eyeExposureBright.eye_LeftLed = 8;
        eyeExposureBright.eye_RightLed = 8;
        eyeExposureBright.eye_LeftBrightness = 20;
        eyeExposureBright.eye_RightBrightness = 20;


        SetAllImageFalse();
        calipoint = calipoint1920x1080;
        calipoint1920x1080.gameObject.SetActive(true);

        eyeImage400x400.gameObject.SetActive(true);

        gazeImage1920x1080.gameObject.SetActive(true);

        SetEyeIcon(eyeImage400x400, gazeImage1920x1080);
    }

    public EyeConfigs LoadConfig(string filePath)
    {
        try
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(filePath);

            // Parse with JsonUtility
            eyeConfigs = JsonUtility.FromJson<EyeConfigs>(jsonContent);

            // Apply the configuration data
            Debug.Log($"note: {eyeConfigs.note}");
            Debug.Log($"model: {eyeConfigs.model}");
            Debug.Log($"ft_wid: {eyeConfigs.ft_wid}");
            Debug.Log($"ft_hgt: {eyeConfigs.ft_hgt}");
            Debug.Log($"ft_foclen: {eyeConfigs.ft_foclen}");
            Debug.Log($"et_tot: {eyeConfigs.et_tot}");
            Debug.Log($"et_wid: {eyeConfigs.et_wid}");
            Debug.Log($"et_hgt: {eyeConfigs.et_hgt}");
            Debug.Log($"et_fps: {eyeConfigs.et_fps}");
            Debug.Log($"et_foclen: {eyeConfigs.et_foclen}");
            Debug.Log($"et_occupy: {eyeConfigs.et_occupy}");
            Debug.Log($"et_oe_d_max: {eyeConfigs.et_oe_d_max}");
            Debug.Log($"et_oe_d_min: {eyeConfigs.et_oe_d_min}");
            Debug.Log($"rt_oe: {eyeConfigs.rt_oe}");
            Debug.Log($"sr_val: {eyeConfigs.sr_val}");
            Debug.Log($"hi_val: {eyeConfigs.hi_val}");

            eyeWidth = eyeConfigs.et_wid;
            eyeHeight = eyeConfigs.et_hgt;
        }
        catch (Exception e)
        {
            MyDebugTool.LogError($"{TAG} LoadConfig failed:{e}");
            SetDefaultConfig();

        }
        return eyeConfigs;

    }

    void SetAllParams(string configPath, string caliDataPath, int leftGain, int rightGain, int leftTimeMs, int rightTimeMs, int index, int leftled, int rightled, int leftBrightness, int rightBrightness)
    {
        configFilePath = configPath;
        eyeConfigs = LoadConfig(configFilePath);
        calibrationFilePath = caliDataPath;

        //Set exposure gain and LED brightness parameters
        eyeExposureBright.exposure_LeftGain = leftGain;
        eyeExposureBright.exposure_RightGain = rightGain;
        eyeExposureBright.exposure_LeftTimeMs = leftTimeMs;
        eyeExposureBright.exposure_RightTimeMs = rightTimeMs;
        eyeExposureBright.eye_Index = index;
        eyeExposureBright.eye_LeftLed = leftled;
        eyeExposureBright.eye_RightLed = rightled;
        eyeExposureBright.eye_LeftBrightness = leftBrightness;
        eyeExposureBright.eye_RightBrightness = rightBrightness;
    }

    void SetAllImageFalse()
    {
        calipoint1440x1440.gameObject.SetActive(false);
        calipoint1920x1200.gameObject.SetActive(false);
        calipoint1920x1080.gameObject.SetActive(false);

        eyeImage640x480.gameObject.SetActive(false);
        eyeImage400x400.gameObject.SetActive(false);

        gazeImage1440x1440.gameObject.SetActive(false);
        gazeImage1920x1200.gameObject.SetActive(false);
        gazeImage1920x1080.gameObject.SetActive(false);
    }

    void SetEyeIcon(Transform eyeImage, Transform gazeImage)
    {
        leftpupil = FindChildByName(eyeImage, "leftpupilpoint");
        rightpupil = FindChildByName(eyeImage, "rightpupilpoint");

        leftpupilbox = FindChildByName(eyeImage, "leftpupilboxImage");
        rightpupilbox = FindChildByName(eyeImage, "rightpupilboxImage");

        lefteyeimage = FindChildByName(eyeImage, "lefteyeimage");
        righteyeimage = FindChildByName(eyeImage, "righteyeimage");

        recomgaze = FindChildByName(gazeImage, "recomgazepoint");
    }

    Transform FindChildByName(Transform parent, string name)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    string configFileBase = "/data/misc/xr/";
    string configFilePath;
    string calibrationFilePath;//Calibration output is saved under /sdcard/Documents
    void Start()
    {
        #region Initialize the configuration
        MyDebugTool.Log($"{TAG} Device sn:{DeviceSN}");
        MyDebugTool.Log($"{TAG} Device version:{DeviceVersion}");
        if (DeviceSN.Contains("B50RE"))
        {
            //Select the new eye-tracking mode
            XvEyeTracking.xslam_set_eyetrack_mode(2);
            MyDebugTool.Log($"{TAG} xslam_set_eyetrack_mode:2");

            //string path = Path.Combine(Application.persistentDataPath, "CaliDataB50.dat");
            string path = "CaliDataB50RE.dat";

            if (GetETparams())
            {
                SetAllParams(configFileBase + "b50.json", path, gazeParams.leftGain, gazeParams.rightGain, gazeParams.leftTime, gazeParams.rightTime, 2, gazeParams.leftLed, gazeParams.rightLed, gazeParams.leftBrightness, gazeParams.rightBrightness);
            }
            else
            {
                SetAllParams(configFileBase + "b50.json", path, 12, 12, 6, 6, 2, 8, 8, 27, 27);
            }

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1920x1080;
            //calipoint1920x1080.gameObject.SetActive(true);

            //eyeImage400x400.gameObject.SetActive(true);

            gazeImage1920x1080.gameObject.SetActive(true);

            SetEyeIcon(eyeImage400x400, gazeImage1920x1080);

        }
        else if (DeviceSN.Contains("B50HE"))
        {
            //Select the new eye-tracking mode
            XvEyeTracking.xslam_set_eyetrack_mode(2);
            MyDebugTool.Log($"{TAG} xslam_set_eyetrack_mode:2");

            //string path = Path.Combine(Application.persistentDataPath, "CaliDataB50HE.dat");
            string path = "CaliDataB50HE.dat";
            //SetAllParams(configFileBase + "b50he.json", path, 30, 10, 2, 8, 10);

            if (GetETparams())
            {
                SetAllParams(configFileBase + "b50he.json", path, gazeParams.leftGain, gazeParams.rightGain, gazeParams.leftTime, gazeParams.rightTime, 2, gazeParams.leftLed, gazeParams.rightLed, gazeParams.leftBrightness, gazeParams.rightBrightness);
            }
            else
            {
                SetAllParams(configFileBase + "b50he.json", path, 13, 13, 6, 6, 2, 8, 8, 27, 27);
            }

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1920x1200;
            //calipoint1920x1200.gameObject.SetActive(true);

            //eyeImage400x400.gameObject.SetActive(true);

            gazeImage1920x1200.gameObject.SetActive(true);

            SetEyeIcon(eyeImage400x400, gazeImage1920x1200);
        }
        else if (DeviceVersion.Contains("lyk1"))//Identify by firmware version (for older devices) or device serial number
        {
            //Select the new eye-tracking mode
            XvEyeTracking.xslam_set_eyetrack_mode(2);
            MyDebugTool.Log($"{TAG} xslam_set_eyetrack_mode:2");

            //string path = Path.Combine(Application.persistentDataPath, "CaliDataK1.dat");
            string path = "CaliDataK1.dat";

            if (GetETparams())
            {
                SetAllParams(configFileBase + "k1.json", path, gazeParams.leftGain, gazeParams.rightGain, gazeParams.leftTime, gazeParams.rightTime, 2, gazeParams.leftLed, gazeParams.rightLed, gazeParams.leftBrightness, gazeParams.rightBrightness);
            }
            else
            {
                SetAllParams(configFileBase + "k1.json", path, 20, 20, 7, 7, 2, 8, 8, 9, 9);
            }

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1440x1440;
            //calipoint1440x1440.gameObject.SetActive(true);

            //eyeImage640x480.gameObject.SetActive(true);

            gazeImage1440x1440.gameObject.SetActive(true);

            SetEyeIcon(eyeImage640x480, gazeImage1440x1440);

            //Adjust the UI position
            eyeImage640x480.parent.GetComponent<RectTransform>().position += new Vector3(0, uiOffset_k1, 0);
            etinteraction.transform.position += new Vector3(0, uiOffset_k1, 0);

        }
        else if (DeviceVersion.Contains("K40-ZY"))//Identify by firmware version (for older devices) or device serial number
        {
            //Select the new eye-tracking mode
            XvEyeTracking.xslam_set_eyetrack_mode(2);
            MyDebugTool.Log($"{TAG} xslam_set_eyetrack_mode:2");

            //string path = Path.Combine(Application.persistentDataPath, "CaliDataZY.dat");
            string path = "CaliDataZY.dat";

            if (GetETparams())
            {
                SetAllParams(configFileBase + "zy.json", path, gazeParams.leftGain, gazeParams.rightGain, gazeParams.leftTime, gazeParams.rightTime, 2, gazeParams.leftLed, gazeParams.rightLed, gazeParams.leftBrightness, gazeParams.rightBrightness);
            }
            else
            {
                SetAllParams(configFileBase + "zy.json", path, 10, 10, 5, 5, 2, 8, 8, 5, 5);
            }

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1920x1080;
            //calipoint1920x1080.gameObject.SetActive(true);

            //eyeImage640x480.gameObject.SetActive(true);

            gazeImage1920x1080.gameObject.SetActive(true);

            SetEyeIcon(eyeImage640x480, gazeImage1920x1080);

            //Adjust the UI position
            eyeImage640x480.parent.GetComponent<RectTransform>().position += new Vector3(0, uiOffset_zy, 0);
            etinteraction.transform.position += new Vector3(0, uiOffset_zy, 0);
        }
        else
        {
            SetDefaultConfig();
        }

        #endregion


        StartCoroutine("ETinit");

        recomgaze.gameObject.SetActive(true);

        //Show the interaction cubes
        etinteraction.SetActive(true);
    }

    GazeParams gazeParams = new GazeParams();
    bool GetETparams()
    {
        try
        {
            if (XvEyeTracking.xslam_get_eyetracking_params(ref gazeParams))
            {

                Debug.Log($"leftGain:{gazeParams.leftGain}\nleftTime:{gazeParams.leftTime}\nleftLed:{gazeParams.leftLed}\nleftBrightness:{gazeParams.leftBrightness}\n" +
                    $"rightGain:{gazeParams.rightGain}\nrightTime:{gazeParams.rightTime}\nrightLed:{gazeParams.rightLed}\nrightBrightness:{gazeParams.rightBrightness}");

                if (gazeParams.leftBrightness == 0 || gazeParams.rightBrightness == 0)
                {
                    Debug.LogError($"{TAG} leftBrightness or rightBrightness is 0!");
                    return false;
                }
                else
                {
                    return true;
                }

            }
            else
            {
                Debug.LogError($"xslam_get_eyetracking_params false!!!");
                return false;
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Glass Device version may not support eyetracking params!!!\n{e}");
            return false;
        }
    }

    //Left and right eyeball center coordinates
    float[] left_oe_v = new float[3];
    float[] right_oe_v = new float[3];
    void GetEyeValue()
    {
        //Query eyeball coordinates in the eye-tracking camera coordinate system
        MyDebugTool.Log($"before GetEyeValue");
        int n_left = XvEyeTracking.xslam_get_oe_value(0, left_oe_v, 0);
        MyDebugTool.Log($"xslam_get_oe_value left opengl pos:{left_oe_v[0]},{left_oe_v[1]},{left_oe_v[2]}");


        //Query eyeball coordinates in the eye-tracking camera coordinate system
        int n_right = XvEyeTracking.xslam_get_oe_value(1, right_oe_v, 0);
        MyDebugTool.Log($"xslam_get_oe_value right opengl pos:{right_oe_v[0]},{right_oe_v[1]},{right_oe_v[2]}");
    }


    byte[] data = null;


    IEnumerator ETinit()
    {
        //Set the configuration file path
        XvEyeTracking.xslam_gaze_set_config_path(configFilePath);
        MyDebugTool.Log($"{TAG} configFilePath:{configFilePath}");

        XvEyeTracking.xslam_set_gaze_configs(eyeConfigs.ft_wid, eyeConfigs.ft_hgt, -1, eyeConfigs.sr_val, eyeConfigs.et_wid, eyeConfigs.et_hgt, 8, eyeConfigs.et_foclen, eyeConfigs.et_occupy, eyeConfigs.ft_foclen, eyeConfigs.hi_val);
        MyDebugTool.Log($"{TAG} xslam_set_gaze_configs");


        bool b_start_gaze = XvEyeTracking.xslam_start_gaze();
        MyDebugTool.Log($"{TAG} xslam_start_gaze:{b_start_gaze}");

        //Wait one second before setting exposure gain to allow initialization
        yield return new WaitForSeconds(1f);


        if (b_start_gaze)
        {
            //Set exposure gain and LED brightness
            bool b_set_exposure = XvEyeTracking.xslam_set_exposure(eyeExposureBright.exposure_LeftGain, eyeExposureBright.exposure_LeftTimeMs, eyeExposureBright.exposure_RightGain, eyeExposureBright.exposure_RightTimeMs);
            MyDebugTool.Log($"{TAG} xslam_set_exposure:{b_set_exposure},exposure_LeftGain:{eyeExposureBright.exposure_LeftGain},exposure_LeftTimeMs:{eyeExposureBright.exposure_LeftTimeMs},exposure_RightGain:{eyeExposureBright.exposure_RightGain},exposure_RightTimeMs:{eyeExposureBright.exposure_RightTimeMs}");


            bool b_set_left_bright = XvEyeTracking.xslam_set_bright(0, eyeExposureBright.eye_LeftLed, eyeExposureBright.eye_LeftBrightness);
            MyDebugTool.Log($"{TAG} xslam_set_bright left:{b_set_left_bright},eye_LeftLed:{eyeExposureBright.eye_LeftLed},eye_LeftBrightness:{eyeExposureBright.eye_LeftBrightness}");
            
            bool b_set_right_bright = XvEyeTracking.xslam_set_bright(1, eyeExposureBright.eye_RightLed, eyeExposureBright.eye_RightBrightness);
            MyDebugTool.Log($"{TAG} xslam_set_bright right:{b_set_right_bright},eye_RightLed:{eyeExposureBright.eye_RightLed},eye_RightBrightness:{eyeExposureBright.eye_RightBrightness}");


            int b_set_gaze_callback = XvEyeTracking.xslam_set_gaze_callback(OnStartGazeCallback);
            MyDebugTool.Log($"{TAG} xslam_set_gaze_callback:{b_set_gaze_callback}");


            
            //Reuse the calibration file if it exists
            if (File.Exists($"/data/misc/xr/{calibrationFilePath}"))
            {
                ReadDataFromFile($"/data/misc/xr/{calibrationFilePath}", out data);

                if (data != null)
                {
                    int result = XvEyeTracking.xslam_set_pref(-1, data, data.Length);
                    Debug.Log($"{TAG} xslam_set_pref:{result}");

                }
            }


            //Query the hi_val and sr_val parameters
            int n1 = XvEyeTracking.xslam_getHiValue();
            MyDebugTool.Log($"{TAG} xslam_getHiValue:{n1}");

            int n2 = XvEyeTracking.xslam_getSrValue();
            MyDebugTool.Log($"{TAG} xslam_getSrValue:{n2}");

        }
        else
        {
            MyDebugTool.Log($"{TAG} xslam_start_gaze filled!!!");
        }


    }



    // Read data from the file
    void ReadDataFromFile(string path, out byte[] data)
    {

        //data = null;

        //try
        //{
        //    // Read the entire file into a byte array
        //    data = File.ReadAllBytes(path);
        //    Debug.Log($"{TAG} data size:{data.Length}");
        //}
        //catch (Exception ex)
        //{
        //    Debug.Log($"{TAG} Error reading file: {ex.Message}");
        //}



        data = null;

        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int length = reader.ReadInt32(); // Read the array length
                data = reader.ReadBytes(length); // Read the array contents

                MyDebugTool.Log($"{TAG} data size:{data.Length}");
            }
        }
        catch (Exception ex)
        {
            MyDebugTool.Log($"{TAG} Error reading file: {ex.Message}");
        }
    }


    public void StartRecordEyeRawImage()
    {
        XvEyeTracking.xslam_gaze_enable_dump(true);
        Debug.Log($"{TAG} xslam_gaze_enable_dump true");
    }
    public void StopRecordEyeRawImage()
    {
        XvEyeTracking.xslam_gaze_enable_dump(false);
        MyDebugTool.Log($"{TAG} xslam_gaze_enable_dump false");
    }


    bool isready = false;
    API.stereo_pdm_calibration fed
        = new API.stereo_pdm_calibration();

    void Update()
    {
        #region 
        if (API.xslam_ready() && !isready)
        {
            //MyDebugTool.Log($"Device sn:{API.GetSerialNumber()}");

            //bool b = API.readStereoDisplayCalibration(ref fed);
            //MyDebugTool.Log($"readDisplayCalibration:{b},pdm left:{fed.calibrations[0].intrinsic.K[0]},{fed.calibrations[0].intrinsic.K[1]},{fed.calibrations[0].intrinsic.K[2]},{fed.calibrations[0].intrinsic.K[3]},{fed.calibrations[0].intrinsic.K[4]}.{fed.calibrations[0].intrinsic.K[5]}," +
            //    $"{fed.calibrations[0].intrinsic.K[6]},{fed.calibrations[0].intrinsic.K[7]},{fed.calibrations[0].intrinsic.K[8]},{fed.calibrations[0].intrinsic.K[9]},{fed.calibrations[0].intrinsic.K[10]}");

            //MyDebugTool.Log($"readDisplayCalibration:{b},pdm right:{fed.calibrations[1].intrinsic.K[0]},{fed.calibrations[1].intrinsic.K[1]},{fed.calibrations[1].intrinsic.K[2]},{fed.calibrations[1].intrinsic.K[3]},{fed.calibrations[1].intrinsic.K[4]}.{fed.calibrations[1].intrinsic.K[5]}," +
            //    $"{fed.calibrations[1].intrinsic.K[6]},{fed.calibrations[1].intrinsic.K[7]},{fed.calibrations[1].intrinsic.K[8]},{fed.calibrations[1].intrinsic.K[9]},{fed.calibrations[1].intrinsic.K[10]}");

            isready = true;
        }
        #endregion



        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                Debug.LogError($"keyCode:{keyCode}");

                if (keyCode.ToString() == "UpArrow")
                {
                    Debug.Log("Up");

                    //Record the actual gaze position and raw-image frame number
                    //Debug.Log($"After calibration record image index:{eyeData.leftExData.blink}");
                    //recordframeText.text = $"Confirmation point\nFrame: {eyeData.leftExData.blink}";

                }

                if (keyCode.ToString() == "LeftArrow")
                {
                    Debug.Log("Left");
                    //StartRecordEyeRawImage();
                }
                if (keyCode.ToString() == "RightArrow")
                {
                    Debug.Log("Right");
                    //StopRecordEyeRawImage();
                }

 
            }
        }



        if (gazeCallback)
        {
            if (eyeDataText != null)
            {
                eyeDataText.text =

                    $"left GAZE_META_FIX:{eyeData.leftPupil.pupilBitMask}\n" +
                    $"left pupilbox left:{eyeData.leftPupil.pupilDistance}\n" +
                    $"left pupilbox top:{eyeData.leftPupil.pupilDiameter}\n" +
                    $"left pupilbox width:{eyeData.leftPupil.pupilDiameterMM}\n" +
                    $"left pupilbox height:{eyeData.leftPupil.pupilMinorAxis}\n" +
                    $"left:{eyeData.leftPupil.pupilMinorAxisMM}\n" +
                    $"left pupilbox confidence:{eyeData.leftGaze.exData[0]}\n" +

                    $"right GAZE_META_FIX:{eyeData.rightPupil.pupilBitMask}\n" +
                    $"right pupilbox left:{eyeData.rightPupil.pupilDistance}\n" +
                    $"right pupilbox top:{eyeData.rightPupil.pupilDiameter}\n" +
                    $"right pupilbox width:{eyeData.rightPupil.pupilDiameterMM}\n" +
                    $"right pupilbox height:{eyeData.rightPupil.pupilMinorAxis}\n" +
                    $"right:{eyeData.rightPupil.pupilMinorAxisMM}\n" +
                    $"right pupilbox confidence:{eyeData.rightGaze.exData[0]}\n" +

                    $"recom has fuse gaze:{eyeData.recomGaze.re}\n" +
                    $"recomgaze gazepoint:{eyeData.recomGaze.gazePoint.x},{eyeData.recomGaze.gazePoint.y},{eyeData.recomGaze.gazePoint.z}\n" +
                    $"recomgaze confidence:{eyeData.recomGaze.exData[0]}\n" +

                    $"leftPupil pupilCenter:{eyeData.leftPupil.pupilCenter.x},{eyeData.leftPupil.pupilCenter.y}\n" +
                    $"rightPupil pupilCenter:{eyeData.rightPupil.pupilCenter.x},{eyeData.rightPupil.pupilCenter.y}\n" +
                    $"leftgaze gazepoint:{eyeData.leftGaze.gazePoint.x} , {eyeData.leftGaze.gazePoint.y} , {eyeData.leftGaze.gazePoint.z}\n" +
                    $"rightgaze gazepoint:{eyeData.rightGaze.gazePoint.x} , {eyeData.rightGaze.gazePoint.y} , {eyeData.rightGaze.gazePoint.z}\n" +
                    $"ipd:{eyeData.ipd}\n" +
                    $"left GAZE_META_READY_FOR_CALI:{eyeData.leftGaze.gazeBitMask}\n" +
                    $"left GAZE_META_ACCEPTED_S_IDX:{(int)eyeData.leftGaze.exDataBitMask}\n" +
                    $"left GAZE_META_ACCEPTED_S_STAT:{eyeData.leftGaze.exData[1]}\n" +
                    $"right GAZE_META_READY_FOR_CALI:{eyeData.rightGaze.gazeBitMask}\n" +
                    $"right GAZE_META_ACCEPTED_S_IDX:{(int)eyeData.rightGaze.exDataBitMask}\n" +
                    $"right GAZE_META_ACCEPTED_S_STAT:{eyeData.rightGaze.exData[1]}\n";

            }

            Debug.Log($"recomgaze gazepoint:{eyeData.recomGaze.gazePoint.x},{eyeData.recomGaze.gazePoint.y}");

            if (eyeData.recomGaze.exData[0] > 0.8f)
            {

                recomgaze.GetComponent<RectTransform>().anchoredPosition = new Vector3(eyeData.recomGaze.gazePoint.x, -eyeData.recomGaze.gazePoint.y, 0);
            }




        }



    }

    //Stop eye tracking and reset its settings
    void StopGazeCalibration()
    {
        StopCoroutine("ETinit");
        gazeCallback = false;

        bool b_set_bright = XvEyeTracking.xslam_set_bright(2, 8, 0);
        MyDebugTool.Log($"{TAG} xslam_set_bright:{b_set_bright}");
        bool unset = XvEyeTracking.xslam_unset_gaze_callback();
        MyDebugTool.Log($"{TAG} xslam_unset_gaze_callback:{unset}");
        //bool b = XvEyeTracking.xslam_stop_gaze();
        //MyDebugTool.Log($"{TAG} xslam_stop_gaze:{b}");


    }


    private void StopGazeBeforeQuit()
    {
        bool b_set_bright = XvEyeTracking.xslam_set_bright(2, 8, 0);
        MyDebugTool.Log($"{TAG} xslam_set_bright:{b_set_bright}");
        bool unset = XvEyeTracking.xslam_unset_gaze_callback();
        MyDebugTool.Log($"{TAG} xslam_unset_gaze_callback:{unset}");
        //bool b = XvEyeTracking.xslam_stop_gaze();
        //MyDebugTool.Log($"{TAG} xslam_stop_gaze:{b}");
    }


}
