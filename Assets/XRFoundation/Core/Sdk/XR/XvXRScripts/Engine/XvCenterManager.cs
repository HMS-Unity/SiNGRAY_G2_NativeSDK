using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Singray.Engine;

public class XvCenterManager : MonoBehaviour
{
    const string TAG = "XvCenterManager";
    Transform leftcam;
    Transform rightcam;

    bool isReady = false;
    API.stereo_pdm_calibration fed;
    Vector3 eyecenterPos;
    // Start is called before the first frame update
    void Start()
    {
        if (transform.parent!=null)
        {
            leftcam = transform.parent.Find("LeftCamera");
            rightcam = transform.parent.Find("RightCamera");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (API.xslam_ready() && XvXRManager.SDK.GetDevice().isConnected && !isReady)
        {
            bool b = XvXRAndroidDevice.readStereoDisplayCalibration(ref fed);
            Debug.Log($"{TAG} readStereoDisplayCalibration:{b}");

            Vector3 LeftdisplayPos = new Vector3((float)fed.calibrations[0].extrinsic.translation[0], -(float)fed.calibrations[0].extrinsic.translation[1], (float)fed.calibrations[0].extrinsic.translation[2]);
            Vector3 RightdisplayPos = new Vector3((float)fed.calibrations[1].extrinsic.translation[0], -(float)fed.calibrations[1].extrinsic.translation[1], (float)fed.calibrations[1].extrinsic.translation[2]);

            //Compute the binocular center position from the left and right display calibration positions
            Vector3 normal = (RightdisplayPos - LeftdisplayPos).normalized;
            float distance = Vector3.Distance(LeftdisplayPos, RightdisplayPos);
            eyecenterPos = normal * (distance * 0.5f) + LeftdisplayPos;



            Debug.Log($"{TAG} LeftdisplayPos:{Math.Round(LeftdisplayPos.x, 3)},{Math.Round(LeftdisplayPos.y, 3)},{Math.Round(LeftdisplayPos.z, 3)},leftcam pos:{Math.Round(leftcam.localPosition.x, 3)},{Math.Round(leftcam.localPosition.y, 3)},{Math.Round(leftcam.localPosition.z, 3)}");
            Debug.Log($"{TAG} leftcam rot:{leftcam.localEulerAngles}");

            Debug.Log($"{TAG} RightdisplayPos:{Math.Round(RightdisplayPos.x, 3)},{Math.Round(RightdisplayPos.y, 3)},{Math.Round(RightdisplayPos.z, 3)},rightcam pos:{Math.Round(rightcam.localPosition.x, 3)},{Math.Round(rightcam.localPosition.y, 3)},{Math.Round(rightcam.localPosition.z, 3)}");
            Debug.Log($"{TAG} rightcam rot:{rightcam.localEulerAngles}");

            transform.localPosition = eyecenterPos;
            //Use the left optical module's rotation
            transform.localEulerAngles = leftcam.localEulerAngles;

            Debug.Log($"{TAG} centercam pos:{Math.Round(transform.localPosition.x, 3)},{Math.Round(transform.localPosition.y, 3)},{Math.Round(transform.localPosition.z, 3)}");
            Debug.Log($"{TAG} centercam rot:{transform.localEulerAngles}");

            isReady = true;


        }
    }
}
