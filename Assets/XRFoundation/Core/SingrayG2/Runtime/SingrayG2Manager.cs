using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Singray.G2
{
    /// <summary>
    /// Stable product-facing entry point. Auto mode selects simulation in the
    /// Editor/non-Android players and the native supplier adapter on Android.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class SingrayG2Manager : MonoBehaviour
    {
        public const string ProductName = "Singray G2";
        public const string ProductCode = "SINGRAY_G2";

        private static SingrayG2Manager instance;

        [Header("Singray G2")]
        [SerializeField]
        private SingrayG2RuntimeMode runtimeMode = SingrayG2RuntimeMode.Auto;

        [SerializeField]
        private bool diagnosticsEnabled = true;

        [SerializeField]
        private bool persistAcrossScenes = true;

        [Header("Sensor defaults")]
        [SerializeField]
        private SingrayG2RgbSettings rgbSettings = new SingrayG2RgbSettings();

        [SerializeField]
        private SingrayG2TofSettings tofSettings = new SingrayG2TofSettings();

        private readonly ConcurrentQueue<SingrayG2DeviceEvent> pendingDeviceEvents = new ConcurrentQueue<SingrayG2DeviceEvent>();
        private ISingrayG2Backend backend;

        public static SingrayG2Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SingrayG2Manager>();
                }
                if (instance == null)
                {
                    GameObject productObject = new GameObject(ProductName);
                    instance = productObject.AddComponent<SingrayG2Manager>();
                }
                return instance;
            }
        }

        public SingrayG2RuntimeMode ConfiguredRuntimeMode => runtimeMode;
        public SingrayG2RuntimeMode ActiveRuntimeMode => Backend.IsSimulation
            ? SingrayG2RuntimeMode.Simulation
            : SingrayG2RuntimeMode.NativeDevice;
        public bool DiagnosticsEnabled => diagnosticsEnabled;
        public bool IsSimulation => Backend.IsSimulation;
        public bool IsSdkReady => Backend.IsReady;

        public event Action<SingrayG2SensorStream, SingrayG2CameraFrame> CameraFrameArrived;
        public event Action<SingrayG2DeviceEvent> DeviceEventArrived;
        public event Action<int, double> AmbientLightChanged;

        private ISingrayG2Backend Backend
        {
            get
            {
                if (backend == null)
                {
                    CreateBackend();
                }
                return backend;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            if (persistAcrossScenes && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            SetDiagnosticsEnabled(diagnosticsEnabled);
            CreateBackend();
            MyDebugTool.LogDiagnostic(
                "STARTUP",
                "product-entry",
                $"product={ProductName} code={ProductCode} configuredMode={runtimeMode} activeMode={ActiveRuntimeMode}",
                0f);
        }

        private void Update()
        {
            Backend.Tick(Time.realtimeSinceStartup);
            while (pendingDeviceEvents.TryDequeue(out SingrayG2DeviceEvent productEvent))
            {
                DeviceEventArrived?.Invoke(productEvent);
                if (productEvent.IsAmbientLight)
                {
                    AmbientLightChanged?.Invoke(productEvent.State, productEvent.HostTimestamp);
                }
            }
        }

        private void OnDestroy()
        {
            DisposeBackend();
            if (instance == this)
            {
                instance = null;
            }
        }

        public void SetRuntimeMode(SingrayG2RuntimeMode mode)
        {
            if (runtimeMode == mode && backend != null)
            {
                return;
            }

            runtimeMode = mode;
            DisposeBackend();
            CreateBackend();
            MyDebugTool.LogDiagnostic("STARTUP", "runtime-mode", $"configuredMode={mode} activeMode={ActiveRuntimeMode}", 0f);
        }

        public void SetDiagnosticsEnabled(bool enabled)
        {
            diagnosticsEnabled = enabled;
            MyDebugTool.diagnosticsEnable = enabled;
        }

        public void ConfigureRgb(SingrayG2RgbSettings settings)
        {
            rgbSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            Backend.ConfigureRgb(rgbSettings);
        }

        public void ConfigureTof(SingrayG2TofSettings settings)
        {
            tofSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            Backend.ConfigureTof(tofSettings);
        }

        public void StartSensor(SingrayG2SensorStream stream) => Backend.StartSensor(stream);
        public void StopSensor(SingrayG2SensorStream stream) => Backend.StopSensor(stream);
        public bool IsSensorRunning(SingrayG2SensorStream stream) => Backend.IsSensorRunning(stream);
        public void StartTofPointCloud() => Backend.StartTofPointCloud();
        public bool TryGetTofPointCloud(out Vector3[] points) => Backend.TryGetTofPointCloud(out points);
        public void StopTofPointCloud() => Backend.StopTofPointCloud();
        public void StartRgbd() => Backend.StartRgbd();
        public void StopRgbd() => Backend.StopRgbd();
        public bool TryGetRgbPixelWorldPosition(Vector2 pixel, out Vector3 worldPosition) => Backend.TryGetRgbPixelWorldPosition(pixel, out worldPosition);
        public int StartImu() => Backend.StartImu();
        public int StopImu() => Backend.StopImu();
        public bool TryGetImuSample(out SingrayG2ImuSample sample) => Backend.TryGetImuSample(out sample);
        public void StartDeviceEvents() => Backend.StartDeviceEvents();
        public void StopDeviceEvents() => Backend.StopDeviceEvents();
        public void SetDisplayBrightness(int level) => Backend.SetDisplayBrightness(Mathf.Clamp(level, 1, 9));

        private void CreateBackend()
        {
            backend = ShouldUseSimulation()
                ? (ISingrayG2Backend)new SingrayG2SimulationBackend()
                : new SingrayG2NativeBackend(transform);
            backend.CameraFrameArrived += OnBackendCameraFrame;
            backend.DeviceEventArrived += OnBackendDeviceEvent;
            backend.ConfigureRgb(rgbSettings);
            backend.ConfigureTof(tofSettings);
        }

        private void DisposeBackend()
        {
            if (backend == null)
            {
                return;
            }

            backend.CameraFrameArrived -= OnBackendCameraFrame;
            backend.DeviceEventArrived -= OnBackendDeviceEvent;
            backend.Dispose();
            backend = null;
            while (pendingDeviceEvents.TryDequeue(out _))
            {
            }
        }

        private bool ShouldUseSimulation()
        {
            if (runtimeMode == SingrayG2RuntimeMode.Simulation)
            {
                return true;
            }
            if (runtimeMode == SingrayG2RuntimeMode.NativeDevice)
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }

        private void OnBackendCameraFrame(SingrayG2SensorStream stream, SingrayG2CameraFrame frame)
        {
            CameraFrameArrived?.Invoke(stream, frame);
        }

        private void OnBackendDeviceEvent(SingrayG2DeviceEvent productEvent)
        {
            // Native callbacks may come from a worker thread. Simulation also
            // uses the same queue so all public events have main-thread semantics.
            pendingDeviceEvents.Enqueue(productEvent);
        }
    }
}
