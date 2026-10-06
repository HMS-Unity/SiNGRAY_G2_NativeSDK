# Hand-ray regression review (2026-09-29)

## Verified outcome

The user reported normal hand meshes but upward-pointing rays after the namespace migration. Runtime cleanup and controller algorithm changes were rolled back first; the user still reported failure. After reverting the internal namespace migration and serialized type references, the user reported that hand rays recovered. This strongly implicates the migration or its import/build effects, but does not isolate a single changed reference.

## Evidence and likely failure path

- Editor.log lines 3377320 and 3377364 record: `No controller profile found for type Singray.MixedReality.Toolkit.XvXR.Input.XvXRController`.
- MRTK BaseController matches a mapping using exact System.Type equality. Without a matching mapping, it builds default interactions.
- ArticulatedHandDefinition default interactions carry no assigned Pointer Pose action. The project ShellHandRayPointer prefab expects action 4 (Pointer Pose) and enables useSourcePoseAsFallback.
- ControllerPoseSynchronizer accepts source pose until a matching pointer action arrives. XvXRController emits gripPose (the palm pose) as source pose. A missing action mapping can therefore make the displayed ray use palm orientation instead of the calculated HandRay direction, while joint visualization remains normal.
- This is a code-supported failure mechanism consistent with the symptom and the migration-era warning, not a captured runtime proof of the faulty APK's pointer state.
- The on-disk referenced DefaultMixedRealityControllerMappingProfile currently contains neither the original nor the migrated XvXRController entry. Therefore that file alone does not prove a newly broken serialized mapping. Editor/runtime-generated state, import results, and the exact failing build require comparison to establish the final trigger. Do not claim that one specific profile reference is proven responsible.

## Current state and decision

MRTK scripts/profiles, native hand-input scripts, and MixedRealityToolkit prefab match commit 159c4ba. Internal XvXR namespaces and serialized references are restored. Singray menu branding is retained. No further ray offsets, angle scaling changes, or namespace experiments were applied during this review.

The earlier quaternion-only checks verified arithmetic, not the full MRTK event/mapping/display path, and were insufficient to establish a device fix. Future namespace migration must verify resolved controller types, left/right action mappings, actual pointer pose source, and device behavior before adoption. Keep this working runtime unchanged.
