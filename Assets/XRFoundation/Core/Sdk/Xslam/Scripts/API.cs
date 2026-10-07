using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using static Singray.Foundation.XvAprilTag;
using System.Drawing;

public class API : MonoBehaviour
{

    // ** Struct **

    public struct Rotation9
    {
        public float r0;
        public float r1;
        public float r2;
        public float r3;
        public float r4;
        public float r5;
        public float r6;
        public float r7;
        public float r8;
    };

    /**
     * @brief Quaternion structure
     */
    public struct Quaternion
    {
        public double x;
        public double y;
        public double z;
        public double w;
    };

    /**
     * @brief 3DOF structure
     */
    public struct Orientation
    {
        public long hostTimestamp; //!<Timestamp in µs read on host
        public long deviceTimestamp; //!<Timestamp in µs read on the device
        public Quaternion quaternion; //!< Absolute quaternion (3DoF)
        public double roll; //!< Absolute roll euler angle (3DoF)
        public double pitch; //!< Absolute pitch euler angle (3DoF)
        public double yaw; //!< Absolute yaw euler angle (3DoF)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] angularVelocity; //!< Instantaneous angular velocity (radian/second)
    };

    /**
     * @brief Rotation and translation structure
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct ctransform
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public double[] rotation; //!< Rotation matrix (row major)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] translation; //!< Translation vector
    };

    /**
     * @brief Polynomial Distortion Model
     */
    public struct pdm
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public double[] K;
        /**
            Projection and raytrace formula can be found here:
            https://docs.opencv.org/3.4.0/d4/d94/tutorial_camera_calibration.html

            K[0] : fx
            K[1] : fy
            K[2] : u0
            K[3] : v0
            K[4] : k1
            K[5] : k2
            K[6] : p1
            K[7] : p2
            K[8] : k3
            K[9] : image width
            K[10] : image height
        */
    };

    /**
     * @brief Unified camera model
     */
    public struct unified
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public double[] K;
        /**
          Projection and raytrace formula can be found here:
          1.  C. Geyer and K. Daniilidis, “A unifying theory for central panoramic systems and practical applications,” in Proc. 6th Eur. Conf. Comput. Vis.
        II (ECCV’00), Jul. 26, 2000, pp. 445–461
          or
          2. "J.P. Barreto. General central projection systems, modeling, calibration and visual
        servoing. Ph.D., University of Coimbra, 2003". Section 2.2.2.

          K[0] : fx
          K[1] : fy
          K[2] : u0
          K[3] : v0
          K[4] : xi
          K[5] : image width
          K[6] : image height

          More details,
          Projection:
            The simplest camera model is represented by projection relation:    p = 1/z K X
            where p=(u v)^T is an image point, X = (x y z)^T is a spatial point to be projected
            and K is a projection matrix: K = (fx 0 u0; 0 fy v0).

            The distortion model is added in the following manner.
            First we project all the points onto the unit sphere S
                Qs = X / ||X|| = 1/rho (x y z)   where rho = sqrt(X^2+Y^2+Z^2)
            and then we apply the perspective projection with center (0 0 -xi)^T of Qs onto plan image
                p = 1/(z/rho + xi) K (x/rho  y/rho).

          Back-projection/raytrace:
            The normalized coordinate of a pixel is (x y 1)^1.
            We know that a line joining this normalized point and the projection center intersects the unit sphere
            at a point Qs. This point is defined as
                Qs = (eta*x  eta*y  eta-xi)
            where scale factor    eta = (xi + sqrt(1 + (x^2+y^2)(1-xi^2))) / (x^2+y^2+1).
        */
    };

    public struct unified_calibration
    {
        public ctransform extrinsic;
        public unified intrinsic;
    };

    public struct stereo_fisheyes
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public unified_calibration[] calibrations;
    };

    public struct pdm_calibration
    {
        public ctransform extrinsic;
        public pdm intrinsic;
    };

    public struct stereo_pdm_calibration
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public pdm_calibration[] calibrations;
    };

    public struct rgb_calibration
    {
        public ctransform extrinsic;
        public pdm intrinsic1080; //!< 1920x1080
        public pdm intrinsic720; //!< 1280x720
        public pdm intrinsic480; //!< 640x480
    };
    public class plane
    {
        public List<Vector3D> points;
        public Vector3D normal;
        public double d;
        public string id;
    };
    public struct imu_bias
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] gyro_offset;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] accel_offset;
    };
    public struct Vector3D
    {
        public double X;
        public double Y;
        public double Z;
    };
    public struct Vector2F
    {
        public float x;
        public float y;
    };
    public struct Vector3F
    {
        public float x;
        public float y;
        public float z;
    };
    public struct Vector4D
    {
        public double X;
        public double Y;
        public double Z;
        public double W;
    };
    public struct Vector4F
    {
        public float x;
        public float y;
        public float z;
        public float w;
    };
    public struct GestureData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] index;                                                 //!<Index array for hands gesture, max size is two, default is -1 means invalid.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public Vector3D[] points;                                                 //!<Position array for hand gesture,  max size is two, 2D points, z isn't used by default.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public Vector3D[] slamPosition;                                                //!<Convert rgb points into slam points, Position array for hand gesture,  max size is two.
        public double hostTimestamp;          //!< host timestamp of the physical measurement (in second based on the `std::chrono::steady_clock`).
        public double edgeTimestampUs; //!< timestamp of the physical measurement (in microsecond based on edge clock).
        public float distance;                                                          //!< reserved, dynamic gesture movement distance.
        public float confidence;                                                        //!<reserved, gesture confidence.
    };
    public struct hand_keypoints
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
        public Vector3[] point;
    };


    // ** Init **

    // Init SDK, the device will be found automatically
    // Note: Not working on non rooted Android device
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_init();

    // Uninit SDK
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_uninit();

    // Init SDK, the device will be open using the giving file descriptor
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_init_with_fd(int fd);

    public enum Component
    {
        ALL = 0xFFFF,

        // init callbacks
        IMU = 0x0001,
        POSE = 0x0002,
        STEREO = 0x0004,
        RGB = 0x0008,
        TOF = 0x0010,
        EVENTS = 0x0040,
        CNN = 0x0080,

        // channels
        HID = 0x0100,
        UVC = 0x0200,
        VSC = 0x0400,
        SLAM = 0x0800,  // depends on HID, UVC and VSC
        EDGEP = 0x1000,  // depends on SLAM, supply 3DOF(temp)
    };

    // Init SDK, the device will be found automatically
    // Usage: xslam_init_components( (int)(Component.IMU | Component.STEREO | Component.EVENTS | Component.HID | Component.UVC) )
    // Note: Not working on non rooted Android device, should use xslam_init_components_with_fd
    // Note: If not ALL, must specify channels and streams to use
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_init_components(int components);

    // Init SDK, the device will be open using the giving file descriptor
    // Usage: xslam_init_components( (int)(Component.IMU | Component.STEREO | Component.EVENTS | Component.HID | Component.UVC) )
    // Note:  If not ALL, must specify channels and streams to use
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_init_components_with_fd(int fd, int components);

    // Return true if the device is open and the SDK ready
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_ready();


    // ** SLAM **

    // Reset the slam to zero
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_reset_slam();
    /*
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct XslamImageTrackerResult
    {
        public int templateId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string templateName;
        /// <summary>
        /// TRACKED
        /// CRITICAL
        /// LOST
        /// </summary>
        public int status;
        public float quality;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] pose;
        public int inlierCount;
        public float hostTimestamp;
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ImageTrackerCallback(IntPtr results, int size);

    // Reset the slam to zero
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_image_tracker(string lic, string name, string picname, float width, float height,int resolution,int cameraTyep, ImageTrackerCallback s_callback);

    // Reset the slam to zero
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_cmr_model_tracker(string lic, string modePath, string dataPath, ImageTrackerCallback s_callback);

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_stop_image_tracker();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_stop_cmr_model_tracker();
    */
    // Set the SLAM source
    // - 0: Edge (SLAM on the device)
    // - 1: Mixed mode (SLAM on the host)
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_slam_type(int type);

    // Get the transformation matrix and the corresponding timestamp form the SLAM source
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_transform(ref Matrix4x4 matrix, ref long timestamp, ref int status);

    // Get the transformation matrix and the corresponding timestamp form the SLAM source
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_transform_matrix([In, Out] float[] matrix, ref long timestamp, ref int status);

    // Get the position vector, the orientation (euler angles) and the corresponding timestamp form the SLAM source
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_6dof(ref Vector3 position, ref Vector3 orientation, ref long timestamp);

    //getpose with prediction
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_pose_prediction([In, Out] double[] poseData, ref long timestamp, double prediction);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_pose_at([In, Out] double[] poseData, double timestamp);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_pose_confidence(ref double confidence);
    

    // ** IMU **
    // start imu
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_imu();

    // stop imu
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_stop_imu();
    // Get the IMU data as array of Vector3 and the corresponding timestamp
    // Note: Initialize the array with a size of 3
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_imu_array([In, Out] Vector3[] imu, ref double timestamp);

    // Get the IMU data as three Vector3 and the corresponding timestamp
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_imu(ref Vector3 accel, ref Vector3 gyro, ref Vector3 magn, ref long timestamp);



    // ** 3DOF **

    // Get the 3DOF data
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_3dof(ref Orientation o);


    // ** Stereo **

    // Get the stereo image width, can return 0 if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_stereo_width();

    // Get the stereo image height, can return 0 if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_stereo_height();

    // Get the stereo left image with RGBA format, set width and height to resize the image or 0 to keep the original size
    // Return false if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_left_image(System.IntPtr data, int width, int height, ref double timestamp);

    // Get the stereo right image with RGBA format, set width and height to resize the image or 0 to keep the original size
    // Return false if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_right_image(System.IntPtr data, int width, int height, ref double timestamp);

    // Get the maximal number of points which can be detected in the image
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_stereo_max_points();

    // Get the points detected in the left image, size will be set to the current number of points
    // Note: Initialize the array with a size of N Vector2 (get N form xslam_get_stereo_max_points)
    // Return false if the data is not available
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_left_points([In, Out] Vector2[] points, ref int size);

    // Get the points detected in the right image, size will be set to the current number of points
    // Note: Initialize the array with a size of N Vector2 (get N form xslam_get_stereo_max_points)
    // Return false if the data is not available
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_right_points([In, Out] Vector2[] points, ref int size);

    // Start stereo stream.
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_stereo_stream();

    // Stop stereo stream.
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_stereo_stream();



    // ** RGB **

    // 0: UVC / 1:VSC
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_rgb_source(int source);

    // Should same to XSlam::VSC::RgbResolution
    // RGB_1920x1080 = 0, ///< RGB 1080p
    // RGB_1280x720  = 1, ///< RGB 720p
    // RGB_640x480   = 2, ///< RGB 480p
    // RGB_320x240   = 3, ///< RGB QVGA
    // RGB_2560x1920 = 4, ///< RGB 5 MP; requires compatible glasses firmware
    // RGB_3840x2160 = 5, ///< Requires compatible glasses firmware
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_rgb_resolution(int res);

    // Get the RGB image width, return 0 if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_rgb_width();

    // Get the RGB image height, return 0 if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_rgb_height();

    // Get the RGB image with RGBA format, set width and height to resize the image or 0 to keep the original size
    // `timestamp` should be saved for next call to avoid get same image
    // Return false if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_rgb_image_RGBA(System.IntPtr data, int width, int height, ref double timestamp);

    [DllImport("xslam-unity-wrapper", EntryPoint = "xslam_get_rgb_image_RGBA")]
    public static extern bool xslam_get_rgb_image_RGBA_Byte([In, Out] byte[] data, int width, int height, ref double timestamp);
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_rgb_image_RGBA_flip(System.IntPtr data, int width, int height, ref double timestamp);
    // Get the RGB image with YUV format(I420), set width and height to resize the image or 0 to keep the original size
    // `timestamp` should be saved for next call to avoid get same image
    // Return false if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_rgb_image_YUV(System.IntPtr data, int width, int height, ref double timestamp);

    // Start RGB stream.
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_rgb_stream();

    // Stop RGB stream.
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_rgb_stream();


    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_get_sony_tof_image([In, Out]  ushort[] data);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_get_tof_image([In, Out]  char[] data);


    /// <summary>
    /// Fixed focus
    /// </summary>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_prime_lens();
    /// <summary>
    /// Zoom
    /// </summary>
    /// <returns></returns>

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_zoom_lens();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="aecMode">   0:auto exposure 1:manual exposure </param>
    /// <param name="exposureGain">exposureGain Only valid in manual exposure mode, [0,255]</param>
    /// <param name="exposureTimeMs">exposureTimeMs Only valid in manual exposure mode    milliseconds</param>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_rgb_set_exposure(int aecMode, int exposureGain, float exposureTimeMs);

    //RGBD


    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_start_rgb_pixel_pose();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_stop_rgb_pixel_pose();

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_get_rgb_pixel_3dpose(ref Vector3F pointerPose, ref Vector2F rgbPixelPoint, float radius);


    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_get_rgb_pixel_pose(ref Vector3F pointerPose, ref Vector2F rgbPixelPoint, double hostTimestamp, float radius);


    [StructLayout(LayoutKind.Sequential)]
    public struct pointer_3dpose
    {
        public Vector2 rgbPixelPoint;
        public Vector3 pointerPose;
        public bool isValid;
    }

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_get_rgb_pixel_buff3d_pose([In, Out] pointer_3dpose[] pointerPose, [In, Out] Vector2[] rgbPixelPoint, int arraySize, double hostTimestamp, float radius);



    // ** TOF **

    // Get the TOF image width, return 0 if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_tof_width();
 

    // Get the TOF image height, return 0 if no image is available
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_tof_height();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_get_tofir_size(ref int width, ref int height);
    [DllImport("xslam-unity-wrapper")]

    public static extern void xslam_tof_enbale_ir_gramma(bool enable);

    // Get the TOF image with RGBA format, set width and height to resize the image or 0 to keep the original size
    // Return false if no image is available
 
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_tof_image(System.IntPtr data, int width, int height, ref double timestamp);


    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_tofir_image(System.IntPtr data, int width, int height, ref double timestamp);


    // Get the TOF depth data
    // Note: Initialize the array with a size of N float (get N form xslam_get_tof_width * xslam_get_tof_height)
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_depth_data([In, Out] float[] data);

    // Get the TOF cloud point
    // Note: Initialize the array with a size of N Vector3 (get N form xslam_get_tof_width * xslam_get_tof_height)
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_cloud_data([In, Out] Vector3[] data);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_cloud_data_ex([In, Out] Vector3[] data);//The API data is already transformed and can be used directly

    // Start TOF stream.
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_tof_stream();



    //[DllImport("xslam-unity-wrapper")]
    //public static extern void xslam_start_tofir_stream();


    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_start_tofir_stream(int libmode, int resulution, int fps);

    // Stop TOF stream.
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_tof_stream();
    
    //Set ToF mode: 0 = DepthOnly, 1 = CloudOnly, 2 = DepthAndCloud, 3 = None, 4 = CloudOnLeftHandSlam
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_tof_set_steam_mode(int cmd);



    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_tof_set_exposure(int aecMode, int exposureGain, float exposureTimeMs);

    [DllImport("xslam-unity-wrapper")]

    public static extern bool xv_get_tof_image_depth([In, Out] float[] data);


    

    // ** Other **

    // Get HID event
    // return event info and timestamp.
    // For GrooveX controller:
    //        key      action   type state
    //        GPIO 3   key down  08   01
    //        GPIO 3   key up    08   02
    //        GPIO 43  key down  08   03
    //        GPIO 43  key up    08   04
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_event(ref int type, ref int state, ref long timestamp);



    // ** HID **

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_write_and_read_hid(IntPtr wdata, int wlen, IntPtr rdata, int rlen);

    // Write HID command and get result
    public static byte[] HidWriteAndRead(byte[] wdata, int wlen)
    {
        try
        {
            byte[] rdata = new byte[128];
            GCHandle wh = GCHandle.Alloc(wdata, GCHandleType.Pinned);
            GCHandle rh = GCHandle.Alloc(rdata, GCHandleType.Pinned);
            bool ret = xslam_write_and_read_hid(wh.AddrOfPinnedObject(), wlen, rh.AddrOfPinnedObject(), 128);
            wh.Free();
            rh.Free();
            if (ret)
            {
                return rdata;
            }
        }
        catch (Exception e)
        {
            MyDebugTool.Log(e.Message);
        }
        return null;
    }


    // ** Read Calibrations **

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readIMUBias(ref imu_bias bias);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readStereoFisheyesCalibration(ref stereo_fisheyes calib, ref int imu_fisheye_shift_us);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readDisplayCalibration(ref pdm_calibration calib);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readToFCalibration(ref pdm_calibration calib);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readRGBCalibration(ref rgb_calibration calib);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readStereoFisheyesPDMCalibration(ref stereo_pdm_calibration calib);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool readStereoDisplayCalibration(ref stereo_pdm_calibration calib);


    // ** CNN **

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_cnn_model(string path);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_cnn_descriptor(string path);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_cnn_source(int source);


    // ** Sound **

    // Play buff
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_play_sound(IntPtr ptr, int len);

    // Play file(Raw path like /sdcard/a.pcm)
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_play_sound_file(string path);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_is_playing();

    // Stop play
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_stop_play();

    // Start record
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_mic_callback(DataCallback cb);

    // Stop record
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_unset_mic_callback();

    public delegate void DataCallback(IntPtr data, int len);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_hand_landmark_xyz([In, Out] Vector3[] handdata, ref int type, ref Matrix4x4 matrix);

    // ** Play Expert **

    // Enable speaker
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_speaker_stream();

    // Disable speaker
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_speaker_stream();

    // Send small sound data (<= 7680 bytes) to device
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_transfer_speaker_buffer(IntPtr ptr, int len);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_detect_plane_from_tof();

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_detect_plane_from_tof_nosurface();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_tof_set_framerate(float framerate);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_plane_from_tof(IntPtr data, ref int len);
    
    // ** hand **

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_skeleton();
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_skeleton(int id);
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_slam_skeleton();
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_slam_skeleton(int id);
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_gesture();
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_gesture(int source);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_dynamic_gesture();
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_detect_plane_from_tof();
    [DllImport("xslam-unity-wrapper")]

    public static extern bool xslam_stop_dynamic_gesture(int source);
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_sony_tof_stream(int libmode, int resulution, int fps);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_set_hand_config_path_s(string path);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_hand_keypoints(ref hand_keypoints keypoints, int type);
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_gesture(ref GestureData gesture);
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_dynamic_gesture(ref GestureData gesture);

    //Gesture
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Point
    {
        public float x;
        public float y;
        public float z;
    };
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RotatePoint
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XvXRSkeleton
    {
        public int size;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 42, ArraySubType = UnmanagedType.Struct)]
        //public Point[] joints;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 52, ArraySubType = UnmanagedType.Struct)]
        public Point[] joints_ex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 52, ArraySubType = UnmanagedType.Struct)]
        public RotatePoint[] rotateData;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.Struct)]
        public float[] scale;//Fine-tune the virtual hand model size
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.Struct)]
        public int[] status;//Static gesture
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.Struct)]
        public double[] timestamp;
        public double fisheye_timestamp;//Current fisheye timestamp

        public long dataFetchTimeMs;
        public long dataTimeStampMs;
    };

    public enum PLATFORM 
    { 
        LINUX_CPU = 0, 
        ANDROID_GPU, 
        ANDROID_DSP, 
        ANDROID_NPU 
    };

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_gesture_platform(int platform);
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_gesture_ego( bool ego);//Set gesture perspective: ego true = first person, false = third person

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_gesture_filter(int level, bool easy_pinch);



    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_skeleton_with_cb(int type, xslam_skeleton_callback cb);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_skeleton_ex_with_cb(xslam_skeleton_callback cb);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_gesture_ex_with_cb(xslam_gesture_callback cb);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_surface_callback(bool enableSuface, bool enableTexturing, xslam_surface_callback cb);

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_enable_surface_reconstruction(bool enable);//Enable or disable mesh generation

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_skeleton_with_cb(int type, int id);
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_slam_skeleton_with_cb(int type, int id);

    public delegate void xslam_skeleton_callback(XvXRSkeleton skeleton);

    public delegate void xslam_gesture_callback(GestureData gesture);

    public delegate void xslam_surface_callback(IntPtr surfaces, int size);

    // ** AprilTag **
    [StructLayout(LayoutKind.Sequential)]
    public struct DetectData
    {
        public int tagID;
        public Vector3F position;
        public Vector3F orientation;
        public Vector4F quaternion;
        public long edgeTimestamp;
        public double hostTimestamp;
        public float confidence;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] qrcode;
    };

    public struct TagData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public DetectData[] detect;
    };

    // start fisheye detect
    //[DllImport("xslam-unity-wrapper")]
    //public static extern int xslam_start_detect_tags(string tagFamily, double size, ref TagData tagsArray, int arraySize);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_detect_tags(string tagFamily, double size, ref TagData tagsArray, int arraySize);

    // stop fisheye detect
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_stop_detect_tags();

    // start rgb detect
    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_rgb_detect_tags(string tagFamily, double size, ref TagData tagsArray, int arraySize);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_start_rgb_detect_tags(string tagFamily, double size);
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_getTagDetectionrgbImage(string tagFamily, double size, TagArrayCallback tagArrayCallback);


    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_rgb_detect_tags(ref TagData tagsArray, int arraySize);


   // stop rgb detect
   [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_stop_rgb_detect_tags();


    //fe
    [DllImport("xslam-unity-wrapper")]
    public static extern  void xslam_start_fisheyes_rectification_thread();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_stop_fisheyes_rectification_thread();

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_fisheyes_rectification_thread();


    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_fe_images_data(ref int width,ref int height, [In, Out] byte[] left, [In, Out] byte[] right, [In, Out] double[] poseData);


    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_fe_mesh_params(ref double focal,ref double baseline, ref int camerasModelWidth, ref int camerasModelHeight,  [In, Out] double[] leftPose,   [In, Out] double[] rightPose);


    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_device_status_callback(device_status_callback_ex cb);

    public delegate void device_status_callback_ex(deviceStatus_package xp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct deviceStatus_package
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public int[] status;
    }

    /// <summary>
    /// mode 0 1 2 
    /// isroot rk true ;bb false
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="isroot"></param>
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_start_mode(int mode, bool isroot);
	
	[DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_start_mode();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_glass_ipd(int value, bool isroot);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_glass_ipd();
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_glass_ipd2(int value, bool isroot);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_glass_ipd2();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_set_glass_Light(int value, bool isroot);

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_glass_Light();

    [DllImport("xslam-unity-wrapper")]
    public static extern int xslam_get_box_channel();


    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_display_set_brightnesslevel(int level); //level specifies the brightness level


    /// <summary>
    /// Start SLAM
    /// </summary>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_start_map();
    /// <summary>
    /// Stop SLAM
    /// </summary>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_stop_map();
    /// <summary>
    /// Save map API
    /// </summary>
    /// <param name="mapStream">Map output path on the local device</param>
    /// <param name="cslamSavedCallback">Callback after saving the map</param>
    /// <param name="cslamLocalizedCallBack">Map localization quality callback</param>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_save_map_and_switch_to_cslam(string mapStream, detectCslamSaved_callback cslamSavedCallback, detectLocalized_callback cslamLocalizedCallBack);//size 0.16 rate 4

    /// <summary>
    /// Load map API
    /// </summary>
    /// <param name="mapStream">Path of the map to load</param>
    /// <param name="cslamSwitchedCallback"></param> Supplier describes this as the loaded map path; confirm the callback contract.
    /// <param name="cslamLocalizedCallBack">Map localization quality callback</param>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_load_map_and_switch_to_cslam(string mapStream, detectSwitched_callback cslamSwitchedCallback, detectLocalized_callback cslamLocalizedCallBack);//size 0.16 rate 4
    /// <summary>
    /// Map-loading callback
    /// </summary>
    /// <param name="map_quality"></param>
    public delegate void detectSwitched_callback(int map_quality);

    /// <summary>
    /// Save SLAM feature-point data
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SlamMap
    {
        public Vector3 vertices;
    };
    /// <summary>
    /// Map-saving callback
    /// </summary>
    /// <param name="status_of_saved_map"></param>
    /// <param name="map_quality"></param>
    public delegate void detectCslamSaved_callback(int status_of_saved_map, int map_quality);

    /// <summary>
    /// Map localization quality callback
    /// </summary>
    /// <param name="percent"></param>
    public delegate void detectLocalized_callback(float percent);

    /// <summary>
    /// Get scene feature points
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    [DllImport("xslam-unity-wrapper")]
    public static extern IntPtr xslam_get_slam_map(ref int count);

    // BLE I500
    [StructLayout(LayoutKind.Sequential)]
    public struct WirelessPos
    {
        public int type;
        public Vector3 position;
        public Vector4 quaternion;
        public float confidence;
        public int keyTrigger;
        public int keySide;
        public int rocker_x;
        public int rocker_y;
        public int keyA;
        public int keyB;
    };

    public enum WirelessType
    {
        LEFT = 0,
        RIGHT = 1
    };

    public enum WirelessSlamMode
    {
        VIO = 1,
        CSLAM = 2,
        SHARE_MAP = 3
    };

    /// <summary>
    /// Get controller battery and temperature data
    /// battery: charge level (0-100)
    /// temp: temperature (0-100)
    /// </summary>
    public struct WirelessControllerDeviceInfo
    {
        public int battery;
        public int temp;
        public int sleep;
        public int charging;
    };

    public delegate void WirelessPoseCallback(ref WirelessPos pose);
    public delegate void wirelessScanCallback(IntPtr name, IntPtr mac);
    public delegate void wirelessUploadCallback(bool ret);

    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_wireless_start();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_wireless_stop();

    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_wireless_register(WirelessPoseCallback callback);

    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_wireless_scan(wirelessScanCallback callback);

    [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void xv_wireless_connect(string name, string mac);

    [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void xv_wireless_upload_map(string path, wirelessUploadCallback callback);

    // type:WirelessType
    [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool xv_wireless_pair(int type, string name, string mac);

    // type:WirelessType mode:WirelessSlamMode
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_wireless_set_slam_type(int type, int mode);

    // type:WirelessType return:WirelessSlamMode  1 vio 3 sharemap 2 cslam
    [DllImport("xslam-unity-wrapper")]
    public static extern int xv_wireless_get_slam_type(int type);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_rgb_device_get_rgba(int width, int height, System.IntPtr data1, System.IntPtr data2);




    /// <summary>
    /// Get controller information
    /// </summary>
    /// <param name="info">See the corresponding structure for parameters</param>
    /// <param name="type">See the corresponding structure for parameters</param>
    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_wireless_get_device_info(ref WirelessControllerDeviceInfo info, int type);

    public enum WirelessState
    {
        DISCONNECT = 0,
        CONNECTED = 1
    };
    public delegate void WirelessStateCallback(IntPtr name, IntPtr mac, int state);

    [DllImport("xslam-unity-wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void xv_wireless_disconnect(string name, string mac);

    [DllImport("xslam-unity-wrapper")]
    public static extern void xv_wireless_register_state(WirelessStateCallback callback);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StmData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] offset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] angles;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] magnetic;
        public int level;
    };

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_stm_start();

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_stm_stop();

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_stm_get_stream(ref StmData data);

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_switch_audio(bool status);



   
    [DllImport("xslam-unity-wrapper")]
    public static extern bool xv_get_sn(IntPtr sn, int bufferSize);

    public static string GetSerialNumber()
    {
        //Allocate a buffer for the serial number 
        int bufferSize = 128;
        //Adjust size according to your needs 
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        //Call the native function
        xv_get_sn(buffer, bufferSize);
        //Convert the result from IntPtr to a C# string
        string serialNumber = Marshal.PtrToStringAnsi(buffer);
        //Free the allocated memory 
        Marshal.FreeHGlobal(buffer);
        return serialNumber;
    }

    // Legacy SDK version API retained for 4.1.1 compatibility.
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_read_version(ref byte version);

    //Get the glasses firmware version
    [DllImport("xslam-unity-wrapper")]
    public static extern void xslam_read_device_version(ref byte version);

    #region Additional xv-wrapper bindings

    // These use a different native library from the xslam bindings above.
    // Keep entry points and signatures unchanged; similar names are not ABI aliases.
    // Initialize the native wrapper device.
    [DllImport("xv-wrapper")]
    public static extern bool initXvDevice();

    /// <summary>
    /// Get a predicted six-degree-of-freedom (6DoF) pose
    /// </summary>
    /// <param name="poseData">Buffer containing pose data</param>
    /// <param name="timestamp">Timestamp output reference</param>
    /// <param name="prediction">Prediction interval in seconds</param>
    /// <returns>True if pose retrieval succeeds; otherwise false</returns>
    [DllImport("xv-wrapper")]
    public static extern bool xv_test_get_6dof([In, Out] double[] poseData, ref long timestamp, double prediction);


    /// <summary>
    /// Set RGB camera exposure parameters
    /// </summary>
    /// <param name="aecMode">Exposure mode: 0 = automatic, 1 = manual</param>
    /// <param name="exposureGain">Manual exposure gain, in the range [0,255]</param>
    /// <param name="exposureTimeMs">Manual exposure time in milliseconds</param>
    [DllImport("xv-wrapper", EntryPoint = "xv_rgb_set_exposure")]
    public static extern void rgb_set_exposure(int aecMode, int exposureGain, float exposureTimeMs);

    //Start the RGB stream
    [DllImport("xv-wrapper")]
    public static extern void startRgbStream();
    //Stop the RGB stream
    [DllImport("xv-wrapper")]
    public static extern void stopRgbStream();



    //Start the ToF stream
    [DllImport("xv-wrapper")]
    public static extern void startTofStream();

    //Stop the ToF stream
    [DllImport("xv-wrapper")]
    public static extern void stopTofStream();

    //Enable PMD ToF infrared functionality
    [DllImport("xv-wrapper")]
    public static extern bool setPmdTofIRFunction();

    /// <summary>
    /// Start gesture recognition and register the keypoint-data callback
    /// </summary>
    /// <returns>Callback ID on success; -1 on failure</returns>
    [DllImport("xv-wrapper")]
    public static extern int xv_start_skeleton_ex_with_cb();

    /// <summary>
    /// Start the eye-tracking stream and register gazeCallback
    /// </summary>
    /// <returns>Callback ID on success; -1 on failure</returns>
    [DllImport("xv-wrapper")]
    public static extern int start_et_gaze_callback();


    /// <summary>
    /// Read stereo fisheye camera calibration parameters
    /// </summary>
    /// <returns>True if calibration parameters are read successfully; otherwise false</returns>
    [DllImport("xv-wrapper")]
    public static extern bool xvReadStereoFisheyesCalibration();

    /// <summary>
    /// Start magnetometer acquisition and register STMDataCallback
    /// </summary>
    /// <returns>True if magnetometer acquisition starts successfully</returns>
    [DllImport("xv-wrapper")]
    public static extern bool stm_start();

    /// <summary>
    /// Stop magnetometer acquisition and unregister the callback
    /// </summary>
    /// <returns>True if magnetometer acquisition stops successfully</returns>
    [DllImport("xv-wrapper")]
    public static extern bool stm_stop();

    //Register the controller callback
    [DllImport("xv-wrapper")]
    public static extern void xv_controller_register();


    //Recognize local audio
    [DllImport("xv-wrapper")]
    public static extern void xv_recognize_from_local(string name);
    /// <summary>
    /// Switch the speech recognition input source
    /// </summary>
    /// <param name="source">1 = live microphone input, 2 = local PCM file</param>
    [DllImport("xv-wrapper")]
    public static extern void xv_audio_recognize_switch_source(int source);



    /// <summary>
    /// Get the infrared tracking sphere pose
    /// </summary>
    /// <param name="poseData"></param>
    /// <returns></returns>

    [DllImport("xv-wrapper")]
    public static extern bool xv_get_prob([In, Out] double[] poseData);



    /// <summary>
    /// Get the IR image
    /// </summary>
    /// <param name="data">RGBA image data</param>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    /// <returns></returns>
    [DllImport("xv-wrapper")]
    public static extern bool xv_get_tofir_image(IntPtr data, int width, int height);


    [DllImport("xv-wrapper")]
    public static extern void startTofIRStream();

    #endregion
}
