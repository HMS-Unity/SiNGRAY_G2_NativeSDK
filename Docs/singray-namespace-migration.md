# Singray namespace migration

Internal root namespaces and corresponding serialized references now use Singray. Vendor native/Java identifiers and script GUIDs remain unchanged. Ray calculation is unchanged from commit 159c4ba.

Hand and joystick input profiles now reference the project-owned SingrayControllerMappingProfile. It retains the existing standard mappings and adds explicit left/right mappings for Singray XvXRController and XvXRJoystickController. Pointer Pose uses action 4, Grip Pose action 3; remaining articulated-hand actions retain the standard assignments.

Static checks passed: four mappings, both handedness values, distinct pointer/grip assignments, unchanged controller algorithm. Follow-up Unity validation passed for both profiles and pointer/grip action mappings. The user confirmed normal hand rays on device after this change. Subsequent maintenance updated 39 legacy event type labels and removed six invalid build menu entries; build scene/settings validation passed. A new APK containing that subsequent maintenance has not yet been built because the Android branding-image decision is pending.
