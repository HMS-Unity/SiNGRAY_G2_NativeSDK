using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Singray.Foundation
{
    public class XvEyeTracking
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public float x;
            public float y;
            public float z;
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct GazePoint
        {

            public uint gazeBitMask;
            public Point gazePoint;
            public Point rawPoint;
            public Point smoothPoint;
            public Point gazeOrigin;
            public Point gazeDirection;
            public float re;
            public uint exDataBitMask;
        };


        [StructLayout(LayoutKind.Sequential)]
        public struct XV_ET_EYE_DATA_EX
        {
            public ulong timestamp;   //!<timestamp.
            public int recommend;    //!<whether if there has the recommend point. 0-no recommend point, 1-use left eye as recommend point, 2-use right eye as recomment point.
            public XV_ET_GAZE_POINT recomGaze; //!<recommend gaze data
            public XV_ET_GAZE_POINT leftGaze;  //!<left eye gaze data
            public XV_ET_GAZE_POINT rightGaze; //!<right eye gaze data

            public XV_ET_PUPIL_INFO leftPupil;  //!<left eye pupil data
            public XV_ET_PUPIL_INFO rightPupil; //!<right eye pupil data

            public XV_ET_EYE_EXDATA leftExData;  //!<left eye extend data(include blink and eyelid data)
            public XV_ET_EYE_EXDATA rightExData; //!<right eye extend data(include blink and eyelid data)

            public int leftEyeMove;//!<0-Eye movement type is no-eye detected. 1-Eye movement type is blink. 2-Eye movement type is noraml.
            public int rightEyeMove;//!<0-Eye movement type is no-eye detected. 1-Eye movement type is blink. 2-Eye movement type is noraml.
            public float ipd; //IPD data becomes valid only after eye-tracking calibration
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct XV_ET_GAZE_POINT
        {
            public uint gazeBitMask;               //!<gaze bit mask, identify the six data below are valid or invalid.
            public XV_ETPoint3D gazePoint;     //!<gaze point, x and y are valid, z default value is 0, x and y scope are related to the input calibration point, not fixed.
            public XV_ETPoint3D rawPoint;      //!<gaze point before smooth, x and y are valid, z default value is 0, x and y scope are as above.
            public XV_ETPoint3D smoothPoint;   //!<gaze point after smooth, x and y are valid, z default value is 0, x and y scope are as above.
            public XV_ETPoint3D gazeOrigin;    //!<origin gaze center coordinate.
            public XV_ETPoint3D gazeDirection; //!<gaze direction.
            public float re;                       //!<gaze re value, confidence level.
            public uint exDataBitMask;             //!<reserved data.
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.R4)]
            public float[] exData;                 //!<reserved data. 32
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct XV_ETPoint3D
        {
            public float x;
            public float y;
            public float z;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct XV_ET_PUPIL_INFO
        {
            public uint pupilBitMask;            //!<pupil bit mask, identify the six data below are valid or invalid.
            public xv_ETPoint2D pupilCenter; //!<pupil center(0-1), the coordinate value of pupil center in the image, normalization value, image height and width is 1.
            public float pupilDistance;          //!<the distance between pupil and camera(mm)
            public float pupilDiameter;          //!<pupil diameter, pupil long axis value(0-1), the ratio of the pixel value of the long axis size of the pupil ellipse to the image width, normalization value.
            public float pupilDiameterMM;        //!<pupil diameter, pupil long axis value(mm).
            public float pupilMinorAxis;         //!<pupil diameter, pupil minor axis value(0-1), the ratio of the pixel value of the minor axis size of the pupil ellipse to the image width, normalization value.
            public float pupilMinorAxisMM;       //!<pupil diameter, pupil minor axis value(mm).
        };


        [StructLayout(LayoutKind.Sequential)]
        public struct xv_ETPoint2D
        {
            public float x;
            public float y;
        };



        [StructLayout(LayoutKind.Sequential)]
        public struct XV_ET_EYE_EXDATA
        {
            public uint eyeDataExBitMask;         //!<eye extend data bit mask, identify the four data below are valid or invalid.
            public int blink;                     //!<blink data, 0-no blink, 1-start blinking, 2-closing process, 3-close eyes, 4-opening process, 5-finish blinking.
            public float openness;                //!<eye openness(0-100), 0-cloing, 100-opening normally, >100-opening on purpose.
            public float eyelidUp;                //!<up eyelid data(0-1), up eyelid's vertical position in the image, normalization value, image height is 1.
            public float eyelidDown;              //!<down eyelid data(0-1), down eyelid's vertical position in the image, normalization value, image height is 1.
        };

        public delegate void fn_gaze_callback(XV_ET_EYE_DATA_EX gazedata);



        //Eye-tracking API
        [DllImport("xslam-unity-wrapper")]
        public static extern void pub_set_usr_eye_ready();

        //[DllImport("xslam-unity-wrapper")]
        //public static extern void xslam_set_gaze_configs(int width, int height);

        [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void xslam_gaze_set_config_path(string coe_path);

        [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int xslam_gaze_calibration_apply(string file);//Reuse a previously generated calibration file

        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_start_gaze();
        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_stop_gaze();
        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_set_exposure(int leftGain, float leftTimeMs, int rightGain, float rightTimeMs);
        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_set_gaze_callback(fn_gaze_callback cb);
        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_unset_gaze_callback();

        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_set_bright(int eye, int led, int brightness);



        //Eye-tracking APIs
        [StructLayout(LayoutKind.Sequential)]
        public struct GazeCalibStatus
        {
            /** The status of API CalibrationEnter */
            public int enter_status;

            /** The status of API CalibrationCollect */
            public int collect_status;

            /** The status of API CalibrationSetup */
            public int setup_status;

            /** The status of API CalibrationComputeApply */
            public int compute_apply_status;

            /** The status of API CalibrationLeave */
            public int leave_status;

            /** The status of API CalibrationReset */
            public int reset_status;
        };

        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_gaze_calibration_enter();//Enter calibration mode

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_leave();//Exit calibration mode after completing the calibration workflow

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_collect(float x, float y, float z, int index);//Calibrate five points

        [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int xslam_gaze_calibration_retrieve(string file);//Retrieve calibration results and save them to a custom file



        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_reset();//Clear all collected calibration points and reset calibration parameters

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_compute_apply();//Compute calibration parameters and apply them to the device

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_setup();//Configure the calibration workflow

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_query_status(ref GazeCalibStatus status);//Query the internal calibration state



        //Eye-tracking image API
        [DllImport("xslam-unity-wrapper")]
        public static extern bool xv_eyetracking_start();

        [DllImport("xslam-unity-wrapper")]
        public static extern bool xv_eyetracking_stop();

        [DllImport("xslam-unity-wrapper")]
        public static extern bool xv_eyetracking_get_rgba(System.IntPtr left, System.IntPtr right, ref int width, ref int height);






        #region New eye-tracking API

        [StructLayout(LayoutKind.Sequential)]
        public struct GazeParams
        {
            public int leftGain;
            public int rightGain;
            public int leftTime;
            public int rightTime;
            public int leftLed;
            public int leftBrightness;
            public int rightLed;
            public int rightBrightness;
        }
        //public GazeParams gazeParams = new GazeParams();
        //Read eye-tracking brightness values stored in the glasses during factory calibration
        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_get_eyetracking_params(ref GazeParams p);


        //Set mode: 1 = old eye tracking, 2 = new eye tracking. Default compatibility mode selects old tracking for firmware containing the dmk marker and new tracking otherwise.
        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_set_eyetrack_mode(int mode);

        /// <summary>
        /// Set display resolution and IPD distance parameters
        /// </summary>
        /// <param name="width">Display resolution width</param>
        /// <param name="height">Display resolution height</param>
        /// <param name="ipdDist">Distance parameter in millimeters. Default -1 uses IPD with both eyes gazing at infinity; supported values are 200, 300, 500, 1000 and 10000. Confirm the parameter semantics with the supplier.</param>
        /// <param name="srValue">Slippage compensation: 0 = off, 2 = medium, 3 = high</param>
        /// <param name="etWidth">Eye-tracking resolution width</param>
        /// <param name="etHeight">Eye-tracking resolution height</param>
        /// <param name="loplength">Smoothing level, 1-8</param>
        /// <param name="etFoclen">Eye-tracking camera intrinsic parameter fx</param>
        /// <param name="etOccupy"></param>
        /// <param name="ftFoclen">Display intrinsic parameter fx</param>
        /// <param name="hiValue">Supplier describes this as display intrinsic fx; confirm the parameter semantics.</param>

        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_set_gaze_configs(int width, int height, float ipdDist, int srValue, int etWidth, int etHeight, int loplength, float etFoclen, float etOccupy, float ftFoclen, int hiValue);


        //Calibration API
        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_set_usr_eye_ready();


        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_begin(int et_idx);

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_set_conf_gaze(int et_idx, int s_idx, [In, Out] float[] conf_gaze_o);

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_gaze_calibration_end(int et_idx);

        [DllImport("xslam-unity-wrapper")]
        public static extern bool xslam_get_gaze_status();

        [DllImport("xslam-unity-wrapper")]
        public static extern void xslam_gaze_enable_dump(bool enable);//Start or stop dataset recording; save the dataset under /sdcard/etImage/

        /// <summary>
        /// Read and reuse a saved calibration file
        /// </summary>
        /// <param name="etIdx">eye index, 0 or 1 for individual side, -1 for both sides</param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_set_pref(int etIdx, byte[] data, int size);
        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_get_pref(int etIdx, IntPtr data, ref int size);


        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_getHiValue();

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_getSrValue();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="etIdx">Left/right eye index</param>
        /// <param name="oe_v">Eyeball position xyz in eye-tracking camera coordinates, in millimeters</param>
        /// <param name="cs">Reference frame: 0 = OpenGL, 1 = OpenCV</param>
        /// <returns></returns>
        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_get_oe_value(int etIdx, [In, Out] float[] oe_v, int cs);

        [DllImport("xslam-unity-wrapper")]
        public static extern int xslam_get_eye_value(int etIdx, [In, Out] float[] eye_v, int cs);        

        #endregion
    }
}