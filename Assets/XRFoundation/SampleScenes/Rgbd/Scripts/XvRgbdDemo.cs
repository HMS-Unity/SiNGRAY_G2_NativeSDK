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
    private GameObject ownedPreview;
    private void OnRgbFrame(cameraData data)
    {
        if (rawImage != null && data != null) rawImage.texture = data.tex;
    }
   
    private void OnEnable()
    {
        if (rawImage == null)
        {
            // Older hub modules omit the preview Canvas entirely.
            GameObject canvasObject = new GameObject("RGBD Preview", typeof(Canvas));
            canvasObject.transform.SetParent(transform.parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            RectTransform panel = canvasObject.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(640, 480);
            panel.localPosition = new Vector3(0, 0, 1.5f);
            panel.localScale = Vector3.one * 0.0015f;
            GameObject preview = new GameObject("RGB Preview", typeof(RectTransform), typeof(RawImage));
            preview.transform.SetParent(panel, false);
            rawImage = preview.GetComponent<RawImage>();
            rawImage.raycastTarget = false;
            rawImage.rectTransform.sizeDelta = panel.sizeDelta;
            ownedPreview = canvasObject;
        }
        XvCameraManager.onARCameraStreamFrameArrived.AddListener(OnRgbFrame);
        RgbdManager.StartRgbPose();
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
        XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(OnRgbFrame);
        if (rgbdManager != null) rgbdManager.StopRgbPose();
        if (ownedPreview != null) Destroy(ownedPreview);
        ownedPreview = null;

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
        if (image != null) image.localPosition = rgbPixelPoint;


        int width = CameraManager.Width;
        int height = CameraManager.Height;
        if (width <= 0 || height <= 0) return;

       
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