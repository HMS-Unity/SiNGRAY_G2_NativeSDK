# 供应商 SDK 合入清单

- 供应商：`E:\UGit\origin\xvxr-plugin-unity-main_20260820`（VersionInfo **1.0.11**）
- 自己的：`E:\UGit\origin\singray_Native_sdk`
- Unity：两边都是 **2022.3.62f2c1**
- 路径已按 `XRFoundation ↔ XvXRFoundation`、`Foundation ↔ XvFoundation`、`Sdk/XR ↔ XvSdk/XvXR` 对齐
- 勾选：`[ ]` 未做 / `[x]` 已完成
- 拷贝时一律带上对应 `.meta`

---

## 0. 合入原则

- [ ] 已备份 / 已 commit `singray_Native_sdk`
- [ ] **不要整包覆盖**供应商工程
- [ ] 保持自己的目录名 `Assets/XRFoundation`（推荐），只按对应路径拷文件
- [ ] 不要覆盖 `SDKSampleHub`、`Sensor.unity`、包名 `com.singray.Sensor`
- [ ] 合完检查 Layer `BGVideo` 还在

建议顺序：原生库 → 新增核心代码 → 覆盖已改 C# → 场景/Prefab → Android 设置（保留包名）→ 真机验收。

---

## 1. P0 原生库（先做）

- [ ] 覆盖 `Assets/Plugins/Android/Libs/arm64-v8a/libxv-wrapper.so`（1.5MB → 35.9MB）
- [ ] 一并覆盖 `libxv-wrapper.so.meta`
- [ ] 覆盖 `Assets/XRFoundation/Core/Sdk/Xslam/Plugins/Android/xvsdk-release.aar`（161MB → 180MB）
- [ ] 把 `xvxrlib-release.aar` 从 `Xslam/Plugins/Android` **挪到** `Sdk/XR/Plugins/Android` 并覆盖（24.7MB → 24.9MB）
- [ ] 删除旧位置 `Xslam/Plugins/Android/xvxrlib-release.aar`（避免双份）
- [ ] 覆盖 `Core/Sdk/Xslam/Plugins/Android/XVisioSDKDemo.java`（3814 → 3840）
- [ ] 覆盖 `Core/Sdk/Joystick/Plugins/Android/AndroidSensorPhone.java`（4157 → 4564）
- [ ] 覆盖 `Core/Sdk/Joystick/Plugins/Android/ISensorPhoneListener.java`（117 → 135）

以下 AAR 大小相同，可不换：`handex-release` / `hand-release` / `jna` / `XXPermissions-12.6` / `libausbc` / `libnative` / `libuvc` / `libuvccommon`。

---

## 2. P0 新增核心代码

- [ ] 新增 `Core/XvFoundation/Native/XvNativeAPI.cs`（xv-wrapper P/Invoke）
- [ ] 新增 `Core/XvFoundation/Common/Scripts/VersionInfo.cs`（1.0.11）
- [ ] 新增 `Core/XvSdk/XvXR/XvXRScripts/Engine/XvCenterManager.cs`
- [ ] 新增 `Core/XvSdk/XvXR/Resources/XvXRDepthOnly.shader`
- [ ] 新增 `Core/XvSdk/XvXR/Meterial/MRTColorDepth.shader`
- [ ] 新增 `Assets/SlamConfidenceControl.cs`（可放到 Samples）

---

## 3. 供应商新增文件（非 DOTween）

### Editor

- [ ] `Core/Editor/AssetSelectPopUpWindow.cs`
- [ ] `Core/Editor/Tools/AssetBundleEditor.cs`
- [ ] `Core/Editor/Tools/Function.cs`
- [ ] `Core/Editor/Tools/ManifestHelper.cs`

### AITalk

- [ ] `Core/XvFoundation/AITalk/Scripts/XvAITalk.cs`
- [ ] `SampleScenes/XvAITalk/Scripts/XvAITalkDemo.cs`
- [ ] `SampleScenes/XvAITalk/Scenes/XvAITalk.unity`

### SeerPad / 手柄（整夹拷）

- [ ] `Core/XvFoundation/Joystick/Scripts/SeerPadApplyHandleController.cs`
- [ ] `Core/XvFoundation/Joystick/Scripts/XvLeftHandleController.cs`
- [ ] `Core/XvFoundation/Joystick/Scripts/XvRightHandleController.cs`
- [ ] `Core/XvFoundation/Joystick/Scripts/XvSeerPadController.cs`
- [ ] `Core/XvFoundation/Joystick/Scripts/XvSeerPadJoystick.cs`
- [ ] `SampleScenes/Joystick/Scripts/XvJoystickDemo.cs`
- [ ] `SampleScenes/Joystick/Scripts/BlueTeethControl.cs`
- [ ] `SampleScenes/Joystick/Scenes/Joystick.unity`
- [ ] `SampleScenes/Joystick/Scenes/SeerPadController.unity`
- [ ] `SampleScenes/Joystick/Prefabs/Env.prefab`
- [ ] `SampleScenes/Joystick/Prefabs/XvJoystickDemo.prefab`
- [ ] `SampleScenes/Joystick/GameAssets/Mode/gamepad.fbx`
- [ ] `SampleScenes/Joystick/GameAssets/Mode/Joystick.prefab`
- [ ] `SampleScenes/Joystick/GameAssets/Materials/gamepadmat 1.mat`
- [ ] `SampleScenes/Joystick/GameAssets/Materials/gamepadmat 2.mat`
- [ ] `SampleScenes/Joystick/GameAssets/Materials/gamepadmat 3.mat`
- [ ] `SampleScenes/Joystick/GameAssets/Materials/gamepadmat L.mat`
- [ ] `SampleScenes/Joystick/GameAssets/Materials/ShaderBallNearPlaneFade.mat`
- [ ] `SampleScenes/Joystick/GameAssets/Font/lt SDF.asset`
- [ ] `SampleScenes/Joystick/GameAssets/Font/txt.txt`

### EyeTrackingNew

- [ ] `SampleScenes/EyeTrackingNew/Scenes/EyeTrackingNew.unity`
- [ ] `SampleScenes/EyeTrackingNew/Scripts/XvEyeInit.cs`
- [ ] `SampleScenes/EyeTrackingNew/Scripts/XvEyeFlowManage.cs`
- [ ] `SampleScenes/EyeTrackingNew/Scripts/ETInteraction.cs`
- [ ] `SampleScenes/EyeTrackingNew/Scripts/GetEyeImage.cs`
- [ ] `SampleScenes/EyeTrackingNew/Scripts/ResetSlamTest.cs`
- [ ] `SampleScenes/EyeTrackingNew/Materials/greenCube.mat`
- [ ] `SampleScenes/EyeTrackingNew/Sprites/pupilbox.png`
- [ ] `SampleScenes/EyeTracking/Materials/sphere.shader`
- [ ] `SampleScenes/EyeTracking/Materials/Custom_sphere.mat`

### 其它新 Sample

- [ ] `SampleScenes/Stm/Stm.unity`
- [ ] `SampleScenes/Stm/StmControl.cs`
- [ ] `SampleScenes/TofPointCloud/Scenes/AR301-3D.unity`
- [ ] `SampleScenes/TofPointCloud/Scenes/TofPointCloud_SZ.unity`
- [ ] `SampleScenes/SDKSamples/Scenes/IRToWorld.unity`
- [ ] `SampleScenes/SDKSamples/Scenes/Joystick.unity`
- [ ] `SampleScenes/Rgbd/Prefabs/RgbImage.prefab`
- [ ] `Core/XvFoundation/MRTK2/Prefabs/Hands/HandAxes.prefab`
- [ ] `Core/XvFoundation/MRTK2/Prefabs/Hands/HandJoint.prefab`
- [ ] `Core/XvFoundation/MRTK2/Prefabs/Hands/HandJointGizmo.prefab`
- [ ] `Core/XvFoundation/MRTK2/Prefabs/Hands/HandJointSmall.prefab`
- [ ] `Core/XvFoundation/MRTK2/Prefabs/Hands/HandJointSphere.prefab`

### Viewer 试验场景（可后置）

- [ ] `SampleScenes/Viewer/Scenes/Cmr.unity`
- [ ] `SampleScenes/Viewer/Scenes/Ocr.unity`
- [ ] `SampleScenes/Viewer/Scenes/Ocr 1.unity`
- [ ] `SampleScenes/Viewer/Scenes/Viewer2.unity`
- [ ] `SampleScenes/Viewer/Scenes/Viewer_t.unity`
- [ ] `SampleScenes/Viewer/Scenes/Scenes/Viewer.unity`
- [ ] `SampleScenes/Viewer/Scenes/Scripts/ViewerAutoControl.cs`

---

## 4. 内容已变的 C#（默认用供应商覆盖）

字节差为正 = 供应商更大。

### Editor

- [ ] `Core/Editor/BuildSettingWidows.cs`  你=2315 供应商=4976 差=+2661
- [ ] `Core/Editor/ProjectBuild.cs`  你=25527 供应商=39155 差=+13628（若你改过打包脚本，先 diff）
- [ ] `Core/Editor/SDKLayerSetup.cs`  你=2109 供应商=2319 差=+210（合完核对 Layer `BGVideo`）
- [ ] `Core/Editor/XvXRFoundation.cs`  你=16622 供应商=16514 差=-108  **覆盖后把 MenuItem 改回 `GameObject/Singray XR/...`**

### Foundation / Bluetooth · Common

- [ ] `Core/Foundation/Bluetooth/Scripts/BluetoothManager.cs`  7605 → 9216
- [ ] `Core/Foundation/Common/Scripts/CSVFile.cs`  6032 → 7159
- [ ] `Core/Foundation/Common/Scripts/FrameRateCounter.cs`  1515 → 1519
- [ ] `Core/Foundation/Common/Scripts/HandInputManager.cs`  4978 → 4980
- [ ] `Core/Foundation/Common/Scripts/MyDebugTool.cs`  1363 → 1366

### Foundation / EyeTracking · Gaze

- [ ] `Core/Foundation/EyeTracking/Scripts/Base/XvEyeTracking.cs`  8540 → 13665  **P0**
- [ ] `Core/Foundation/EyeTracking/Scripts/XvEyeTrackingManager.cs`  14682 → 21775  **P0**
- [ ] `Core/Foundation/Gaze/Scripts/UserInputEvent/Scripts/Base/XvInputControllerBase.cs`  3762 → 4332
- [ ] `Core/Foundation/Gaze/Scripts/UserInputEvent/Scripts/Base/XvRaycaster.cs`  9891 → 10563
- [ ] `Core/Foundation/Gaze/Scripts/UserInputEvent/Scripts/Base/XvXRInputModule.cs`  10127 → 12539
- [ ] `Core/Foundation/Gaze/Scripts/UserInputEvent/Scripts/Controller/XvHandInputController.cs`  2710 → 2938
- [ ] `Core/Foundation/Gaze/Scripts/XvGazeButton.cs`  1601 → 1600
- [ ] `Core/Foundation/Gaze/Scripts/XvHeadGazeInputController.cs`  3885 → 4114

### Foundation / Joystick · Keyboard

- [ ] `Core/Foundation/Joystick/Scripts/XvJoystickManager.cs`  15809 → 21062  **P0**
- [ ] `Core/Foundation/Joystick/Scripts/XvHandleController.cs`  4148 → 1294  **类被拆成左右手柄，检查业务引用**
- [ ] `Core/Foundation/Keyboard/Scripts/CustomInputField.cs`  7417 → 7451
- [ ] `Core/Foundation/Keyboard/Scripts/InputContentPanel.cs`  11107 → 11209
- [ ] `Core/Foundation/Keyboard/Scripts/MRKeyboard.cs`  11494 → 11681
- [ ] `Core/Foundation/Keyboard/Scripts/PinYin.cs`  3928 → 3930

### Foundation / MediaRecorder · MRVideoCapture

- [ ] `Core/Foundation/MediaRecorder/XvMediaRecorder.cs`  7762 → 9433
- [ ] `Core/Foundation/MediaRecorder/XvMediaRecorderManager.cs`  5905 → 4425
- [ ] `Core/Foundation/MediaRecorder/XvMedioRecordTips.cs`  1203 → 1406
- [ ] `Core/Foundation/MediaRecorder/NatCorder/Examples/JPG/JPG.cs`  3824 → 3960
- [ ] `Core/Foundation/MediaRecorder/NatCorder/Examples/ReplayCam/ReplayCam.cs`  6367 → 29828  （示例，可后置）
- [ ] `Core/Foundation/MRVideoCapture/Scripts/XvMRVideoCaptureManager.cs`  7496 → 11747

### Foundation / Plane · RGBD · RTSP · Spatial · Speech · Gesture · System · Tag

- [ ] `Core/Foundation/PlaneDetection/Scripts/XvPlaneManager.cs`  6542 → 5760
- [ ] `Core/Foundation/PlaneDetection/Scripts/XvPlaneMeshVisualizer.cs`  4887 → 5368
- [ ] `Core/Foundation/Rgbd/Scripts/XvRgbdManager.cs`  5244 → 4142
- [ ] `Core/Foundation/RTSPStreamer/Scripts/XvRTSPStreamerManager.cs`  7199 → 8206
- [ ] `Core/Foundation/SpatialMap/Scripts/XvSpatialMapManager.cs`  4569 → 6839
- [ ] `Core/Foundation/SpatialMesh/Scripts/XvSpatialMeshManager.cs`  7177 → 8192
- [ ] `Core/Foundation/SpatialMesh/Scripts/XvSpatialMeshVisualizer.cs`  5473 → 5877
- [ ] `Core/Foundation/SpeechVoice/Scripts/XvSpeechVoiceManager.cs`  10446 → 13696
- [ ] `Core/Foundation/SpeechVoice/Scripts/XvAitalkModels.cs`  788 → 840
- [ ] `Core/Foundation/StaticGesture/Scripts/XvStaticGestureManager.cs`  11261 → 11247
- [ ] `Core/Foundation/SystemSetting/Base/XvSystemSetting.cs`  1357 → 1244
- [ ] `Core/Foundation/SystemSetting/XvSystemSettingManager.cs`  3071 → 3017
- [ ] `Core/Foundation/TagRecognizer/Scripts/Base/XvAprilTag.cs`  6541 → 7848
- [ ] `Core/Foundation/TagRecognizer/Scripts/XvTagRecognizerBehavior.cs`  4942 → 5444
- [ ] `Core/Foundation/TagRecognizer/Scripts/XvTagRecognizerManager.cs`  10333 → 10533

### Foundation / Viewer

- [ ] `Core/Foundation/Viewer/Scripts/XvCameraManager.cs`  18860 → 17411
- [ ] `Core/Foundation/Viewer/Scripts/Base/XvARCamera.cs`  11183 → 14516
- [ ] `Core/Foundation/Viewer/Scripts/Base/XvTofCamera.cs`  3521 → 3775
- [ ] `Core/Foundation/Viewer/Scripts/Base/XvTofIRCamera.cs`  4924 → 5520
- [ ] `Core/Foundation/Viewer/Scripts/Base/XvWebCamera.cs`  2997 → 3002
- [ ] `Core/Foundation/Viewer/Scripts/Base/Manager/XvARCameraManager.cs`  1719 → 1710
- [ ] `Core/Foundation/Viewer/Scripts/Base/Manager/XvStereoCameraManager.cs`  2769 → 2756
- [ ] `Core/Foundation/Viewer/Scripts/Base/Manager/XvTofManager.cs`  8749 → 8673
- [ ] `Core/Foundation/Viewer/Scripts/Base/Manager/XvWebCameraManager.cs`  1609 → 1627

### Sdk / 引擎 · 设备 · Android

- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvXREye.cs`  19021 → 31156  **P0**
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/Devices/XvXRAndroidDevice.cs`  14837 → 19504  **P0**
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/Devices/XvXRBaseDevice.cs`  18211 → 20991
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/Devices/XvXRMobileDevice.cs`  2942 → 3904
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvDeviceManager.cs`  10029 → 11616
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvXRData.cs`  2808 → 3076
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvXRHeadTracking.cs`  1483 → 1739
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvXRManager.cs`  12821 → 12934
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvXRPostRender.cs`  1810 → 1850
- [ ] `Core/Sdk/XR/XvXRScripts/Engine/XvXRSdkConfig.cs`  1075 → 1463
- [ ] `Core/Sdk/XR/XvXRScripts/Event/android/AndroidConnection.cs`  25771 → 30023
- [ ] `Core/Sdk/XR/XvXRScripts/Event/android/AndroidEvent.cs`  1516 → 1621
- [ ] `Core/Sdk/XR/XvXRScripts/Event/android/AndroidInterface.cs`  4001 → 4849
- [ ] `Core/Sdk/XR/XvXRScripts/Input/Hands/HandsManager.cs`  4411 → 4422
- [ ] `Core/Sdk/XR/XvXRScripts/Input/XvXRInput.cs`  5876 → 5939
- [ ] `Core/Sdk/XR/XvXRScripts/utils/UvcPluginWrapper.cs`  7709 → 8443
- [ ] `Core/Sdk/XR/XvXRScripts/utils/WifiDisplayPluginWrapper.cs`  4241 → 4771

### Sdk / Joystick · Xslam

- [ ] `Core/Sdk/Joystick/Scripts/XvXRJoystick.cs`  8886 → 15182  **P0**
- [ ] `Core/Sdk/Joystick/Scripts/XvXRJoystickManager.cs`  6447 → 10985  **P0**
- [ ] `Core/Sdk/Joystick/Scripts/XvXRJoystickController.cs`  13666 → 13708
- [ ] `Core/Sdk/Xslam/Scripts/API.cs`  43379 → 44844
- [ ] `Core/Sdk/Xslam/Scripts/Bar.cs`  792 → 857
- [ ] `Core/Sdk/Xslam/Scripts/LoadIMU.cs`  913 → 1197
- [ ] `Core/Sdk/Xslam/Scripts/ShowFPS.cs`  2361 → 2529

### Sample 脚本（可跟 Demo 一起后置）

- [ ] `SampleScenes/Bluetooth/Scripts/BluetoothDemo.cs`  17797 → 19272
- [ ] `SampleScenes/EyeTracking/Scripts/EyeImageDemo.cs`  2003 → 1980
- [ ] `SampleScenes/EyeTracking/Scripts/XvEyeCalibrationDemo.cs`  16283 → 15832
- [ ] `SampleScenes/EyeTracking/Scripts/XvEyeTrackingDemo.cs`  6113 → 6126
- [ ] `SampleScenes/IRToWorld/Scripts/IRToWorldDemo.cs`  4410 → 4133
- [ ] `SampleScenes/MRVideoCapture/Scripts/MRVideoCaptureDemo.cs`  1509 → 1125
- [ ] `SampleScenes/PlaneDetection/Scripts/PlaneDetectionDemo.cs`  11697 → 10542
- [ ] `SampleScenes/Rgbd/Scripts/XvRgbdDemo.cs`  3501 → 3123
- [ ] `SampleScenes/SDKSamples/Scripts/XvLoadScenesManager.cs`  1004 → 1020
- [ ] `SampleScenes/SpatialMap/Scripts/SpatialMapDemo.cs`  14355 → 14663
- [ ] `SampleScenes/SpatialMesh/Scripts/SpatialMeshDemo.cs`  10762 → 10752
- [ ] `SampleScenes/SpeechVoice/Scripts/XvSpeechVoiceDemo.cs`  3840 → 3763
- [ ] `SampleScenes/StaticGesture/Scripts/XvStaticGestureDemo.cs`  2204 → 2251
- [ ] `SampleScenes/SystemSetting/Scripts/XvSystemSettingDemo.cs`  5485 → 6737
- [ ] `SampleScenes/TagRecognizer/Scripts/XvTagRecognizerUIController.cs`  4315 → 4456
- [ ] `SampleScenes/TofPointCloud/Scripts/XvParticlesCloudPoint.cs`  5849 → 9560
- [ ] `SampleScenes/TofPointCloud/Scripts/XvPointCloudDemo.cs`  4001 → 9876
- [ ] `SampleScenes/Viewer/Scripts/XvCameraDemo.cs`  8103 → 10917
- [ ] `SampleScenes/Wifi/Scripts/WifiControlDemo.cs`  8177 → 8005

---

## 5. 内容已变的场景 / Prefab / 资源

### 必换

- [ ] `Core/Foundation/Joystick/Resources/XvJoystickManager.prefab`  7752 → 613834  **旧 prefab 已不是同一套**
- [ ] `SampleScenes/TagRecognizer/Scenes/TagRecognizer.unity`  43033 → 484897  **场景几乎重做**
- [ ] `SampleScenes/SystemSetting/Scenes/SystemSetting.unity`  25548 → 50319
- [ ] `SampleScenes/SDKSamples/Prefabs/XvLoadScenesDemo.prefab`  146275 → 164533（会多 Joystick / IRToWorld 入口）

### 覆盖前先 diff（你可能改过）

- [ ] `SampleScenes/SDKSamples/Scenes/BaseScene.unity`  61776 → 39755
- [ ] `SampleScenes/Viewer/Scenes/Viewer.unity`  33594 → 30391  **不要动 Sensor.unity**

### 其它场景

- [ ] `SampleScenes/Bluetooth/Prefabs/BluetoothDemo.prefab`  80582 → 83139
- [ ] `SampleScenes/EyeTracking/Scenes/EyeCalibration.unity`  19424 → 19055
- [ ] `SampleScenes/EyeTracking/Scenes/EyeTracking.unity`  19035 → 20534
- [ ] `SampleScenes/EyeTracking/Materials/mat.mat`  5494 → 5522
- [ ] `SampleScenes/IRToWorld/Scenes/IRToWorld.unity`  170895 → 171008
- [ ] `SampleScenes/MediaRecorder/Scenes/MediaRecorder.unity`  95016 → 93701
- [ ] `SampleScenes/MRTK2/Scenes/MRTK2.unity`  988175 → 975181
- [ ] `SampleScenes/MRVideoCapture/Scenes/MRVideoCapture.unity`  25839 → 25275
- [ ] `SampleScenes/Rgbd/Scenes/Rgbd.unity`  43332 → 29310
- [ ] `SampleScenes/RTSPStreamer/Scenes/RTSPStreamer.unity`  26437 → 29770
- [ ] `SampleScenes/SDKSamples/Scenes/EyeCalibration.unity`  18796 → 18427
- [ ] `SampleScenes/SDKSamples/Scenes/Rgbd.unity`  16600 → 20395
- [ ] `SampleScenes/SDKSamples/Scenes/TofPointCloud.unity`  18501 → 18680
- [ ] `SampleScenes/SDKSamples/Scenes/Viewer.unity`  15079 → 19170
- [ ] `SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceDemo.prefab`  58108 → 59890
- [ ] `SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity`  40299 → 35339
- [ ] `SampleScenes/TofPointCloud/Scenes/TofPointCloud.unity`  26134 → 26254
- [ ] `SampleScenes/Wifi/Scenes/Wifi.unity`  58897 → 57988

### 核心 Prefab / 材质（跟脚本一起覆盖）

- [ ] `Core/Foundation/MediaRecorder/Resources/XvMediaRecorder.prefab`  225503 → 235196
- [ ] `Core/Foundation/MRVideoCapture/Resources/XvMRVideoCaptureManager.prefab`  8971 → 8972
- [ ] `Core/Foundation/Viewer/Prefabs/XvCameraManager.prefab`  1437 → 1731
- [ ] `Core/Sdk/XR/Resources/XvXRManager.prefab`  20713 → 21300
- [ ] `Core/Sdk/XR/Resources/XvXRInput.prefab`  4044 → 3909
- [ ] `Core/Sdk/XR/Resources/Cheese.prefab`  29234 → 28281
- [ ] `Core/Sdk/XR/Resources/RgbObject.prefab`  10798 → 10430
- [ ] `Core/Sdk/XR/Resources/ThrowScene.prefab`  40885 → 39741
- [ ] `Core/Sdk/XR/Resources/UvcPlugin.prefab`  1388 → 1340
- [ ] `Core/Sdk/XR/Meterial/Grayscale.mat` / `play.mat` / `RgbBackground.mat` / `YUV2RGBBackground.mat`
- [ ] `Core/Sdk/XR/Meterial/GrayscaleShader.shader` / `YUV420ToRGB.shader`
- [ ] `Core/Sdk/MRTK/Profiles/XvXR Hand MixedRealityHandTrackingProfile.asset`  1102 → 1080

---

## 6. 必须保留（禁止用供应商覆盖）

- [ ] 保留 `SampleScenes/SDKSampleHub/` 整目录
- [ ] 保留 `SampleScenes/SDKSampleHub/Editor/SdkSamplesHubSceneBuilder.cs`
- [ ] 保留 `SampleScenes/SDKSampleHub/Scripts/ISdkSampleLifecycle.cs`
- [ ] 保留 `SampleScenes/SDKSampleHub/Scripts/MrtkKeyboardSample.cs`
- [ ] 保留 `SampleScenes/SDKSampleHub/Scripts/SdkSamplesHubController.cs`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/KeyboardSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/MediaRecorderSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/MRTK2SampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/MRVideoCaptureSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/PlaneDetectionSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/RgbdSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/RTSPStreamerSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/SpatialMapSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/SpatialMeshSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/StaticGestureSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/SystemSettingSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/TagRecognizerSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/TofPointCloudSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Modules/ViewerSampleModule.prefab`
- [ ] 保留 `SampleScenes/SDKSampleHub/Scenes/SDKSamples.unity`
- [ ] 保留 `SampleScenes/Viewer/Scenes/Sensor.unity`
- [ ] 保留 `SampleScenes/Viewer/Scripts/XvSensorCalibrationDump.cs`
- [ ] 保留 `Core/Foundation/MediaRecorder/NatCorder/Plugins/iOS/libNatCorder.a`
- [ ] 保留编辑器菜单 `GameObject/Singray XR/...`
- [ ] 保留包名 `com.singray.Sensor`、公司名 singray、产品名 Sensor
- [ ] 保留 TagManager Layer 6 `BGVideo`（供应商没有这层）
- [ ] 保留 README / git / Exports
- [ ] 保留 Packages 里的 `com.unity.feature.development`（不必改成 VS/VSCode 包）

`Core/Sdk/Xslam/Plugins/Android/xvxrlib-release.aar` 不是要保留旧文件，而是 **挪走后删旧位置**（见第 1 节）。

---

## 7. Android / ProjectSettings

- [ ] `AndroidManifest.xml`：权限对齐；`launchMode` 改为供应商的 `singleTask`；versionName 继续用自己的
- [ ] `mainTemplate.gradle`：可跟供应商（compileSdk 32 + skipSystemPropertyCheck）
- [ ] `launcherTemplate.gradle`：可跟供应商（compileSdk 32）
- [ ] `LauncherManifest.xml`：仅空白差异，可忽略
- [ ] minSdk/targetSdk：供应商 30/35；设备允许再抬，否则先保持 28/33
- [ ] applicationIdentifier **保持** `com.singray.Sensor`
- [ ] EditorBuildSettings **保持** SDKSamples + Sensor，不要改成只打 MRTK2
- [ ] GraphicsSettings 先不换
- [ ] Unity 版本不用升

---

## 8. 可后置（第二批）

- [ ] `ThirdParty/Demigiant` DOTween + DOTweenPro（约 143 个文件）
- [ ] `Core/Font/fontMedical.asset`（2MB → 37MB）
- [ ] `Core/Font/txt.txt`
- [ ] NatCorder 字体 `lt SDF.asset`（2.3MB → 9.7MB）
- [ ] `ReplayCam.cs` 示例膨胀
- [ ] TMP `LiberationSans SDF - Fallback.asset`
- [ ] StreamingAssets/Assetbundles 空目录
- [ ] Viewer 试验场景：Ocr / Cmr / Viewer2 / Viewer_t（见第 3 节）

---

## 9. 真机验收

### P0

- [ ] 启动 / 6DoF 头追稳定
- [ ] SLAM confidence 能读到
- [ ] RGB / TOF 出图
- [ ] Sensor 标定 dump 仍可用
- [ ] 旧眼动 EyeTracking + 新 EyeTrackingNew
- [ ] 左右手柄 / SeerPad
- [ ] 静态手势
- [ ] SDKSampleHub 每个 Module 还能进

### P1

- [ ] 空间网格 / 平面 / SpatialMap
- [ ] Tag / AprilTag（场景几乎重做）
- [ ] 语音 SpeechVoice + AITalk
- [ ] MediaRecorder / MRVideoCapture
- [ ] RTSP / Wifi / 蓝牙
- [ ] TOF 点云 + AR301-3D / SZ

### P2

- [ ] 键盘拼音
- [ ] STM 地磁
- [ ] 打 APK：USB 权限、OpenCL native library

---

## 10. 合完自检

- [ ] Unity 编译无报错
- [ ] 无 Missing Script（尤其手柄 Prefab、XvXRManager）
- [ ] TagManager 仍有 Layer `BGVideo`
- [ ] 菜单仍是 Singray XR
- [ ] 包名仍是 `com.singray.Sensor`
