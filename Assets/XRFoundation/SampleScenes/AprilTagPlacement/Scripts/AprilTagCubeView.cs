using System.Globalization;
using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>A unit-mesh cube with a separate, constant-size pose label.</summary>
    public sealed class AprilTagCubeView : MonoBehaviour
    {
        [SerializeField] private Transform cube;
        [SerializeField] private Renderer cubeRenderer;
        [SerializeField] private TMP_Text label;
        [SerializeField, Tooltip("Cube center offset along the tag's local XYZ axes, in rendered cube edge lengths. Use +0.5, -0.5 or 0 per axis.")]
        private Vector3 centerOffsetInEdges = new Vector3(0.5f, -0.5f, -0.5f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock colors;
        private Vector3 position;
        private Quaternion rotation;
        private float tagSize, displayScale = 1;
        private int tagId, shot;
        private string state = "Ready";
        private Color color;
        private bool initialized;

        public float EdgeMeters => tagSize * displayScale;
        public int TagId => tagId;
        public int Shot => shot;
        public Vector3 SavedPosition => position;
        public Quaternion SavedRotation => rotation;
        public Vector3 CenterOffsetInEdges => centerOffsetInEdges;
        public Vector3 VisualCenter => cube != null ? cube.position : transform.position;

        public void SetPose(TagDetection tag, float physicalSize, int number, Color tint)
        {
            tagId = tag.id; tagSize = physicalSize; shot = number; color = tint;
            position = tag.translation;
            var q = tag.quaternion;
            rotation = new Quaternion(q.x, q.y, q.z, q.w).normalized;
            transform.SetPositionAndRotation(position, rotation);
            initialized = true;
            SetState(number > 0 ? "Saved" : "Tracking");
        }

        public void SetDisplayScale(float value)
        {
            displayScale = value;
            ApplyGeometry();
            WriteLabel();
        }

        public void SetCenterOffset(Vector3 edgeFactors)
        {
            if (!XvAprilTag.Finite(edgeFactors.x) || !XvAprilTag.Finite(edgeFactors.y) || !XvAprilTag.Finite(edgeFactors.z)) return;
            centerOffsetInEdges = edgeFactors;
            ApplyGeometry();
        }

        private void ApplyGeometry()
        {
            if (cube == null) return;
            cube.localScale = Vector3.one * EdgeMeters;
            // Keep the root/raw tag pose intact. Child-local translation applies
            // the offset in tag axes and keeps the selected cube corner at the tag
            // origin when display scale changes. This is an experimental offset.
            cube.localPosition = centerOffsetInEdges * EdgeMeters;
        }

        private void OnValidate() { if (initialized) ApplyGeometry(); }

        public void SetState(string value)
        {
            state = value;
            ApplyGeometry();
            if (cubeRenderer != null)
            {
                if (colors == null) colors = new MaterialPropertyBlock();
                cubeRenderer.GetPropertyBlock(colors);
                colors.SetColor(ColorId, state == "Lost" || state == "Stopped" ? color * 0.45f : color);
                cubeRenderer.SetPropertyBlock(colors);
            }
            WriteLabel();
        }

        private void WriteLabel()
        {
            if (label == null || !initialized) return;
            var euler = rotation.eulerAngles;
            string title = shot > 0 ? "Tag " + tagId + " | Shot " + shot : "Tag " + tagId + " | " + state;
            string scale = Mathf.Approximately(displayScale, 1) ? "1:1" : displayScale.ToString("0.##", CultureInfo.InvariantCulture) + "x";
            label.text = title + "\nTag " + (tagSize * 100).ToString("0.#", CultureInfo.InvariantCulture) + " cm | Cube " +
                (EdgeMeters * 100).ToString("0.#", CultureInfo.InvariantCulture) + " cm | " + scale +
                "\nP " + Format(position) + " m\nR " + Format(euler) + " deg";
        }

        private static string Format(Vector3 value) => string.Format(CultureInfo.InvariantCulture,
            "{0:0.000}, {1:0.000}, {2:0.000}", value.x, value.y, value.z);

        private void LateUpdate()
        {
            if (!initialized || label == null || cube == null) return;
            // World-up label placement and a unit-scaled parent keep text readable
            // when the cube turns or its display size changes.
            var bounds = cubeRenderer.bounds;
            var anchor = label.canvas != null ? label.canvas.transform : label.transform;
            anchor.position = new Vector3(bounds.center.x, bounds.max.y + 0.07f, bounds.center.z);
            var camera = Camera.main;
            if (camera == null) return;
            var forward = anchor.position - camera.transform.position;
            if (forward.sqrMagnitude > 0.00001f) anchor.rotation = Quaternion.LookRotation(forward, camera.transform.up);
        }
    }
}
