using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Singray.G2.Validation
{
    /// <summary>
    /// Standalone sensor validation UI. This sample deliberately depends only
    /// on Singray.G2 and never calls vendor APIs directly.
    /// </summary>
    public sealed class SingrayG2ValidationController : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0.035f, 0.047f, 0.067f, 1f);
        private static readonly Color PanelColor = new Color(0.075f, 0.094f, 0.125f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.10f, 0.39f, 0.66f, 1f);
        private static readonly Color StopButtonColor = new Color(0.59f, 0.20f, 0.22f, 1f);
        private static readonly Color AccentColor = new Color(0.20f, 0.78f, 0.66f, 1f);

        private readonly SingrayG2RgbResolution[] rgbResolutions =
        {
            SingrayG2RgbResolution.R320x240,
            SingrayG2RgbResolution.R640x480,
            SingrayG2RgbResolution.R1280x720,
            SingrayG2RgbResolution.R1920x1080,
            SingrayG2RgbResolution.R2560x1920,
            SingrayG2RgbResolution.R3840x2160
        };

        private readonly SingrayG2TofResolution[] tofResolutions =
        {
            SingrayG2TofResolution.Hqvga,
            SingrayG2TofResolution.Qvga,
            SingrayG2TofResolution.Vga
        };

        private readonly SingrayG2TofFrameRate[] tofFrameRates =
        {
            SingrayG2TofFrameRate.Fps5,
            SingrayG2TofFrameRate.Fps10,
            SingrayG2TofFrameRate.Fps15,
            SingrayG2TofFrameRate.Fps20,
            SingrayG2TofFrameRate.Fps25,
            SingrayG2TofFrameRate.Fps30
        };

        private readonly Queue<string> recentMessages = new Queue<string>();
        private SingrayG2Manager g2;
        private Font uiFont;
        private RawImage preview;
        private Text sdkStatusText;
        private Text frameStatusText;
        private Text imuStatusText;
        private Text lightStatusText;
        private Text pointCloudStatusText;
        private Text logText;
        private Text rgbResolutionText;
        private Text tofResolutionText;
        private Text tofFrameRateText;
        private int rgbResolutionIndex = 2;
        private int tofResolutionIndex = 1;
        private int tofFrameRateIndex = 5;
        private bool imuRunning;
        private bool pointCloudRunning;
        private float nextStatusRefresh;
        private int rgbFrames;
        private float rgbFramesStartedAt;

        private void Awake()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildUi();

            g2 = SingrayG2Manager.Instance;
            g2.SetDiagnosticsEnabled(true);
            g2.CameraFrameArrived += OnCameraFrame;
            g2.AmbientLightChanged += OnAmbientLightChanged;

            ApplyRgbSettings();
            ApplyTofSettings();
            AppendLog("Singray G2 validation module initialized.");
            if (g2.IsSimulation)
            {
                AppendLog("SIMULATION active: all displayed sensor values are synthetic.");
            }
            else if (!g2.IsSdkReady)
            {
                AppendLog("No G2 device detected. Controls remain safe but sensor data is unavailable.");
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextStatusRefresh)
            {
                nextStatusRefresh = Time.unscaledTime + 0.25f;
                if (g2.IsSimulation)
                {
                    sdkStatusText.text = "MODE: SIMULATION | SYNTHETIC DATA";
                    sdkStatusText.color = new Color(0.32f, 0.72f, 1f, 1f);
                }
                else
                {
                    sdkStatusText.text = g2.IsSdkReady
                        ? "MODE: NATIVE | Singray G2 connected"
                        : $"SDK: NOT READY | {Application.platform} | connect a Singray G2 device";
                    sdkStatusText.color = g2.IsSdkReady ? AccentColor : new Color(1f, 0.72f, 0.25f, 1f);
                }
            }

            if (imuRunning && g2.TryGetImuSample(out SingrayG2ImuSample imu))
            {
                imuStatusText.text =
                    $"IMU t={imu.Timestamp:F6}\nAccel {FormatVector(imu.Acceleration)}\n" +
                    $"Gyro {FormatVector(imu.AngularVelocity)}\nMag {FormatVector(imu.MagneticField)}";
            }

            if (pointCloudRunning && g2.TryGetTofPointCloud(out Vector3[] points))
            {
                pointCloudStatusText.text = $"Point cloud: {points.Length:N0} points received";
            }
        }

        private void OnDestroy()
        {
            if (g2 == null)
            {
                return;
            }

            g2.CameraFrameArrived -= OnCameraFrame;
            g2.AmbientLightChanged -= OnAmbientLightChanged;
            if (imuRunning)
            {
                g2.StopImu();
            }
            if (pointCloudRunning)
            {
                g2.StopTofPointCloud();
            }
        }

        private void BuildUi()
        {
            GameObject canvasObject = CreateUiObject("Canvas", transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image background = CreatePanel("Background", canvasObject.transform, BackgroundColor);
            Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

            Text title = CreateText("Title", background.transform, "SINGRAY G2 SENSOR VALIDATION", 38, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetAnchoredRect(title.rectTransform, 30f, -18f, -30f, 72f, true);
            title.color = AccentColor;

            sdkStatusText = CreateText("SdkStatus", background.transform, "SDK: checking...", 23, FontStyle.Bold, TextAnchor.MiddleRight);
            SetAnchoredRect(sdkStatusText.rectTransform, 820f, -20f, -30f, 68f, true);

            GameObject body = CreateUiObject("Body", background.transform);
            Stretch(body.GetComponent<RectTransform>(), 24f, 24f, 24f, 98f);
            HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 18f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            BuildPreviewPanel(body.transform);
            BuildControlPanel(body.transform);
        }

        private void BuildPreviewPanel(Transform parent)
        {
            Image panel = CreatePanel("PreviewPanel", parent, PanelColor);
            LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 1080f;
            layout.flexibleWidth = 1.35f;

            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18, 18, 18, 18);
            vertical.spacing = 12f;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;

            Text previewTitle = CreateText("PreviewTitle", panel.transform, "CAMERA PREVIEW", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
            previewTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            GameObject previewObject = CreateUiObject("Preview", panel.transform);
            preview = previewObject.AddComponent<RawImage>();
            preview.color = new Color(0.02f, 0.025f, 0.035f, 1f);
            LayoutElement previewLayout = previewObject.AddComponent<LayoutElement>();
            previewLayout.flexibleHeight = 1f;
            previewLayout.minHeight = 480f;

            frameStatusText = CreateText(
                "FrameStatus",
                panel.transform,
                "No camera frame received.\nStart RGB or ToF on a connected device.",
                22,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            frameStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 118f;

            imuStatusText = CreateText("ImuStatus", panel.transform, "IMU: stopped", 20, FontStyle.Normal, TextAnchor.UpperLeft);
            imuStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 102f;

            lightStatusText = CreateText("LightStatus", panel.transform, "Ambient light: waiting for device events", 20, FontStyle.Normal, TextAnchor.MiddleLeft);
            lightStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            pointCloudStatusText = CreateText("PointCloudStatus", panel.transform, "Point cloud: stopped", 20, FontStyle.Normal, TextAnchor.MiddleLeft);
            pointCloudStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        }

        private void BuildControlPanel(Transform parent)
        {
            Image panel = CreatePanel("ControlPanel", parent, PanelColor);
            LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 720f;
            layout.flexibleWidth = 1f;

            GameObject viewportObject = CreateUiObject("Viewport", panel.transform);
            Stretch(viewportObject.GetComponent<RectTransform>(), 10f, 10f, 10f, 10f);
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUiObject("Content", viewportObject.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup vertical = content.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(14, 14, 14, 14);
            vertical.spacing = 9f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportObject.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 36f;

            CreateSectionTitle(content.transform, "RGB CAMERA");
            rgbResolutionText = CreateCycleButton(content.transform, "RGB Resolution: 1280 x 720", CycleRgbResolution);
            CreateButtonRow(content.transform,
                ("Start RGB", ButtonColor, () => StartStream(SingrayG2SensorStream.Rgb)),
                ("Stop RGB", StopButtonColor, () => StopStream(SingrayG2SensorStream.Rgb)));

            CreateSectionTitle(content.transform, "TOF");
            tofResolutionText = CreateCycleButton(content.transform, "ToF Resolution: QVGA", CycleTofResolution);
            tofFrameRateText = CreateCycleButton(content.transform, "ToF Frame Rate: 30 FPS", CycleTofFrameRate);
            CreateButtonRow(content.transform,
                ("Start Depth", ButtonColor, () => StartStream(SingrayG2SensorStream.TofDepth)),
                ("Stop Depth", StopButtonColor, () => StopStream(SingrayG2SensorStream.TofDepth)));
            CreateButtonRow(content.transform,
                ("Start IR", ButtonColor, () => StartStream(SingrayG2SensorStream.TofInfrared)),
                ("Stop IR", StopButtonColor, () => StopStream(SingrayG2SensorStream.TofInfrared)));
            CreateButtonRow(content.transform,
                ("Start Point Cloud", ButtonColor, StartPointCloud),
                ("Stop Point Cloud", StopButtonColor, StopPointCloud));

            CreateSectionTitle(content.transform, "IMU");
            CreateButtonRow(content.transform,
                ("Start IMU", ButtonColor, StartImu),
                ("Stop IMU", StopButtonColor, StopImu));

            CreateSectionTitle(content.transform, "LIGHT / DISPLAY");
            CreateButtonRow(content.transform,
                ("Start Events", ButtonColor, StartDeviceEvents),
                ("Stop Events", StopButtonColor, StopDeviceEvents));
            CreateButtonRow(content.transform,
                ("Brightness 1", ButtonColor, () => SetBrightness(1)),
                ("Brightness 6", ButtonColor, () => SetBrightness(6)),
                ("Brightness 9", ButtonColor, () => SetBrightness(9)));

            CreateSectionTitle(content.transform, "VALIDATION LOG");
            logText = CreateText("Log", content.transform, "", 17, FontStyle.Normal, TextAnchor.UpperLeft);
            logText.gameObject.AddComponent<LayoutElement>().preferredHeight = 210f;
            logText.color = new Color(0.78f, 0.84f, 0.92f, 1f);
        }

        private void StartStream(SingrayG2SensorStream stream)
        {
            if (!RequireDevice(stream + " start"))
            {
                return;
            }

            TryAction("Start " + stream, () => g2.StartSensor(stream));
            if (stream == SingrayG2SensorStream.Rgb)
            {
                rgbFrames = 0;
                rgbFramesStartedAt = Time.unscaledTime;
            }
        }

        private void StopStream(SingrayG2SensorStream stream)
        {
            TryAction("Stop " + stream, () => g2.StopSensor(stream));
        }

        private void StartPointCloud()
        {
            if (!RequireDevice("point cloud start"))
            {
                return;
            }

            TryAction("Start point cloud", g2.StartTofPointCloud);
            pointCloudRunning = true;
            pointCloudStatusText.text = "Point cloud: waiting for first frame";
        }

        private void StopPointCloud()
        {
            TryAction("Stop point cloud", g2.StopTofPointCloud);
            pointCloudRunning = false;
            pointCloudStatusText.text = "Point cloud: stopped";
        }

        private void StartImu()
        {
            if (!RequireDevice("IMU start"))
            {
                return;
            }

            int result = g2.StartImu();
            imuRunning = result >= 0;
            imuStatusText.text = $"IMU start result={result}; waiting for sample";
            AppendLog($"Start IMU result={result}");
        }

        private void StopImu()
        {
            int result = g2.StopImu();
            imuRunning = false;
            imuStatusText.text = $"IMU: stopped (result={result})";
            AppendLog($"Stop IMU result={result}");
        }

        private void StartDeviceEvents()
        {
            if (!RequireDevice("device event start"))
            {
                return;
            }

            TryAction("Start device events", g2.StartDeviceEvents);
        }

        private void StopDeviceEvents()
        {
            TryAction("Stop device events", g2.StopDeviceEvents);
        }

        private void SetBrightness(int level)
        {
            if (!RequireDevice("display brightness"))
            {
                return;
            }

            TryAction("Set brightness " + level, () => g2.SetDisplayBrightness(level));
        }

        private void CycleRgbResolution()
        {
            rgbResolutionIndex = (rgbResolutionIndex + 1) % rgbResolutions.Length;
            ApplyRgbSettings();
        }

        private void ApplyRgbSettings()
        {
            SingrayG2RgbResolution resolution = rgbResolutions[rgbResolutionIndex];
            g2?.ConfigureRgb(new SingrayG2RgbSettings { resolution = resolution, framesPerSecond = 30 });
            if (rgbResolutionText != null)
            {
                rgbResolutionText.text = "RGB Resolution: " + FormatRgbResolution(resolution);
            }
        }

        private void CycleTofResolution()
        {
            tofResolutionIndex = (tofResolutionIndex + 1) % tofResolutions.Length;
            ApplyTofSettings();
        }

        private void CycleTofFrameRate()
        {
            tofFrameRateIndex = (tofFrameRateIndex + 1) % tofFrameRates.Length;
            ApplyTofSettings();
        }

        private void ApplyTofSettings()
        {
            SingrayG2TofResolution resolution = tofResolutions[tofResolutionIndex];
            SingrayG2TofFrameRate frameRate = tofFrameRates[tofFrameRateIndex];
            g2?.ConfigureTof(new SingrayG2TofSettings
            {
                resolution = resolution,
                frameRate = frameRate,
                enableInfraredGamma = false
            });

            if (tofResolutionText != null)
            {
                tofResolutionText.text = "ToF Resolution: " + resolution.ToString().ToUpperInvariant();
            }
            if (tofFrameRateText != null)
            {
                tofFrameRateText.text = $"ToF Frame Rate: {(int)frameRate} FPS";
            }
        }

        private void OnCameraFrame(SingrayG2SensorStream stream, SingrayG2CameraFrame frame)
        {
            if (stream == SingrayG2SensorStream.Rgb || stream == SingrayG2SensorStream.TofDepth || stream == SingrayG2SensorStream.TofInfrared)
            {
                preview.texture = frame.Texture;
            }

            if (stream == SingrayG2SensorStream.Rgb)
            {
                rgbFrames++;
            }

            float elapsed = Mathf.Max(0.001f, Time.unscaledTime - rgbFramesStartedAt);
            float rgbFps = rgbFramesStartedAt > 0f ? rgbFrames / elapsed : 0f;
            frameStatusText.text =
                $"{(g2.IsSimulation ? "[SIMULATION] " : string.Empty)}Stream: {stream} | {frame.Width} x {frame.Height} | t={frame.Timestamp:F6}\n" +
                $"Position {FormatVector(frame.Position)} | RGB average {rgbFps:F1} FPS\n" +
                $"Intrinsics fx={frame.Intrinsics.x:F2} fy={frame.Intrinsics.y:F2} cx={frame.Intrinsics.z:F2} cy={frame.Intrinsics.w:F2}";
        }

        private void OnAmbientLightChanged(int state, double timestamp)
        {
            lightStatusText.text = $"Ambient light: state={state}, t={timestamp:F6}";
            AppendLog($"Ambient light state={state}, timestamp={timestamp:F6}");
        }

        private bool RequireDevice(string operation)
        {
            if (g2.IsSdkReady)
            {
                return true;
            }

            AppendLog($"SKIPPED {operation}: Singray G2 SDK is not ready.");
            return false;
        }

        private void TryAction(string label, Action action)
        {
            try
            {
                action();
                AppendLog(label + " requested.");
            }
            catch (Exception exception)
            {
                AppendLog($"ERROR {label}: {exception.GetType().Name} {exception.Message}");
                Debug.LogException(exception);
            }
        }

        private void AppendLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            recentMessages.Enqueue(line);
            while (recentMessages.Count > 10)
            {
                recentMessages.Dequeue();
            }

            if (logText != null)
            {
                logText.text = string.Join("\n", recentMessages);
            }
        }

        private Text CreateCycleButton(Transform parent, string label, Action action)
        {
            Button button = CreateButton(parent, label, ButtonColor, action);
            return button.GetComponentInChildren<Text>();
        }

        private void CreateButtonRow(Transform parent, params (string label, Color color, Action action)[] definitions)
        {
            GameObject row = CreateUiObject("ButtonRow", parent);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            row.AddComponent<LayoutElement>().preferredHeight = 54f;

            for (int i = 0; i < definitions.Length; ++i)
            {
                (string label, Color color, Action action) definition = definitions[i];
                CreateButton(row.transform, definition.label, definition.color, definition.action);
            }
        }

        private Button CreateButton(Transform parent, string label, Color color, Action action)
        {
            Image image = CreatePanel(label.Replace(" ", string.Empty), parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            image.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

            Text text = CreateText("Label", image.transform, label, 19, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f, 8f, 5f, 5f);
            return button;
        }

        private void CreateSectionTitle(Transform parent, string title)
        {
            Text text = CreateText(title.Replace(" ", string.Empty), parent, title, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.color = AccentColor;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        }

        private Image CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreateUiObject(name, parent);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.layer = 5;
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetAnchoredRect(RectTransform rect, float left, float top, float right, float height, bool anchorTop)
        {
            rect.anchorMin = anchorTop ? new Vector2(0f, 1f) : Vector2.zero;
            rect.anchorMax = anchorTop ? new Vector2(1f, 1f) : Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
        }

        private static string FormatRgbResolution(SingrayG2RgbResolution resolution)
        {
            return resolution.ToString().Substring(1).Replace("x", " x ");
        }
    }
}
