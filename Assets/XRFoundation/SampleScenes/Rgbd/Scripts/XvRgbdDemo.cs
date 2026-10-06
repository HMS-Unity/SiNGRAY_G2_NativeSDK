using UnityEngine;
using UnityEngine.UI;
using Singray.Foundation;
using static API;

public class XvRgbdDemo : MonoBehaviour
{
    public RectTransform image;
    [SerializeField]
    private XvRgbdManager rgbdManager;
    public XvRgbdManager RgbdManager
    {
        get
        {

            if (rgbdManager == null)
            {
                rgbdManager = FindObjectOfType<XvRgbdManager>();
            }

            if (rgbdManager == null)
            {
                rgbdManager = new GameObject("XvRgbdManager").AddComponent<XvRgbdManager>();
            }
            return rgbdManager;

        }
    }

    [SerializeField]
    private XvCameraManager cameraManager;


    public XvCameraManager CameraManager
    {
        get
        {

            if (cameraManager == null)
            {
                cameraManager = FindObjectOfType<XvCameraManager>();
            }

            if (cameraManager == null)
            {
                cameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
            }
            return cameraManager;

        }
    }

    public RawImage rawImage;
   
    private void OnEnable()
    {
        RgbdManager.StartRgbPose();

        XvCameraManager.onARCameraStreamFrameArrived.AddListener((data) => {
            rawImage.texture = data.tex;
        });
    }

    //Vector2[] rgbPixelPointList;
    //pointer_3dpose[] spacePoseList;
    //private void Start()
    //{
    //    rgbPixelPointList = new Vector2[3072];
    //    spacePoseList = new pointer_3dpose[3072];
    //    for (int i = 0; i < rgbPixelPointList.Length; i++)
    //    {
    //        rgbPixelPointList[i] = new Vector2(100, 100);
    //    }
    //}

    private void OnDisable()
    {
        RgbdManager.StopRgbPose();

    }

    Vector2 rgbPixelPoint = new Vector2(320, 240);

  
    private void Update()
    {
        Vector3 pointerPose = new Vector3();

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rgbPixelPoint.x -= 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rgbPixelPoint.x += 1;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            rgbPixelPoint.y += 1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            rgbPixelPoint.y -= 1;
        }
        image.localPosition = rgbPixelPoint;


        int width = cameraManager.Width;
        int height = cameraManager.Height;

       
        Vector3 screenPoint = rgbPixelPoint;

        screenPoint.x = (rgbPixelPoint.x / 640) * width;
        screenPoint.y = (rgbPixelPoint.y / 480) * height;

        screenPoint.y = height - screenPoint.y;

       

        if (Time.frameCount % 5 == 0)
        {
            if (RgbdManager.GetRgbPixel3DPose(screenPoint, ref pointerPose))
            {
                MyDebugTool.Log("rgbd:screenPoint:" + screenPoint+"    "+ pointerPose);
                transform.position = pointerPose;
            }
        }
    }
}