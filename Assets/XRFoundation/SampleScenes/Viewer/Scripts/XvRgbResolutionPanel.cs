using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Singray.Foundation
{
    /// <summary>Device RGB mode controls for Viewer; displays measured frame size, not just the requested mode.</summary>
    public sealed class XvRgbResolutionPanel : MonoBehaviour
    {
        private static readonly RgbResolution[] Modes =
        {
            RgbResolution.RGB_320x240, RgbResolution.RGB_640x480, RgbResolution.RGB_1280x720,
            RgbResolution.RGB_1920x1080, RgbResolution.RGB_2560x1920, RgbResolution.RGB_3840x2160
        };
        private static readonly string[] Labels = { "320x240", "640x480", "1280x720", "1920x1080", "2560x1920", "3840x2160" };
        private CameraDemo owner;
        private XvCameraManager cameras;
        private GameObject controls;
        private TMP_Text status;
        private Coroutine switchRoutine;
        private bool requested;
        private bool receivedFrame;
        private float startedAt;
        private float nextStatusAt;
        private bool timeoutReported;

        internal void Initialize(CameraDemo demo, XvCameraManager manager)
        {
            owner = demo;
            cameras = manager;
            if (controls != null || owner.arCameraImage == null) return;
            var template = owner.arCameraImage.GetComponentInChildren<Interactable>(true);
            if (template == null)
            {
                Debug.LogWarning("Viewer RGB: missing MRTK button template; resolution controls unavailable.", this);
                return;
            }
            controls = new GameObject("RGB Resolution Controls");
            controls.transform.SetParent(owner.arCameraImage.transform, false);
            controls.SetActive(false);
            TMP_Text templateText = template.GetComponentInChildren<TMP_Text>(true);
            for (int i = 0; i < Modes.Length; i++)
            {
                int index = i;
                var button = Instantiate(template, controls.transform, false);
                button.name = "RGB " + Labels[i];
                button.transform.localPosition = new Vector3(1250, 450 - i * 180, -110);
                button.transform.localScale = Vector3.one * 4500;
                button.OnClick = new UnityEvent();
                button.OnClick.AddListener(() => SelectResolution(Modes[index]));
                var config = button.GetComponent<ButtonConfigHelper>();
                if (config != null) config.MainLabelText = Labels[i];
                else
                {
                    var text = button.GetComponentInChildren<TMP_Text>(true);
                    if (text != null) text.text = Labels[i];
                }
                Singray.UI.MrtkClickGate.Install(button);
                button.gameObject.SetActive(true);
            }
            var background = new GameObject("RGB Status", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(controls.transform, false);
            var rectangle = background.GetComponent<RectTransform>();
            rectangle.sizeDelta = new Vector2(1800, 120);
            rectangle.localPosition = new Vector3(0, -640, -125);
            background.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            background.GetComponent<Image>().raycastTarget = false;
            var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(rectangle, false);
            status = label.GetComponent<TMP_Text>();
            if (templateText != null) status.font = templateText.font;
            status.fontSize = 34;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;
            status.raycastTarget = false;
            status.rectTransform.sizeDelta = new Vector2(1760, 110);
            SetStatus("RGB stopped. Select a resolution to start.\nModes depend on device/firmware support.");
            controls.SetActive(isActiveAndEnabled);
        }

        internal void SetVisible(bool visible) { if (controls != null) controls.SetActive(visible && isActiveAndEnabled); }

        private void OnEnable()
        {
            XvCameraManager.onARCameraStreamFrameArrived.AddListener(OnFrame);
            if (controls != null) controls.SetActive(true);
        }

        private void OnDisable()
        {
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(OnFrame);
            if (owner != null && requested) owner.StopARCamera();
            else NotifyStopped();
            if (controls != null) controls.SetActive(false);
        }

        internal void SelectResolution(RgbResolution mode)
        {
            if (owner == null || cameras == null || !isActiveAndEnabled || Array.IndexOf(Modes, mode) < 0) return;
            if (XvCaptureLease.HasOtherOwners(XvCameraStreamType.ARCameraStream, cameras))
            {
                SetStatus("RGB is in use by another feature. Stop that feature before switching resolution.");
                Debug.LogWarning("[Viewer RGB] resolution switch blocked by another RGB owner: " + mode, this);
                return;
            }
            if (requested && !timeoutReported && cameras.XvARCameraParameter.rgbResolution == mode) return;
            // Stop clears the previous native stream and texture; allow deferred texture destruction to run.
            owner.StopARCamera();
            SetStatus("Switching RGB to " + Labels[Array.IndexOf(Modes, mode)] + "...");
            switchRoutine = StartCoroutine(RestartPreview(mode));
        }

        private IEnumerator RestartPreview(RgbResolution mode)
        {
            yield return null;
            yield return null;
            switchRoutine = null;
            if (XvCaptureLease.HasOtherOwners(XvCameraStreamType.ARCameraStream, cameras))
            {
                SetStatus("RGB is in use by another feature. Select a resolution after it stops.");
                Debug.LogWarning("[Viewer RGB] restart blocked by another RGB owner", this);
                yield break;
            }
            cameras.XvARCameraParameter.rgbResolution = mode;
            owner.StartARCamera();
        }

        internal void NotifyStarted()
        {
            requested = true;
            receivedFrame = timeoutReported = false;
            startedAt = Time.realtimeSinceStartup;
            nextStatusAt = 0;
            SetStatus("Requested: " + ModeLabel() + "\nWaiting for a real RGB frame...");
            Debug.Log("[Viewer RGB] requested=" + ModeLabel(), this);
        }

        internal void NotifyStopped()
        {
            if (switchRoutine != null) StopCoroutine(switchRoutine);
            switchRoutine = null;
            requested = false;
            SetStatus("RGB stopped. Select a resolution to start.");
        }

        private void Update()
        {
            if (requested && !receivedFrame && !timeoutReported && Time.realtimeSinceStartup - startedAt >= 10f)
            {
                timeoutReported = true;
                SetStatus("Requested: " + ModeLabel() + "\nNo RGB frame after 10s. Try another mode or Stop/Start.");
                Debug.LogWarning("[Viewer RGB] no frame after 10s; requested=" + ModeLabel(), this);
            }
        }

        private void OnFrame(cameraData frame)
        {
            if (!requested || frame == null || frame.tex == null || frame.parameter.timeStamp <= 0) return;
            bool first = !receivedFrame;
            receivedFrame = true;
            if (!first && Time.realtimeSinceStartup < nextStatusAt) return;
            nextStatusAt = Time.realtimeSinceStartup + 0.5f;
            string actual = frame.texWidth + "x" + frame.texHeight;
            string result = "Requested: " + ModeLabel() + "   Actual: " + actual +
                (ModeLabel() == actual ? "" : "   SIZE MISMATCH") + "\nt=" + frame.parameter.timeStamp.ToString("F3");
            SetStatus(result);
            if (first) Debug.Log("[Viewer RGB] " + result, this);
        }

        private string ModeLabel()
        {
            int index = cameras == null ? -1 : Array.IndexOf(Modes, cameras.XvARCameraParameter.rgbResolution);
            return index < 0 ? "unknown" : Labels[index];
        }
        private void SetStatus(string text) { if (status != null) status.text = text; }
    }
}
