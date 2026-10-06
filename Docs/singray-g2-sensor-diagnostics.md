# Singray G2 sensor diagnostics

Singray G2 runtime diagnostics use this product-owned prefix:

```text
[SINGRAY-G2][CATEGORY]
```

Diagnostics are enabled by `SingrayG2Manager.SetDiagnosticsEnabled(true)`. The legacy runtime log switch must also remain enabled while validating the vendor SDK integration.

## Capture logs on a device

Clear old messages, launch the application, and capture only Singray G2 diagnostics:

```powershell
adb logcat -c
adb logcat -v threadtime | findstr /C:"[SINGRAY-G2]"
```

Save a complete log while reproducing a problem:

```powershell
adb logcat -c
adb logcat -v threadtime > singray-g2-sensor-test.log
```

## Categories

| Category | Expected signal |
| --- | --- |
| `STARTUP` | Singray G2 entry point and SDK configuration |
| `RENDER` | Color/depth frame submission and pose matching |
| `6DOF` | Current and predicted head pose validity/timestamps |
| `HAND` | Skeleton tracking state and gesture codes |
| `RGB` | Native stream result, frame size/pose, and frame timeout |
| `TOF` | Depth/IR/point-cloud configuration, frames, and timeout |
| `RGBD` | RGB + ToF startup, timestamp, and shutdown |
| `IMU` | Start result and acceleration/gyro/magnetometer samples |
| `LIGHT` | Ambient light sensor events (`type == 6`) |
| `EYE_LED` | Eye-tracking infrared LED brightness commands |
| `DISPLAY` | Display brightness commands |
| `PERF` | Runtime frame rate |
| `SIMULATION` | Synthetic backend configuration, streams, and lifecycle |

Start with one sensor feature at a time. A start message without a frame/sample message, followed by a timeout warning, distinguishes a stream that was requested from one that is actually producing data.

## No-device validation boundary

An Editor or Android build can verify C# compilation, native API declarations, resource packaging, and that non-native Editor paths do not block. Camera calibration, timestamps, frame rate, exposure, ToF depth accuracy, IMU values, and physical light/LED behavior require a Singray G2 headset.
