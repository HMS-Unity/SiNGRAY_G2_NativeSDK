# AprilTag Placement Demo

Independent G2 sample using MRTK 2.8.3. Existing examples, Hub and build-scene settings are unchanged.

This is the scene for **`G2_AprilTagPlacement_RGB_Recording_OffsetPNN.apk`**:
**G2 AprilTag Demo**, package **`com.singray.apriltagplacement`**, version
**1.0 / code 6**. Its source branch is
**`codex/apriltag-placement-rgb-recording`** in
[`HMS-Unity/SiNGRAY_G2_NativeSDK`](https://github.com/HMS-Unity/SiNGRAY_G2_NativeSDK).
The separate original `TagRecognizer` scene is a different sample.

Open `Scenes/AprilTagPlacement.unity`. In an existing initialized SDK scene, use
`Prefabs/AprilTagPlacementDemo.prefab` instead; do not add a second XR host.
Use Unity **2022.3.62f3** and retrieve the repository's Git LFS dependencies
before importing. Follow the SDK [integration guide](../../../../Docs/Delivery/Integration-Guide.md)
for Android setup and the supported G2 launcher workflow.

## Technical route

```text
RGB camera + vendor native AprilTag detector (36h11, measured tag size)
  -> XvAprilTag: copy ID, translation, quaternion and confidence
  -> XvTagRecognizerManager: shared session / Unity main-thread event
  -> AprilTagPlacementDemo: validate pose, choose ID, reject cached delivery
     -> Live: update one cube from the same tag
     -> Single: save one cube + shot number, stop detector
  -> AprilTagCubeView: raw-pose root + 1:1 mesh + local half-edge offset
  -> independent camera-facing label: Tag / Shot / P / R

MRTK official sample cube -> ObjectManipulator -> one/two-hand move/scale
Left-palm Record -> SDK MR compositor -> GPU readback / encoder worker
  -> NatCorder MP4 + configured audio -> finish/drain -> exported file
```

Detection and pose estimation use the SDK's **native RGB AprilTag path**,
`XvAprilTag.StartRgbDetector` / `xslam_getTagDetectionrgbImage`. No separate
OpenCV/solvePnP implementation or head-following target is introduced. The native
callback copies results; scene objects are updated on Unity's main thread.
Tracking follows the tag's delivered pose. Single captures retain their copied
poses after recognition stops, so later detections do not move earlier cubes.

`AprilTagPlacementDemo.cs` owns Live/Single/history and size controls;
`AprilTagCubeView.cs` owns mesh geometry and pose labels;
`AprilTagPalmRecording.cs` owns the recording-only palm menu and capture requests;
`HandCubeLabel.cs` keeps the manipulation label above the hand cube.
`Editor/AprilTagPlacementBuilder.cs` creates only this sample and builds its
single-scene APK. `Editor/AprilTagPlacementValidation.cs` contains optional
Editor/injected checks. Required shared SDK code remains in `Core/Foundation`:
TagRecognizer, camera/MR capture, MediaRecorder and the MRTK press-cycle guard.

## Controls

- **Grab & Scale**: move the blue hand cube with one hand; scale with two hands.
  Release to leave it in place. **Reset Cube** restores its initial 16 cm edges.
- **Live**: **Start** locks the first valid target ID and follows that tag.
  **Lost** keeps the last pose; the same ID can reacquire. **Stop** freezes it.
- **Single**: **Start** saves one valid pose and automatically stops recognition.
  Each success adds a cube labelled **Shot 1**, **Shot 2**, etc. Old cubes remain.
- **Clear** stops recognition, removes all tag cubes and resets the shot count.
  The hand cube is retained.
- **Tag** buttons set physical marker size in centimetres. Choose the measured
  marker edge required by the detector, not the paper edge or a guessed print size.
  Physical size and ID controls are locked while recognition is running.
- **Any** selects the lowest valid ID in the first batch. ID buttons select 0–3;
  the sample's `targetId` field / `SetTargetId` also supports other nonnegative IDs.
- **Cube: 1:1** means the rendered cube edge equals that result's physical tag
  edge in metres. **- / +** change display scale only (0.25x–4x); **1:1** restores
  true dimensions. History retains each capture's own physical size.

## Left-palm recording

Raise the left palm and press **Record**. **Cancel / Waiting** means the recorder
is waiting for a real RGB frame (up to 10 seconds). **Stop / Recording** and the
main panel's **REC** indicator mean recording is active. Lowering the hand hides
the button and leaves recording active. Raise it again and press **Stop**;
wait for **Saving...** to become **Saved** before closing the app.

This uses the existing SDK MR compositor/recorder: RGB camera plus Unity content,
including cubes and UI. The button has the existing physical press-cycle guard.
The SDK's left-palm solver settings and visibility events are copied into a new,
compact recording-only menu. Existing Hub and customer menus are not edited.
Recorder services are created on demand; recording and RGB AprilTag share the
same SDK camera manager and independent capture ownership. Stopping recognition
or clearing tag results leaves recording active. Pause/disable requests recording
finalization and releases this demo's occupancy; resume does not restart it.

Android output defaults to `/sdcard/DCIM/guest/` with unique MP4 names. If shared
folder export fails, the backend retains its completed source file. The actual
path is exposed by `AprilTagPalmRecording.LastSavedPath` and logged as
`[TAG-REC] Saved: ...`. Audio settings are inherited from the existing recorder
prefab; microphone capture is not enabled by this integration.

All visible scene text uses short English labels. Saved tag cubes cannot be
grabbed, preserving their captured poses for visual comparison. Result labels
remain above the mesh and face the camera without scaling with it. Very close
or overlapping captures can also overlap their labels; records are not moved
apart artificially because that would change the spatial comparison.

## Experimental cube offset

Current trial: shift the tag cube's center by **half its edge on tag-local +X, -Y
and -Z**, per the user's direction correction. At 1:1 a 16 cm cube uses
(+8, -8, -8) cm. Alignment accuracy still requires device verification.

`AprilTagCube.prefab > AprilTagCubeView > Center Offset In Edges` defaults to
`(0.5, -0.5, -0.5)`. Each component accepts a negative value or zero; zero restores
the former centered placement. `SetCenterOffset(Vector3)` changes a view at runtime.
There are no additional on-device controls in this first trial.

`cubeCenter = tagPosition + tagRotation * (centerOffsetInEdges * cubeEdgeMeters)`

Only the cube child is translated. The root, saved Tag pose and **P / R** readout
remain the original recognition values. Offset rotates with the tag and scales
with the displayed cube edge, so the same selected corner stays at the raw tag
origin when using **- / + / 1:1**. Live and Single use the same result prefab;
the independent hand cube and all recording controls are unaffected.

## Data and lifetime

Default: AprilTag **36h11**, **16 cm**, **RGB camera**, manual start. The saved
prefab and script default use `RecognizerMode.RGB_Apriltag`. No camera stream
selector is exposed. This is AprilTag recognition through RGB, not QR recognition.
The hand cube is derived from **Cube1 / ManipulationExamples** in the retained
MRTK2 scene, originating from the MRTK 2.8.3 HandInteractionExamples sample.
It retains the Microsoft package mesh/material and MRTK manipulation components.
Its demo text/event bindings were removed and its release physics made stationary.
Microsoft's existing package licenses apply to those reused assets.

**P** is the copied SDK translation (metres), applied using the existing sample
pose convention. **R** is Unity Euler angles (degrees), derived from the normalized
SDK quaternion; raw native orientation fields are not assumed to be degrees.
Native reference frame, handedness, camera extrinsics, world alignment, physical
size accuracy and overlay accuracy still need G2 validation. Historical cube
positions are scene-session records, not persistent anchors or calibrated error
measurements. Restarting the scene discards them.

The manager retains its existing public API and repeated render-frame events.
An internal delivery sequence lets this demo reject duplicate cached dispatches;
delivery age is not a sensor exposure timestamp. It cannot prove native freshness.
Fisheye workers publish to their own session, preventing retired-worker output
from entering a later session. The current RGB detector's native callback has no
session token; very late callbacks across RGB restarts remain a native/device
verification limit. Repeated RGB Start/Stop and single-shot freshness must be
checked on G2; managed dispatch metadata does not eliminate that native ambiguity.

Pause, focus loss, disable and mode changes stop detection. Resume requires Start.
Stopping releases this demo's occupancy; other SDK consumers retain theirs.

## Editor and G2 validation

Menu: **Singray > AprilTag Placement**

1. **Prepare Demo** regenerates only this demo's prefabs/scene. Save and select
   another scene first. It never changes the Hub or the global build-scene list.
2. **Check Editor** reopens the saved scene and checks references, English text,
   persistent button actions, mesh units, dimensions and stopped defaults.
3. **Check Runtime (Injected)** temporarily enters an isolated Play Mode scene,
   substitutes native delegates, checks the state machine, and restores the prior
   saved scene. The fixture uses the prefab's configured RGB mode while replacing
   native readiness/start/stop and camera leases. It does not execute native RGB
   detection or physical hand tracking.
   Recording checks also substitute a five-byte IMediaRecorder completion fixture;
   they do not produce playable video/audio or invoke the native encoder.
4. **Build G2 APK** builds this scene only as **G2 AprilTag Demo** /
   `com.singray.apriltagplacement` (version 1.0, code 6), then restores the original project identity.
   It does not replace the existing application's identity or build-scene list.
   See the validation status below before treating build success as a device result.

5. **Add Left-Palm Recording** adds this integration to an existing demo content
   prefab without regenerating the recognition layout; running it again adds no
   duplicate controller/menu. Full **Prepare Demo** also includes recording.

Current +X/-Y/-Z APK/evidence: `Builds/AprilTagPlacement_20261116/OffsetPNN`.
Axis arrows, axis labels and their rendering assets were removed at the user's request.
RGB recognition and left-palm recording are retained. Earlier recording, RGB and
parent-folder fisheye APKs are historical builds.
Editor results and injected poses are not G2 hardware acceptance.

## Validation status for the code-6 APK

The local OffsetPNN build succeeded with **0 errors**, Android **ARM64 / IL2CPP**.
The saved-scene checks passed **580 assertions** and the injected checks passed
**96 assertions**. Source correlation checks cover the original build's recorded
inputs. The APK signature and required vendor/recorder native libraries were
verified; this does not establish clean-clone reproducibility with different
native dependency revisions.

Reference APK: **331041362 bytes**; SHA256
`a5d1c11836cd1df154770cad4566d87db1cba1c4757db62db59b6f4a884b52d4`.
The source publication includes the sample and required managed delivery metadata;
local APKs, recordings, test fixtures and build logs remain outside Git.

The half-edge offset is still an experimental placement choice, not a calibrated
correction. Verify physical tag origin, all three offset directions, RGB restart
freshness, Live reacquisition, repeated Single captures, left-palm presses and
playable MR audio/video on G2. Historical managed fixtures do not prove native
behavior. Existing native 16 KB alignment warnings also remain relevant to
Android 15+ compatibility.
