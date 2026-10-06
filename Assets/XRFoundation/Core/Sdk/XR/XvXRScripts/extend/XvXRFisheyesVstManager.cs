using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singray.utils;

public class XvXRFisheyesVstManager : MonoBehaviour
{


    public GameObject leftFishEyeCamera;
    public GameObject leftFisheyePlane;

    public GameObject rightFishEyeCamera;
    public GameObject rightFisheyePlane;

    public GameObject leftDisplayEyeCamera;
  
    public GameObject rightDisplayEyeCamera;

    private bool isReadMeshData = false;

    private double focal;
    private double baseline;
    private double[] leftPose = new double[7];
    private double[] rightPose = new double[7];
    private int camerasModelWidth;
    private int camerasModelHeight;


    private int lastWidth = 0;
    private int lastHeight = 0;
    private int width;
    private int height;
    private byte[] leftBuffer = new byte[1920*1080*4];
    private byte[] rightBuffer = new byte[1920 * 1080 * 4];
    private double[] poseData = new double[7];

    private IntPtr leftPtr = IntPtr.Zero;
    private IntPtr rightPtr = IntPtr.Zero;
    private Texture2D texLeft = null;
    private Texture2D texRight = null;


    private void Start()
    {
        leftPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(leftBuffer, 0);
        rightPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(rightBuffer, 0);
    }


    // Update is called once per frame
    void Update()
    {
        //if (leftDisplayEyeCamera != null && rightDisplayEyeCamera != null && leftFishEyeCamera != null && rightFishEyeCamera != null)
        //{
        //    leftFishEyeCamera.transform.localPosition = leftDisplayEyeCamera.transform.localPosition;
        //    leftFishEyeCamera.transform.localEulerAngles = leftDisplayEyeCamera.transform.localEulerAngles;

        //    rightFishEyeCamera.transform.localPosition = rightDisplayEyeCamera.transform.localPosition;
        //    rightFishEyeCamera.transform.localEulerAngles = rightDisplayEyeCamera.transform.localEulerAngles;

        //    Debug.Log("vr_log: leftFishEyeCamera.transform.localPosition:" + leftFishEyeCamera.transform.localPosition + "," + leftFishEyeCamera.transform.localEulerAngles);
        //}


        if (API.xslam_get_fisheyes_rectification_thread())
        {

            if (leftFisheyePlane?.activeSelf == false)
            {
                leftFisheyePlane?.SetActive(true);
            }
            if (rightFisheyePlane?.activeSelf == false)
            {
                rightFisheyePlane?.SetActive(true);
            }

            if (!isReadMeshData)
            {
               

               

                if (API.xslam_get_fe_mesh_params(ref focal, ref baseline, ref camerasModelWidth, ref camerasModelHeight, leftPose, rightPose))
                {
                    isReadMeshData = true;
                    //set pose and rotation
              
                    float d = 1;
                    float sw = d / 3;
                    float sh = d / 3;

                    if(focal>0 && baseline > 0)
                    {
                        d = 10;
                        sw = (float)(d *baseline * camerasModelWidth / focal);
                        sh = (float)(d *baseline* camerasModelHeight / focal);
                    }
                    MyDebugTool.Log("vr_log: xslam_get_fe_mesh_params:" + focal + "," + baseline + "," + camerasModelWidth + "," + camerasModelHeight + "," + string.Join("|", leftPose) + "," + string.Join("|", rightPose)+","+sw+","+sh);


                    leftFishEyeCamera.transform.localPosition = new Vector3((float)leftPose[4], -(float)leftPose[5], (float)leftPose[6]);
                    leftFishEyeCamera.transform.localRotation = new Quaternion((float)leftPose[0], -(float)leftPose[1], (float)leftPose[2], (float)leftPose[3]);

                    leftFisheyePlane.transform.localPosition = new Vector3(0, 0, d);
                    leftFisheyePlane.transform.localScale = new Vector3(sw, 1, sh);

                    rightFishEyeCamera.transform.localPosition = new Vector3((float)rightPose[4], -(float)rightPose[5], (float)rightPose[6]);
                    rightFishEyeCamera.transform.localRotation = new Quaternion((float)rightPose[0], -(float)rightPose[1], (float)rightPose[2], (float)rightPose[3]);

                    rightFisheyePlane.transform.localPosition = new Vector3(0, 0, d);
                    rightFisheyePlane.transform.localScale = new Vector3(sw, 1, sh);
                }
                else
                {
                    MyDebugTool.Log(" Fisheye FE vr_log:cannot get the xslam_get_fe_mesh_params........xxx ");
                }
            }

            if (API.xslam_get_fe_images_data(ref width, ref height, leftBuffer, rightBuffer, poseData))
            {
                MyDebugTool.Log("Fisheye FE vr_log: xslam_get_fe_images_data:" + width + "," + height + "," + string.Join("|", poseData));
                this.transform.localPosition = new Vector3((float)poseData[4], -(float)poseData[5], (float)poseData[6]);
                this.transform.localRotation = new Quaternion(-(float)poseData[0], (float)poseData[1], -(float)poseData[2], (float)poseData[3]);

                if(width>0&&height>0 &&(lastWidth != width || lastHeight != height))
                {
                    lastWidth = width;
                    lastHeight = height;

                    XvXRLog.LogInfo("Fisheye FE XvXRFisheyesVstManager create texture..........." + width+","+height);

                    texLeft = new Texture2D(width, height, TextureFormat.Alpha8, false);
                    texRight = new Texture2D(width, height, TextureFormat.Alpha8, false);
                    texLeft.Apply();
                    texRight.Apply();
                
                    leftFisheyePlane.GetComponent<Renderer>().material.mainTexture = texLeft;
                    rightFisheyePlane.GetComponent<Renderer>().material.mainTexture = texRight;



                }
                if(texLeft !=null && texRight != null)
                {
                    texLeft.LoadRawTextureData(leftPtr, width * height);
                    texRight.LoadRawTextureData(rightPtr, width * height);
                    texLeft.Apply();
                    texRight.Apply();
                }


            }
            else
            {
                MyDebugTool.Log(" Fisheye FE vr_log:cannot get the xslam_get_fe_images_data .....xxx...xxx ");
            }

        }
        else
        {
            if (leftFisheyePlane?.activeSelf == true)
            {
                leftFisheyePlane?.SetActive(false);
            }
            if (rightFisheyePlane?.activeSelf == true)
            {
                rightFisheyePlane?.SetActive(false);
            }
            MyDebugTool.Log("Fisheye FE thread is not start...........");
        }
    }
}
