using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class XvNativeAPI 
{
    //Initialize the Xvisio device and obtain its pointer
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

    //
}
