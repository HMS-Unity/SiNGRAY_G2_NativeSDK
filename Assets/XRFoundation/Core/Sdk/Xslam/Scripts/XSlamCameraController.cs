using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using UnityEditor;

using System;
using System.Runtime.InteropServices;
using System.Text;
using Singray.SystemEvents;

public class XSlamCameraController : MonoBehaviour
{

    [Header("Movement Settings")]
    [Tooltip("Exponential boost factor on translation"), Range(0.05f, 25f)]
    public float boost = 1.0f;

    [Header("Origin Settings")]
    [Tooltip("Position of the origin")]
    public Vector3 positionOrigin = new Vector3(0.0f, 0.0f, 0.0f);
    //public Vector3 rotationOrigin = new Vector3(0.0f, -99.10201f, 0.0f);

	
	public enum SlamModes // your custom enumeration
	{
		Device = 0, 
		Host
	};
    [Header("SLAM")]
	public SlamModes slamMode = SlamModes.Device;  // this public var should appear as a drop down


	public enum CnnSources // your custom enumeration
	{
		Left = 0, 
		Right,
		RGB,
		TOF
	};
	
    [Header("CNN")]
    public string cnnModel = "";
    public string cnnDescriptor = "";
    public CnnSources cnnSource = CnnSources.Left;

    void OnEnable()
    {
    
    }

    void Awake()
    {
    }

    void Start()
    {
        MyDebugTool.Log("Start");
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //StartCoroutine("UpdatePose");
    }

    void Quit()
	{
	
	}
	
	public void setSlamMode( SlamModes mode )
	{
		slamMode = mode;
		API.xslam_slam_type( slamMode == SlamModes.Device ? 0 : 1 );
	}
    
	public void ResetSlam()
	{
		API.xslam_reset_slam();
	}
    
}
