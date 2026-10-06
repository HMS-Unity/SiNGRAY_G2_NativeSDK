# Singray G2 validation scene

Open this scene in Unity:

```text
Assets/XRFoundation/SampleScenes/SingrayG2Validation/Scenes/SingrayG2Validation.unity
```

The scene is intentionally separate from the supplier samples. Its runtime assembly references `Singray.G2.Runtime` and contains no direct `XvXR`, `API.*`, or `xslam_*` calls.

## Available checks

- RGB resolution selection, start/stop, image preview, timestamp, pose, intrinsics, and average frame rate
- ToF resolution/frame-rate selection, Depth/IR start/stop, preview, and point-cloud count
- IMU start/stop and acceleration/gyro/magnetic-field values
- Device-event start/stop and ambient-light state
- Display brightness levels 1, 6, and 9
- On-screen validation log and SDK-ready indicator

In the Unity Editor, `Auto` mode selects the simulation backend and the page displays `MODE: SIMULATION | SYNTHETIC DATA`. Sensor controls remain active and produce synthetic RGB/ToF frames, point cloud, IMU samples, and ambient-light events without invoking native sensor functions.

Default synthetic data:

- RGB color-bar preview at 30 FPS with reported product resolution and moving pose
- ToF Depth color gradient and grayscale IR preview
- 160 x 120 point cloud (19,200 points) with a wave surface and foreground obstacle
- 100 Hz nominal IMU values with gravity, angular velocity, and magnetic field
- ambient-light states 0, 1, and 2 cycling every 1.5 seconds

On Android, `Auto` mode selects the native device backend; the validation APK does not silently replace missing hardware with simulated success.

## Regenerate or build

Unity menu:

```text
Singray G2 > Regenerate Validation Scene
```

The command-line Android build entry is:

```text
Singray.G2.Validation.Editor.SingrayG2ValidationSceneBuilder.BuildAndroid
```

The scene is present in Build Settings but disabled so it does not change the normal application startup scene.

## Automated no-device check

`SingrayG2ValidationPlayModeTests` creates the validation UI in Play Mode, verifies that Editor `Auto` mode selects simulation, and starts RGB, ToF, RGBD, point-cloud, IMU, event-stream, and brightness product APIs. It requires a preview texture, a 19,200-point cloud, and a valid IMU sample, and fails on unexpected runtime exceptions.

## First G2 device run: native P0 checks

Clear old logs before launching the APK:

```powershell
adb logcat -c
```

Capture the Unity and native-loader diagnostics:

```powershell
adb logcat -v time Unity:I AndroidRuntime:E libc:E *:S | findstr /C:"[SINGRAY-G2]" /C:"EntryPointNotFoundException" /C:"UnsatisfiedLinkError" /C:"dlopen failed"
```

Check these results before continuing with sensor accuracy tests:

- `[SINGRAY-G2][HAND] required library libhandskeleton_wrapper.so loaded successfully` means the required gesture wrapper loaded.
- `libhandskeleton_all.so` or `libhandskeleton.so` may be reported as an optional implementation. Preserve the complete loader error if either fails; it identifies the exact missing OpenCV/system SONAME.
- A failed required gesture wrapper disables gesture startup instead of repeatedly throwing native-load exceptions.
- `[SINGRAY-G2][RENDER] ... updateCalibra ... skipped` means the supplier render library still lacks the old IPD calibration entry. Managed display distance continues to update, but native calibration must be confirmed with the supplier.
- No `EntryPointNotFoundException`, `UnsatisfiedLinkError`, native crash, or repeated gesture start loop should appear.

For the first run, test in this order: application startup and rendering, 6DoF pose, RGB start/stop and exposure, ToF Depth/IR, IMU, ambient-light event, then gesture startup. Stop at the first native loader/crash error and save the full logcat from application launch.
