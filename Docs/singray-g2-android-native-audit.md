# Singray G2 Android 原生包离线审计

审计日期：2026-08-22

## 1. 审计范围

- 基线：`release_version_4.1.1`，提交 `b478f9857e1a45ab7c7dd91dd211546b92da2c46`
- 当前验证分支：`dev/merge-vendor-1.0.11`，提交 `11f257e93410b82b48c41140a618bea5633ed83b`
- 最终验证 APK：`build/Singray-G2-Validation-Simulation.apk`
- APK SHA-256：`01424E34D5A0E4569674DB2E2733A47557BAD82ED11D84797CF4A667C9334452`
- 本报告只记录可离线验证的 AAR、SO、Java class、ABI、ELF、C# P/Invoke、Manifest 和最终 APK 打包结果，不把模拟数据当成真机功能结论。

## 2. 顶层 Android 二进制变更

工程共发现 13 个顶层 Android 二进制包。与 4.1.1 比较，10 个完全相同，3 个发生变化。

| 文件 | 4.1.1 | 当前分支 | 结论 |
|---|---:|---:|---|
| `libxv-wrapper.so` | 1,593,752 B | 35,901,544 B | 内容变化，增大约 22.5 倍 |
| `xvsdk-release.aar` | 160,861,861 B | 180,481,294 B | 内容变化，增加约 18.7 MiB |
| `xvxrlib-release.aar` | 24,679,614 B | 24,908,366 B | 内容变化，并从 `Xslam/Plugins/Android` 移到 `XR/Plugins/Android` |

SHA-256：

| 文件 | 4.1.1 SHA-256 | 当前 SHA-256 |
|---|---|---|
| `libxv-wrapper.so` | `E46E766D99DC21F86EEC54CD6B48BBC2C55CB0523CD7E1C44732B42ABC31AF55` | `BABDCA67799C2397845C5728FC25047A76CE45EE84569C03724819C65F247E8A` |
| `xvsdk-release.aar` | `6C1127A5F8922E3C1C267E1B1075953600957B0454FFB48A64CA774F9278AFB0` | `8B7E8A0559E5C39CF5E0F5F6665DA26C63F2F10C85A54C2D7CEFBC0B53DC15F1` |
| `xvxrlib-release.aar` | `9831AE7AC49D117CE7C72F72FC8D91CA538BEAC36826405E313DA012DBA55827` | `3CD3B9E375CAE7E27F5A294AA75A8F50F2F04485AF43FF6D47FC45AB0208DE6A` |

完全未变的 10 个包：

- `hand-release.aar`
- `handex-release.aar`
- `jna.aar`
- `libausbc-release.aar`
- `libnative-release.aar`
- `libuvc-release.aar`
- `libuvccommon-release.aar`
- `NatCorder.aar`
- `NatRender.aar`
- `XXPermissions-12.6.aar`

## 3. AAR 内部精确变更

### 3.1 xvsdk-release.aar

原生库有 2 个新增、11 个修改，没有删除：

| 状态 | arm64-v8a 文件 | 4.1.1 大小 | 当前大小 |
|---|---|---:|---:|
| 新增 | `libcmrImageTrackerCore.so` | - | 8,613,712 B |
| 新增 | `libcmrModelTrackerCore.so` | - | 42,053,472 B |
| 修改 | `libwirelessController.so` | 1,587,808 B | 1,595,784 B |
| 修改 | `libxslam_wrapper.so` | 1,178,152 B | 1,190,312 B |
| 修改 | `libxslam-edge-sdk.so` | 1,120,760 B | 1,120,760 B |
| 修改 | `libxslam-hid-sdk.so` | 1,833,512 B | 1,899,056 B |
| 修改 | `libxslam-unity-wrapper.so` | 8,205,928 B | 18,550,080 B |
| 修改 | `libxslam-usb-sdk.so` | 1,260,056 B | 1,268,248 B |
| 修改 | `libxslam-uvc-sdk.so` | 1,354,264 B | 1,509,928 B |
| 修改 | `libxslam-vsc-sdk.so` | 2,398,736 B | 2,398,736 B |
| 修改 | `libxslam-xv-sdk.so` | 6,618,024 B | 6,675,648 B |
| 修改 | `libxvuvc.so` | 88,920 B | 89,008 B |
| 修改 | `libxvxr_client.so` | 183,352 B | 185,984 B |

Java 层仍为 45 个 class：42 个未变，3 个变化：

- `org.xv.xvsdk.BuildConfig`
- `org.xv.xvsdk.XCamera`
- `org.xv.xvsdk.XCamera$1`

`XCamera` 新增公开方法 `getXvsdkVersion()`，其他已存在的公开方法签名未删除。

资源和配置还有 4 项变化：`AndroidManifest.xml`、`assets/config.properties`、`res/values/values.xml`、`res/xml/usb_filter.xml`。SDK 资源中的版本文本仍写成 `1.0.1`，构建时间由 `2026-06-02 15:09:34` 更新为 `2026-08-14 10:31:50`，未找到与“1.0.11”对应的可验证版本号。

### 3.2 xvxrlib-release.aar

原生库有 8 个修改，没有新增或删除：

| ABI | 文件 | 4.1.1 大小 | 当前大小 |
|---|---|---:|---:|
| arm64-v8a | `libble_wrapper.so` | 6,088,184 B | 6,152,072 B |
| arm64-v8a | `libTwoDShowPlugin.so` | 5,904 B | 5,968 B |
| arm64-v8a | `libUvcPlugin.so` | 5,904 B | 5,968 B |
| arm64-v8a | `libWifiDisplayPlugin.so` | 177,936 B | 186,192 B |
| arm64-v8a | `libXvXRRenderPlugin.so` | 3,370,832 B | 3,473,920 B |
| armeabi-v7a | `libTwoDShowPlugin.so` | 13,864 B | 13,904 B |
| armeabi-v7a | `libUvcPlugin.so` | 13,864 B | 13,904 B |
| armeabi-v7a | `libWifiDisplayPlugin.so` | 116,264 B | 120,400 B |

Java 层由 528 个 class 变为 526 个：

- 新增 12 个 class，包含新的 `BootVideoSurfaceView`、BLE 写请求和新的 `XvMainActivity` 内部实现。
- 删除 14 个内部/匿名 class，主要属于 `XvARLauncher` 和旧的 `XvMainActivity` 内部实现。
- 74 个 class 字节码变化，包括 `XvMainActivity`、BLE、启动器、视频流和 Wi-Fi Display。
- 新增公开组件 `top.xv.xrlib.common.BootVideoSurfaceView`，用于启动视频 Surface 生命周期与尺寸恢复。
- 所有当前 AAR 之间未发现重复 Java class。

资源层有 29 项变化：Manifest、多个启动/显示布局、GLSL、合成器配置和时区文件发生变化；`bootani.mp4` 被替换，并新增 `bootanik1.mp4`、`hmsani.mp4`。`aar_build_time` 从 `2026-06-01 17:01:38` 更新为 `2026-08-14 10:31:51`。

## 4. ABI 与最终 APK

- 当前工程中的 AAR 同时存在 `arm64-v8a` 和少量 `armeabi-v7a` 内容。
- Unity `AndroidTargetArchitectures: 2` 表示只构建 ARM64。
- 最终 APK 只包含 `arm64-v8a`，没有把 AAR 中的 `armeabi-v7a` 打进去。
- APK 共打入 105 个 arm64 `.so`，未发现同名 native library 重复打包。
- APK 的 application id 为 `com.singray.Sensor`，`minSdk=28`，`targetSdk=33`，`versionName=0.1`，`versionCode=1`。
- 当前产物仍带有验证期名称和版本信息，不能直接作为正式 G2 发布包。

## 5. 16 KB 页大小检查

检查分为两层：

1. Android Build Tools 36.1 的 `zipalign -c -P 16 -v 4` 对最终 APK 检查通过。
2. 使用 Unity NDK 的 `llvm-readelf -lW` 检查每个 ELF 的 `LOAD` 段对齐。

最终 APK 的 105 个 `.so` 结果：

| 结果 | 数量 | 含义 |
|---|---:|---|
| 通过 | 23 | ELF 最小 `p_align` 为 `0x4000` 或 `0x10000` |
| 未通过 | 80 | ELF 最小 `p_align` 仍为 `0x1000`，即 4 KB |
| 非 ELF | 2 | `lib_gesture_xvision.so`、`libmodel_211110.so` 实际是算法/模型数据 |

主要未通过项包括：

- `libxv-wrapper.so`
- `libxslam-unity-wrapper.so`
- `libxslam-xv-sdk.so`
- `libXvXRRenderPlugin.so`
- `libble_wrapper.so`
- 手势相关 `libhandskeleton*.so`
- `libopencv_java4.so`
- USB/UVC、眼动、SLAM、SNPE 等大部分供应商 ELF

本次新增的 `libcmrImageTrackerCore.so` 和 `libcmrModelTrackerCore.so` 均为 `0x4000`，自身满足 16 KB；但它们所在的整体依赖链并未全部满足。

结论：ZIP 对齐通过不等于 ELF 兼容。若 G2 固定使用 4 KB 页大小系统，当前设备版本可能仍能运行；若要支持 Android 15 的 16 KB 设备或后续系统，必须要求供应商用支持 flexible page sizes 的 NDK/链接参数重新编译这些原生库。参考：[Android 16 KB page-size guidance](https://developer.android.com/guide/practices/page-sizes)。

## 6. C# DllImport 与原生导出符号

### 6.1 当前分支新引入的确定问题

1. `XvNativeAPI.rgb_set_exposure` 在 `libxv-wrapper.so` 中不存在。
   - C# 当前声明：`rgb_set_exposure`
   - 原生实际导出：`xv_rgb_set_exposure`
   - 该 C# 声明在 4.1.1 中不存在，是当前合并新增项。
   - 已在真机测试前修复：保留现有 C# 方法名，并设置 `EntryPoint = "xv_rgb_set_exposure"`。

2. 供应商源码片段列出的图像/模型跟踪入口在整个 APK 中不存在：
   - `xslam_image_tracker`
   - `xslam_stop_image_tracker`
   - `xslam_start_cmr_model_tracker`
   - `xslam_stop_cmr_model_tracker`

   复查源文件后确认，这四个 DllImport 目前位于 `/* ... */` 注释块中，并未编译，也没有被业务代码暴露或调用，因此不会在当前真机验证中触发 `EntryPointNotFoundException`。不过 AAR 新增的两个 CMR 核心库只导出 `cmrit*` / `cmr*` API，`libxslam-unity-wrapper.so` 仍未提供 Unity 桥接入口；在供应商补齐接口前，应继续把 CMR 功能视为“未接入”。

### 6.2 从 4.1.1 延续的潜在问题

`libxslam-unity-wrapper.so` 还有 4 个既有 C# 声明未找到同名导出：

- `pub_set_usr_eye_ready`（另一个库 `libvueta-and.so` 有同名符号，但 DllImport 指向错误）
- `xslam_stop_skeleton_with_cb`（原生存在近似名 `xslam_stop_slam_skeleton_with_cb`）
- `xv_get_sony_tof_image`
- `xv_rgb_device_get_rgba`（原生存在近似名 `xslam_device_get_rgba2`）

`libXvXRRenderPlugin.so` 的 25 个直接/常量 DllImport 中有 3 个未导出：

- `SetFrame`
- `SetUForecast`
- `updateCalibra`

其中 `updateCalibra` 在 `XvSystemSettingManager` 和 `XvDeviceManager` 中存在实际调用，因此属于真实的潜在运行时错误。现已把这三个入口改为安全包装：符号存在时正常调用，符号缺失时只记录一次明确诊断并返回失败/忽略请求，不再让应用因 `EntryPointNotFoundException` 中断。新渲染库反而新增了 `SetFrameTextureIds`、`SubmitFrame`，仍需要供应商说明新旧帧提交接口的迁移关系。

已确认匹配：

- `XvPlane` 使用的 3 个 `XvXRRenderPlugin` 入口全部存在。
- `TwoDShowPlugin`、`WifiDisplayPlugin` 当前声明的入口存在。
- NatCorder 的主要录制入口存在。

## 7. 动态依赖检查

最终 APK 的 ELF 共解析出 562 条 `NEEDED` 依赖关系。以下依赖不在 APK 内，必须由 G2 系统/供应商运行环境提供或由供应商补包：

- 多个 SNPE/HTP 库依赖 `libcdsprpc.so`。
- HTP Skel 库依赖 `libc++.so.1`、`libc++abi.so.1`。
- 手势库 `libhandskeleton.so`、`libhandskeleton_all.so` 依赖 `libopencv_core.so`、`libopencv_imgproc.so`、`libopencv_imgcodecs.so`，但 APK 只有 `libopencv_java4.so`。
- `libcalculator_skel.so` 依赖 `libgcc.so`。

其中 `libcdsprpc.so` 很可能是 G2/Qualcomm 系统侧能力，必须由供应商给出系统镜像前置条件；OpenCV 三个缺失 SONAME 与手势识别直接相关，优先要求供应商确认，否则手势库可能在加载阶段失败。

APK 还自行打包了名为 `liblog.so` 的库，与 Android 系统 `liblog.so` 同名。该项在 4.1.1 已存在，但建议供应商明确用途并改名或移除，避免动态链接解析歧义。

## 8. 包体积与符号优化

- APK 内 105 个 native library 的解压后总大小约 515.8 MiB，压缩后约 207.6 MiB。
- ELF 中可识别的 `.debug_*`、`.symtab`、`.strtab` 总计约 67.0 MiB（70,256,503 B）。
- `libxv-wrapper.so` 当前绝大部分增量来自调试信息；其大小由 1.59 MB 增至 35.90 MB。
- `libxslam-unity-wrapper.so` 当前含约 17.5 MB debug section，旧版约 7.25 MB。

建议供应商交付两套文件：APK 使用 strip 后的 release `.so`，符号服务器/归档保留带 build-id 的独立 debug symbol。不要由 Unity 工程临时 strip 后覆盖供应商原件，以免失去崩溃符号定位和版本可追溯性。

## 9. Manifest 与发布配置

- 最终 APK 合并后共有 49 个权限，包括 `INJECT_EVENTS`、`INTERACT_ACROSS_USERS_FULL`、`WRITE_SECURE_SETTINGS`、`MANAGE_USB`、`REBOOT`、`SHUTDOWN` 等系统/签名级权限。
- `xvxrlib-release.aar` 新旧版本均声明 53 个权限，因此权限膨胀不是本次供应商更新新增，但仍是 G2 发布必须明确的系统集成条件。
- 需要确认正式 G2 包是否为系统预装/系统签名应用；如果不是，大量权限不会被授予，并应从 Manifest 中按功能最小化。
- 当前 Manifest 已清理若干无效或重复权限，并把 Android 12+ 蓝牙/旧存储权限按 SDK 范围约束；最终仍需供应商提供“功能—权限—系统签名”矩阵。
- 当前 Gradle 模板已移除旧版 `com.android.support:appcompat-v7:28.0.0` 与 AndroidX 并存，改为由 Unity 注入 compile SDK/build tools/NDK 路径，这是正确方向。

## 10. 处理优先级

### P0：进入真机功能验证前修复

1. **已处理**：修正 `rgb_set_exposure` 到原生 `xv_rgb_set_exposure`。
2. **已处理**：`updateCalibra`、`SetFrame`、`SetUForecast` 增加缺失符号保护和诊断日志；功能替代关系仍需供应商确认，但不再阻断真机启动。
3. **已处理**：手势启动前预加载 `libhandskeleton_wrapper.so`，并检查 `libhandskeleton_all.so`、`libhandskeleton.so`；加载失败会输出 `[SINGRAY-G2][HAND]` 诊断，必需 wrapper 失败时不会继续启动手势。
4. **已确认不阻断**：CMR 四个缺失入口目前全部在注释块中，不参与编译；继续保持未接入状态，直到供应商提供 Unity 桥接 API。

### P1：发布兼容性

1. 要求所有会在 Android 进程加载的 arm64 ELF 重新编译为 16 KB page-size compatible。
2. 要求供应商提供 G2 系统侧 native 依赖清单，尤其是 `libcdsprpc.so` 和系统签名权限。
3. 要求提供正式的 SDK/AAR/SO 版本号、变更日志、构建 commit、NDK 版本和 SHA-256 清单；目前只能从资源中确认 2026-08-14 构建时间，不能验证“1.0.11”。

### P2：包体积与维护性

1. 交付 strip 后的 release SO，并单独保存 debug symbols。
2. 清理或改名自带 `liblog.so`。
3. 按 G2 实际启用功能裁剪 49 个权限和不使用的算法库，避免所有眼动、手势、SNPE、模型跟踪库全部进入单一 APK。
4. 正式发布前统一 `productName`、application id、`versionName` 和 `versionCode`，当前仍是 `Sensor / com.singray.Sensor / 0.1 / 1`。

## 11. 当前离线结论

- 供应商更新不是简单替换一个 SDK：它同时改变了主 native wrapper、XVSdk、XR 渲染/启动/蓝牙/视频层，并加入图像与模型跟踪核心。
- ARM64 打包和 APK ZIP 对齐正确，Java class 无重复冲突，APK能够完成构建。
- 原生入口不匹配已经增加修正或安全降级；CMR 跟踪 API 当前未编译、未接入。
- 16 KB ELF、系统级权限和设备侧动态依赖尚未满足通用发布条件。
- 当前代码侧 P0 已具备进入真机验证的条件；真机日志需要重点观察 `[SINGRAY-G2][HAND]`、`[SINGRAY-G2][RENDER]`、RGB 曝光以及手势启动返回值。16 KB 与供应商二进制依赖仍属于发布前问题。
