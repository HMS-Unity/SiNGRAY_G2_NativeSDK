> 2026-09-29: Namespace migration reverted for hardware regression verification. Internal XvXR types and serialized references are restored; Singray editor menu labels remain. Earlier migration/validation notes below are historical.

# SDK cleanup and code review

## Implementation update: runtime rollback

At the user's request, F1-F3 runtime changes were reverted to commit `159c4ba`, retaining only the Singray namespace migration. XvXRController was also restored to that baseline, including its original quaternion-x scaling; the later supplied reference-script change was superseded by this rollback. No new ray correction was added. Hardware behavior is pending verification.

F4 build-menu validation remains, together with the requested Singray menu consolidation. The editor validation menu now checks only build scene validation and preservation of project settings; runtime regression checks for reverted changes were removed.

Scope: the current `Assets/` tree on `dev/4.2`, inspected on 2026-09-29. This is a focused static review, not a complete audit. The current tree includes vendor code and integration changes; findings are not automatically attributed to the supplier. No source was changed or deleted during this review.

## Cleanup decision table

| Decision | Items | Evidence / condition |
| --- | --- | --- |
| First cleanup batch | `RTSPStreamerDemo.cs`, `XvXRLogFile.cs`, `SlamConfidenceControl.cs`, `ResetSlamTest.cs`, `RayCastTest.cs` | Empty implementation or diagnostic-only; no discovered in-project type or script-GUID references. About 4.3 KiB including metadata. Confirm external clients do not depend on the test types. |
| Confirm feature scope | `JPGButton.cs`, `UpdateHanZiHotWord.cs`, `BackgroudLoadRGB.cs`, `HandEx.cs`, `LoadHandAR.cs` | No discovered in-project references, but they implement actual optional behavior. |
| Review scene variants | `Viewer2`, `Viewer_t`, nested `Scenes/Viewer`, `Ocr`, `Ocr 1`, `Cmr`, `Sensor`, `Stm`, `TofPointCloud_SZ` | Not byte-identical duplicates. Current build selection alone does not establish whether an SDK example is useful. |
| Keep | Supplier AARs/native libraries, public API wrappers, runtime Resources, required NatCorder example implementations | Native, dynamic-resource, or explicit runtime dependencies. |

Full asset paths and reference-scan limitations are recorded in `Docs/sdk-cleanup-candidates.md`. Only Assets is a deletion candidate scope; no cache/output cleanup is proposed here.

## Functional findings

P1: address before customer validation of the affected feature. P2: address during the next focused cleanup. Findings describe source-level behavior; device failures have not been reproduced in this review.

| ID | Priority | Source and lines | Trigger and consequence | Suggested correction and verification |
| --- | --- | --- | --- | --- |
| F1 | P1 | `Assets/XRFoundation/Core/Sdk/XR/XvXRScripts/Engine/XvDeviceManager.cs:24-32,63-72` | If a caller requests Manager without an existing rig, the fallback creates an empty GameObject and adds XvDeviceManager. Awake immediately dereferences `Head` and `Head/XvXRCamera`, which the fallback never creates. The fallback initialization path can throw a null-reference error. | Instantiate a validated rig prefab, or explicitly reject missing configuration before attaching the component. Verify access in an empty scene and with a correctly configured rig. |
| F2 | P1 | `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/Base/XvARCamera.cs:195-218`; `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/Base/Manager/XvARCameraManager.cs:35,40-53` | A resolution change replaces the Texture2D without destroying the old one. StopCapture releases the managed camera reference without an explicit texture-disposal path; repeated start/stop creates new camera objects. Native Unity texture resources can accumulate until later resource unloading. | Define texture ownership and an explicit cleanup method, used on replacement and final stop. Avoid destroying textures still used by consumers. Measure texture count/memory over repeated start/stop and resolution changes on device. |
| F3 | P1 | `Assets/XRFoundation/Core/Sdk/Xslam/Scripts/API.cs:637-656` | HidWriteAndRead pins two arrays, but frees them only after the native call returns normally. If allocation or native entry invocation throws, the catch returns null without freeing already allocated handles. Caller-controlled wlen is also passed without checking it against the buffer length. | Validate null/length inputs, release allocated handles in finally, and preserve diagnostic context. Verify invalid input and forced native-call exceptions; valid commands must remain byte-for-byte compatible. |
| F4 | P1 | `Assets/XRFoundation/Core/Editor/ProjectBuild.cs:91-111` (Wifi example; repeated throughout file) | The menu points to the missing `Assets/XvXRFoundation/...` scene path. It changes product name, package ID, and signing settings before building, with no restoration in this method. A failed menu build can leave the customer's project settings changed. All 56 distinct scene-path literals in this file were missing at audit time; this count includes literals in commented code. | Replace duplicated build methods with validated build profiles. Validate paths before changes; restore temporary settings in finally. Verify both successful and intentionally failing builds leave project identity unchanged. |
| F5 | P2 | `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/Base/XvARCamera.cs:268` | `(long)rgbTimestamp * 1000000` removes the fractional component before scaling. For a timestamp 1.25 the expression produces 1000000 instead of 1250000. This path runs only when user-pose mode is enabled; the native timestamp unit must be confirmed before changing it. | Confirm units with the supplier/native contract, then use a named conversion helper with deliberate rounding and overflow behavior. Verify fractional timestamps and RGB/pose synchronization. |
| F6 | P2 | `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/Base/XvARCamera.cs:197-224` | Texture/buffer initialization exceptions are silently caught. Update retries while the caller receives neither a useful error nor a failure state, making a black preview difficult to diagnose. | Log a rate-limited error with dimensions and exception details, expose initialization failure state, and bound retries. Verify a simulated allocation/initialization failure reports an actionable cause. |

## Maintainability findings

| ID | Priority | Source | Why it is hard to maintain | Direction |
| --- | --- | --- | --- | --- |
| M1 | P2 | `Assets/XRFoundation/Core/Foundation/MediaRecorder/XvMediaRecorderManager.cs:1,10-17,49-58` | Production recording directly depends on NatSuite.Examples.ReplayCam and JPG. Folder naming suggests removable examples even though they are runtime dependencies. | Separate required recording implementation from demonstrations while preserving script GUIDs, serialized references, and third-party notices. |
| M2 | P2 | `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/Base/XvARCamera.cs:124-128,231-236` | Startup waits for 100 Update calls and frame processing skips every other call. Behavior depends on Unity frame rate and is not clearly connected to the public camera fps setting. These may be hardware workarounds, so removing them blindly is unsafe. | Document the hardware rationale; express readiness and throttling through explicit state/timing/configuration after device validation. |
| M3 | P2 | `Assets/XRFoundation/Core/Sdk/XR/XvXRScripts/utils/SingleTon.cs:7-113`; `XvDeviceManager.cs:19-38`; `XvARCameraManager.cs:8-17` | Multiple singleton patterns have different creation, lifetime, and cleanup rules. The generic plain-C# singleton and Unity component singleton are different responsibilities and should not be merged indiscriminately. | Standardize component lifecycle conventions and keep native service ownership explicit. Preserve public entry points until client migration is planned. |
| M4 | P2 | `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/XvCameraManager.cs:63-149`; `XvARCamera.cs:54-89` | Resolution mappings are repeated for Width, Height, and native configuration. Adding one resolution requires coordinated edits. | Use one explicit mapping from public enum to dimensions/native value; retain existing ABI enum values. |
| M5 | P2 | `Assets/XRFoundation/Core/Editor/ProjectBuild.cs`; `Assets/XRFoundation/Core/Sdk/Xslam/Scripts/API.cs` | Large files (924 and 1209 lines respectively), commented legacy sections, and interleaved responsibilities make review difficult. File length alone is not a defect; duplicated build setup and mixed native declarations/managed wrappers are the concrete concerns. | Consolidate build configuration; separate native declarations from managed validation/ownership wrappers without changing exported entry-point signatures. |

## Proposed order

1. Remove only the agreed empty/test script batch and its metadata; compile and check scene components.
2. Fix F1-F3 in small changes, testing missing-rig initialization, camera resource lifetime, and HID exception cleanup.
3. Fix F4 before recommending the old SDK build menus to customers.
4. Verify the timestamp contract and device timing behavior before changing F5/M2.
5. Refactor recording boundaries, singleton conventions, and configuration duplication after runtime behavior is covered.

Do not combine the review with mass class/file renaming or changes to AARs, native symbols, Java package names, and GUIDs. No Android build, device test, or destructive cleanup was performed during this review.
