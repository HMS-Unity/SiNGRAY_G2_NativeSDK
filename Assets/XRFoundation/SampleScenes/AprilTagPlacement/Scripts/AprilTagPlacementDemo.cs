using System.Collections.Generic;
using System.Globalization;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>Independent MRTK manipulation and AprilTag tracking/snapshot sample.</summary>
    public sealed class AprilTagPlacementDemo : MonoBehaviour
    {
        [SerializeField] private XvTagRecognizerManager manager;
        [SerializeField] private RecognizerMode detector = RecognizerMode.RGB_Apriltag;
        [SerializeField] private AprilTagCubeView resultPrefab;
        [SerializeField] private Transform results;
        [SerializeField] private Transform handCube;
        [SerializeField] private TMP_Text statusText, sizeText, scaleText, handText, modeText, targetText;
        [SerializeField] private Interactable[] sizeButtons;
        [SerializeField] private Interactable[] buttons;
        [SerializeField, Tooltip("-1 selects the lowest valid ID in the first matching batch.")] private int targetId = -1;
        [SerializeField] private bool single;
        [SerializeField, Range(0.1f, 2)] private float lostAfterSeconds = 0.5f;
        private readonly List<AprilTagCubeView> shots = new List<AprilTagCubeView>();
        private static readonly Color[] ShotColors = { new Color(1, 0.68f, 0.2f), new Color(0.75f, 0.4f, 1), new Color(0.25f, 1, 0.65f), new Color(1, 0.35f, 0.6f) };
        private AprilTagCubeView live;
        private bool requested, received;
        private long lastSequence;
        private int lockedId = -1, successCount;
        private float runSize, displayScale = 1, lastSeen;
        private string status = "Ready";
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 homeScale;

        public int SuccessCount => successCount;
        public bool IsRecognizing => requested;
        public bool Single => single;
        public float DisplayScale => displayScale;

        private void Awake()
        {
            if (handCube != null)
            {
                homePosition = handCube.localPosition; homeRotation = handCube.localRotation;
                homeScale = handCube.localScale;
            }
            if (manager == null || resultPrefab == null || results == null)
            { enabled = false; Debug.LogError("[TAG-DEMO] Missing demo references."); }
        }
        private void OnEnable()
        {
            if (Application.isPlaying && buttons != null)
                foreach (var button in buttons) Singray.UI.MrtkClickGate.Install(button);
            if (manager != null) manager.OnDetectedAprilTagEvent.AddListener(OnResult);
            RefreshLabels();
        }
        private void OnDisable()
        {
            StopRecognition();
            if (manager != null) manager.OnDetectedAprilTagEvent.RemoveListener(OnResult);
            EndHandInteraction();
        }
        private void OnApplicationPause(bool paused) { if (paused) { StopRecognition(); EndHandInteraction(); } }
        private void OnApplicationFocus(bool focused) { if (!focused && Application.isPlaying) { StopRecognition(); EndHandInteraction(); } }

        public void Continuous() => SelectMode(false);
        public void SingleShot() => SelectMode(true);
        private void SelectMode(bool value)
        {
            StopRecognition(); single = value; status = "Ready"; RefreshLabels();
        }
        public void StartRecognition()
        {
            if (requested || manager == null || !isActiveAndEnabled) return;
            if (detector != RecognizerMode.FishEye_Apriltag && detector != RecognizerMode.RGB_Apriltag) return;
            if (live != null) live.SetState("Stopped");
            // Always retire our preceding session before arming another snapshot.
            manager.StopTagDetector();
            runSize = (float)manager.Size;
            if (!XvAprilTag.Finite(runSize) || runSize <= 0) { status = "Invalid size"; RefreshLabels(); return; }
            requested = true; received = false; lastSequence = 0; lockedId = targetId;
            lastSeen = Time.unscaledTime; status = "Starting";
            manager.StartTagDetector(detector);
            SetSizeButtons(false); RefreshLabels();
        }
        public void StopRecognition()
        {
            bool active = requested;
            requested = false; // Stop emits null synchronously; snapshots must survive it.
            if (manager != null) manager.StopTagDetector();
            if (live != null) live.SetState("Stopped");
            if (active) status = "Stopped";
            SetSizeButtons(true); RefreshLabels();
        }
        public void ClearResults()
        {
            StopRecognition();
            foreach (var view in shots) Remove(view);
            shots.Clear(); Remove(live); live = null;
            successCount = 0; status = "Ready"; RefreshLabels();
        }
        private static void Remove(AprilTagCubeView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        public void SetTagCentimeters(int cm)
        {
            if (requested || manager == null || cm <= 0 || cm > 100) return;
            manager.Size = cm / 100.0; RefreshLabels();
        }
        public void SetTargetId(int id)
        {
            if (requested || id < -1) return;
            targetId = id; RefreshLabels();
        }
        public void Smaller() => SetDisplayScale(displayScale / 1.25f);
        public void Larger() => SetDisplayScale(displayScale * 1.25f);
        public void ActualSize() => SetDisplayScale(1);
        private void SetDisplayScale(float value)
        {
            displayScale = Mathf.Clamp(value, 0.25f, 4);
            if (live != null) live.SetDisplayScale(displayScale);
            foreach (var shot in shots) if (shot != null) shot.SetDisplayScale(displayScale);
            RefreshLabels();
        }
        public void ResetHandCube()
        {
            if (handCube == null) return;
            EndHandInteraction();
            handCube.localPosition = homePosition; handCube.localRotation = homeRotation; handCube.localScale = homeScale;
        }
        private void EndHandInteraction()
        {
            if (buttons != null) foreach (var button in buttons)
                if (button != null) button.GetComponent<Singray.UI.MrtkClickGate>()?.CancelPending();
            if (handCube == null) return;
            handCube.GetComponent<ObjectManipulator>()?.ForceEndManipulation();
        }
        private void SetSizeButtons(bool value)
        {
            if (sizeButtons != null) foreach (var button in sizeButtons) if (button != null) button.IsEnabled = value;
        }
        private static bool Valid(TagDetection tag, float confidence)
        {
            if (tag == null || tag.id < 0 || !XvAprilTag.Finite(tag.confidence) || tag.confidence < confidence) return false;
            var p = tag.translation; var q = tag.quaternion;
            return XvAprilTag.Finite(p.x) && XvAprilTag.Finite(p.y) && XvAprilTag.Finite(p.z) &&
                XvAprilTag.Finite(q.x) && XvAprilTag.Finite(q.y) && XvAprilTag.Finite(q.z) && XvAprilTag.Finite(q.w) &&
                XvAprilTag.Finite(q.sqrMagnitude) && q.sqrMagnitude > 0.0000001f;
        }
        private void OnResult(TagDetection[] tags)
        {
            if (!requested || tags == null || !manager.TryGetFrameDelivery(out long sequence, out double age) ||
                sequence == lastSequence || age > lostAfterSeconds) return;
            lastSequence = sequence;
            TagDetection best = null;
            foreach (var tag in tags)
                if (Valid(tag, manager.Confidence) && (lockedId < 0 || tag.id == lockedId) && (best == null || tag.id < best.id)) best = tag;
            if (best == null) return;
            lockedId = best.id; received = true; lastSeen = Time.unscaledTime;
            if (single)
            {
                // Disarm before creating a snapshot or stopping the manager: at most
                // one saved cube and one increment for this explicit Start action.
                requested = false;
                int number = successCount + 1;
                var shot = Instantiate(resultPrefab, results);
                shot.name = "Tag " + best.id + " Shot " + number;
                shot.SetPose(best, runSize, number, ShotColors[(number - 1) % ShotColors.Length]);
                shot.SetDisplayScale(displayScale);
                shots.Add(shot); successCount = number;
                manager.StopTagDetector(); SetSizeButtons(true); status = "Saved";
                Debug.Log("[TAG-DEMO] Saved shot=" + number + " id=" + best.id + " tagMeters=" + runSize.ToString("R", CultureInfo.InvariantCulture));
            }
            else
            {
                if (live == null) { live = Instantiate(resultPrefab, results); live.name = "Live Tag"; }
                live.SetPose(best, runSize, 0, new Color(0.25f, 1, 0.65f));
                live.SetDisplayScale(displayScale); status = "Tracking";
            }
            RefreshLabels();
        }
        private void Update()
        {
            if (requested)
            {
                if (!manager.IsStarting && manager.RecognizerMode == RecognizerMode.None)
                { requested = false; status = "Start failed"; SetSizeButtons(true); if (live != null) live.SetState("Stopped"); }
                else if (!manager.IsStarting && !received) status = "Searching";
                else if (received && Time.unscaledTime - lastSeen > lostAfterSeconds)
                { status = "Lost"; if (live != null) live.SetState("Lost"); }
            }
            RefreshLabels();
            if (handText != null && handCube != null)
                handText.text = "Grab & Scale\n" + (handCube.lossyScale.x * 100).ToString("0.#", CultureInfo.InvariantCulture) + " cm";
        }
        private static void Write(TMP_Text text, string value) { if (text != null && text.text != value) text.text = value; }
        private void RefreshLabels()
        {
            Write(statusText, status + "  |  Shots " + successCount);
            Write(modeText, single ? "Mode: Single" : "Mode: Live");
            Write(sizeText, "Tag: " + (manager != null ? (manager.Size * 100).ToString("0.#", CultureInfo.InvariantCulture) : "--") + " cm");
            Write(scaleText, Mathf.Approximately(displayScale, 1) ? "Cube: 1:1" : "Cube: " + displayScale.ToString("0.##", CultureInfo.InvariantCulture) + "x");
            Write(targetText, "ID: " + (targetId < 0 ? "Any" : targetId.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
