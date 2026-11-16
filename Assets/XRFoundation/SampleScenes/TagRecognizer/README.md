# AprilTag recognition with left-hand recording

This is the original SiNGRAY SDK `TagRecognizer` sample for G2. It displays the
native tag pose and reuses the existing MRTK palm menu to record the mixed-reality
view. The standalone scene's menu is restricted to the **left hand**. Its original
recognition panel, targets, transforms and event bindings are retained.

## Run and build

1. Follow the SDK [integration guide](../../../../Docs/Delivery/Integration-Guide.md),
   including Git LFS dependencies. Use Unity **2022.3.62f3**, the bundled MRTK
   **2.8.3**, and Android Build Support with SDK/NDK/OpenJDK.
2. Open [`Scenes/TagRecognizer.unity`](Scenes/TagRecognizer.unity). The scene
   already supplies the SDK XR host, recognition targets, camera services,
   MR compositor and recording backend. Do not add a second XR rig or recorder.
3. Build Android **ARM64 / IL2CPP** using
   **Singray > Toolkit > Build Scenes > TagRecognizer**. The menu builds this
   scene and writes `build/TagRecognizer_<timestamp>.apk`. It uses the project's
   current application identifier, label, version and signing settings; inspect
   those settings before building. The ordinary Build Settings scene list may
   select another sample.
4. Install on G2 and open through its supported launcher workflow.
5. For RGB testing, press **RGB_Apriltag**, select the printed tag's physical
   side length, and show a **36h11** tag to the RGB camera. Size values are in
   meters internally; for example, **10cm** means **0.10 m**.
6. Raise the left palm to show the existing menu. Press **Start Recording**, then
   **Stop Recording**, and wait for the saved-path message before leaving.

The original saved defaults remain **FishEye_Apriltag / 0.30 m**. Select
**RGB_Apriltag** explicitly for the RGB path. This sample also retains the
original fisheye and QR controls and the existing camera/screenshot/RTSP buttons.

## Recognition pipeline

```text
Original UI: RGB_Apriltag + physical size
  -> XvTagRecognizerManager.StartTagDetector(RGB_Apriltag)
  -> wait for native SDK readiness; acquire shared RGB camera ownership
  -> XvAprilTag.StartRgbDetector("36h11", size, callback)
  -> native xslam_getTagDetectionrgbImage
  -> copy native detections into a bounded managed result buffer
  -> Unity Update dispatches OnDetectedAprilTagEvent
  -> XvTagRecognizerBehavior matches ID and checks confidence/finite pose
  -> apply translation, quaternion and physical-size scale
  -> existing Found/Lost UnityEvents update target visibility/content
```

RGB image processing and pose estimation are supplied by the vendor native SDK.
This sample does not run a separate OpenCV detector, solvePnP, image mirroring
stage or camera-to-world calibration in application code. The native callback
copies result data; Unity objects are updated on the Unity main thread.

The manager shares compatible native detector sessions and holds a camera lease
for RGB recognition. Start/stop, mode/size changes and teardown are managed through
the existing SDK lifecycle. Render-frame dispatch can reuse the latest detection;
it must not be treated as an independent sensor observation on every frame.
Managed results older than two seconds are treated as stale.

The original scene contains four configured tag targets. Each
`XvTagRecognizerBehavior` selects its matching ID, applies
`transform.SetPositionAndRotation(translation, quaternion)`, and sets its uniform
root scale to `XvTagRecognizerManager.Size`. A unit cube under that root therefore
has the corresponding physical side length, subject to its existing child and
parent scales. Size configures the **physical marker used by pose estimation**;
it is not an arbitrary visual zoom control.

The sample uses the native translation/quaternion directly in the configured XR
scene. It adds no half-edge offset, axis flip, pose smoothing or corrective
rotation. The exact marker origin and world alignment must be checked on G2;
this sample does not claim to fix the reported corner/center discrepancy. Inspect
the existing child transforms when comparing a visible mesh center with the
native target origin.

The per-target **Stop/Start** controls freeze/resume application of that target's
pose. **StopTagDetector** stops recognition. This original sample does not create
a new cube for every observation or implement snapshot history/clear controls.

## Left-hand recording pipeline

```text
MRTK HandConstraintPalmUp + SolverHandler (Left / HandJoint / Palm)
  -> existing VideoRecording Interactable / UnityEvent
  -> XvMediaRecorder.VideoRecording
  -> XvMediaRecorderManager: acquire capture; wait for a real frame
  -> ReplayCam: read the existing MR compositor RenderTexture
  -> GPU readback -> bounded frame buffers -> encoder worker
  -> NatCorder MP4 encoder (+ configured audio input)
  -> stop/drain/finalize -> export MP4 -> saved-path callback
```

The scene already had the menu and complete recording backend. The scene change
is only `SolverHandler.trackedHandedness: Both (3) -> Left (1)` on the active menu.
The inactive legacy menu is retained. No duplicate recording object is created.
The shared `MrtkClickGate` is installed on menu buttons to guard a physical press
cycle; physical interaction still requires hardware verification.

`XvMRVideoCaptureManager` combines the camera image and virtual content in **MR**
mode. `ReplayCam` reads that existing output rather than rendering the scene a
second time. It uses async GPU readback when supported, with a synchronous
fallback, and a worker that commits the latest available frame to the native
encoder. Actual frame throughput depends on the device; a target frame rate is
not a measured output guarantee.

The recorder remains busy while stopping: audio callbacks stop, the worker exits,
GPU readbacks drain, and the encoder finalizes before resources are released.
Concurrent stop requests await the same finalization, and another start is gated
until completion. Android audio callbacks may attach Unity's borrowed audio
thread to JNI; they must not detach that engine-owned thread per audio buffer.
Existing public recording APIs and serialized asset GUIDs are preserved.

Completed Android MP4s are exported to **`/sdcard/DCIM/guest`** with a
millisecond/GUID filename. The original app-local encoder file is retained. If
shared-folder export fails, the callback returns that original nonempty file and
logs a warning. `StartCapture` / recording / screenshot requests use separate
capture ownership; releasing one owner should not close another owner's stream.
See the [recording manager](../../Core/Foundation/MediaRecorder/XvMediaRecorderManager.cs)
for the shared capture lifecycle.

To retrieve a completed recording, use the connected device's actual ADB serial
(wireless ports may change):

```sh
adb devices -l
adb -s DEVICE_SERIAL shell ls -lt /sdcard/DCIM/guest
adb -s DEVICE_SERIAL pull /sdcard/DCIM/guest/FILE.mp4 ./
```

## Source map and dependencies

| Purpose | Source, relative to `Assets/XRFoundation` |
| --- | --- |
| Original scene and configured sample | `SampleScenes/TagRecognizer/Scenes`, `Prefabs`, `Scripts` |
| UI mode and physical size | `SampleScenes/TagRecognizer/Scripts/XvTagRecognizerUIController.cs` |
| Native detector session and main-thread dispatch | `Core/Foundation/TagRecognizer/Scripts/XvTagRecognizerManager.cs` |
| Native wrapper and result conversion | `Core/Foundation/TagRecognizer/Scripts/Base/XvAprilTag.cs` |
| Tag filtering, pose, scale and Found/Lost events | `Core/Foundation/TagRecognizer/Scripts/XvTagRecognizerBehavior.cs` |
| Existing hand-menu actions | `Core/Foundation/MediaRecorder/XvMediaRecorder.cs` |
| Recording ownership and first-frame readiness | `Core/Foundation/MediaRecorder/XvMediaRecorderManager.cs` |
| Frame capture, encoding and export | `Core/Foundation/MediaRecorder/NatCorder/Examples/ReplayCam/ReplayCam.cs` |
| MR composition | `Core/Foundation/MRVideoCapture/Scripts/XvMRVideoCaptureManager.cs` |
| Audio-thread recorder bridge | `Core/Foundation/MediaRecorder/NatCorder/Plugins/Managed/Inputs/AudioInput.cs` |

This is a sample within the SDK repository, not a standalone Unity package.
Keep the existing SDK native XR/Xslam AARs, XR Resources/profiles, MRTK assets,
camera/compositor dependencies, NatCorder/NatRender plugins and their metadata.
This publication does not replace native libraries or modify Hub/build settings.

## Validation and remaining checks

On 2026-11-16, the saved scene was reopened and checked: **Left/Palm**, existing
recording button/backend, four targets, and the original 535 objects. The scene
diff is one hand-selection value; original layouts and recognition defaults are
preserved. A local original-scene ARM64/IL2CPP APK built with **0 errors**;
signature and required native dependencies were checked. Its diagnostic app
identity was `G2 AprilTag Original / com.singray.tagrecognizer.recording / 1.0 (1)`;
those temporary identity settings were restored after building.

A subsequently retrieved G2 MP4 parsed as **60.585 seconds, H.264 1280x720,
AAC 48 kHz stereo**, with identical device/local SHA256. Shared-folder metadata
does not establish the recording's originating application, visual alignment,
playback quality or audible microphone content.

Remaining G2 checks: RGB tag ID/pose and actual size, marker origin versus mesh
center, left-palm/button behavior, repeated start/stop and pause/reentry, playable
MR video and expected audio. Existing warnings include disabled measurement
objects with missing scripts, legacy prefab/TextMeshPro warnings and vendor
native 16 KB alignment warnings for Android 15+. Build success does not establish
hardware acceptance or Android 15+ compatibility. APKs, recordings and local
diagnostic logs are not included in source publication.
