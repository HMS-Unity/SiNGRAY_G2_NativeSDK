# Singray G2 Unity integration

New application code should use the product namespace:

```csharp
using Singray.G2;
```

The primary entry point is `SingrayG2Manager.Instance`. Vendor types under `XvXR`, raw `API.xslam_*` calls, Android AARs, and native libraries remain an internal compatibility layer because their names are part of the supplied SDK ABI.

## Runtime modes

`SingrayG2RuntimeMode.Auto` is the default:

| Player | Auto backend |
| --- | --- |
| Unity Editor / non-Android | `Simulation` |
| Android player | `NativeDevice` |

The active backend is visible through `SingrayG2Manager.ActiveRuntimeMode` and `IsSimulation`. A mode can also be selected explicitly:

```csharp
SingrayG2Manager.Instance.SetRuntimeMode(SingrayG2RuntimeMode.Simulation);
```

Simulation data is for UI, state-machine, and error-handling validation only. It is never evidence of camera calibration, sensor accuracy, native ABI compatibility, or physical device behavior.

## Camera and ToF

```csharp
SingrayG2Manager g2 = SingrayG2Manager.Instance;

g2.ConfigureRgb(new SingrayG2RgbSettings
{
    resolution = SingrayG2RgbResolution.R1280x720,
    framesPerSecond = 30
});

g2.CameraFrameArrived += (stream, frame) =>
{
    // frame.Texture, frame.Timestamp, frame.Position, frame.Intrinsics...
};

g2.StartSensor(SingrayG2SensorStream.Rgb);
g2.StartSensor(SingrayG2SensorStream.TofDepth);
```

Point cloud has a separate lifecycle:

```csharp
g2.StartTofPointCloud();
if (g2.TryGetTofPointCloud(out Vector3[] points))
{
    // Consume the latest point cloud.
}
g2.StopTofPointCloud();
```

## IMU

```csharp
int startResult = g2.StartImu();
if (g2.TryGetImuSample(out SingrayG2ImuSample sample))
{
    // sample.Acceleration, sample.AngularVelocity, sample.MagneticField
}
g2.StopImu();
```

## Ambient light and display brightness

```csharp
g2.AmbientLightChanged += (state, timestamp) =>
{
    // Device light event. Interpret state using the G2 firmware specification.
};

g2.StartDeviceEvents();
g2.SetDisplayBrightness(6);
```

Native device events are queued and delivered from `SingrayG2Manager.Update`, so subscribers run on Unity's main thread.

## Migration rule

- New product code uses `Singray.G2` only.
- Existing scenes and prefabs can continue using `XvXR` components during validation.
- Do not rename `xslam_*`, `xv_*`, library filenames, Java classes, or package symbols without a matching vendor binary change.
- Migrate samples and customer-facing APIs incrementally; remove legacy wrappers only after a full device regression pass.
