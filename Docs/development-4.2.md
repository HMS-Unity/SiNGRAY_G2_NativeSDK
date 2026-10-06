# 4.2.0-dev.1 development snapshot

Base: `bada120` on `dev/merge-vendor-1.0.11`.
Distribution: development Git branch `codex/dev-4.2`; no release tag or GitHub Release.

## Included

- Supplier SDK 1.0.11 integration, including the previously updated xvxrlib and xvsdk AARs.
- Singray G2 product APIs, native and simulation backends, sensor diagnostics and validation tools.
- MRTK2 default scene, SampleHub and Sensor calibration tooling.
- Android API 30/35, Singray branding, Unity splash screen disabled and Git-sourced Unity MCP.

The supplier `VersionInfo` remains 1.0.11 to distinguish it from the product development version 4.2.0-dev.1.

## Validation

Validation attempted on 2026-09-29 with Unity 2022.3.62f2c1 in a separate clean worktree.

- Git LFS fsck passed. All four local MRTK tarballs were hydrated and opened successfully with tarfile.
- Android build did not reach script compilation: Unity Package Manager could not fetch the locked Unity MCP revision from GitHub over HTTPS (connection failure).
- The first attempt also saw unhydrated LFS tarballs; these were subsequently hydrated and verified. A successful Android build is still required.
- No APK is distributed with this snapshot. No new physical-device regression results are claimed.
- Existing Play Mode test source is gated by UNITY_INCLUDE_TESTS and SINGRAY_G2_ENABLE_TESTS; it was not executed for this snapshot.

This branch is suitable for sharing source for development and follow-up validation, not as a build-verified or production-ready SDK.

## Scope

The separate working directory on release_version_4.1.1 contains uncommitted changes. They are deliberately excluded from this snapshot, as requested. Historical validation reports refer to their recorded commits and binaries, not automatically to this development version.
