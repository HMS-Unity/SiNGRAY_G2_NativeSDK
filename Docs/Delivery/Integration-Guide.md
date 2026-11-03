# SiNGRAY SDK customer integration guide

This guide describes the current `dev/4.2` source layout. Use Unity 2022.3.62f3 and its Android Build Support, SDK, NDK and OpenJDK. The SDK targets SiNGRAY G2 hardware; Editor simulation does not provide native device acceptance.

## Required files

| Existing path | Integration role |
| --- | --- |
| `Assets/XRFoundation/Core/Sdk` | Native API bindings, XR device/render/input integration and MRTK provider profiles |
| `Assets/XRFoundation/Core/Foundation` | Reusable feature managers, shared capture ownership, shaders, prefabs and Resources; retain dependencies of selected features |
| `Assets/XRFoundation/Core/Input` | Customer key-input APIs, profiles and simulation; its Tests are internal validation |
| `Assets/XRFoundation/Core/Font` | Font assets used by serialized SDK UI references |
| `Assets/XRFoundation/ThirdParty` | Bundled managed dependencies and TMP assets; preserve license and importer metadata |
| `Assets/Plugins/Android` | Application/Activity manifest integration, Java startup, Gradle templates and native wrapper |
| `Assets/XRFoundation/Core/Editor` | Required customer setup mixed with optional build and internal validation tools; use File-Groups.csv before excluding files |
| `Assets/MRTK`, `Assets/MixedRealityToolkit.Generated` | Referenced shaders and IL2CPP linker preservation |
| `Assets/Singray` | Referenced sample branding assets; inspect dependencies before omitting |
| `Packages/MixedReality` | Four MRTK 2.8.3 archives used by the direct package manifest |

These are conservative integration groups, not a tested minimum subset. No new assembly boundaries have been introduced. Do not import only `Core/Sdk` or only `CameraDemo.prefab` and expect the existing feature code and serialized references to resolve. Keep every retained asset's `.meta` file and required parent metadata. SDK prefabs store numeric layers and references to profiles/resources outside their immediate folders.

The text reference audit found six links from assets under Core into sample assets: gaze material, a TagRecognizer material used by the embedded gaze sample scene, a legacy keyboard sprite, Viewer video material, SpatialMesh material and a TagRecognizer material. Exact paths are in `Shared-Asset-Dependencies.csv`. Retain them and their dependencies when retaining the referring features; deleting all SampleScenes would break these references. Their physical relocation remains deferred.

`Native-Dependencies.csv` records the actual AAR/SO/DLL/TGZ inputs. AAR contents are inspected in the local native archive report. Do not replace libraries just because two files have similar names. The Unity project version is not proof of the native library version or firmware compatibility.

## Package and Android setup

1. Retrieve actual AAR and TGZ binaries using Git LFS when obtaining sources through Git. Check that the files are not LFS pointer text, then verify hashes against the selected delivery snapshot.
2. Resolve the four local MRTK packages using their existing `file:MixedReality/...` entries. Retain UGUI, TextMeshPro, Android JNI and Unity modules used by the selected runtime. The current manifest also contains IDE/collaboration utilities. Unity-MCP is internal tooling and is removed only from the local review copy's package manifest/lock, not from this working project.
3. Switch the target to Android. Current project settings are IL2CPP, ARM64, minimum API 30, target API 35, OpenGLES3 and landscape. Use the recorded Unity toolchain. `mainTemplate.gradle` separately fixes compile SDK/build tools to 34; this discrepancy is recorded rather than presented as a clean-project compatibility result.
4. Review the Android templates as a set. Current sources use Android Gradle Plugin 7.4.2, AndroidX/Jetifier and both AndroidX and old support dependencies. The exact Maven declarations are retained in the reference templates; dependency resolution/network access is still required. A cached build does not establish an offline installation.
5. Set your own application ID, version and signing configuration. `com.singray.ToFViewer` and `singray SDK` identify this checkout's demonstration app. Do not replace an existing application project's complete ProjectSettings folder. The reference settings still contain a developer-specific keystore path; no signing key is included or required to be copied from that path.
6. Merge the Android manifest with the customer application deliberately. Preserve the existing vendor Activity/Launcher handoff and native integration until independently verified. Review feature permissions and resulting merged manifests; do not treat a permission declaration as proof that an ordinary app receives privileged hardware capabilities.

The current `com.xv.XvApp` startup imports and initializes speech and DLNA classes. Therefore disabling the SpeechVoice example does not authorize removing its AAR or the vendor startup dependencies. Keep native entry points and vendor namespaces unchanged.

## Initialize a scene

Start with a copy of `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer.unity` as the reference host. For a new integration, preserve the customer's existing scene and work on a copy while validating setup.

The customer setup entry is **Singray > Toolkit > Add To Scene and Configure**, implemented by `Core/Editor/XvXRFoundation.cs`. It loads `MixedRealityToolkit`, `XvXRInput` and `XvXRManager` through Resources. When creating the XR manager, this command deletes the first existing Camera and EventSystem it finds. Inspect the host before using it; it is not a non-destructive merge tool.

Do not run the old **Singray > Toolkit > Project Settings** shortcut as the final configuration step: `BuildSettingWidows.cs` still writes minimum API 26 and target API 27. Configure the current 30/35 values manually and verify the final build settings. This documentation pass does not modify that behavior.

`SDKLayerSetup.cs` is required customer setup, not an internal-only diagnostic. It creates `BGVideo` and `XvBGVideo` in available slots. The reference project has these at indices 6 and 8. In an existing project, compare serialized object layers, masks and camera culling when names occupy different indices; adding names alone does not remap prefab numeric layers.

Add the selected feature manager and content prefab only after host initialization. For CameraDemo, use `SampleScenes/Viewer/Prefabs/CameraDemo.prefab` and explicitly bind its camera/capture manager fields. Reuse host managers; avoid duplicate native stream owners or duplicate XR rigs. Inspect the standalone scene for the intended configured dependencies.

Keep the SDK MRTK input profiles and distinct actions: Pointer Pose is action 4; Grip Pose is action 3. The optional **Singray > Validation > Check MRTK Input Mappings** command checks configured mappings and the existing pointer prefab. It is a development validation helper, not runtime initialization.

## Resource and feature lifetime

Preserve the relative paths under `Resources`. Source lookup strings and their matching assets are listed in the local `resource-lookups.csv`; dynamic lookups and package references still require Unity import validation. Required XR/manager prefabs are not replaceable with identically named empty objects.

Subscribe when the owning component becomes active, unsubscribe and cancel pending work when it leaves, and avoid retaining native callbacks or Unity objects across scene generations. Callback thread behavior, buffer ownership and coordinate/timestamp units are feature-specific; follow [API Reference](../API-Reference.md) and the existing manager wrappers. A callback does not generally authorize writing Unity UI from its native thread.

Camera/ToF capture is shared. Use the manager's supported start/stop methods and retain/release lifecycle; do not insert unconditional global native stops into an application's navigation. Managed ownership guards do not guarantee simultaneous incompatible native sensor configurations. Texture/point buffers may be reused; copy data if retaining it beyond its documented valid interval. IR/cloud dimension agreement is not proof of frame synchronization.

## Build and hardware check

Select exactly the intended entry scene in Build Settings, then use **Build**. Optional scene build menus are in `Core/Editor/ProjectBuild.cs`; the legacy BaseScene build includes additional scenes and is not a minimal delivery export. Hub content depends on both configured modules and generator source assets; do not delete the old scenes or rebuild the Hub as import cleanup.

Install the resulting APK explicitly, for example:

```sh
adb -s DEVICE_SERIAL install -r path/to/CustomerDemo.apk
```

Open through the device's supported Launcher flow. Confirm real frames, input, stop/restart, pause/resume, exit/reentry, selected-feature concurrency, file output when applicable, and sustained memory/FPS. Preserve firmware, device identity, APK hash and results. Installation, compilation and user acceptance of one scene are separate records.

See [Delivery checklist](Delivery-Checklist.md) for clean-project acceptance. No final release archive or customer migration is performed by this documentation pass.
