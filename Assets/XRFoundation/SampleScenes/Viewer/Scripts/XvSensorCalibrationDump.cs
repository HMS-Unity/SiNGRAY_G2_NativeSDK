using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation
{
    /// <summary>
    /// Reads sensor intrinsics/extrinsics from the SDK and writes them to a local .txt file.
    /// IMPORTANT: the native read functions only work once the device is initialized and the
    /// sensor stream is producing frames - calling them too early crashes the app natively
    /// (a crash that C# try/catch CANNOT catch). So the read is deferred via a coroutine that
    /// waits for readiness first.
    /// Wire the MRTK button OnClick to DumpRGBCalibration / DumpTofCalibration / DumpStereoCalibration.
    /// </summary>
    public class XvSensorCalibrationDump : MonoBehaviour
    {
        [Tooltip("Optional. If a UI Text is assigned, the result is also shown on it. " +
                 "Leave empty to only write the .txt file and log (no on-screen UI).")]
        public Text displayText;

        [Tooltip("Seconds to wait for the device / sensor stream to become ready before giving up.")]
        public float readyTimeout = 8f;

        // ---- Button entry points ---------------------------------------------------

        [ContextMenu("Dump RGB Calibration")]
        public void DumpRGBCalibration() { BeginDump("RGB"); }

        [ContextMenu("Dump ToF Calibration")]
        public void DumpTofCalibration() { BeginDump("ToF"); }

        [ContextMenu("Dump Stereo Calibration")]
        public void DumpStereoCalibration() { BeginDump("Stereo"); }

        private void BeginDump(string sensor)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(DumpWhenReady(sensor));
            }
            else
            {
                DoDump(sensor); // editor/not playing: native call will simply fail and be reported
            }
        }

        // Wait for the device (and, for ToF, for frames) before touching the native read,
        // otherwise the native SDK crashes the whole app.
        private IEnumerator DumpWhenReady(string sensor)
        {
            float t = 0f;
            while (!SafeReady() && t < readyTimeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (!SafeReady())
            {
                Fail(sensor, null);
                yield break;
            }

            if (sensor == "ToF")
            {
                t = 0f;
                while (SafeTofWidth() <= 0 && t < readyTimeout)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                if (SafeTofWidth() <= 0)
                {
                    Fail(sensor, null);
                    yield break;
                }
            }
            else
            {
                // give the stream a moment to produce data
                yield return new WaitForSeconds(0.5f);
            }

            DoDump(sensor);
        }

        private bool SafeReady()
        {
            try { return API.xslam_ready(); }
            catch { return false; }
        }

        private int SafeTofWidth()
        {
            try { return API.xslam_get_tof_width(); }
            catch { return 0; }
        }

        // ---- Actual reads ----------------------------------------------------------

        private void DoDump(string sensor)
        {
            if (sensor == "RGB") { DoDumpRGB(); }
            else if (sensor == "ToF") { DoDumpToF(); }
            else if (sensor == "Stereo") { DoDumpStereo(); }
        }

        private void DoDumpRGB()
        {
            API.rgb_calibration calib = default(API.rgb_calibration);
            bool ok;
            try { ok = API.readRGBCalibration(ref calib); }
            catch (Exception e) { Fail("RGB", e); return; }
            if (!ok) { Fail("RGB", null); return; }

            StringBuilder sb = Header("RGB");
            sb.AppendLine(FormatExtrinsic(calib.extrinsic));
            sb.AppendLine(FormatPdm("Intrinsic 1920x1080", calib.intrinsic1080));
            sb.AppendLine(FormatPdm("Intrinsic 1280x720", calib.intrinsic720));
            sb.AppendLine(FormatPdm("Intrinsic 640x480", calib.intrinsic480));
            Save("RGB", sb.ToString());
        }

        private void DoDumpToF()
        {
            API.pdm_calibration calib = default(API.pdm_calibration);
            bool ok;
            try { ok = API.readToFCalibration(ref calib); }
            catch (Exception e) { Fail("ToF", e); return; }
            if (!ok) { Fail("ToF", null); return; }

            StringBuilder sb = Header("ToF");
            sb.AppendLine(FormatExtrinsic(calib.extrinsic));
            sb.AppendLine(FormatPdm("Intrinsic", calib.intrinsic));
            Save("ToF", sb.ToString());
        }

        private void DoDumpStereo()
        {
            API.stereo_fisheyes calib = default(API.stereo_fisheyes);
            int shiftUs = 0;
            bool ok;
            try { ok = API.readStereoFisheyesCalibration(ref calib, ref shiftUs); }
            catch (Exception e) { Fail("Stereo", e); return; }
            if (!ok) { Fail("Stereo", null); return; }

            StringBuilder sb = Header("Stereo Fisheye");
            sb.AppendLine("imu_fisheye_shift_us: " + shiftUs);
            if (calib.calibrations != null)
            {
                for (int i = 0; i < calib.calibrations.Length; i++)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Camera " + i + " ---");
                    sb.AppendLine(FormatExtrinsic(calib.calibrations[i].extrinsic));
                    sb.AppendLine(FormatUnified("Intrinsic (unified)", calib.calibrations[i].intrinsic));
                }
            }
            Save("Stereo", sb.ToString());
        }

        // ---- Formatting ------------------------------------------------------------

        private StringBuilder Header(string sensor)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========== " + sensor + " Calibration ==========");
            sb.AppendLine("Saved: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();
            return sb;
        }

        private string FormatExtrinsic(API.ctransform t)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Extrinsic] (sensor -> reference / IMU)");
            if (t.rotation != null && t.rotation.Length >= 9)
            {
                sb.AppendLine("Rotation (row-major 3x3):");
                sb.AppendLine(string.Format("  {0:F9}  {1:F9}  {2:F9}", t.rotation[0], t.rotation[1], t.rotation[2]));
                sb.AppendLine(string.Format("  {0:F9}  {1:F9}  {2:F9}", t.rotation[3], t.rotation[4], t.rotation[5]));
                sb.AppendLine(string.Format("  {0:F9}  {1:F9}  {2:F9}", t.rotation[6], t.rotation[7], t.rotation[8]));
            }
            if (t.translation != null && t.translation.Length >= 3)
            {
                sb.AppendLine(string.Format("Translation (m): {0:F9}  {1:F9}  {2:F9}",
                    t.translation[0], t.translation[1], t.translation[2]));
            }
            return sb.ToString();
        }

        // Polynomial Distortion Model: K[0]=fx K[1]=fy K[2]=u0 K[3]=v0
        // K[4]=k1 K[5]=k2 K[6]=p1 K[7]=p2 K[8]=k3 K[9]=width K[10]=height
        private string FormatPdm(string title, API.pdm p)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[" + title + "] (Polynomial Distortion Model)");
            if (p.K != null && p.K.Length >= 11)
            {
                sb.AppendLine(string.Format("  fx={0:F6}  fy={1:F6}", p.K[0], p.K[1]));
                sb.AppendLine(string.Format("  cx(u0)={0:F6}  cy(v0)={1:F6}", p.K[2], p.K[3]));
                sb.AppendLine(string.Format("  k1={0:F9}  k2={1:F9}  p1={2:F9}  p2={3:F9}  k3={4:F9}",
                    p.K[4], p.K[5], p.K[6], p.K[7], p.K[8]));
                sb.AppendLine(string.Format("  width={0}  height={1}", (int)p.K[9], (int)p.K[10]));
            }
            else
            {
                sb.AppendLine("  (no data)");
            }
            return sb.ToString();
        }

        // Unified camera model: K[0]=fx K[1]=fy K[2]=u0 K[3]=v0 K[4]=xi K[5]=width K[6]=height
        private string FormatUnified(string title, API.unified u)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[" + title + "] (Unified camera model)");
            if (u.K != null && u.K.Length >= 7)
            {
                sb.AppendLine(string.Format("  fx={0:F6}  fy={1:F6}", u.K[0], u.K[1]));
                sb.AppendLine(string.Format("  cx(u0)={0:F6}  cy(v0)={1:F6}", u.K[2], u.K[3]));
                sb.AppendLine(string.Format("  xi={0:F9}", u.K[4]));
                sb.AppendLine(string.Format("  width={0}  height={1}", (int)u.K[5], (int)u.K[6]));
            }
            else
            {
                sb.AppendLine("  (no data)");
            }
            return sb.ToString();
        }

        // ---- IO --------------------------------------------------------------------

        private void Save(string sensor, string content)
        {
            string fileName = string.Format("{0}_calibration_{1}.txt", sensor, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string path = Path.Combine(Application.persistentDataPath, fileName);
            try
            {
                File.WriteAllText(path, content);
                Debug.Log("[Calib] " + sensor + " calibration saved to:\n" + path + "\n\n" + content);
                Show("Saved: " + path + "\n\n" + content);
            }
            catch (Exception e)
            {
                Debug.LogError("[Calib] failed to save " + sensor + " calibration: " + e.Message);
                Show(sensor + " save failed: " + e.Message);
            }
        }

        private void Fail(string sensor, Exception e)
        {
            if (e != null)
            {
                Debug.LogWarning("[Calib] read " + sensor + " calibration threw: " + e.Message);
                Show(sensor + " calibration unavailable (native SDK only runs on device).\n" + e.Message);
            }
            else
            {
                Debug.LogWarning("[Calib] " + sensor + " calibration not ready (device/stream not ready in time).");
                Show(sensor + " calibration not ready (start the sensor first / device not ready).");
            }
        }

        private void Show(string message)
        {
            if (displayText != null)
            {
                displayText.text = message;
            }
        }
    }
}
