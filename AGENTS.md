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
