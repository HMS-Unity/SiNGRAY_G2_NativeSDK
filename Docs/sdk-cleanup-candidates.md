# SDK cleanup candidates

Audit date: 2026-09-29. Project: `E:/UGit/origin/singray_Native_sdk`, branch `dev/4.2`.

No files have been deleted. Sizes are approximate MiB (1,048,576 bytes). Paths below are relative to this project.

## Scope

Only contents under `Assets/` are in scope for this cleanup. Project-root archives, Library, build, Exports, Packages, and other directories outside Assets are excluded. No file has been deleted.

## First cleanup batch

Fresh GUID and type-name scans found no in-project references for these five small scripts. Their code is either empty or diagnostic-only:

| File | Reason |
| --- | --- |
| `Assets/XRFoundation/SampleScenes/RTSPStreamer/Scripts/RTSPStreamerDemo.cs` | Empty Start and Update methods. |
| `Assets/XRFoundation/Core/Sdk/XR/XvXRScripts/utils/XvXRLogFile.cs` | Empty internal class; all logging implementation is commented out. |
| `Assets/SlamConfidenceControl.cs` | Standalone native pose-confidence polling/logging test. |
| `Assets/XRFoundation/SampleScenes/EyeTrackingNew/Scripts/ResetSlamTest.cs` | Standalone SLAM-reset test handler. |
| `Assets/XRFoundation/Core/Foundation/Gaze/Scripts/UserInputEvent/Scenes/RayCastTest.cs` | Pointer and drag event logging test. |

These five files plus their `.meta` files total about 4.3 KiB. Removing them would improve source clarity, not materially shrink the SDK. This classification assumes no external customer project consumes these test types.

## Additional candidates needing a feature decision

These also have no discovered in-project GUID/type-name references, but contain real functionality:

| File | Function to confirm before removal |
| --- | --- |
| `Assets/XRFoundation/Core/Foundation/MediaRecorder/NatCorder/Examples/JPG/JPGButton.cs` | Pointer-down/up UnityEvent button component; distinct from the required JPG recorder. |
| `Assets/XRFoundation/Core/Sdk/Xslam/Scripts/BackgroudLoadRGB.cs` | Native RGB texture display. |
| `Assets/XRFoundation/Core/Sdk/Xslam/Scripts/HandEx.cs` | Extended hand-joint visualization helper. |
| `Assets/XRFoundation/Core/Sdk/Xslam/Scripts/LoadHandAR.cs` | Legacy AR hand demonstration. |

Do not delete the containing SDK or NatCorder folders wholesale.

## Strong source cleanup candidates

For the following scripts, the scan found no references to their script GUIDs in Assets text resources and no type-name references in other scanned Assets text files. This is evidence of being unused within this project, not proof that no external customer or reflection-based code uses them.

| Path | Evidence and proposed disposition |
| --- | --- |
| `Assets/XRFoundation/SampleScenes/RTSPStreamer/Scripts/RTSPStreamerDemo.cs` | Empty Start/Update methods and no other behavior. Strong deletion candidate. Keep the actual RTSP manager and scene. |
| `Assets/SlamConfidenceControl.cs` | Standalone diagnostic: polls native pose confidence and writes a log/error message. No discovered references. Candidate for removing from customer delivery. |
| `Assets/XRFoundation/SampleScenes/EyeTrackingNew/Scripts/ResetSlamTest.cs` | Test button handler for native SLAM reset; no discovered references. Remove if this manual test is retired. |
| `Assets/XRFoundation/Core/Foundation/Gaze/Scripts/UserInputEvent/Scenes/RayCastTest.cs` | Only logs pointer/drag events; no discovered references. Strong diagnostic cleanup candidate. |
| `Assets/XRFoundation/Core/Foundation/Gaze/Scripts/UserInputEvent/Scenes/SampleScene.unity` | Separate test scene, not enabled in current build settings; no external GUID references found into this Scenes folder. Keep the surrounding Gaze runtime implementation. |
| `Assets/XRFoundation/Core/Foundation/Keyboard/Scripts/UpdateHanZiHotWord.cs` | Chinese keyboard hot-word maintenance UI with no discovered references. Candidate if this maintenance UI is not delivered; this does not imply the pinyin dictionary is unused. |
| `Assets/XRFoundation/Core/Editor/Tools/Function.cs` | Generic legacy command-line/file helper with no discovered in-project type references. Confirm external build scripts do not invoke it before removing. |

When later deleting Unity assets, remove their matching `.meta` files together through Unity's asset workflow.

## Scenes requiring a product decision

Only `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer.unity` is enabled in the current build settings. Other examples can still be valuable SDK documentation and are not automatically unused. The older sample launcher loads scenes dynamically by name (`XvLoadScenesManager.LoadScenes`), so zero GUID references alone do not establish scene safety.

Review these variant/test scenes first:

- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer2.unity`
- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer_t.unity`
- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Scenes/Viewer.unity`
- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Cmr.unity`
- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Ocr.unity`
- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Ocr 1.unity`
- `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Sensor.unity`
- `Assets/XRFoundation/SampleScenes/Stm/Stm.unity`
- `Assets/XRFoundation/SampleScenes/TofPointCloud/Scenes/TofPointCloud_SZ.unity`

No byte-identical `.unity` scenes were found; these cannot be described as proven duplicate copies. Review actual behavior before choosing which to retire. `SDKSamples/`, `SDKSampleHub/`, and `SingrayG2Validation/` provide different sample/test entry points and likewise need a product decision.

## Keep, or investigate before removing

- Supplier `xvxrlib-release.aar`, `xvsdk-release.aar`, `Assets/Plugins/Android/Libs/arm64-v8a/libxv-wrapper.so`, and native API wrappers: required platform integration. Lack of a C# reference is not sufficient evidence that a native/public API is unused.
- `NatCorder/Examples/`: **do not delete this entire directory**. `XvMediaRecorderManager` directly uses `NatSuite.Examples.ReplayCam` and `JPG` with RequireComponent. Assets inside Examples are also referenced by the media recorder prefab, MRTK2, TagRecognizer, and several Viewer scenes. Separate necessary implementation from sample content before trimming.
- `Resources/` assets: managers, keyboard, shaders, and gesture textures are loaded by name via Resources.Load. Static GUID scans miss these dependencies.
- Public wrappers and optional capabilities such as `XvsdkDeviceManager`, `XSlamCameraController`, `XvXRSdkPlugin`, `XvNativeAPI`, `XvPlane`, HEVC/WAV recorders, and controller extensions: no discovered in-project consumers for some types, but customer-facing or optional APIs must not be removed on that basis.
- Editor menu tools, automatic branding/build hooks, and tests: no regular call sites may be expected. They are not dead code merely because static reference counts are zero.
- Chinese speech keywords, pinyin input data, fonts, and licenses: functional/reference/legal content, not automatically useless because the public UI is English.

## Verification scope

Inspected local file sizes, current build settings, manifest dependencies, script behavior, text asset GUID references, type-name references, dynamic resource/scene loads, and exact scene hashes. Package archive internals, native binaries, external customer projects, reflection, and full runtime reachability were not exhaustively analyzed.

The namespace change now appears in the compiled Assembly-CSharp.dll; Unity is idle after domain reload and its error Console query returned zero entries. No Android device regression test was performed. Cleanup must be followed by compilation and testing of the affected sample/features.
