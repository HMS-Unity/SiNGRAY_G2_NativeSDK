# Recording Review: 2026-11-16

Reviewed file: `20261116_182251_669_a5ed5476.mp4`, SHA256
`1205bbd3cf2af969826d8634aed4c1e0f9e096eac7a56090549e8e1a98569801`.
The original video remains local. This report publishes technical observations,
not workplace imagery, raw video, or per-device captures/calibration.
All times are relative to the video. Filename and container creation times are
not used to estimate sensor latency.

## Conclusion

The clip demonstrates the basic independent detection, corner overlay, world
Plane display, and MR recording pipeline. It does not establish a completed
fixed-target walk-around test after Lock, or a calibration fault on one device.
Where the physical Tag is visible, virtual and physical patterns overlap broadly
but some cell edges separate. This cannot be converted into absolute 3D error in
millimeters, and the thick orange outline's outer edge is not a detected corner.

## Method and observable evidence

- FFmpeg decoded all 2830 H.264 frames: 1280x720, 32.08 seconds, no audio track.
  Container average is about 88.23 fps, not fresh RGB or the 8 Hz detection rate.
- States/motion were sampled every 0.5 seconds. Enlarged key images include 5,
  10, 14.5, 15, 17, 17.5, 19.5, 20.5, 21, and 25 seconds. This is not exhaustive
  OCR proof of obscured text; very brief or unreadable states cannot be excluded.
- Readable states are `LIVE | Tracking` or `LIVE | ID 0 not visible`, with
  `Lock a fresh valid Plane, then walk around`. No clear `LOCKED` and
  `Live vs lock` segment was identified.
- The orange world-Plane outline also appears in LIVE. Its presence does not
  prove a successful lock.
- The board is handheld for much of the clip, appears to rest near the laptop
  around 18-21 seconds, and is lifted again. There is no confirmed complete
  sequence of stationary board, successful lock, glasses movement, and return.

| Time | Observation | Supported interpretation |
| --- | --- | --- |
| 0 to about 4.5 s | Detection starts; Tag is not stably visible | Unsuitable for spatial error measurement |
| About 5-15 s | LIVE follows handheld Tag; green/magenta corners are close; panel often occludes physical Tag | Same-frame detection/PnP fit works; timing and spatial effects remain mixed |
| About 17-17.5 s | Physical board and virtual pattern visible together; broad overlap with local edge separation | Visible alignment differences, insufficient for calibration or metric ground truth |
| About 18-21 s | Tag below panel; orange Plane appears/disappears; panel often reports no valid pose | Consistent with LIVE visibility changes, not evidence of locked-reference drift |
| About 24-27 s | Handheld LIVE tracking resumes; 25 s fit error is very low | Low reprojection error does not prove world or eye-view alignment |
| About 30-32 s | Palm shows Stop / Recording and recording is ended | Recording state is visible; post-stop save completion is outside the clip |

Readable Corner RMSE examples: **1.602 px** at 5 s, **0.511 px** at 8 s,
**1.350 px** at 10 s, **0.075 px** at 12 s, and **0.042 px** at 25 s.
These are selected on-screen values, not raw-corner statistics or global extrema.
They describe their processed RGB frames, not necessarily the recording
background's current exposure frame.

## Potential error sources, ordered by this clip's evidence

1. **Test state and target motion are the clearest current limitations.** Lock
   is unconfirmed and the handheld board moves. LIVE pose changes are not drift.
   Independently measure detection-edge size and check board flatness/borders.
2. **Timing deserves early investigation.** Processing defaults to 8 Hz;
   asynchronous results, recorded background, and rendering are not identical
   time samples. A moving Tag can show discrete follow/lag. Measure again at rest.
   An 8 Hz update alone does not prove a fixed target must drift under accurate
   tracking, and this MP4 cannot establish a precise latency in milliseconds.
3. **Rendering and observation can exaggerate differences.** The opaque panel
   hides ground truth, thick orange lines cover boundaries, and transparency,
   compression, and blur complicate edge reading. An extra outer border is not
   proof of incorrect size or extrinsics. Compare cell/detection-edge centerlines
   together with original Save data.
4. **Basis/extrinsic direction, intrinsics/distortion, and composite projection
   remain open checks.** Four-corner fitting can have low RMSE with incorrect
   marker size or intrinsics. Distorted RGB projection and ordinary Unity
   pinhole rendering also differ. Persistent static offset warrants checking
   these items; the clip does not establish which is incorrect.
5. **SLAM, optical eye view, and per-device calibration cannot yet be isolated.**
   Obtain a confirmed locked walk-around/return sequence and matched tests on a
   second device. An RGB composite cannot replace observation through the lens.

## Next test to separate causes

1. Measure the black detection square as 80 mm; mount it on a rigid stationary
   support. Keep the control panel clear of the physical marker.
2. Keep glasses and board stationary for five seconds, then Save. Record RMSE,
   alternative candidate error, measured distance, and validity flags.
3. Press Lock and clearly record `LOCKED` plus `Live vs lock` before moving.
4. Move only the glasses: about 20-30 cm left/right, closer/farther, slowly view
   obliquely, then return. Stop for 3-5 seconds and Save at each position.
5. Repeat with faster motion and check recovery at rest. Motion-only differences
   prioritize timing; persistent static differences prioritize transforms,
   calibration, or tracking, subject to further evidence.
6. Repeat several runs on at least two devices with the same board, distance,
   and lighting, reading each device's own calibration. Compare camera-relative
   or locked-reference residuals, not raw coordinates from different SLAM origins.

Retain video and paired PNG/JSON captures at every position, then plot live-to-lock
translation and angle against time. This review has no corresponding per-frame
JSON, independent ground truth, or optical eye-view recording, so it does not
present an invented millimeter-accuracy curve.
