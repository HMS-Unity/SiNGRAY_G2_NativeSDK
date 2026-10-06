using UnityEngine;
using System.Runtime.InteropServices;
using System;
namespace Singray.Foundation
{
    //public class XvTofIRCameraParameter : XvCameraParameterSetting
    //{
    //    public int width;
    //    public int height;
    //    public int fps;
    //    public TofStreamType tofStreamType;


    //    //public TofFramerate tofFramerate;
    //    //public SonyTofLibMode sonyTofLibMode;
    //    //public TofResolution tofResolution;

    //}
    public class XvTofIRCamera : XvCameraBase
{
    public XvTofIRCamera(XvTofCameraParameter cameraParameter, FrameArrived frameArrived) : base(cameraParameter, frameArrived)
    {
            this.cameraParameter = cameraParameter;

        }

        private Texture2D tex = null;


    private Color32[] pixel32;
    private GCHandle pixelHandle;
    private IntPtr pixelPtr;
    private XvTofCameraParameter cameraParameter;

        public override void StartCapture()
    {
        if (!isOpen)
        {
               
                isOpen = true;
        }
    }

    public override void StopCapture()
    {
        if (isOpen)
        {
            if (pixelHandle.IsAllocated)
            {
                pixelHandle.Free();
            }
            if (tex != null)
            {
                UnityEngine.Object.Destroy(tex);
            }
            tex = null;
            pixel32 = null;
            pixelPtr = IntPtr.Zero;

        }
        isOpen = false;

    }
        int width=0;
        int height=0;



        private bool readRgbCalibrationFlag = false;
        private API.pdm_calibration pdm_Calibration = default;
        private double[] _R;
        private double[] _T;
        private double[] _EulerAngles;
        private Vector3 offsetPosition;
        private Quaternion offsetRotation;
        private double[] _poseData = new double[7];
        public override void Update()
    {


          
            if (isOpen && API.xslam_ready())
        {
                //if (!readRgbCalibrationFlag)
                //{
                //    ReadRgbCalibration();
                //}

                API.xslam_get_tofir_size(ref width, ref height);

               
            
               
                MyDebugTool.Log("Create TOF IR Update " + width + "x" + height);
            if (width > 0 && height > 0)
            {

                if (!tex)
                {
                    MyDebugTool.Log("Create TOF IR texture " + width + "x" + height);
                    TextureFormat format = TextureFormat.RGBA32;
                    tex = new Texture2D(width, height, format, false);


                    pixel32 = tex.GetPixels32();
                    pixelHandle = GCHandle.Alloc(pixel32, GCHandleType.Pinned);
                    pixelPtr = pixelHandle.AddrOfPinnedObject();
                }

                    double timestamp = 0;

                    if (API.xslam_get_tofir_image(pixelPtr, tex.width, tex.height,ref timestamp))
                {
                    //Update the Texture2D with array updated in C++
                    tex.SetPixels32(pixel32);
                    tex.Apply();
                    cameraData.tex = tex;
                    cameraData.texWidth = tex.width;

                    cameraData.texHeight = tex.height;

                     cameraData.parameter.timeStamp = timestamp;

                        //double rgbTimestamp = 0;
                        //if (API.xslam_get_pose_at(_poseData, rgbTimestamp))
                        //{
                        //    cameraData.parameter.rotation = new Quaternion(-(float)_poseData[0], (float)_poseData[1], -(float)_poseData[2], (float)_poseData[3]) * offsetRotation;
                        //    cameraData.parameter.position = new Vector3((float)_poseData[4], -(float)_poseData[5], (float)_poseData[6]) + offsetPosition;
                        //}
                        //else
                        //{
                        //    MyDebugTool.Log("RGBRecord xslam_get_pose_at faild");
                        //}
                        frameArrived?.Invoke(cameraData);

                        MyDebugTool.Log("xslam_get_tof_image " + width + "x" + height + " timestamp：  " + timestamp);
                      
                    }
                else
                {
                    MyDebugTool.Log("Invalid TOFIR texture");
                }
            }
        }
    }

        void ReadRgbCalibration()
        {

            if (API.readToFCalibration(ref pdm_Calibration))
            {
                _T = new double[3] { pdm_Calibration.extrinsic.translation[0], -pdm_Calibration.extrinsic.translation[1], pdm_Calibration.extrinsic.translation[2] };

                //Convert the left-eye calibration rotation matrix to Euler angles
                _R = new double[9] { pdm_Calibration.extrinsic.rotation[0], -pdm_Calibration.extrinsic.rotation[1], pdm_Calibration.extrinsic.rotation[2], -pdm_Calibration.extrinsic.rotation[3], pdm_Calibration.extrinsic.rotation[4],
                            -pdm_Calibration.extrinsic.rotation[5],pdm_Calibration.extrinsic.rotation[6],-pdm_Calibration.extrinsic.rotation[7],pdm_Calibration.extrinsic.rotation[8]};
                Singray.Engine.XvXREye.RotationMatrixToEulerAngles(ref _EulerAngles, _R);


                offsetPosition = new Vector3((float)_T[0], (float)_T[1], (float)_T[2]);
                Vector3 localEuler = new Vector3((float)_EulerAngles[0], (float)_EulerAngles[1], (float)_EulerAngles[2]);

                offsetRotation = Quaternion.Euler(localEuler);

                readRgbCalibrationFlag = true;
            }
        }
    }
}
