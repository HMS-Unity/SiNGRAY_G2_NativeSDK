# SDK English Wording Audit

## Scope and policy

The entire tracked customer SDK repository on `dev/4.2` is covered: source
comments, diagnostics, tooltips, serialized UI text, sample configuration,
documentation, filenames, integration tools and project metadata. This is a
repository-wide wording policy, not an RGB Demo-only check.

Customer-facing wording must be English. Existing approved functional language
data is retained, as explicitly requested by the user. English wording does not
mean deleting multilingual input or speech capabilities, rebuilding font atlases,
or translating text entered by customers. Private-use icon glyphs and third-party
binary data are not prose.

Run from the repository root:

```text
python Tools/Validation/check_sdk_english.py
python Tools/Validation/check_sdk_english.py --report Builds/sdk-english-audit.json
```

The checker scans every tracked filename and every recognized text file. It
reads UTF-8/BOM, UTF-16/BOM and legacy GB18030 text without replacement decoding.
It checks literal text, serialized Unicode escapes and HTML character references.
This prevents an escaped Unity label or an older source encoding from bypassing
a literal UTF-8 search. Undecodable expected text files fail the check.

Binary assets and vendor archive contents are not translated or rebuilt by this
audit. TextMeshPro line-breaking tables and existing icon codepoints remain data.
The gate detects East Asian script characters outside explicitly reviewed data;
it is not a natural-language grammar checker or an ASCII-only requirement.

## Changes in this pass

- Source comments and documentation were already English after the preceding
  Demo documentation update. Remaining Han text belongs to the reviewed data
  listed below or an ASCII Unicode-range validation expression.
- Normalize full-width colon/exclamation punctuation in English diagnostics and
  the `Similarity:`, `Log:` and `Size:` labels across 11 selected files.
- Normalize the keyboard diagnostic that was stored with a legacy GB18030 colon;
  the resulting source is ASCII. No API or keyboard dictionary behavior changes.
- Keep all serialized transforms, anchors, component references, asset GUIDs,
  Resources paths, font atlases, dictionaries and speech command bindings.
- Add the repository-wide English policy and reproducible checker so future
  comments, UI strings and documents receive the same check.

The existing Wi-Fi/Bluetooth publication exclusion remains in force. Those
modules are not modified or published by this pass. Their existing diagnostic
phrases contain English words; non-ASCII punctuation alone is not treated as a
Chinese-language sentence. No unrelated local edits are included.

## Approved functional and technical data

`Tools/Validation/sdk_language_data.json` records exact paths, reasons and approved
SHA256 hashes. Approval hashes normalize CRLF to LF so Git checkout policy does
not create platform-dependent failures. Changed data fails until its exemption
is deliberately reviewed. This task separately verifies raw byte preservation.
For serialized assets, only the specific vocabulary or glyph-sequence fields are
exempt; a new Chinese UI label elsewhere in the same asset still fails.

| Path | Retained purpose and boundary |
| --- | --- |
| `Assets/XRFoundation/Core/Font/txt.txt` | Existing font glyph repertoire input; whole file preserved |
| `Assets/XRFoundation/Core/Foundation/MediaRecorder/NatCorder/Font/txt.txt` | Legacy font glyph repertoire input; whole file preserved |
| `Assets/XRFoundation/Core/Foundation/Keyboard/Resources/Files/pinyin.csv` | Pinyin-to-character mapping, including its existing data header; whole file preserved |
| `Assets/XRFoundation/ThirdParty/TextMesh Pro/Resources/LineBreaking Leading Characters.txt` | Third-party Unicode line-breaking rules |
| `Assets/XRFoundation/ThirdParty/TextMesh Pro/Resources/LineBreaking Following Characters.txt` | Third-party Unicode line-breaking rules |
| `Assets/XRFoundation/Core/Font/fontMedical.asset` | Only the font-creation `characterSequence` field |
| `Assets/XRFoundation/Core/Foundation/MediaRecorder/NatCorder/Font/lt SDF.asset` | Only the font-creation `characterSequence` field |
| `Assets/XRFoundation/SampleScenes/SpeechVoice/Prefabs/XvSpeechVoiceManager.prefab` | Only existing `aitalkWords` vocabulary fields for the Mandarin model |
| `Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity` | Only existing speech-vocabulary overrides; existing callbacks unchanged |
| `Tools/Delivery/validate_delivery_review.py` | Only the ASCII Unicode-range assertion used to reject non-English documentation |

The SpeechVoice example stays in its existing disabled menu/build state. Its
English UI explains the existing model; translating the vocabulary would not
supply an English speech engine. No speech sample restoration or native model
change is performed.

## Verification and limits

The repository-wide gate passes for 1,704 tracked paths and 1,608 text files,
with ten explicitly approved functional/technical data entries. Nine controlled
language-gate checks pass, including literal/escaped/HTML/legacy-encoding cases,
a technical icon control, hash mismatch rejection, and a UI label outside an
approved font-data region. LF-only checkout compatibility is also checked.

Unity loads all four expected updated labels from the two prefabs and saved
scene. Android Player script compilation passes. Raw-byte preservation and
commit-scope evidence are recorded in `Docs/SDK-Iteration-Log.txt`.

Negative fixtures must reject literal non-English comments, escaped non-English
Unity labels and HTML-encoded non-English text. They must accept English text
with a technical icon and reject a changed approval hash. Approved data must stay
byte-identical, not merely be hidden by the report.

This pass changes wording only. It does not claim new hardware acceptance,
recording accuracy, speech recognition, keyboard input, rendering performance or
native compatibility results. No APK installation or device launch is required
for these punctuation/documentation edits.
