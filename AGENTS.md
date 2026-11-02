# Repository scope

This repository manages SiNGRAY Native SDK and its SDK samples. The primary
working directory is `E:/UGit/origin/singray_Native_sdk`; the current SDK
development branch is `dev/4.2`.

- Keep medical viewer, AR Engine, and spatial alignment application development
  in their application Unity project. Do not use this SDK repository to manage
  application-specific scenes, controllers, build settings, or releases.
- Existing `codex/mrtk283-*` branches and the application worktree are legacy
  application work. Their existence does not make them SDK development branches.
  When reporting the latest SDK commit, inspect the active SDK branch rather than
  selecting the newest commit across all local branches.
- Synchronize reusable key-input and MRTK button fixes into SDK components and
  SDK samples where applicable. Preserve customer-facing input APIs; application
  actions such as exit, IPD adjustment, and selection belong to the application.
- Do not migrate, split, delete, or modify application projects or their Git
  history as part of SDK work unless the user requests that work.

## Customer SDK delivery

- The user explicitly requires this project to be delivered to customers as an
  SDK. Prioritize customer integration, reusable APIs/components, required native
  dependencies, documentation, and reproducible validation. A sample APK is a
  demonstration artifact and does not establish customer SDK delivery readiness.
- Track three separate work areas: customer SDK implementation and required
  integration assets; optional customer-facing integration samples; internal
  development, diagnostics, and validation tools. Determine dependencies before
  physically separating folders or assemblies. Required customer Editor setup
  tools must not be mistaken for internal-only tooling.
- Preserve existing standalone-scene, BaseScene-menu, and Hub layouts during
  directory/entry consolidation. Do not redesign UI, change transforms/anchors,
  replace keyboards, or rebuild the Hub interface as incidental cleanup.
- Preserve public APIs, asset GUIDs, serialized type references, and required
  Resources paths unless the user explicitly authorizes the relevant change.
  Removing a sample entry does not authorize removing its underlying SDK ability.
- Prefer vendor-neutral customer-facing sample names. The user requested
  `CameraDemo.cs` / `CameraDemo.prefab` in place of the former `XvCameraDemo`
  names. Handle further prefix removal per module with reference migration;
  never blanket-replace native entry points, library names, or SDK APIs.
- Before deleting legacy sample scenes, inspect the Hub generator's current
  source dependencies. Shared camera, capture, recording, input, and native
  resources must be retained while any supported SDK feature depends on them.
- Do not automatically introduce a new package/distribution format or migrate
  customer/application projects. Define and verify the SDK delivery contents
  separately from sample builds and internal tools.
- The current work breakdown and status are recorded in
  `Docs/SDK-Customer-Delivery-Plan.txt`. Keep planned work distinct from completed
  changes and device-verified results. The scene-consolidation work remains at
  planning/review scope until implementation is requested.

## Key-input locations

- Device input API: `Assets/XRFoundation/Core/Input`.
- SDK key-input example: `Assets/XRFoundation/SampleScenes/KeyInput`.
- MRTK physical press-cycle guard:
  `Assets/XRFoundation/Core/Foundation/Viewer/Scripts/MrtkClickGate.cs`.
- SDK recording menu integration:
  `Assets/XRFoundation/Core/Foundation/MediaRecorder/XvMediaRecorder.cs`.
- SDK sample hub integration:
  `Assets/XRFoundation/SampleScenes/SDKSampleHub/Scripts/SdkSamplesHubController.cs`.

Native device input and physical hand interaction require G2 hardware validation;
source comparison and Editor validation do not establish device behavior.
