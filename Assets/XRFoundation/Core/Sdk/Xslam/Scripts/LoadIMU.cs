using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using TMPro;

public class LoadIMU : MonoBehaviour
{	   
    /*
	public Canvas accel;
	public Canvas gyro;
	public Canvas magn;
    */
    public TextMeshProUGUI imuTxt;

    bool isReady = false;
    private readonly Vector3[] imu = new Vector3[3];
    private double timestamp;

    void Start()
    {

    }
    
    void Update()
    {
        if (API.xslam_ready() && !isReady)
        {
            isReady = true;
            int startResult = API.xslam_start_imu();
            MyDebugTool.LogDiagnostic("IMU", "start", $"xslam_start_imu result={startResult}", 0f);
        }

        if (isReady)
        {
            if (API.xslam_get_imu_array(imu, ref timestamp))
            {
                if (imuTxt != null)
                {
                    imuTxt.text = imu[0].x +" "+ imu[0].y + " " + imu[0].z + "\n" + imu[1].x + " " + imu[1].y + " " + imu[1].z + "\n" + imu[2].x + " " + imu[2].y + " " + imu[2].z;
                }

                MyDebugTool.LogDiagnostic(
                    "IMU",
                    "sample",
                    $"timestamp={timestamp:F6} accel={imu[0]} gyro={imu[1]} magnetic={imu[2]}",
                    2f);
            }
            else
            {
                MyDebugTool.LogDiagnosticWarning("IMU", "no-sample", $"no new sample; lastTimestamp={timestamp:F6}", 2f);
            }
        }


        //// Test 3DOF
        //API.Orientation o = new API.Orientation();
        //if( API.xslam_get_3dof( ref o ) ){
        //	Debug.LogFormat( "3DOF: {0} {1} {2} {3}", o.quaternion.x, o.quaternion.y, o.quaternion.z, o.quaternion.w );
        //}
    }
}
