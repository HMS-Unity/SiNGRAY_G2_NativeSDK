# SiNGRAY G2 Native SDK: API Reference

This reference describes the application-facing C# API in the `dev/4.2` source tree. It covers the complete public surface of `SingrayG2Manager`, its data types, and the main Foundation feature interfaces used by the demos. Low-level native declarations remain defined by the checked-in bindings; this document does not assert support for every native feature on every device.

For installation, scene selection, builds, and demos, see the [Demo and User Guide](Demo-and-User-Guide.md).

For device keys and MRTK buttons, see [Keys and MRTK Buttons](Keys-and-Buttons.md) and the [Key Input API Guide](../Assets/XRFoundation/Core/Input/README.md). The input component is `Singray.InputSystem.SingrayKeyInput`; MRTK physical button protection is installed with `Singray.UI.MrtkClickGate.Install(Interactable)`.

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

For indexed fisheye readers, construct `XvStereoCamera` with an `XvStereoCameraParameter` and a frame callback. `StereoCameraIndex.LeftEye = 0` and `RightEye = 1` retain their values; `Camera2 = 2` and `Camera3 = 3` address the additional physical channels. Call `StartCapture()`, tick `Update()` on Unity's main thread, and call `StopCapture()` when leaving or disabling the owner. The active index is captured at start; changed settings take effect on the next start. Channels 2/3 check the connected device's calibration count before indexed native reads and remain unavailable on two-camera devices. Do not infer physical mounting directions from indices 2/3.

All `XvStereoCamera` readers share the native stream: the first reader starts it and the final reader stops it. Stop releases that reader's pinned buffer and texture. Returned `cameraData.tex` is borrowed and may be replaced or destroyed on resolution change/stop; do not retain it as a permanent image. `CameraDemo` demonstrates channels 2/3 with separate Start/Stop controls, while existing `XvCameraManager` left/right events remain unchanged.

Point-cloud methods are `StartTofPointCloud()`, `GetPointCloudData(out Vector3[] data)`, and `StopTofPointCloud()`. ToF exposure overloads are `SetTofExposure(int exposureTimeMs)` and `SetTofExposure(int libmode, int resulution, int fps, float exposureTimeMs)`; preserve the supplier-specific parameter semantics.

`XvRgbdManager` provides `StartRgbPose()`, `StopRgbPose()`, `GetRgbPixel3DPose(Vector2 rgbPoint, ref Vector3 spacePoint)`, and `GetRgbPixelPoseList(Vector2[] rgbPoint, ref pointer_3dpose[] spacePose)`.

RGBD and point-cloud startup wait asynchronously for native SDK readiness, with a ten-second timeout. Start is a request, not a guarantee of available data. Stop cancels pending startup; RGBD also stops when its component is disabled. These Foundation native paths are guarded for Android players and do not provide Editor simulation.

RGBD queries return false until running with a real RGB texture and a positive, finite timestamp. Duplicate/backward timestamps are ignored. Pixel bounds use the incoming texture's actual width/height, not the requested resolution metadata. Single-query failure leaves the caller's `spacePoint` unchanged; successful output retains the existing conversion to Unity coordinates (native Y negated). Batch queries require non-null arrays, finite in-bounds input pixels, and an output array at least as long as the input array. Only the input-count portion is processed; unused output-tail entries remain untouched. Check each entry's `isValid`; invalid/non-finite results are not world positions.

RGBD managers share one native pixel-pose session and acquire internal RGB/depth capture ownership. The final RGBD manager stops that session; the streams stay active while another `XvCameraManager` owner still needs them. Public camera Start/Stop remains idempotent per manager instance, so callers sharing one manager must coordinate their own usage. Direct native start/stop calls bypass this ownership. Event textures are borrowed and may be replaced or destroyed by the camera backend on resolution change/stop; consumers must neither destroy nor retain them as immutable snapshots.

`RgbdDemo` is the customer-facing sample component, derived from the retained `XvRgbdDemo` compatibility type. Its public `image`, `rawImage`, `RgbdManager`, and `CameraManager` members remain available, and the original serialized script GUID resolves to `RgbdDemo`. The sample handles preview UV/material orientation, actual frame dimensions, a two-second freshness check, and invalid world-point hiding; callers using `XvRgbdManager` directly must implement any required freshness policy themselves.

### Spatial understanding

| Manager | Main interface | Result handling |
| --- | --- | --- |
| `XvPlaneManager` | `StartPlaneDetction()`, `StopPlaneDetection()` | Instance `planesChanged` event with `plane[]` |
| `XvSpatialMeshManager` | `StartMeshDetection()`, `StopMeshDetection()` | Static `meshChanged` event with `NowXslamSurface` |
| `XvSpatialMapManager` | `StartSlamMap()`, `SaveSlamMap()`, `LoadSlamMap(string mapPath)`, `GetFeaturePoint()` | Save returns a path; static save/load/matching events report progress or completion |
| `XvTagRecognizerManager` | `StartTagDetector(RecognizerMode recognizerMode)`, `StopTagDetector()`, `SetDetectStatus(bool isDetect)` | Configure and consume results as shown in the TagRecognizer sample |

`XvSpatialMeshManager` keeps its existing API/event signatures. Startup waits for SDK readiness without blocking the main thread (ten-second timeout); Stop and disable cancel pending startup. Manager instances share native reconstruction registration and release only their own scanning ownership. The final owner disables reconstruction. The callback registration ID is logged as a raw native value; its success/failure enum is not inferred. Native initialization is assumed to remain alive for the application session; restarting the native SDK independently requires supplier verification.

`meshChanged` remains a static callback-thread event. Payload lists are managed copies of the borrowed native buffers; native pointers are never destroyed or retained. Consumers touching Unity objects must dispatch to the Unity main thread themselves. Remove listeners during teardown. `XvSpatialMeshVisualizer` performs its own main-thread dispatch/coalescing, keeps `MeshDic`, `autoDetection`, `AutoDetection`, `EnableCollider`, `EnableRender`, `SetCollider(bool)` and `SetVisualizer(bool)`, and preserves the original Y inversion and triangle winding. Generated Mesh objects are owned by the visualizer; do not destroy them externally.

`XvTagRecognizerManager` retains `XvCameraManager`, `TagGroupName`, `Size`, `Confidence`, `RecognizerMode`, `OnDetectedAprilTagEvent`, `StartTagDetector(RecognizerMode)`, `StopTagDetector()` and `SetDetectStatus(bool)`. `RecognizerMode` keeps numeric values None=0, RGB_QRCode=1, RGB_Apriltag=2 and FishEye_Apriltag=3; `CameraType` remains Rgb=0/FishEye=1. `XvAprilTag` retains its public RGB/fisheye start/read/stop methods and `TagArrayCallback`; `TagDetection` fields remain compatible. Raw native count handling is bounded to the declared 64-tag ABI array; missing arrays, nonfinite pose data and invalid callback counts are rejected. Native pointers are borrowed and never freed by managed consumers.

Tag results are dispatched on the main thread once per render frame while active. The latest copied result may be reused between native updates for up to two seconds; this is not a unique-frame guarantee. Empty/expired results and explicit stop report a null result. Found/Lost and per-target pose freeze retain their existing event/control semantics. Use a valid physical `Size` in meters. A changed size/family restarts a single-owner session; shared sessions reject conflicting configuration changes. Stop releases only the caller's camera occupancy, and final-owner stop closes the native detector. Fisheye cleanup may remain pending if its native call does not return; new recognition requests must wait. Direct `XvAprilTag` or native calls bypass manager ownership.

The RGB callback ABI supplies no session token. Cancelled in-flight callbacks cannot publish to their captured session, but an old callback first delivered after a later same-mode start cannot be conclusively identified in managed code. Native stop/callback-drain guarantees require supplier/device verification. No native libraries are changed by this cleanup. The required Resources paths `XvTagRecognizerManager` and `XvTagRecognizerBehavior` remain intact.

Spatial-map events are `onMapSaveCompleteEvent` (`UnityEvent<int, int>`), `onMapLoadCompleteEvent` (`UnityEvent<int>`), and `onMapMatchingEvent` (`UnityEvent<float>`). Their native codes are not translated into a product-level status enum.

SpatialMap retains `StartSlamMap()`, `SaveSlamMap()`, `LoadSlamMap(string mapPath)`, `ShowFeaturePoint`, `SwitchFeaturePointState()` and `GetFeaturePoint()`, along with existing timestamp helpers and event signatures. The sample's Create Map action resets SLAM to a new scanning origin; it does not produce a saved map file. Save is a separate request. Accepted save/load requests share an operation gate across managers; duplicate requests and reset during an outstanding save/load are rejected. `SaveSlamMap()` returns null if busy, unready, or rejected by the native bool result. Loading checks for a nonempty existing file, then waits for reset/fresh-pose recovery before submitting the native load; a rejected load is not reported as completed. Existing native callbacks retain their raw status/quality values, whose success enums still require supplier confirmation.

Callbacks are dispatched on the Unity main thread. In-flight operation completion survives disabling its manager; disabling one scene does not discard the shared callback queue. Localization similarity updates coalesce to the latest value between updates, so they do not accumulate indefinitely while no map scene is active. A native completion timeout reports that the operation is still pending and retains its gate; it does not cancel an unsafe native operation or authorize overlapping requests. These protections do not prove that the known supplier pose-lock stall during `xslam_reset_slam` is resolved.

`StartPlaneDetction()` uses a cancellable readiness coroutine with a ten-second timeout instead of blocking the Unity main thread. `StopPlaneDetection()` and component disable cancel pending startup. Plane payload parsing validates declared lengths and counts before reading or allocating; rejected data is not a valid plane result. These checks cannot recover from a crash inside the native implementation.

Spatial-map operations require an Android player and a ready SDK. `SaveSlamMap()` returns **null** when unavailable; otherwise it returns the requested output path before asynchronous completion. Wait for the save event and verify the file before loading or sharing it. `StartSlamMap()` resets SLAM; it does not itself enable feature-point visualization.

Use `SwitchFeaturePointState()` to toggle feature-point acquisition and inspect the read-only `ShowFeaturePoint` property. `GetFeaturePoint()` returns null when unavailable, disabled, or given an invalid native pointer or count. Returned positions apply the manager's Y-axis sign inversion. Native map buffers remain SDK-owned and must not be freed or destroyed by callers.

Map callbacks are queued and their UnityEvents are invoked from the manager's `Update()` on the main thread. Keep the manager active while awaiting results. Disabling it stops an active feature-point stream and clears queued events; it does not establish cancellation of a native save/load operation. Delegates are retained in static fields for native callback lifetime. Use a single active map manager and remove your event listeners during teardown.

### Gestures

Static-gesture compatibility is retained: `XvStaticGestureManager.GetCurrentStaticGesture(HandType)`, `GetKeyDown(StaticGestureStatus gestureStatus, HandType handType)`, `GetKeyUp(StaticGestureStatus buttonKey, HandType handType)` and `GetKey(StaticGestureStatus buttonKey, HandType handType)` keep their signatures/named arguments. Public `GestureState` and `GestureKeyState` constructors, query/update methods and `buttonKeyDic` remain available. `StaticGestureStatus` preserves UNKONOW=-1 and GRAB=0 through Pinch=9; `HandType` keeps None=0, Left=1, Right=2.

A manager lazily initializes state for safe queries before Start. Its queries share one render-frame snapshot; GetKey is true on the same first frame as GetKeyDown, and GetKeyUp is a one-frame pulse after release/tracking loss. Disabled/paused managers return cleared local state. Direct helper users still explicitly call `GestureState.Update()` / `GestureKeyState.LateUpdate()` and are responsible for their own update cadence. Numeric keyboard simulation remains additive to native input. Invalid/untracked source data cannot become a default GRAB. `GetCurrentStaticGesture` reports native tracking status; keyboard simulation affects key-state queries only. Component teardown resets its state and does not stop the shared native hand tracker.


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

MR compositor APIs, enum values, asset GUIDs and the `Resources/XvMRVideoCaptureManager` path are preserved. `CaptureType` remains `OnlyCamera`, `OnlyUnityScene`, or `MR`; `CameraManager`, `BgCamera`, `CameraRenderTexture` and `IsOn` remain available. RGB/MR readiness requires a real texture, valid positive intrinsics, and a positive finite timestamp; duplicate/backward timestamps are ignored. Unity-only readiness follows its render loop. The preview/camera are held back before valid RGB, and calibration offsets are recomputed from the original background position on each frame rather than accumulated across restarts.

SDK preview, recording and RTSP consumers have separate internal compositor ownership and share RGB capture leases. A consumer's stop retains resources still needed by another. Public Start/Stop is idempotent for the existing public caller; `StopCapture(false)` preserves the underlying RGB camera through the existing camera-manager owner, which can later call `StopCapture(ARCameraStream)`. Disabled/destroyed compositors unsubscribe, clear borrowed RGB, and release their output when no native output holder remains. RTSP retains the same texture pointer while initialized and releases that retention on disable. Output textures are borrowed and may be destroyed on final release; cached references must be refreshed after restart. The output size follows configured camera dimensions at allocation and remains stable while borrowed.

`StartRecording()` waits up to ten seconds for readiness if needed. `IsVideoRecording()` also reports a pending recording request, allowing the existing toggle/Stop API to cancel preparation; stopping a pending request reports a null path. Screenshot callbacks return null on timeout/cancellation/save failure, and JPG input/writer cleanup completes on disable. The optional `MRVideoCaptureDemo` excludes the UI layer from composite output while active, without changing layout transforms; this sample behavior is not a restriction on the standalone SDK compositor's configured mask. Device encoding, audio, native plugin pointer timing and physical alignment remain hardware validation items.

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

The separate `XvNativeAPI` class has been removed. Its 15 remaining declarations reside in the global `API` class under `Additional xv-wrapper bindings`, with their original native library, entry points, and signatures preserved. Replace `XvNativeAPI.Method(...)` with `API.Method(...)` in application code.

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
| `bool` | `xvReadStereoFisheyesCalibration()` |
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
