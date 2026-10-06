using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Singray.Foundation;
using Singray.Foundation.SampleScenes;
namespace Singray.Foundation.SampleScenes {


public class IRToWorldDemo : MonoBehaviour
{
    [SerializeField]
    private XvCameraManager cameManager;
    public RawImage tofIRCameraImage;

    public RectTransform image;


    private void Awake()
    {
        if (cameManager == null)
        {
            cameManager = FindObjectOfType<XvCameraManager>();

            if (cameManager == null)
            {
                cameManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
            }
        }
     
    }


        private void SetxPosureHid() {

            cameManager.SetTofExposure(10);
            // Set ToF exposure parameters; the final argument is the exposure value
            //byte[] hid = new byte[] { 0x02, 0xae, 0xF5, 0x02, 0x14 };
            //API.HidWriteAndRead(hid, hid.Length);
        }
    private void Start()
    {
#if !UNITY_EDITOR
        StartTofIRCamera();
        StartTofPointCloud();
#endif

       // Invoke("SetxPosureHid",5);
        }
        Vector3[] vecGroup;
    public XvParticlesCloudPoint particlesCloudPoint;
    Vector2 irPixelPoint = new Vector2(960, 540);
    public Transform sphere;

      
    private void Update()
    {

        if (cameManager.GetPointCloudData(out vecGroup))
        {
            int width = API.xslam_get_tof_width();
            int height = API.xslam_get_tof_height();

            //particlesCloudPoint.gameObject.SetActive(true);
            //particlesCloudPoint.StartDraw(vecGroup);
            Vector3 screenPoint = irPixelPoint;

            screenPoint.x = (irPixelPoint.x / 1920)* width;
            screenPoint.y = (irPixelPoint.y / 1080)* height;

            screenPoint.y = height - screenPoint.y;

            sphere.position= GetWorldPosition(screenPoint, vecGroup);
        }


        if (Input.GetKey(KeyCode.LeftArrow))
        {
            irPixelPoint.x -= 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            irPixelPoint.x += 1;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            irPixelPoint.y += 1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            irPixelPoint.y -= 1;
        }
        image.localPosition = irPixelPoint;


        //Vector2 screenPoint = Vector2.one;

        //screenPoint.x = (irPixelPoint.x / 1920) * 640;
        //screenPoint.y = (irPixelPoint.y / 1080) * 480;

        //screenPoint.y = 480 - screenPoint.y;

        //MyDebugTool.Log(screenPoint);



    }

    public Vector3 GetWorldPosition(Vector2 irPixelPoint, Vector3[] vecGroup) {


      int  width = API.xslam_get_tof_width();
      
      //int height = API.xslam_get_tof_height();
      int index=  (int)irPixelPoint.y* width + (int)irPixelPoint.x;
        MyDebugTool.Log("irPixelPoint:x=" + irPixelPoint.x+"   y="+ irPixelPoint.y+"   "+ vecGroup.Length+"  "+ index);

        if (index>= vecGroup.Length) {
            MyDebugTool.LogError("Index out of range: " + index+"  "+ vecGroup.Length);

            return vecGroup[0];

        }

        return vecGroup[index];
    }


    public void StartTofPointCloud()
    {
        cameManager.StartTofPointCloud();

    }
    public void StartTofIRCamera()
    {
        XvCameraManager.onTofIRCameraStreamFrameArrived.AddListener(onTofIRCameraFrameArrived);
        cameManager.StartCapture(XvCameraStreamType.TofIRCameraStream);

    }
    public void StopTofIRCamera()
    {
        XvCameraManager.onTofIRCameraStreamFrameArrived.RemoveListener(onTofIRCameraFrameArrived);
        cameManager.StopCapture(XvCameraStreamType.TofIRCameraStream);
        tofIRCameraImage.texture = null;
    }
    private void onTofIRCameraFrameArrived(cameraData cameraData)
    {

        if (tofIRCameraImage != null)
        {
            tofIRCameraImage.texture = cameraData.tex;
        }
    }

}
}
