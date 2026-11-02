using UnityEngine;
using UnityEngine.Events;
namespace Singray.Foundation
{
    /// <summary>Applies recognition poses and preserves the configured Found/Lost UnityEvents.</summary>
    public class XvTagRecognizerBehavior : MonoBehaviour
    {
        [Tooltip("AprilTag ID")] public int id = -1;
        [Tooltip("Match QR-code text by substring")] public string qrcodeID;
        public bool ifCheck = true;
        public UnityEvent OnFoundEvent = new UnityEvent();
        public UnityEvent OnLostEvent = new UnityEvent();
        [SerializeField] private XvTagRecognizerManager aprilTagManager;
        private TagDetection tagDetection;
        public TagDetection TagDetection => tagDetection;
        private bool found;
        public void btnClick(GameObject btn)
        {
            if (btn == null) return;
            if (btn.name == "StopBtn") ifCheck = false;
            else if (btn.name == "StartBtn") ifCheck = true;
        }
        private void Awake()
        {
            if (aprilTagManager == null) aprilTagManager = GetComponentInParent<XvTagRecognizerManager>();
            if (aprilTagManager == null) aprilTagManager = FindAnyObjectByType<XvTagRecognizerManager>();
            if (id == -1 && int.TryParse(gameObject.name, out var parsed)) id = parsed;
        }
        private void OnEnable()
        {
            if (aprilTagManager != null) aprilTagManager.OnDetectedAprilTagEvent.AddListener(OnDetectedAprilTagEvent);
        }
        private void OnDisable()
        {
            if (aprilTagManager != null) aprilTagManager.OnDetectedAprilTagEvent.RemoveListener(OnDetectedAprilTagEvent);
            tagDetection = null; found = false;
            OnLostEvent?.Invoke();
        }
        private void Start()
        {
            if (OnFoundEvent == null) OnFoundEvent = new UnityEvent();
            if (OnLostEvent == null) OnLostEvent = new UnityEvent();
            OnLostEvent.Invoke();
        }
        private void OnDetectedAprilTagEvent(TagDetection[] detections)
        {
            // The existing per-tag Stop button freezes its current pose until Start is selected.
            if (!ifCheck || aprilTagManager == null) return;
            TagDetection match = null;
            if (detections != null)
            {
                foreach (var detection in detections)
                {
                    if (detection == null || !XvAprilTag.Finite(detection.confidence) || detection.confidence < aprilTagManager.Confidence) continue;
                    bool matches = aprilTagManager.TagGroupName == "36h11" ? detection.id == id :
                        !string.IsNullOrEmpty(XvAprilTag.QrText(detection.qrcode)) && XvAprilTag.QrText(detection.qrcode).Contains(qrcodeID ?? string.Empty);
                    if (!matches) continue;
                    var p = detection.translation; var q = detection.quaternion;
                    if (!XvAprilTag.Finite(p.x) || !XvAprilTag.Finite(p.y) || !XvAprilTag.Finite(p.z) ||
                        !XvAprilTag.Finite(q.x) || !XvAprilTag.Finite(q.y) || !XvAprilTag.Finite(q.z) || !XvAprilTag.Finite(q.w) || q.sqrMagnitude < 0.0000001f) continue;
                    match = detection; break;
                }
            }
            if (match != null)
            {
                transform.SetPositionAndRotation(match.translation, new Quaternion(match.quaternion.x, match.quaternion.y, match.quaternion.z, match.quaternion.w));
                transform.localScale = Vector3.one * (float)aprilTagManager.Size;
                tagDetection = match;
                if (!found) Debug.Log($"[TAG-LIFE] found id={match.id} mode={aprilTagManager.RecognizerMode}");
                found = true; OnFoundEvent?.Invoke();
            }
            else
            {
                if (found) Debug.Log("[TAG-LIFE] lost id=" + id);
                found = false; tagDetection = null; OnLostEvent?.Invoke();
            }
        }
    }
}
