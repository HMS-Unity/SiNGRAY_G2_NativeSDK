# Publication scope and verification

This optional RGB diagnostic sample is integrated into the customer SDK
`dev/4.2` branch. Its original clean-checkout validation used SDK base `20c49d8`.

Required shared customer integration dependencies:

- `Core/Foundation/MediaRecorder/XvMediaRecorderManager.cs`: distinct capture
  ownership for recording/screenshots, cancellation and stop cleanup.
- `Core/Foundation/MediaRecorder/NatCorder/Examples/ReplayCam/ReplayCam.cs`:
  asynchronous finalization/worker and GPU callback draining, ownership held until
  completion, restart gating and nonempty output/fallback reporting.
- `Core/Foundation/Viewer/Scripts/MrtkClickGate.cs`: cancellation on disable and
  fresh-press guards used by the sample's independent controls.

These shared implementations already exist in the merge target `60eb9a8` and
are unchanged by this Demo merge. They reproduce this sample's tested
recording lifecycle; public customer APIs remain available. Their broader hardware
regression coverage is not implied by a diagnostic APK or controlled fixture.
No other sample, layout, global scene/build setting, native supplier library,
Wi-Fi/Bluetooth module, or application project is changed by this publication.

`Editor` and `Tools/RgbTagAlignment` contain internal reproducibility checks.
They are not a new runtime requirement or a new SDK packaging format. Device raw
frames, per-device captures/calibration, videos, build outputs and the local Editor
transport client are excluded. OpenCV source, prebuilt bridge libraries, import
metadata, marker assets and notices are included.

Historical main-workspace evidence: version 1.2/code 3 APK build/install successful;
243 controlled Editor/native checks and 30 recording fixture assertions passed.
Video review demonstrates a decodable MR recording with visible recording UI, but
no confirmed locked walk-around segment or absolute/cross-device accuracy result.
Fresh publication-checkout validation is recorded in the publication receipt;
never infer it from the historical working-project results.

The publication preserves the existing base XR/Xslam AARs. Xslam/OpenCV matches
the historical working-project input; the base XR AAR hash differs. This source
checkout is not a byte-identical reproduction of the previously installed 1.2
APK. No XR/IPD investigation or supplier version mismatch is claimed.

Fresh publication checkout: 15 actual Windows bridge synthetic cases, 233
headless saved-scene/native/lifecycle assertions, 30 controlled Play Mode recording
assertions, and Android player script compilation passed. All 32 referenced asset
GUIDs resolve through repository assets and pinned TMP/UGUI/MRTK packages.
Headless rendering was skipped explicitly; no new APK/native device acceptance
is claimed. The local batch harness is orchestration only and is excluded.
See Validation-Receipt.json.

Integration into dev/4.2 was revalidated against target 60eb9a8: 233 headless
Editor/native assertions, 30 controlled recording assertions and Android player
script compilation passed. The merge itself changes no shared SDK source, vendor
archive, other sample or global setting; its dependencies were already present.
