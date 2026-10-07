# SiNGRAY G2 Native SDK Release Notes

Development updates for the `dev/4.2` branch, starting October 6, 2026. Dates use Asia/Shanghai time. The version format is `4.2.0.x`, where the final component increments for each documented development update. These entries describe source snapshots, not certified device releases or published APKs. The October 6 baseline is assigned `4.2.0.1` retrospectively; its original files used earlier version labels.

## 4.2.0.2 - October 7, 2026

### Environment and versioning

- Updated the recorded Unity Editor version to `2022.3.62f3` and package registry references to `packages.unity.com`. This branch uses Unity 2022, not Unity 6.
- Aligned `VersionInfo.version` and the Android application version with `4.2.0.2`; incremented the Android version code to `2`.
- Retained Android IL2CPP, ARM64, minimum API 30, and target API 35 settings.

### SDK Samples corrections

- **RGBD:** corrected the hub module's manager reference, added a fallback world-space preview when no `RawImage` is assigned, and paired frame-event registration with cleanup. Added cancellable SDK-readiness waiting and checks for valid timestamps and query buffers.
- **ToF Point Cloud:** enabled drawing when the sample starts instead of requiring keyboard input. Added cancellation of pending startup and cleanup of the point-cloud lifecycle.
- **Plane Detection:** replaced blocking readiness polling with a cancellable coroutine and timeout. Added payload length/count validation and guaranteed release of pinned managed buffers.
- **Spatial Map:** added native readiness checks, queued map events for Unity main-thread delivery, retained callback delegates, and validated feature-point pointers/counts. Removed destruction of SDK-owned map memory and added feature-point cleanup on disable.

These changes address issues identified in source. The reported blank RGBD preview, missing ToF points, and Plane Detection/Spatial Map crashes still require confirmation on G2 hardware.

### Integration cleanup

- Moved all 19 distinct `XvNativeAPI` declarations into the global `API` class while retaining their `xv-wrapper` ABI declarations. Removed the redundant source file.
- Changed the `XvGazeButton` namespace from `Xvisio.Input.UI` to `Singray.UI.Input`, preserving its Unity asset GUID.
- Removed the obsolete Android branding postprocessor and legacy Java demo activities.
- Removed the AI Talk implementation, sample, and associated build menu entry. Separate speech examples remain.
- Preserved required native libraries, SDK initialization, and Android manifest integration.

### Documentation

- Updated the README, Demo and User Guide, and API Reference in English.
- Documented migrated bindings, lifecycle requirements, map callback handling, and the four-feature device regression checklist.
- Added an actual Android Build Settings screenshot. It illustrates configuration and does not represent live device output.
- Removed the obsolete root-level vendor merge checklist and introduced these release notes.

### Validation and remaining work

- Runtime and Editor managed compilation completed with no errors; existing warnings remain.
- Eight plane-parser checks passed.
- APK build, G2 frame delivery, native crash regression, pause/resume, and repeated sample entry/exit remain unverified for this update.
- No device crash logs were available. Record device model, firmware, APK commit, and logcat when running the [device regression checklist](Docs/Demo-and-User-Guide.md#device-regression-checklist).

## 4.2.0.1 - October 6, 2026

### Development baseline

- Established the `dev/4.2` development snapshot in `HMS-Unity/SiNGRAY_G2_NativeSDK` without importing the original repository's historical commits.
- Updated `xvsdk-release.aar` to the supplied native SDK binary. Its SHA-256 is `a9d322d8aab91ccdba7695ecad87597a15c92af8783aeb9a941b6f034ef3c0fc`. This identifies the binary; it is not a vendor semantic version or a verified build date.
- Removed the previous custom logo and waiting-screen image assets.
- Replaced internal documentation under `Docs` with the English Demo and User Guide and API Reference, and refreshed the README.

### Validation scope

This entry records the repository and native-library baseline. It does not establish successful APK execution or device feature validation. Native behavior must be checked against the target G2 firmware.

## Upgrade notes

- Open the project with Unity `2022.3.62f3`, install Android Build Support, and fetch Git LFS content before import.
- Replace application calls to `XvNativeAPI` with the corresponding `API` methods. Replace `using Xvisio.Input.UI;` with `using Singray.UI.Input;` for `XvGazeButton` consumers.
- Stop owned streams and remove event listeners during teardown. Await map completion events before consuming saved files.
- Use [the Demo and User Guide](Docs/Demo-and-User-Guide.md) for scene selection and device testing, and [the API Reference](Docs/API-Reference.md) for method contracts.
