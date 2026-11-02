using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
namespace Singray.Foundation.SampleScenes
{
    public class XvTagRecognizerUIController : MonoBehaviour
    {
        public XvTagRecognizerManager tagManager;
        public Text recognizerTex;
        public Text tagSize;
        private void Awake()
        {
            if (tagManager == null) tagManager = GetComponentInChildren<XvTagRecognizerManager>();
            if (tagManager == null) tagManager = FindAnyObjectByType<XvTagRecognizerManager>();
            if (tagManager == null) { Debug.LogError("[TAG-LIFE] Sample has no recognition manager."); return; }
            SyncMode(); WriteSize();
        }
        private void Update() => SyncMode();
        private void SyncMode()
        {
            if (tagManager == null || recognizerTex == null) return;
            string state = tagManager.IsStarting ? "Starting..." : tagManager.RecognizerMode.ToString();
            if (recognizerTex.text != state) recognizerTex.text = state;
        }
        private void WriteSize()
        {
            if (tagSize != null) tagSize.text = tagManager.Size.ToString(CultureInfo.InvariantCulture);
        }
        public void btnClick(GameObject btn)
        {
            if (btn == null || tagManager == null) return;
            switch (btn.name)
            {
                case "1cmBtn": case "4cmBtn": case "6cmBtn": case "8cmBtn": case "10cmBtn":
                case "12cmBtn": case "14cmBtn": case "16cmBtn": case "18cmBtn": case "30cmBtn":
                    tagManager.Size = int.Parse(btn.name.Substring(0, btn.name.Length - 5), CultureInfo.InvariantCulture) / 100.0;
                    WriteSize(); break;
                case "SizeBtn":
                    if (tagSize != null)
                    {
                        bool parsed = double.TryParse(tagSize.text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || double.TryParse(tagSize.text, out value);
                        if (parsed && XvAprilTag.ValidSize(value)) tagManager.Size = value;
                        else Debug.LogWarning("[TAG-LIFE] Invalid size input; keeping previous physical size.");
                        WriteSize();
                    }
                    break;
                case "RGB_Apriltag": tagManager.StartTagDetector(RecognizerMode.RGB_Apriltag); break;
                case "FishEye_Apriltag": tagManager.StartTagDetector(RecognizerMode.FishEye_Apriltag); break;
                case "RGB_QRCode": tagManager.StartTagDetector(RecognizerMode.RGB_QRCode); break;
                case "StopTagDetector": tagManager.StopTagDetector(); break;
            }
            SyncMode();
        }
    }
}
