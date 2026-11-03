# SiNGRAY SDK delivery checklist

Updated: 2026-11-03. Status: documentation and local file review prepared; production delivery not frozen.

## Contents

| Item | Status | Required action |
| --- | --- | --- |
| Runtime and setup file groups | Inventoried | Preserve GUIDs, Resources and shared dependencies |
| Native/managed binaries and MRTK archives | Hashed locally | Confirm final selected versions and redistribution rights |
| Sample scene and prefab entries | Indexed | Select supported entry points and retain their dependency closure |
| Customer setup tools | Classified | Retain XR setup/layer tooling; fix or supersede outdated settings command before release |
| Internal tooling | Classified | Exclude validation scripts/tests, development MCP, caches and device evidence from customer sources |
| Wi-Fi/Bluetooth work | Local-only | Exclude dedicated modules and review shared file hunks/dependencies/configuration before GitHub publication |
| Third-party notices | Partially present | Preserve MRTK MIT, Newtonsoft notice and original headers; verify vendor/NatCorder/NPinyin/font and other redistribution terms |
| API and key-input guides | Existing | Read alongside the current integration guide; retain public/native names |
| SDK release version and firmware matrix | Pending | Record supported G2 firmware and native inputs independently from app version |
| Clean customer project import/build | Pending | Execute the procedure below without reusing Library or prior project caches |
| Clean customer device initialization | Pending | Capture real native initialization and selected feature results |

## Known release review items

1. `BuildSettingWidows.cs` configures Android API 26/27; the tested source project uses 30/35. The shortcut must not silently undo the documented configuration.
2. `mainTemplate.gradle` fixes compile SDK/build tools to 34 while the current target API is 35. Resolve and validate a reproducible toolchain before freezing it; this inventory does not upgrade Gradle or Android settings.
3. Shared `ProjectBuild.cs`, Android bridge code, manifests and vendor AARs mix required functionality with local-only features. Path exclusions cannot determine which historical/native code is an unauthorized new addition. Review actual publication diffs; do not delete whole required files or vendor APIs to remove sample entries.
4. The package manifest contains internal Unity-MCP and its lock dependency graph. Only the local review copy is filtered. The source project remains unchanged; the filtered copy is not clean-import tested.
5. The reference ProjectSettings contain machine-specific signing configuration. Supply customer instructions and use customer identity/signing; do not distribute developer keys or overwrite customer ProjectSettings.
6. Canceled speech sample entries stay disabled, but `XvApp.java` still initializes speech and DLNA. Preserve the current native/Java dependency chain until a separately verified feature split exists.
7. Serialized layer indices, Resources paths, hidden MRTK example source dependencies and package GUIDs require import checks. Text GUID scans do not replace `AssetDatabase.GetDependencies` or Unity missing-component validation.
8. The checkout has existing staged eye-tracking deletions and other unpublished work. This review does not approve those deletions or change the Git index. A final source tree and commit must be selected before publishing checksums as an official release.

## Acceptance evidence

MRTK2 and IRToWorld have user-reported G2 acceptance with no observed issues on 2026-11-03. Earlier feature acceptance remains in `../SDK-Customer-Delivery-Plan.txt` and `../SDK-Iteration-Log.txt`. Brief user feedback does not establish every checklist scenario or sustained shared-feature regression.

IPD and RTSP playback issues remain deferred, not passed. Electrochromic source/managed checks do not establish optical hardware acceptance. Gaze and other unconfirmed per-scenario tests remain subject to their feature records. PlaneDetection was skipped; SpeechVoice example was canceled. Do not advertise all capabilities as device-accepted.

## Clean customer project procedure

1. Freeze the selected source revision and file hashes. Resolve held publication/licensing/configuration items first; do not treat the review snapshot as release-ready.
2. Create a new Unity 2022.3.62f3 3D project in a separate directory. Start with no source-project Library, Temp, user settings or installed SDK assets.
3. Resolve the selected package manifest and local MRTK archives. Record all package versions, archive hashes and package resolution errors. Verify only one compatible Newtonsoft assembly is imported after removing internal MCP dependencies.
4. Copy the selected SDK assets and their metadata, required Editor setup, Android integration files and the selected optional sample dependencies. Merge customer project configuration deliberately. Verify layer indices and Android importer settings.
5. Allow import/compilation. Check missing scripts, duplicate GUIDs, unresolved serialized callbacks, shader/resource availability and assembly collisions in the actual imported project. Run Unity dependency checks for the selected sample and its Resources dependencies.
6. Configure Android IL2CPP/ARM64, verified graphics/toolchain, customer identity and signing. Create a reference SDK host and CameraDemo, then build exactly that scene. Capture the full build log and merged manifest/native library list.
7. Install without automatic launch when following the current user workflow. The user launches via MPLauncher. Record native initialization, permission flow, real camera output, key/MRTK input and stop/reentry cleanup.
8. Regress IR/cloud shared ownership, recording and other supported features selected for delivery. Capture sustained FPS/PSS and exit/reentry evidence, including failures.
9. Record actual outcome, firmware and artifact hashes. Only mark a step passed when its evidence exists. Publish separate English commits after reviewing local-only exclusions and existing staged work.

The local review has passed file/hash, archive, selected source-setting and documentation checks only. This task does not claim execution of the clean-project or hardware procedure.
