# SiNGRAY SDK delivery review

Updated: 2026-11-03. Source branch: `dev/4.2`. Configured project version: `4.2.0.2`.

This is the customer delivery inventory and integration review for the existing Unity project. The reviewed files are a development snapshot, not a frozen SDK release. Sample acceptance and Android builds are recorded separately from clean customer project acceptance.

Publication scope: these inventories describe the local reviewed working tree, including unpublished feature changes. This documentation upload does not synchronize those runtime/sample changes to GitHub. A listed local prefab or validation outcome is not proof that the corresponding implementation is present at the remote branch tip. Select and synchronize the final source revision before using these inventories as a release manifest.

## Read these files

| File | Purpose |
| --- | --- |
| [Integration guide](Integration-Guide.md) | Required files, setup, scene initialization, ownership and build workflow |
| [Delivery checklist](Delivery-Checklist.md) | Release gates, exclusions and remaining work |
| [File groups](File-Groups.csv) | Required runtime, customer setup, optional samples and internal tools |
| [Native dependencies](Native-Dependencies.csv) | Actual archive/library paths, sizes and SHA-256 values |
| [Package dependencies](Package-Dependencies.csv) | Direct Unity package requirements from the current manifest |
| [Sample entries](Sample-Entries.csv) | Standalone scenes, reusable content and acceptance scope |
| [Shared asset dependencies](Shared-Asset-Dependencies.csv) | Runtime-folder assets that reference assets located in sample folders |

The CSV files are generated from the current checkout by the internal tool `Tools/Delivery/prepare_delivery_review.py`. Full file hashes, Resources lookup results, native archive contents, dependency warnings and the review copy are stored locally under `Builds/SDKDelivery_20261103`. That directory is ignored by Git. The review copy preserves original paths and metadata; it is not an installer or a new SDK package format.

For a new snapshot, run `python Tools/Delivery/prepare_delivery_review.py --output Builds/SDKDelivery_NEW_LABEL`. Use a new output path to preserve previous evidence. The tool reads source assets, generates the CSV documents and copies review inputs; it does not build, install, launch, commit or push.

## Delivery boundaries

Deliver runtime code, required native/managed dependencies, Resources assets, compatible MRTK profiles and customer setup tools together. Samples are integration examples; a sample prefab alone does not initialize the SDK. Keep original asset GUIDs and public/native identifiers.

Wi-Fi and Bluetooth modules and their associated new work remain local-only. Internal diagnostics, device logs, caches, APKs, signing keys and machine-specific settings are outside customer source delivery. Shared files containing both required SDK integration and excluded feature entries require a scoped publication review; removing a folder alone does not satisfy that review.

IPD, RTSP playback and sample structural consolidation remain deferred. SpeechVoice example menu/build entries remain disabled; its required startup dependencies and compatibility APIs remain. PlaneDetection has no new acceptance in this pass. Do not infer a production support commitment from a legacy scene's presence.

## Current result

The documentation and file inventory are prepared. The local review copy is integrity-checked against its manifest. Clean-project import/build/initialization and a final selected-feature G2 regression remain release gates. The existing isolated sample build workspace contains prior imports and caches, so its successful builds do not meet the clean-project gate.
