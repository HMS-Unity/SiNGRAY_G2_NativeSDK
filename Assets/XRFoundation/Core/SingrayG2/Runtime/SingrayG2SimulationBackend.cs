using System;
using System.Collections.Generic;
using UnityEngine;

namespace Singray.G2
{
    /// <summary>
    /// Deterministic synthetic sensor backend for Editor and non-Android
    /// validation. Values are illustrative and are not hardware calibration.
    /// </summary>
    internal sealed class SingrayG2SimulationBackend : ISingrayG2Backend
    {
        private const int PreviewRgbWidth = 512;
        private const int PreviewRgbHeight = 288;
        private const int PreviewTofWidth = 320;
        private const int PreviewTofHeight = 240;
        private const int CloudWidth = 160;
        private const int CloudHeight = 120;

        private static readonly Color[] RgbBars =
        {
            new Color(0.95f, 0.18f, 0.18f),
            new Color(0.95f, 0.75f, 0.15f),
            new Color(0.18f, 0.82f, 0.34f),
            new Color(0.12f, 0.72f, 0.92f),
            new Color(0.28f, 0.34f, 0.95f),
            new Color(0.74f, 0.23f, 0.90f)
        };

        private readonly HashSet<SingrayG2SensorStream> activeStreams = new HashSet<SingrayG2SensorStream>();
        private readonly Dictionary<SingrayG2SensorStream, double> nextFrameTimes = new Dictionary<SingrayG2SensorStream, double>();
        private readonly Dictionary<SingrayG2SensorStream, double> nextTextureTimes = new Dictionary<SingrayG2SensorStream, double>();
        private readonly Dictionary<SingrayG2SensorStream, Texture2D> textures = new Dictionary<SingrayG2SensorStream, Texture2D>();
        private readonly Dictionary<SingrayG2SensorStream, Color32[]> texturePixels = new Dictionary<SingrayG2SensorStream, Color32[]>();

        private SingrayG2RgbSettings rgbSettings = new SingrayG2RgbSettings();
        private SingrayG2TofSettings tofSettings = new SingrayG2TofSettings();
        private Vector3[] pointCloud;
        private bool pointCloudRunning;
        private bool rgbdRunning;
        private bool imuRunning;
        private bool deviceEventsRunning;
        private double currentTime;
        private double nextPointCloudTime;
        private double nextLightEventTime;
        private int ambientLightState;
        private int displayBrightness = 6;

        public bool IsReady => true;
        public bool IsSimulation => true;

        public event Action<SingrayG2SensorStream, SingrayG2CameraFrame> CameraFrameArrived;
        public event Action<SingrayG2DeviceEvent> DeviceEventArrived;

        public void Tick(double timeSeconds)
        {
            currentTime = timeSeconds;

            foreach (SingrayG2SensorStream stream in activeStreams)
            {
                if (!nextFrameTimes.TryGetValue(stream, out double nextFrame) || timeSeconds >= nextFrame)
                {
                    int framesPerSecond = GetFrameRate(stream);
                    nextFrameTimes[stream] = timeSeconds + 1.0 / Mathf.Max(1, framesPerSecond);
                    PublishFrame(stream, timeSeconds);
                }
            }

            if (pointCloudRunning && (pointCloud == null || timeSeconds >= nextPointCloudTime))
            {
                nextPointCloudTime = timeSeconds + 0.1;
                UpdatePointCloud(timeSeconds);
            }

            if (deviceEventsRunning && timeSeconds >= nextLightEventTime)
            {
                nextLightEventTime = timeSeconds + 1.5;
                ambientLightState = (ambientLightState + 1) % 3;
                DeviceEventArrived?.Invoke(new SingrayG2DeviceEvent(
                    timeSeconds,
                    (long)(timeSeconds * 1000000.0),
                    6,
                    ambientLightState));
                MyDebugTool.LogDiagnostic("LIGHT", "simulation-event", $"backend=simulation state={ambientLightState}", 0f);
            }
        }

        public void ConfigureRgb(SingrayG2RgbSettings settings)
        {
            rgbSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            MyDebugTool.LogDiagnostic(
                "SIMULATION",
                "rgb-config",
                $"RGB resolution={settings.resolution} fps={settings.framesPerSecond}; previewTexture={PreviewRgbWidth}x{PreviewRgbHeight}",
                0f);
        }

        public void ConfigureTof(SingrayG2TofSettings settings)
        {
            tofSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            MyDebugTool.LogDiagnostic(
                "SIMULATION",
                "tof-config",
                $"ToF resolution={settings.resolution} fps={(int)settings.frameRate} gamma={settings.enableInfraredGamma}",
                0f);
        }

        public void StartSensor(SingrayG2SensorStream stream)
        {
            activeStreams.Add(stream);
            nextFrameTimes[stream] = 0.0;
            nextTextureTimes[stream] = 0.0;
            MyDebugTool.LogDiagnostic("SIMULATION", "sensor-start-" + stream, $"SIMULATION stream={stream} started", 0f);
        }

        public void StopSensor(SingrayG2SensorStream stream)
        {
            activeStreams.Remove(stream);
            nextFrameTimes.Remove(stream);
            MyDebugTool.LogDiagnostic("SIMULATION", "sensor-stop-" + stream, $"SIMULATION stream={stream} stopped", 0f);
        }

        public bool IsSensorRunning(SingrayG2SensorStream stream)
        {
            return activeStreams.Contains(stream);
        }

        public void StartTofPointCloud()
        {
            pointCloudRunning = true;
            nextPointCloudTime = 0.0;
            StartSensor(SingrayG2SensorStream.TofDepth);
            MyDebugTool.LogDiagnostic("SIMULATION", "point-cloud-start", $"SIMULATION cloud={CloudWidth}x{CloudHeight}", 0f);
        }

        public bool TryGetTofPointCloud(out Vector3[] points)
        {
            points = pointCloud;
            return pointCloudRunning && pointCloud != null;
        }

        public void StopTofPointCloud()
        {
            pointCloudRunning = false;
            pointCloud = null;
            MyDebugTool.LogDiagnostic("SIMULATION", "point-cloud-stop", "SIMULATION point cloud stopped", 0f);
        }

        public void StartRgbd()
        {
            rgbdRunning = true;
            StartSensor(SingrayG2SensorStream.Rgb);
            StartSensor(SingrayG2SensorStream.TofDepth);
            MyDebugTool.LogDiagnostic("SIMULATION", "rgbd-start", "SIMULATION RGBD started", 0f);
        }

        public void StopRgbd()
        {
            rgbdRunning = false;
            StopSensor(SingrayG2SensorStream.Rgb);
            StopSensor(SingrayG2SensorStream.TofDepth);
            MyDebugTool.LogDiagnostic("SIMULATION", "rgbd-stop", "SIMULATION RGBD stopped", 0f);
        }

        public bool TryGetRgbPixelWorldPosition(Vector2 pixel, out Vector3 worldPosition)
        {
            if (!rgbdRunning)
            {
                worldPosition = Vector3.zero;
                return false;
            }

            GetRgbSize(out int width, out int height);
            const float depthMeters = 1.5f;
            float normalizedX = pixel.x / Mathf.Max(1, width) - 0.5f;
            float normalizedY = 0.5f - pixel.y / Mathf.Max(1, height);
            worldPosition = new Vector3(normalizedX * depthMeters * 1.25f, normalizedY * depthMeters, depthMeters);
            return true;
        }

        public int StartImu()
        {
            imuRunning = true;
            MyDebugTool.LogDiagnostic("SIMULATION", "imu-start", "SIMULATION IMU started at 100 Hz nominal", 0f);
            return 0;
        }

        public int StopImu()
        {
            imuRunning = false;
            MyDebugTool.LogDiagnostic("SIMULATION", "imu-stop", "SIMULATION IMU stopped", 0f);
            return 0;
        }

        public bool TryGetImuSample(out SingrayG2ImuSample sample)
        {
            if (!imuRunning)
            {
                sample = default;
                return false;
            }

            float time = (float)currentTime;
            Vector3 acceleration = new Vector3(
                0.08f * Mathf.Sin(time * 1.7f),
                9.81f + 0.04f * Mathf.Sin(time * 0.9f),
                0.06f * Mathf.Cos(time * 1.3f));
            Vector3 angularVelocity = new Vector3(
                0.03f * Mathf.Sin(time * 0.8f),
                0.18f * Mathf.Cos(time * 0.45f),
                0.02f * Mathf.Sin(time * 1.1f));
            Vector3 magneticField = new Vector3(
                24f + 2f * Mathf.Sin(time * 0.2f),
                -8f + Mathf.Cos(time * 0.3f),
                41f + 1.5f * Mathf.Sin(time * 0.25f));
            sample = new SingrayG2ImuSample(currentTime, acceleration, angularVelocity, magneticField);
            return true;
        }

        public void StartDeviceEvents()
        {
            deviceEventsRunning = true;
            nextLightEventTime = 0.0;
            MyDebugTool.LogDiagnostic("SIMULATION", "device-events-start", "SIMULATION ambient-light events started", 0f);
        }

        public void StopDeviceEvents()
        {
            deviceEventsRunning = false;
            MyDebugTool.LogDiagnostic("SIMULATION", "device-events-stop", "SIMULATION device events stopped", 0f);
        }

        public void SetDisplayBrightness(int level)
        {
            displayBrightness = Mathf.Clamp(level, 1, 9);
            MyDebugTool.LogDiagnostic("SIMULATION", "brightness", $"SIMULATION displayBrightness={displayBrightness}", 0f);
        }

        public void Dispose()
        {
            activeStreams.Clear();
            foreach (Texture2D texture in textures.Values)
            {
                if (texture == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(texture);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            textures.Clear();
            texturePixels.Clear();
            pointCloud = null;
        }

        private void PublishFrame(SingrayG2SensorStream stream, double timeSeconds)
        {
            Texture2D texture = GetOrCreateTexture(stream);
            if (!nextTextureTimes.TryGetValue(stream, out double nextTexture) || timeSeconds >= nextTexture)
            {
                nextTextureTimes[stream] = timeSeconds + 0.1;
                UpdateTexture(stream, texture, timeSeconds);
            }

            GetReportedSize(stream, out int width, out int height);
            float time = (float)timeSeconds;
            Vector3 position = new Vector3(
                0.025f * Mathf.Sin(time * 0.7f),
                1.62f + 0.01f * Mathf.Sin(time * 0.5f),
                0.02f * Mathf.Cos(time * 0.6f));
            Quaternion rotation = Quaternion.Euler(
                1.5f * Mathf.Sin(time * 0.45f),
                8f * Mathf.Sin(time * 0.35f),
                0.8f * Mathf.Cos(time * 0.5f));
            float fx = width * 0.82f;
            float fy = height * 0.82f;

            CameraFrameArrived?.Invoke(stream, new SingrayG2CameraFrame(
                width,
                height,
                texture,
                timeSeconds,
                position,
                rotation,
                new Vector3(0.018f, -0.012f, 0.006f),
                Quaternion.identity,
                new Vector4(fx, fy, width * 0.5f, height * 0.5f),
                new Vector4(-0.045f, 0.012f, 0.0004f, -0.0003f),
                0.0f));
        }

        private Texture2D GetOrCreateTexture(SingrayG2SensorStream stream)
        {
            if (textures.TryGetValue(stream, out Texture2D texture) && texture != null)
            {
                return texture;
            }

            bool isTof = stream == SingrayG2SensorStream.TofDepth || stream == SingrayG2SensorStream.TofInfrared;
            int width = isTof ? PreviewTofWidth : PreviewRgbWidth;
            int height = isTof ? PreviewTofHeight : PreviewRgbHeight;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Singray G2 SIMULATION " + stream,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            textures[stream] = texture;
            texturePixels[stream] = new Color32[width * height];
            return texture;
        }

        private void UpdateTexture(SingrayG2SensorStream stream, Texture2D texture, double timeSeconds)
        {
            Color32[] pixels = texturePixels[stream];
            int width = texture.width;
            int height = texture.height;
            int movingLine = (int)(timeSeconds * 90.0) % width;
            float time = (float)timeSeconds;

            for (int y = 0; y < height; ++y)
            {
                float normalizedY = y / (float)Mathf.Max(1, height - 1);
                for (int x = 0; x < width; ++x)
                {
                    float normalizedX = x / (float)Mathf.Max(1, width - 1);
                    Color color;
                    if (stream == SingrayG2SensorStream.TofDepth)
                    {
                        float depth = Mathf.Clamp01(0.5f + 0.28f * Mathf.Sin(normalizedX * 8f + time) + 0.18f * Mathf.Cos(normalizedY * 7f - time * 0.7f));
                        color = new Color(depth, 0.25f + 0.45f * (1f - depth), 1f - depth, 1f);
                    }
                    else if (stream == SingrayG2SensorStream.TofInfrared)
                    {
                        float infrared = Mathf.Clamp01(0.45f + 0.35f * Mathf.Sin(normalizedX * 15f + time * 1.2f) * Mathf.Cos(normalizedY * 11f));
                        color = new Color(infrared, infrared, infrared, 1f);
                    }
                    else
                    {
                        int band = Mathf.Clamp((x * 6) / width, 0, 5);
                        color = RgbBars[band] * (0.62f + normalizedY * 0.38f);
                        color.a = 1f;
                    }

                    if (Mathf.Abs(x - movingLine) <= 2 || x % 128 == 0 || y % 96 == 0)
                    {
                        color = Color.white;
                    }
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void UpdatePointCloud(double timeSeconds)
        {
            if (pointCloud == null || pointCloud.Length != CloudWidth * CloudHeight)
            {
                pointCloud = new Vector3[CloudWidth * CloudHeight];
            }

            float time = (float)timeSeconds;
            for (int y = 0; y < CloudHeight; ++y)
            {
                float normalizedY = y / (float)(CloudHeight - 1) - 0.5f;
                for (int x = 0; x < CloudWidth; ++x)
                {
                    float normalizedX = x / (float)(CloudWidth - 1) - 0.5f;
                    float depth = 1.55f + 0.12f * Mathf.Sin(normalizedX * 12f + time) * Mathf.Cos(normalizedY * 9f - time * 0.6f);
                    if (Mathf.Abs(normalizedX) < 0.12f && Mathf.Abs(normalizedY) < 0.18f)
                    {
                        depth -= 0.32f;
                    }
                    pointCloud[y * CloudWidth + x] = new Vector3(normalizedX * 1.6f, -normalizedY * 1.2f, depth);
                }
            }
        }

        private int GetFrameRate(SingrayG2SensorStream stream)
        {
            if (stream == SingrayG2SensorStream.Rgb)
            {
                return Mathf.Clamp(rgbSettings.framesPerSecond, 1, 60);
            }
            if (stream == SingrayG2SensorStream.TofDepth || stream == SingrayG2SensorStream.TofInfrared)
            {
                return (int)tofSettings.frameRate;
            }
            return 30;
        }

        private void GetReportedSize(SingrayG2SensorStream stream, out int width, out int height)
        {
            if (stream == SingrayG2SensorStream.Rgb)
            {
                GetRgbSize(out width, out height);
                return;
            }

            if (stream == SingrayG2SensorStream.TofDepth || stream == SingrayG2SensorStream.TofInfrared)
            {
                switch (tofSettings.resolution)
                {
                    case SingrayG2TofResolution.Hqvga:
                        width = 240;
                        height = 160;
                        return;
                    case SingrayG2TofResolution.Vga:
                        width = 640;
                        height = 480;
                        return;
                    default:
                        width = 320;
                        height = 240;
                        return;
                }
            }

            width = PreviewRgbWidth;
            height = PreviewRgbHeight;
        }

        private void GetRgbSize(out int width, out int height)
        {
            switch (rgbSettings.resolution)
            {
                case SingrayG2RgbResolution.R320x240: width = 320; height = 240; break;
                case SingrayG2RgbResolution.R640x480: width = 640; height = 480; break;
                case SingrayG2RgbResolution.R1920x1080: width = 1920; height = 1080; break;
                case SingrayG2RgbResolution.R2560x1920: width = 2560; height = 1920; break;
                case SingrayG2RgbResolution.R3840x2160: width = 3840; height = 2160; break;
                default: width = 1280; height = 720; break;
            }
        }
    }
}
