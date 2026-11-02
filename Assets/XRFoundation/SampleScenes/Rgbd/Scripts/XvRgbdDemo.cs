using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Singray.Foundation;

/// <summary>Compatibility base: existing XvRgbdDemo references also find the neutral RgbdDemo component.</summary>
[AddComponentMenu("")]
public class XvRgbdDemo : MonoBehaviour
{
    public RectTransform image;
    [SerializeField] private XvRgbdManager rgbdManager;
    [SerializeField, FormerlySerializedAs("xvCameraManager")] private XvCameraManager cameraManager;
    public RawImage rawImage;
    [SerializeField] private Transform worldMarker;
    private GameObject ownedPreview;
    private Vector2 rgbPixelPoint = new Vector2(320, 240);
    private int frameWidth, frameHeight;
    private double frameTimestamp;
    private float lastFrameAt = -1;
    private Renderer[] pointRenderers;
    private bool[] originalVisibility;

    public XvRgbdManager RgbdManager
    {
        get
        {
            if (rgbdManager == null) rgbdManager = GetComponentInChildren<XvRgbdManager>(true);
            if (rgbdManager == null) rgbdManager = FindObjectOfType<XvRgbdManager>();
            if (rgbdManager == null) rgbdManager = new GameObject("XvRgbdManager").AddComponent<XvRgbdManager>();
            return rgbdManager;
        }
    }
    public XvCameraManager CameraManager
    {
        get
        {
            if (cameraManager == null) cameraManager = RgbdManager.CameraManager;
            return cameraManager;
        }
    }
    protected virtual void OnEnable()
    {
        if (rawImage == null) CreateLegacyPreview();
        var canvas = rawImage.GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = Camera.main;
        frameWidth = frameHeight = 0; frameTimestamp = 0; lastFrameAt = -1;
        if (worldMarker == null) worldMarker = transform.Find("Sphere");
        CachePointRenderers(); SetPointVisible(false);
        XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(OnRgbFrame);
        XvCameraManager.onARCameraStreamFrameArrived.AddListener(OnRgbFrame);
        RgbdManager.StartRgbPose();
    }
    private void CreateLegacyPreview()
    {
        var canvasObject = new GameObject("RGBD Preview", typeof(Canvas));
        canvasObject.transform.SetParent(transform.parent, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = Camera.main;
        var panel = canvasObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(640, 480); panel.localPosition = new Vector3(0, 0, 1.5f);
        panel.localScale = Vector3.one * 0.0015f;
        var preview = new GameObject("RGB Preview", typeof(RectTransform), typeof(RawImage));
        preview.transform.SetParent(panel, false);
        rawImage = preview.GetComponent<RawImage>(); rawImage.raycastTarget = false;
        rawImage.rectTransform.sizeDelta = panel.sizeDelta; ownedPreview = canvasObject;
    }
    protected virtual void OnDisable()
    {
        XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(OnRgbFrame);
        try { if (rgbdManager != null) rgbdManager.StopRgbPose(); }
        finally
        {
            if (rawImage != null) rawImage.texture = null;
            frameWidth = frameHeight = 0; frameTimestamp = 0; lastFrameAt = -1;
            SetPointVisible(false);
            if (ownedPreview != null) { rawImage = null; Destroy(ownedPreview); }
            ownedPreview = null;
        }
    }
    private void OnRgbFrame(cameraData data)
    {
        if (rawImage == null || data == null || data.tex == null || data.parameter.timeStamp <= 0 ||
            double.IsNaN(data.parameter.timeStamp) || double.IsInfinity(data.parameter.timeStamp) || data.parameter.timeStamp <= frameTimestamp) return;
        rawImage.texture = data.tex;
        frameWidth = data.tex.width; frameHeight = data.tex.height;
        frameTimestamp = data.parameter.timeStamp; lastFrameAt = Time.realtimeSinceStartup;
        MyDebugTool.LogDiagnostic("RGBD", "preview", $"actual={frameWidth}x{frameHeight} timestamp={frameTimestamp:F6}", 2f);
    }
    protected virtual void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow)) rgbPixelPoint.x -= 1;
        if (Input.GetKey(KeyCode.RightArrow)) rgbPixelPoint.x += 1;
        if (Input.GetKey(KeyCode.UpArrow)) rgbPixelPoint.y += 1;
        if (Input.GetKey(KeyCode.DownArrow)) rgbPixelPoint.y -= 1;
        rgbPixelPoint.x = Mathf.Clamp(rgbPixelPoint.x, 0, 640);
        rgbPixelPoint.y = Mathf.Clamp(rgbPixelPoint.y, 0, 480);
        UpdateSelectionMarker();
        if (Time.frameCount % 5 != 0) return;
        QuerySelection();
    }
    private void UpdateSelectionMarker()
    {
        if (image == null || rawImage == null) return;
        Rect rect = rawImage.rectTransform.rect;
        var local = new Vector3(rect.xMin + rgbPixelPoint.x / 640f * rect.width,
            rect.yMin + rgbPixelPoint.y / 480f * rect.height, 0);
        Vector3 position = rawImage.rectTransform.TransformPoint(local);
        Vector3 inParent = image.parent == null ? position : image.parent.InverseTransformPoint(position);
        inParent.z = image.localPosition.z;
        image.localPosition = inParent;
    }
    private bool TryGetSelectedPixel(out Vector2 pixel)
    {
        pixel = default;
        if (rawImage == null || rawImage.texture == null || frameWidth <= 0 || frameHeight <= 0 || frameTimestamp <= 0 ||
            lastFrameAt < 0 || Time.realtimeSinceStartup - lastFrameAt > 2f) return false;
        Rect uv = rawImage.uvRect;
        var material = rawImage.material;
        Vector2 scale = Vector2.one, offset = Vector2.zero;
        if (material != null && material.HasProperty("_MainTex"))
        {
            scale = material.mainTextureScale;
            offset = material.mainTextureOffset;
        }
        float u = MapTextureCoordinate(uv.x, uv.width, rgbPixelPoint.x / 640f,
            scale.x, offset.x, rawImage.texture.wrapModeU);
        float v = MapTextureCoordinate(uv.y, uv.height, rgbPixelPoint.y / 480f,
            scale.y, offset.y, rawImage.texture.wrapModeV);
        pixel = new Vector2(Mathf.Clamp(u * frameWidth, 0, frameWidth - 1),
            Mathf.Clamp((1 - v) * frameHeight, 0, frameHeight - 1));
        return true;
    }
    private static float MapTextureCoordinate(float start, float extent, float position,
        float scale, float offset, TextureWrapMode wrap)
    {
        float first = start * scale + offset;
        float last = (start + extent) * scale + offset;
        float value = Mathf.Lerp(first, last, position);
        switch (wrap)
        {
            case TextureWrapMode.Repeat:
                // Keep a full mirrored range inclusive at its edges, then clamp to the final pixel.
                value -= Mathf.Floor(Mathf.Min(first, last));
                return value >= 0 && value <= 1 ? value : Mathf.Repeat(value, 1);
            case TextureWrapMode.Mirror:
                return Mathf.PingPong(value, 1);
            case TextureWrapMode.MirrorOnce:
                return Mathf.Clamp01(Mathf.Abs(value));
            default:
                return Mathf.Clamp01(value);
        }
    }
    private void QuerySelection()
    {
        Vector3 point = default;
        if (!TryGetSelectedPixel(out Vector2 pixel) || !RgbdManager.GetRgbPixel3DPose(pixel, ref point))
        {
            SetPointVisible(false);
            MyDebugTool.LogDiagnosticWarning("RGBD", "point-unavailable", "no fresh RGB or valid RGBD point; world marker hidden", 2f);
            return;
        }
        Transform target = worldMarker != null ? worldMarker : transform;
        target.position = point;
        SetPointVisible(true);
        MyDebugTool.LogDiagnostic("RGBD", "point", $"pixel={pixel} actual={frameWidth}x{frameHeight} timestamp={frameTimestamp:F6} world={point}", 2f);
    }
    private void CachePointRenderers()
    {
        if (pointRenderers != null) return;
        var target = worldMarker != null ? worldMarker : transform;
        pointRenderers = target.GetComponentsInChildren<Renderer>(true);
        originalVisibility = new bool[pointRenderers.Length];
        for (int i = 0; i < pointRenderers.Length; i++) originalVisibility[i] = pointRenderers[i].enabled;
    }
    private void SetPointVisible(bool visible)
    {
        CachePointRenderers();
        for (int i = 0; i < pointRenderers.Length; i++)
            if (pointRenderers[i] != null) pointRenderers[i].enabled = visible && originalVisibility[i];
    }
}
