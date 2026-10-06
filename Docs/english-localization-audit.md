# English localization audit

Date: 2026-09-29. Project: E:/UGit/origin/singray_Native_sdk, branch dev/4.2.

## Applied changes

Translated Chinese comments, Inspector labels, editor menus, runtime messages and sample UI text into English. Covered 127 source/scene/prefab files (94 C#, 3 Java, 3 shaders, 6 scenes, 21 prefabs), plus one material rename. Chinese display strings stored as Unicode escapes in Unity YAML were decoded for inspection and translated. Legacy GB18030 source files that changed were saved as UTF-8.

Renamed Gaze/Materials/Image.mat and its .meta together through Unity; the previous filename was Chinese. Although the MCP move response reported an error, on-disk verification confirmed the move, English material name, and unchanged .meta/GUID. No shader or texture references were changed.

Existing user edits to VersionInfo.cs and EditorBuildSettings.asset were not overwritten. XvDeviceManager.cs retained the user's existing code while its Chinese text was translated. No commits or pushes were performed for this localization pass.

## Validation

- A before/after lexical check confirmed that executable code structure was unchanged in all inspected C#/Java/shader files after excluding comments, region labels and string literal contents.
- Scene/prefab edits were confined to quoted scalar contents; serialized keys, object IDs and GUID references were preserved by the translation operation.
- Re-scanning UTF-8, GB18030 and Unicode-escaped text found no Chinese or replacement characters in the scanned .cs, .java or .shader sources.
- Unity MCP selected singray_Native_sdk@f5667fbd at E:/UGit/origin/singray_Native_sdk. Requested asset refresh and script compilation; Unity recovered after domain reload and reported ready. The subsequent Console error query returned zero entries.
- This does not constitute an Android build, full visual layout pass or physical-device regression. Check long calibration instructions and the speech demo panel for wrapping/clipping on the device.
- Vendor AAR/SO binaries and embedded images/audio were not translated or OCR-audited. Native UI and runtime responses may still contain Chinese.

## Functional language data retained

The remaining Chinese text is functional data, not a failed translation of ordinary labels. Do not replace it with English indiscriminately:

1. Speech recognition commands: the supplier recognizer uses Chinese command words. The speech demo instructions are now English with phonetic command spellings, but this does not enable English recognition. An English speech model/grammar must be confirmed with the supplier, or the Chinese speech sample must be excluded from the customer build.
2. Pinyin dictionary: contains Chinese input candidates. Keyboard language labels are English, but selecting Chinese input can still produce Chinese text. Removing that mode is a separate behavior change.
3. Font source text and TMP characterSequence: these define supported glyphs and are not ordinary UI messages. Translating them would change font-generation inputs, not localize the application. Decide whether Chinese font support should be retained or removed from the customer package.

| File | Chinese-bearing lines | First line | Purpose |
|---|---:|---:|---|
| `Assets/XRFoundation/Core/Font/fontMedical.asset` | 1 | 628 | Font character set / generation source |
| `Assets/XRFoundation/Core/Font/txt.txt` | 152 | 3 | Font character set / generation source |
| `Assets/XRFoundation/Core/Foundation/Keyboard/Resources/Files/pinyin.csv` | 405 | 1 | Pinyin input dictionary |
| `Assets/XRFoundation/Core/Foundation/MediaRecorder/NatCorder/Font/lt SDF.asset` | 4 | 11349 | Font character set / generation source |
| `Assets/XRFoundation/Core/Foundation/MediaRecorder/NatCorder/Font/txt.txt` | 9 | 1 | Font character set / generation source |
| `Assets/XRFoundation/SampleScenes/Joystick/GameAssets/Font/lt SDF.asset` | 2 | 2116 | Font character set / generation source |
| `Assets/XRFoundation/SampleScenes/Joystick/GameAssets/Font/txt.txt` | 4 | 1 | Font character set / generation source |
| `Assets/XRFoundation/SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceManager.prefab` | 4 | 49 | Speech recognition grammar |
| `Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity` | 4 | 455 | Speech recognition grammar |

## Supplier wording that needs technical confirmation

The following English comments preserve the uncertainty in the supplier text; no native calls or parameter values were changed:

- Core/Foundation/EyeTracking/Scripts/Base/XvEyeTracking.cs:233, ipdDist: the original description calls it IPD but lists 200, 300, 500, 1000 and 10000 mm. Confirm whether it is a fixation-distance parameter rather than anatomical IPD.
- Same file:241, hiValue: the original description duplicates the display fx description. Confirm its actual meaning.
- Core/Sdk/Xslam/Scripts/API.cs:1014, cslamSwitchedCallback: the original description refers to a map path even though the parameter is a callback. Confirm the callback contract.
- Core/Sdk/XR/XvXRScripts/utils/WifiDisplayPluginWrapper.cs:93 and :106, and Core/Foundation/RTSPStreamer/Scripts/XvRTSPStreamerManager.cs:242: original comments disagree with the Boolean passed to setUseDLNA. The code values were preserved; comments flag the conflict.
- Previously corrupted comments in XvCameraDemo, XvPointCloudDemo, XvMediaRecorderManager and XvMRVideoCaptureManager were reconstructed from neighboring code. Bluetooth HID examples retain command 021a9601. These are code-based interpretations rather than recovery of the original encoding.

## Remaining speech command locations

- `Assets/XRFoundation/SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceManager.prefab:49` — `word: "旋转"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceManager.prefab:66` — `word: "停止旋转"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceManager.prefab:83` — `word: "放大"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceManager.prefab:100` — `word: "缩小"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity:455` — `value: "旋转"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity:459` — `value: "放大"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity:463` — `value: "缩小"`
- `Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity:467` — `value: "关闭识别"`
