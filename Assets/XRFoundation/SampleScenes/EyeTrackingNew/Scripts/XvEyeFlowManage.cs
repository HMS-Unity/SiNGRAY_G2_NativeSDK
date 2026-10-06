using AOT;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Singray.Foundation;

public class XvEyeFlowManage : MonoBehaviour
{
    int currentCalipoint = 0;
    float[] conf_gaze_o = new float[2];

    //[Header("Head")]
    //public Transform head;

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

    const string TAG = "XvEyeFlowManage";

    [Header("Eye-tracking data text")]
    public TMP_Text eyeDataText;
    //public TMP_Text ipdText;

    [Header("Eye image script")]
    public GetEyeImage getEyeImage;

    [Header("Calibration result text")]
    public Text resultText;

    //[Header("Confirmation-point frame number text")]
    //public Text recordframeText;


    [Header("Eye-tracking interaction parent")]
    public GameObject etinteraction;




    bool isCalibrating = false;
    public bool IsCalibrating
    {
        get
        {
            return isCalibrating;
        }
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
        public int exposure_Gain;
        public int exposure_TimeMs;
        public int eye_Index;
        public int eye_Led;
        public int eye_Brightness;
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


    void SetDefaultConfig()
    {
        //Use the default configuration
        configFilePath = "/data/misc/xr/default.json";
        eyeConfigs = LoadConfig(configFilePath);
        calibrationFilePath = Path.Combine(Application.persistentDataPath, "CaliData_default.dat");

        //Set exposure gain and LED brightness parameters
        eyeExposureBright.exposure_Gain = 10;
        eyeExposureBright.exposure_TimeMs = 5;
        eyeExposureBright.eye_Index = 2;
        eyeExposureBright.eye_Led = 8;
        eyeExposureBright.eye_Brightness = 20;


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


        }
        catch (Exception e)
        {
            MyDebugTool.LogError($"{TAG} LoadConfig failed:{e}");
            SetDefaultConfig();
            
        }
        return eyeConfigs;

    }

    void SetAllParams(string configPath,string caliDataPath, int gain,int timeMs,int index,int led,int brightness)
    {
        configFilePath = configPath;
        eyeConfigs = LoadConfig(configFilePath);
        calibrationFilePath = caliDataPath;

        //Set exposure gain and LED brightness parameters
        eyeExposureBright.exposure_Gain = gain;
        eyeExposureBright.exposure_TimeMs = timeMs;
        eyeExposureBright.eye_Index = index;
        eyeExposureBright.eye_Led = led;
        eyeExposureBright.eye_Brightness = brightness;
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

    void SetEyeIcon(Transform eyeImage,Transform gazeImage)
    {
        leftpupil = FindChildByName(eyeImage, "leftpupilpoint");
        rightpupil = FindChildByName(eyeImage, "rightpupilpoint");

        leftpupilbox = FindChildByName(eyeImage, "leftpupilboxImage");
        rightpupilbox = FindChildByName(eyeImage, "rightpupilboxImage");

        lefteyeimage = FindChildByName(eyeImage, "lefteyeimage");
        righteyeimage = FindChildByName(eyeImage, "righteyeimage");

        recomgaze = FindChildByName(gazeImage, "recomgazepoint");
    }

    Transform FindChildByName(Transform parent,string name)
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
    string calibrationFilePath;//Calibration output is saved under /storage/emulated/0/Android/data/<application-id>/files/
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

            string path = Path.Combine(Application.persistentDataPath, "CaliDataB50.dat");
            SetAllParams(configFileBase + "b50.json", path, 10, 6, 2, 8, 27);

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1920x1080;
            calipoint1920x1080.gameObject.SetActive(true);

            eyeImage400x400.gameObject.SetActive(true);

            gazeImage1920x1080.gameObject.SetActive(true);

            SetEyeIcon(eyeImage400x400, gazeImage1920x1080);

        }
        else if (DeviceSN.Contains("B50HE"))
        {
            string path = Path.Combine(Application.persistentDataPath, "CaliDataB50HE.dat");
            SetAllParams(configFileBase + "b50he.json", path, 30, 10, 2, 8, 10);

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1920x1200;
            calipoint1920x1200.gameObject.SetActive(true);

            eyeImage400x400.gameObject.SetActive(true);

            gazeImage1920x1200.gameObject.SetActive(true);

            SetEyeIcon(eyeImage400x400, gazeImage1920x1200);
        }
        else if (DeviceVersion.Contains("lyk1"))//Identify by firmware version (for older devices) or device serial number
        {
            string path = Path.Combine(Application.persistentDataPath, "CaliDataK1.dat");
            SetAllParams(configFileBase + "k1.json", path, 20, 7, 2, 8, 8);

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1440x1440;
            calipoint1440x1440.gameObject.SetActive(true);

            eyeImage640x480.gameObject.SetActive(true);

            gazeImage1440x1440.gameObject.SetActive(true);

            SetEyeIcon(eyeImage640x480, gazeImage1440x1440);

            //Adjust the UI position
            eyeImage640x480.parent.GetComponent<RectTransform>().position += new Vector3(0, uiOffset_k1, 0);
            etinteraction.transform.position += new Vector3(0, uiOffset_k1, 0);

        }
        else if (DeviceVersion.Contains("K40-ZY"))//Identify by firmware version (for older devices) or device serial number
        {
            string path = Path.Combine(Application.persistentDataPath, "CaliDataZY.dat");
            SetAllParams(configFileBase + "zy.json", path, 10, 5, 2, 8, 5);

            //Select the UI matching the resolution
            SetAllImageFalse();
            calipoint = calipoint1920x1080;
            calipoint1920x1080.gameObject.SetActive(true);

            eyeImage640x480.gameObject.SetActive(true);

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


        //Initialize eye tracking and wait for the user to adjust the glasses
        StartCoroutine("InitThenAdjust");


        //InvokeRepeating("GetEyeValue", 4, 1);
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

    IEnumerator InitThenAdjust()
    {

        StartCoroutine("ETinit");
        yield return new WaitForSeconds(3);

        while (leftpupil.gameObject.activeSelf == false || rightpupil.gameObject.activeSelf == false)
        //while (leftpupil.GetComponent<Image>().color==Color.red || rightpupil.GetComponent<Image>().color==Color.red)
        {
            yield return null;
            MyDebugTool.Log($"{TAG} waitting for leftpupil & rightpupil both true...");
        }

        yield return new WaitForSeconds(4);

        isETInitFinished = true;
    }


    byte[] data = null;


    public static bool isETInitFinished = false;

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
            bool b_set_exposure = XvEyeTracking.xslam_set_exposure(eyeExposureBright.exposure_Gain, eyeExposureBright.exposure_TimeMs, eyeExposureBright.exposure_Gain, eyeExposureBright.exposure_TimeMs);
            MyDebugTool.Log($"{TAG} xslam_set_exposure:{b_set_exposure}");

            bool b_set_bright = XvEyeTracking.xslam_set_bright(eyeExposureBright.eye_Index, eyeExposureBright.eye_Led, eyeExposureBright.eye_Brightness);
            MyDebugTool.Log($"{TAG} xslam_set_bright:{b_set_bright}");

            int b_set_gaze_callback = XvEyeTracking.xslam_set_gaze_callback(OnStartGazeCallback);
            MyDebugTool.Log($"{TAG} xslam_set_gaze_callback:{b_set_gaze_callback}");


            //Reuse the calibration file if it exists
            if (File.Exists(calibrationFilePath))
            {
                ReadDataFromFile(calibrationFilePath, out data);
                
                if (data != null)
                {
                    int result = XvEyeTracking.xslam_set_pref(-1, data, data.Length);
                    MyDebugTool.Log($"{TAG} xslam_set_pref:{result}");

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



    public void StartGazeCalibration()
    {

        //Start calibration
        StartCalibration();
    }



    public void StartCalibration()
    {

        //pupilImage.SetActive(true);

        etinteraction.SetActive(false);

        isCalibrating = true;

        currentCalipoint = 0;

        resultText.gameObject.SetActive(false);

        StartCoroutine("Calibrate");


    }

    void SetConfGaze(int currentCalipoint)
    {
        conf_gaze_o[0] = calipoint.GetChild(currentCalipoint).GetComponent<RectTransform>().anchoredPosition.x;
        conf_gaze_o[1] = -calipoint.GetChild(currentCalipoint).GetComponent<RectTransform>().anchoredPosition.y;
        //Left eye
        int cali0 = XvEyeTracking.xslam_gaze_set_conf_gaze(0, currentCalipoint, conf_gaze_o);
        MyDebugTool.Log($"{TAG} xslam_gaze_set_conf_gaze 0,currentCalipoint:{currentCalipoint},conf_gaze_o[0]:{conf_gaze_o[0]},conf_gaze_o[1]:{conf_gaze_o[1]},result:{cali0}");
        //Right eye
        int cali1 = XvEyeTracking.xslam_gaze_set_conf_gaze(1, currentCalipoint, conf_gaze_o);
        MyDebugTool.Log($"{TAG} xslam_gaze_set_conf_gaze 1,currentCalipoint:{currentCalipoint},conf_gaze_o[0]:{conf_gaze_o[0]},conf_gaze_o[1]:{conf_gaze_o[1]},result:{cali1}");

    }

    IEnumerator Calibrate()
    {
        recomgaze.gameObject.SetActive(false);

        //Ready for calibration
        MyDebugTool.Log($"{TAG} before xslam_set_usr_eye_ready");
        XvEyeTracking.xslam_set_usr_eye_ready();
        MyDebugTool.Log($"{TAG} xslam_set_usr_eye_ready");

        yield return new WaitForSeconds(0.2f);

        int begin0 = XvEyeTracking.xslam_gaze_calibration_begin(0);
        MyDebugTool.Log($"{TAG} xslam_gaze_calibration_begin 0:{begin0}");
        int begin1 = XvEyeTracking.xslam_gaze_calibration_begin(1);
        MyDebugTool.Log($"{TAG} xslam_gaze_calibration_begin 1:{begin1}");


        yield return new WaitForSeconds(1);



        #region Calibrate three points in one pass
        int round = 1;
        while (round > 0)
        {
            while (currentCalipoint < 3)
            {

                //Show the current calibration point
                for (int i = 0; i < calipoint.transform.childCount; i++)
                {
                    if (i == currentCalipoint)
                    {
                        calipoint.transform.GetChild(i).gameObject.SetActive(true);
                    }
                    else
                    {
                        calipoint.transform.GetChild(i).gameObject.SetActive(false);
                        //Set inactive points to green
                        calipoint.GetChild(currentCalipoint).GetComponent<Image>().color = new Color(0, 1f, 0.4f, 1f);
                    }
                }
                //Calibrate only during fixation, when both fix and ready for cali are 1
                if (eyeData.leftPupil.pupilBitMask == 1 && eyeData.rightPupil.pupilBitMask == 1 && eyeData.leftGaze.gazeBitMask == 1 && eyeData.rightGaze.gazeBitMask == 1)
                {
                    //Turn the calibration point yellow while collecting calibration data
                    calipoint.GetChild(currentCalipoint).GetComponent<Image>().color = new Color(1, 1, 0, 1f);
                    yield return new WaitForSeconds(0.5f);//Allow the user to fixate on the calibration point before collecting data

                    if (currentCalipoint == 0)
                    {
                        do
                        {
                            yield return new WaitForSeconds(0.5f);
                            SetConfGaze(currentCalipoint);
                            //After xslam_gaze_set_conf_gaze, wait one second for stat to update. Values 0, 1, 3, 7 correspond to binary 000, 001, 011, 111; 1, 3, 7 indicate completion of points 1, 2, 3.
                            yield return new WaitForSeconds(1f);
                            MyDebugTool.Log($"{TAG} after xslam_gaze_set_conf_gaze currentCalipoint == 0,left stat:{eyeData.leftGaze.exData[1]},right stat:{eyeData.rightGaze.exData[1]}");

                        } while (eyeData.leftGaze.exData[1] == 0 || eyeData.rightGaze.exData[1] == 0);
                    }
                    else if (currentCalipoint == 1)
                    {
                        do
                        {
                            yield return new WaitForSeconds(0.5f);
                            SetConfGaze(currentCalipoint);

                            yield return new WaitForSeconds(1f);
                            MyDebugTool.Log($"{TAG} after xslam_gaze_set_conf_gaze currentCalipoint == 1,left stat:{eyeData.leftGaze.exData[1]},right stat:{eyeData.rightGaze.exData[1]}");

                        } while (eyeData.leftGaze.exData[1] == 1 || eyeData.rightGaze.exData[1] == 1);
                    }
                    else if (currentCalipoint == 2)
                    {
                        do
                        {
                            yield return new WaitForSeconds(0.5f);
                            SetConfGaze(currentCalipoint);

                            yield return new WaitForSeconds(1f);
                            MyDebugTool.Log($"{TAG} after xslam_gaze_set_conf_gaze currentCalipoint == 2,left stat:{eyeData.leftGaze.exData[1]},right stat:{eyeData.rightGaze.exData[1]}");

                        } while (eyeData.leftGaze.exData[1] == 3 || eyeData.rightGaze.exData[1] == 3);
                    }


                    //Advance to the next point after the current point is calibrated
                    currentCalipoint++;

                }
                yield return new WaitForSeconds(1.5f);


            }
            round--;
            currentCalipoint = 0;
        }
        #endregion


        //yield return new WaitForSeconds(1);
        int end0 = XvEyeTracking.xslam_gaze_calibration_end(0);
        MyDebugTool.Log($"{TAG} xslam_gaze_calibration_end end0:{end0}");
        int end1 = XvEyeTracking.xslam_gaze_calibration_end(1);
        MyDebugTool.Log($"{TAG} xslam_gaze_calibration_end end1:{end1}");

        resultText.gameObject.SetActive(true);
        if (end0 == 1 && end1 == 1)
        {
            //resultText.text = $"Calibration succeeded\nend0: {end0}, end1: {end1}";
            resultText.text = $"Calibration succeeded\nIPD: {eyeData.ipd}mm";

            //Save the calibration file
            SaveCalibrationData();
        }
        else
        {
            //resultText.text = $"Calibration failed\nend0: {end0}, end1: {end1}";
            resultText.text = $"Calibration failed";
        }

        for (int i = 0; i < calipoint.transform.childCount; i++)
        {
            calipoint.transform.GetChild(i).gameObject.SetActive(false);
        }


        isCalibrating = false;

        recomgaze.gameObject.SetActive(true);

        //Show the interaction cubes
        etinteraction.SetActive(true);



    }


    
    public void SaveCalibrationData()
    {
        int size = 0;
        byte[] caliData = new byte[4096];
        GCHandle cd = GCHandle.Alloc(caliData, GCHandleType.Pinned);
        int result = XvEyeTracking.xslam_get_pref(-1, cd.AddrOfPinnedObject(), ref size);
        MyDebugTool.Log($"{TAG} xslam_get_pref:{result},size:{size}");
        cd.Free();


        try
        {
            if (File.Exists(calibrationFilePath))
            {
                File.Delete(calibrationFilePath);

            }
            WriteDataToFile(calibrationFilePath, caliData);


        }
        catch (Exception ex)
        {
            MyDebugTool.Log($"{TAG} check calibration file error:{ex}");
        }


    }

    // Write data to the file
    void WriteDataToFile(string path, byte[] data)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(data.Length); // Write the array length
                writer.Write(data);         // Write the array contents


            }

            MyDebugTool.Log($"{TAG} Data written to file: {path}");
        }
        catch (Exception ex)
        {
            MyDebugTool.Log($"{TAG} Error writing file: {ex.Message}");
        }
    }



    // Read data from the file
    void ReadDataFromFile(string path, out byte[] data)
    {
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

                if (keyCode.ToString() == "Return")
                {
                    if (isCalibrating == true)
                    {
                        //StopGazeCalibration();
                    }
                    else
                    {
                        if (isETInitFinished == true)
                        {
                            StartGazeCalibration();
                        }
                        else
                        {
                            

                        }

                    }
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
                    $"left pupilbox average:{eyeData.leftPupil.pupilMinorAxisMM}\n" +
                    $"left pupilbox confidence:{eyeData.leftGaze.exData[0]}\n" +

                    $"right GAZE_META_FIX:{eyeData.rightPupil.pupilBitMask}\n" +
                    $"right pupilbox left:{eyeData.rightPupil.pupilDistance}\n" +
                    $"right pupilbox top:{eyeData.rightPupil.pupilDiameter}\n" +
                    $"right pupilbox width:{eyeData.rightPupil.pupilDiameterMM}\n" +
                    $"right pupilbox height:{eyeData.rightPupil.pupilMinorAxis}\n" +
                    $"right pupilbox average:{eyeData.rightPupil.pupilMinorAxisMM}\n" +
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


            #region Update the pupil and pupil-box icon positions in real time
            leftpupil.GetComponent<RectTransform>().anchoredPosition = new Vector2(eyeConfigs.et_wid - eyeData.leftPupil.pupilCenter.x, eyeConfigs.et_hgt - eyeData.leftPupil.pupilCenter.y);
            rightpupil.GetComponent<RectTransform>().anchoredPosition = new Vector2(eyeConfigs.et_wid - eyeData.rightPupil.pupilCenter.x, eyeConfigs.et_hgt - eyeData.rightPupil.pupilCenter.y);


            float lefttmp = eyeData.leftPupil.pupilDiameterMM / 2;
            leftpupilbox.GetComponent<RectTransform>().anchoredPosition = new Vector2(eyeConfigs.et_wid - (eyeData.leftPupil.pupilDistance + lefttmp), eyeConfigs.et_hgt - (eyeData.leftPupil.pupilDiameter + lefttmp));
            leftpupilbox.GetComponent<RectTransform>().sizeDelta = new Vector2(eyeData.leftPupil.pupilDiameterMM, eyeData.leftPupil.pupilDiameterMM);


            float righttmp = eyeData.rightPupil.pupilDiameterMM / 2;
            rightpupilbox.GetComponent<RectTransform>().anchoredPosition = new Vector2(eyeConfigs.et_wid - (eyeData.rightPupil.pupilDistance + righttmp), eyeConfigs.et_hgt - (eyeData.rightPupil.pupilDiameter + righttmp));
            rightpupilbox.GetComponent<RectTransform>().sizeDelta = new Vector2(eyeData.rightPupil.pupilDiameterMM, eyeData.rightPupil.pupilDiameterMM);


            #region Check whether the pupil is inside the green area to guide glasses adjustment
            if (leftpupil.GetComponent<RectTransform>().anchoredPosition.x > eyeConfigs.et_wid * 0.25f && leftpupil.GetComponent<RectTransform>().anchoredPosition.x < eyeConfigs.et_wid * 0.75f && leftpupil.GetComponent<RectTransform>().anchoredPosition.y > eyeConfigs.et_hgt * 0.25f && leftpupil.GetComponent<RectTransform>().anchoredPosition.y < eyeConfigs.et_hgt * 0.75f)
            {
                //leftpupil.GetComponent<Image>().color = Color.green;
                leftpupil.gameObject.SetActive(true);
            }
            else
            {
                //leftpupil.GetComponent<Image>().color = Color.red;
                leftpupil.gameObject.SetActive(false);
            }

            if (rightpupil.GetComponent<RectTransform>().anchoredPosition.x > eyeConfigs.et_wid * 0.25f && rightpupil.GetComponent<RectTransform>().anchoredPosition.x < eyeConfigs.et_wid * 0.75f && rightpupil.GetComponent<RectTransform>().anchoredPosition.y > eyeConfigs.et_hgt * 0.25f && rightpupil.GetComponent<RectTransform>().anchoredPosition.y < eyeConfigs.et_hgt * 0.75f)
            {
                //rightpupil.GetComponent<Image>().color = Color.green;
                rightpupil.gameObject.SetActive(true);
            }
            else
            {
                //rightpupil.GetComponent<Image>().color = Color.red;
                rightpupil.gameObject.SetActive(false);
            }
            #endregion

            //leftgaze.GetComponent<RectTransform>().anchoredPosition = new Vector3(eyeData.leftGaze.gazePoint.x, -eyeData.leftGaze.gazePoint.y, 0);
            //rightgaze.GetComponent<RectTransform>().anchoredPosition = new Vector3(eyeData.rightGaze.gazePoint.x, -eyeData.rightGaze.gazePoint.y, 0);

            if (eyeData.recomGaze.exData[0] > 0.8f)
            {

                recomgaze.GetComponent<RectTransform>().anchoredPosition = new Vector3(eyeData.recomGaze.gazePoint.x, -eyeData.recomGaze.gazePoint.y, 0);
            }


            #endregion



        }



    }

    //Stop eye tracking and reset its settings
    void StopGazeCalibration()
    {
        isETInitFinished = false;
        StopCoroutine("ETinit");
        StopCoroutine("Calibrate");
        gazeCallback = false;

        bool b_set_bright = XvEyeTracking.xslam_set_bright(2, 8, 0);
        MyDebugTool.Log($"{TAG} xslam_set_bright:{b_set_bright}");
        bool unset = XvEyeTracking.xslam_unset_gaze_callback();
        MyDebugTool.Log($"{TAG} xslam_unset_gaze_callback:{unset}");
        bool b = XvEyeTracking.xslam_stop_gaze();
        MyDebugTool.Log($"{TAG} xslam_stop_gaze:{b}");


        currentCalipoint = 0;
        resultText.gameObject.SetActive(false);
        resultText.text = $" ";
        //Hide all calibration points
        for (int i = 0; i < calipoint.transform.childCount; i++)
        {
            calipoint.transform.GetChild(i).gameObject.SetActive(false);
            //Set inactive points to green
            calipoint.GetChild(currentCalipoint).GetComponent<Image>().color = new Color(0, 1f, 0.4f, 1f);
        }


        //ipdText.text = $"ipd:";

        isCalibrating = false;

    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            bool b_set_bright = XvEyeTracking.xslam_set_bright(2, 8, 0);
            MyDebugTool.Log($"{TAG} xslam_set_bright:{b_set_bright}");
            bool unset = XvEyeTracking.xslam_unset_gaze_callback();
            MyDebugTool.Log($"{TAG} xslam_unset_gaze_callback:{unset}");
            bool b = XvEyeTracking.xslam_stop_gaze();
            MyDebugTool.Log($"{TAG} xslam_stop_gaze:{b}");
        }

    }

    private void OnApplicationQuit()
    {

    }



    //Get a single frame
    public void GetCurrentFrameEyeData()
    {
        //Left and right eyeball center coordinates
        Debug.Log($"left opengl pos:{left_oe_v[0]},{left_oe_v[1]},{left_oe_v[2]}");
        Debug.Log($"right opengl pos:{right_oe_v[0]},{right_oe_v[1]},{right_oe_v[2]}");
        //2D gaze-point coordinates
        Debug.Log($"recommend gaze:{recomgaze.GetComponent<RectTransform>().anchoredPosition}");
    }

}
