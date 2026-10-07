# SiNGRAY G2 Native SDK: API Reference

This reference describes the application-facing C# API in the `dev/4.2` source tree. It covers the complete public surface of `SingrayG2Manager`, its data types, and the main Foundation feature interfaces used by the demos. Low-level native declarations remain defined by the checked-in bindings; this document does not assert support for every native feature on every device.

For installation, scene selection, builds, and demos, see the [Demo and User Guide](Demo-and-User-Guide.md).

The current SDK version is `4.2.0.2`, exposed by `Singray.Foundation.VersionInfo.version` and aligned with the Android application version. `VersionInfo.Print()` logs the SDK version during XR initialization. See the [Release Notes](../RELEASE_NOTES.md) for the `4.2.0.x` update history. SDK version numbers do not identify the vendor AAR version or device firmware.

## 1. API layers

| Layer | Entry point | Responsibility |
| --- | --- | --- |
| Product API | `Singray.G2.SingrayG2Manager` | Sensor configuration, camera events, point clouds, RGBD, IMU, device events, and brightness |
| Feature API | Managers in `Singray.Foundation` | Demo feature implementations, including mapping, meshes, gestures, and media |
| Native bindings | Global `API` class | Supplier P/Invoke declarations and native data structures |

Source locations:

- [Product manager](../Assets/XRFoundation/Core/SingrayG2/Runtime/SingrayG2Manager.cs)
- [Product types](../Assets/XRFoundation/Core/SingrayG2/Runtime/SingrayG2Types.cs)
- [Native adapter](../Assets/XRFoundation/Core/SingrayG2/Runtime/SingrayG2NativeBackend.cs)
- [Simulation backend](../Assets/XRFoundation/Core/SingrayG2/Runtime/SingrayG2SimulationBackend.cs)
- [Native binding declarations](../Assets/XRFoundation/Core/Sdk/Xslam/Scripts/API.cs)

Existing public class and method names retain the `Xv` prefix where present in source. Use the exact identifiers shown here, including the existing spelling of `StartPlaneDetction`.

## 2. Manager access and lifecycle

```csharp
using Singray.G2;

SingrayG2Manager sdk = SingrayG2Manager.Instance;
```

`Instance` finds an existing manager or creates a GameObject with a manager component. Its default execution order is -200. By default, a manager on a root GameObject persists across scene changes.

| Member | Contract |
| --- | --- |
| `const string ProductName` | `"Singray G2"` |
| `const string ProductCode` | `"SINGRAY_G2"` |
| `ConfiguredRuntimeMode` | Requested `SingrayG2RuntimeMode` |
| `ActiveRuntimeMode` | Resolved `Simulation` or `NativeDevice` mode |
| `IsSimulation` | Whether the selected backend generates synthetic data |
| `IsSdkReady` | Backend readiness; simulation returns true |
| `DiagnosticsEnabled` | Current diagnostics setting |
| `void SetRuntimeMode(SingrayG2RuntimeMode mode)` | Disposes and recreates the backend when the configuration changes |
| `void SetDiagnosticsEnabled(bool enabled)` | Updates the manager setting and shared diagnostic logging flag |

`SingrayG2RuntimeMode` contains `Auto`, `NativeDevice`, and `Simulation`. `Auto` selects native operation only in an Android player. Explicit `NativeDevice` selection outside Android does not provide working hardware access; individual native adapter methods have differing platform guards.

Stop owned streams before changing runtime mode, then restart the required features after the new backend is ready. Previously running features are not automatically restored. Access the manager and Unity objects on the Unity main thread.

## 3. Camera streams and settings

### Stream identifiers

| `SingrayG2SensorStream` | Native Foundation stream |
| --- | --- |
| `Rgb` | `ARCameraStream` |
| `TofDepth` | `TofDepthCameraStream` |
| `TofInfrared` | `TofIRCameraStream` |
| `LeftTrackingCamera` | `LeftStereoCameraStream` |
| `RightTrackingCamera` | `RightStereoCameraStream` |
| `ComputeCamera` | `WebCameraStream` |

### Configuration types

`SingrayG2RgbSettings` is a serializable class with two public fields:

| Field | Default | Values |
| --- | --- | --- |
| `resolution` | `R1280x720` | `R320x240`, `R640x480`, `R1280x720`, `R1920x1080`, `R2560x1920`, `R3840x2160` |
| `framesPerSecond` | `30` | Inspector range 1-60; the native adapter clamps to this range |

`SingrayG2TofSettings` is a serializable class with these fields:

| Field | Default | Values |
| --- | --- | --- |
| `resolution` | `Qvga` | `SingrayG2TofResolution.Hqvga`, `Qvga`, `Vga` |
| `frameRate` | `Fps30` | `SingrayG2TofFrameRate.Fps5`, `Fps10`, `Fps15`, `Fps20`, `Fps25`, `Fps30` |
| `enableInfraredGamma` | `false` | Boolean gamma option |

Enum availability is not a guarantee that a connected device supports every combination.

### Methods

```csharp
void ConfigureRgb(SingrayG2RgbSettings settings);
void ConfigureTof(SingrayG2TofSettings settings);
void StartSensor(SingrayG2SensorStream stream);
void StopSensor(SingrayG2SensorStream stream);
bool IsSensorRunning(SingrayG2SensorStream stream);
```

Configuration methods throw `ArgumentNullException` for null settings. They retain the supplied settings object rather than creating a defensive copy. Configure before starting capture; stop and restart a stream when changing modes. Start and stop methods return no success code. Confirm readiness and frame delivery independently; a running flag is not proof of fresh frames.

### Camera event and frame data

```csharp
event Action<SingrayG2SensorStream, SingrayG2CameraFrame> CameraFrameArrived;
```

`SingrayG2CameraFrame` is a readonly struct:

| Property | Type | Meaning |
| --- | --- | --- |
| `Width`, `Height` | `int` | Frame dimensions reported by the backend |
| `Texture` | `UnityEngine.Texture` | Preview texture supplied by the backend |
| `Timestamp` | `double` | Backend-provided frame timestamp |
| `Position` | `Vector3` | Forwarded camera pose position |
| `Rotation` | `UnityEngine.Quaternion` | Forwarded camera pose rotation |
| `ExtrinsicPosition` | `Vector3` | Forwarded extrinsic translation |
| `ExtrinsicRotation` | `UnityEngine.Quaternion` | Forwarded extrinsic rotation |
| `Intrinsics` | `Vector4` | `fx`, `fy`, `cx`, `cy` |
| `Distortion` | `Vector4` | `k1`, `k2`, `p1`, `p2` |
| `DistortionK3` | `float` | Third radial distortion coefficient |

The facade forwards calibration fields without validating their availability for each stream. Do not assume every field is calibrated or populated for every camera. Native timestamp units and coordinate conventions are not normalized by the facade; confirm them against the binding and device before combining data sources.

Textures remain backend-owned and can be updated or reused. Copy the content if an immutable frame is required, and do not destroy the supplied texture. Simulation can report configured dimensions while providing smaller preview textures; use the texture's actual dimensions for texture operations.

Camera events are forwarded directly by the facade without an additional thread-dispatch queue. Do not assume the same dispatch guarantee as device events when implementing a different backend. Avoid changing stream collections inside a camera callback; defer start/stop operations until after callback processing.

### Example: RGB preview component

Attach this component to an active GameObject and assign a UI `RawImage` to `preview`. It owns the RGB stream; do not attach multiple independent owners for the same stream.

```csharp
using System.Collections;
using Singray.G2;
using UnityEngine;
using UnityEngine.UI;

public sealed class G2RgbPreview : MonoBehaviour
{
    [SerializeField] private RawImage preview;
    private SingrayG2Manager sdk;
    private Coroutine startup;
    private bool started;

    private void OnEnable()
    {
        sdk = SingrayG2Manager.Instance;
        sdk.CameraFrameArrived += OnFrame;
        startup = StartCoroutine(StartWhenReady());
    }

    private IEnumerator StartWhenReady()
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (sdk != null && !sdk.IsSdkReady)
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Debug.LogWarning("G2 SDK readiness timeout.");
                yield break;
            }
            yield return null;
        }

        if (sdk == null) yield break;
        sdk.ConfigureRgb(new SingrayG2RgbSettings
        {
            resolution = SingrayG2RgbResolution.R1280x720,
            framesPerSecond = 30
        });
        sdk.StartSensor(SingrayG2SensorStream.Rgb);
        started = true;
    }

    private void OnFrame(SingrayG2SensorStream stream,
                         SingrayG2CameraFrame frame)
    {
        if (stream == SingrayG2SensorStream.Rgb && preview != null)
            preview.texture = frame.Texture;
    }

    private void OnDisable()
    {
        if (startup != null) StopCoroutine(startup);
        startup = null;
        if (sdk != null)
        {
            sdk.CameraFrameArrived -= OnFrame;
            if (started) sdk.StopSensor(SingrayG2SensorStream.Rgb);
        }
        started = false;
        if (preview != null) preview.texture = null;
    }
}
```

## 4. ToF point clouds and RGBD

| Method | Behavior |
| --- | --- |
| `void StartTofPointCloud()` | Starts point-cloud production; native operation applies current ToF settings |
| `bool TryGetTofPointCloud(out Vector3[] points)` | Returns whether a cloud is available; ignore output when false |
| `void StopTofPointCloud()` | Stops point-cloud production |
| `void StartRgbd()` | Starts RGB pixel-pose support through the RGBD backend |
| `bool TryGetRgbPixelWorldPosition(Vector2 pixel, out Vector3 worldPosition)` | Queries a 3D position for an RGB image pixel |
| `void StopRgbd()` | Stops RGBD operation |

Start a feature once, poll its `TryGet` method from `Update`, and stop it when finished. Treat arrays as borrowed data; clone them before retaining them across updates or handing them to background processing. Verify coordinate conventions and scale on the native device before combining results with scene geometry.

RGBD input uses image pixels, not normalized UVs or display-screen pixels. A failed query does not define a valid position. Simulation produces illustrative positions at a fixed depth and is not a depth measurement.

Simulation point-cloud startup also starts the ToF depth stream; stopping point-cloud production does not itself stop that simulated depth stream. Explicitly stop owned sensor streams during cleanup.

## 5. IMU

```csharp
int StartImu();
int StopImu();
bool TryGetImuSample(out SingrayG2ImuSample sample);
```

The native adapter returns `-2` from `StartImu` when the SDK is not ready, and `-1` outside the supported native platform. On Android, other results are forwarded from the native API; do not invent a universal success-code interpretation. Simulation returns its own synthetic results.

`SingrayG2ImuSample` exposes `Timestamp` (`double`), `Acceleration`, `AngularVelocity`, and `MagneticField` (`Vector3`). The native adapter maps these vectors from indices 0, 1, and 2 of its native IMU buffer. The facade does not convert or document physical units. Establish units and timestamps from the native SDK contract before sensor fusion. Ignore the sample when `TryGetImuSample` returns false.

## 6. Device events and display control

```csharp
void StartDeviceEvents();
void StopDeviceEvents();
void SetDisplayBrightness(int level);

event Action<SingrayG2DeviceEvent> DeviceEventArrived;
event Action<int, double> AmbientLightChanged;
```

Subscribe before starting device events and unsubscribe during teardown. The manager queues backend device events and delivers them from its Unity `Update` method. For ambient-light events it invokes `DeviceEventArrived` first, then `AmbientLightChanged` with the state and host timestamp.

| `SingrayG2DeviceEvent` property | Meaning |
| --- | --- |
| `HostTimestamp` | Backend host timestamp as `double` |
| `DeviceTimestampMicroseconds` | Device timestamp as `long`, in microseconds |
| `Type` | Native event type as `int` |
| `State` | Native event state as `int` |
| `IsAmbientLight` | True when `Type == 6` |

The facade does not interpret other event types or convert ambient-light states to lux. `SetDisplayBrightness` clamps levels to 1-9 before delegating to the backend. Simulation stores the selected level without controlling physical hardware.

## 7. Foundation feature APIs

Use `using Singray.Foundation;` for these managers. Most require device services, serialized settings, or prefab references. Begin with the corresponding sample rather than adding an unconfigured component to an empty scene. Pair event subscriptions with unsubscriptions, especially for static events.

### Cameras and RGBD

`XvCameraManager` exposes `StartCapture(XvCameraStreamType)`, `StopCapture(XvCameraStreamType)`, and `IsOn(XvCameraStreamType)`. Its settings are exposed through `XvARCameraParameter`, `XvWebCameraParameter`, and `XvTofCameraParameter`.

Static `UnityEvent<cameraData>` events are `onARCameraStreamFrameArrived`, `onLeftStereoStreamFrameArrived`, `onRightStereoStreamFrameArrived`, `onTofDepthCameraStreamFrameArrived`, `onTofIRCameraStreamFrameArrived`, and `onWebCameraStreamFrameArrived`. Subscribe with `AddListener` and remove the same handler with `RemoveListener`.

Point-cloud methods are `StartTofPointCloud()`, `GetPointCloudData(out Vector3[] data)`, and `StopTofPointCloud()`. ToF exposure overloads are `SetTofExposure(int exposureTimeMs)` and `SetTofExposure(int libmode, int resulution, int fps, float exposureTimeMs)`; preserve the supplier-specific parameter semantics.

`XvRgbdManager` provides `StartRgbPose()`, `StopRgbPose()`, `GetRgbPixel3DPose(Vector2 rgbPoint, ref Vector3 spacePoint)`, and `GetRgbPixelPoseList(Vector2[] rgbPoint, ref pointer_3dpose[] spacePose)`.

RGBD and point-cloud startup wait asynchronously for native SDK readiness, with a ten-second timeout. Start is a request, not a guarantee of available data. Stop cancels pending startup; RGBD also stops when its component is disabled. RGBD queries return false until running with a positive frame timestamp. Batch queries require non-null arrays and an output array at least as long as the input array. These Foundation native paths are guarded for Android players and do not provide Editor simulation.

### Spatial understanding

| Manager | Main interface | Result handling |
| --- | --- | --- |
| `XvPlaneManager` | `StartPlaneDetction()`, `StopPlaneDetection()` | Instance `planesChanged` event with `plane[]` |
| `XvSpatialMeshManager` | `StartMeshDetection()`, `StopMeshDetection()` | Static `meshChanged` event with `NowXslamSurface` |
| `XvSpatialMapManager` | `StartSlamMap()`, `SaveSlamMap()`, `LoadSlamMap(string mapPath)`, `GetFeaturePoint()` | Save returns a path; static save/load/matching events report progress or completion |
| `XvTagRecognizerManager` | `StartTagDetector(RecognizerMode recognizerMode)`, `StopTagDetector()`, `SetDetectStatus(bool isDetect)` | Configure and consume results as shown in the TagRecognizer sample |

Spatial-map events are `onMapSaveCompleteEvent` (`UnityEvent<int, int>`), `onMapLoadCompleteEvent` (`UnityEvent<int>`), and `onMapMatchingEvent` (`UnityEvent<float>`). Their native codes are not translated into a product-level status enum.

`StartPlaneDetction()` uses a cancellable readiness coroutine with a ten-second timeout instead of blocking the Unity main thread. `StopPlaneDetection()` and component disable cancel pending startup. Plane payload parsing validates declared lengths and counts before reading or allocating; rejected data is not a valid plane result. These checks cannot recover from a crash inside the native implementation.

Spatial-map operations require an Android player and a ready SDK. `SaveSlamMap()` returns **null** when unavailable; otherwise it returns the requested output path before asynchronous completion. Wait for the save event and verify the file before loading or sharing it. `StartSlamMap()` resets SLAM; it does not itself enable feature-point visualization.

Use `SwitchFeaturePointState()` to toggle feature-point acquisition and inspect the read-only `ShowFeaturePoint` property. `GetFeaturePoint()` returns null when unavailable, disabled, or given an invalid native pointer or count. Returned positions apply the manager's Y-axis sign inversion. Native map buffers remain SDK-owned and must not be freed or destroyed by callers.

Map callbacks are queued and their UnityEvents are invoked from the manager's `Update()` on the main thread. Keep the manager active while awaiting results. Disabling it stops an active feature-point stream and clears queued events; it does not establish cancellation of a native save/load operation. Delegates are retained in static fields for native callback lifetime. Use a single active map manager and remove your event listeners during teardown.

### Gestures

`XvStaticGestureManager` provides:

```csharp
StaticGestureStatus GetCurrentStaticGesture(HandType handType);
bool GetKeyDown(StaticGestureStatus gestureStatus, HandType handType);
bool GetKeyUp(StaticGestureStatus buttonKey, HandType handType);
bool GetKey(StaticGestureStatus buttonKey, HandType handType);
```

Use transition queries during the frame update loop. Keep the hand-input and MRTK provider configuration from a working sample when integrating gesture-driven UI.

### Media

`XvMediaRecorderManager` exposes `StartCapture()`, `StopCapture(bool closeCamera = false)`, `IsCameraFrameReady()`, `StartRecording()`, `IsVideoRecording()`, `StopRecording(UnityAction<string> callback)`, `SaveScreenshot(UnityAction<string> callback)`, and `IsTakingScreenshots()`.

Wait for frame readiness before capturing useful output. Stop-recording and screenshot callbacks provide output paths; wait for completion before disposing dependent objects. Create the recorder from the configured `Resources/XvMediaRecorderManager` prefab as the hub does, or supply equivalent references.

`XvMRVideoCaptureManager` exposes `StartCapture()`, `StopCapture(bool closeCamera = false)`, and the `HasFrame` property. `XvRTSPStreamerManager` exposes `StartRtspStreaming()` and `StopRtspStreaming()`; configure its serialized settings using the RTSP sample.

### System settings

`XvSystemSettingManager` provides `GetBrightnessLevel()`, `SetBrightnessLevel(int level)`, `SetIPD(float ipd)`, `GetIPD()`, `GetVolumeCurrent()`, `GetVolumeMax()`, and `AdjustVolume(int direction)`. Device-event access uses `XSlamStartEventStream(device_stream_callback cb)` and `XSlamStopEventStream()`. Prefer the product manager for queued device-event delivery and brightness clamping.

## 8. Native interop

The global `API` class declares the supplier ABI in `Core/Sdk/Xslam/Scripts/API.cs`. Examples include:

```csharp
bool API.xslam_ready();
bool API.xslam_get_6dof(ref Vector3 position, ref Vector3 orientation,
                      ref long timestamp);
int API.xslam_start_imu();
int API.xslam_stop_imu();
bool API.xslam_get_imu_array(Vector3[] imu, ref double timestamp);
bool API.xslam_start_rgb_stream();
bool API.xslam_stop_rgb_stream();
bool API.xslam_start_stereo_stream();
bool API.xslam_stop_stereo_stream();
```

These lines describe callable signatures rather than a standalone C# declaration block. Use the source declarations for attributes, marshaling, callback types, and exact ABI layout. `API.Quaternion` is a native interop structure and is distinct from `UnityEngine.Quaternion`.

The configured XR stack owns native initialization. Do not independently initialize or uninitialize it while feature managers are using it. Native image APIs with pointers or arrays require correctly sized buffers. Keep callback delegates alive for the entire registration period, and do not invoke Unity object APIs from a native worker-thread callback.

Calls such as `readRGBCalibration`, `readToFCalibration`, and stereo calibration access require the readiness sequence implemented by `XvSensorCalibrationDump`. Consult the binding for exact names and signatures before direct use. Invalid native calls can terminate the process even when surrounded by C# exception handling.

## 9. Compatibility and validation

### Binding and namespace migration

The separate `XvNativeAPI` class has been removed. Its 19 distinct declarations now reside in the global `API` class under `Additional xv-wrapper bindings`, with their original native library, entry points, and signatures preserved. Replace `XvNativeAPI.Method(...)` with `API.Method(...)` in application code.

| Return type | Migrated declaration |
| --- | --- |
| `bool` | `initXvDevice()` |
| `bool` | `xv_test_get_6dof(double[] poseData, ref long timestamp, double prediction)` |
| `void` | `rgb_set_exposure(int aecMode, int exposureGain, float exposureTimeMs)` |
| `void` | `startRgbStream()` |
| `void` | `stopRgbStream()` |
| `void` | `startTofStream()` |
| `void` | `stopTofStream()` |
| `bool` | `setPmdTofIRFunction()` |
| `int` | `xv_start_skeleton_ex_with_cb()` |
| `int` | `start_et_gaze_callback()` |
| `bool` | `xvReadStereoFisheyesCalibration()` |
| `bool` | `stm_start()` |
| `bool` | `stm_stop()` |
| `void` | `xv_controller_register()` |
| `void` | `xv_recognize_from_local(string name)` |
| `void` | `xv_audio_recognize_switch_source(int source)` |
| `bool` | `xv_get_prob(double[] poseData)` |
| `bool` | `xv_get_tofir_image(IntPtr data, int width, int height)` |
| `void` | `startTofIRStream()` |

These declarations use `DllImport("xv-wrapper")`; `rgb_set_exposure` maps to the native entry point `xv_rgb_set_exposure`. The array parameters retain their `[In, Out]` attributes in source. Array sizes, native status codes, and feature support must follow the supplier contract; this table does not define new buffer layouts or success codes. Similarly named `xslam_*` methods use a different binding contract and are not interchangeable aliases. Keep `libxv-wrapper.so` and the required AARs when using these interfaces.

`XvGazeButton` now belongs to `Singray.UI.Input`. Replace `using Xvisio.Input.UI;` with `using Singray.UI.Input;` for consumers of that component. Its Unity asset GUID is preserved, so existing serialized references are retained.

The AI Talk implementation and sample have been removed; they are no longer supported integration entry points. Separate speech examples remain. The obsolete branding postprocessor and legacy Java demo activities were also removed. This cleanup does not remove the native SDK initialization stack, required Android manifest entries, or supplier libraries.

### Verification scope

The recorded project editor is Unity `2022.3.62f3`. Managed runtime and Editor compilation completed with no errors, with existing warnings. Eight plane-parser checks passed. Device verification of RGBD, ToF Point Cloud, Plane Detection, and Spatial Map remains pending; see the [regression checklist](Demo-and-User-Guide.md#device-regression-checklist).

Replacing an AAR can change native behavior without changing any C# signature. Validate startup, frame delivery, timestamps, sensor settings, pause/resume, and shutdown on the target firmware after a native library update. The documented interfaces were checked against source; this reference is not a claim that device regression tests have passed for the current binary.
