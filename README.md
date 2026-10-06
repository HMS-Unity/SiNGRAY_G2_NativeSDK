# SiNGRAY G2 Native SDK

Native SDK and Unity integration for SiNGRAY G2 AR devices, including Android native libraries, MRTK2 integration, sensor APIs, and sample applications.

## Documentation

- [Demo and User Guide](Docs/Demo-and-User-Guide.md): environment setup, demo scenes, Android builds, device operation, integration, and troubleshooting.
- [API Reference](Docs/API-Reference.md): product interfaces, sensor settings, events, data types, Foundation feature managers, and native interop guidance.

## Development branch

The `dev/4.2` branch contains the `4.2.0-dev.1` development project. Use Unity `2022.3.62f2c1` with Android Build Support. The current Android configuration uses IL2CPP, ARM64, minimum API 30, and target API 35.

```sh
git lfs install
git clone --branch dev/4.2 git@github.com:HMS-Unity/SiNGRAY_G2_NativeSDK.git
cd SiNGRAY_G2_NativeSDK
git lfs pull
git lfs fsck
```

Open the repository root in Unity Hub and allow package import to finish. The current enabled build scene is `Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer.unity`. For the categorized demo hub, open `Assets/XRFoundation/SampleScenes/SDKSampleHub/Scenes/SDKSamples.unity` and configure it as the entry scene in Build Settings.

This is a development build. Editor simulation does not validate native device behavior; test the selected features on the target G2 hardware and firmware before deployment.
