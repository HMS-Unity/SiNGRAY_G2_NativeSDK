# Singray G2 simulation backend

The simulation backend lets application and validation UI development continue without a headset. It implements the same product-facing operations as the native backend and is selected automatically in the Unity Editor.

```text
SingrayG2Manager
  -> SimulationBackend   (Editor / non-Android Auto mode)
  -> NativeBackend       (Android Auto mode)
```

The validation page always displays `SIMULATION` when synthetic data is active. Simulation logs use `[SINGRAY-G2][SIMULATION]`.

## Limits

Simulation does not validate:

- supplier AAR/SO loading or native entry points;
- RGB/ToF calibration and distortion accuracy;
- real sensor timestamps, frame rate, latency, exposure, or depth range;
- IMU units, axes, bias, or drift on G2 hardware;
- firmware-specific meanings of ambient-light state values;
- physical display or eye-LED brightness.

Use simulation to verify UI, subscriptions, stream start/stop behavior, data flow, timeouts, state transitions, and application error handling. Repeat all sensor acceptance checks with a real Singray G2 before merging into the release branch.
