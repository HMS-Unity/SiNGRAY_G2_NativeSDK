using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>Pixel-space outlines over the exact processed RGB frame; origin top-left.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RgbTagOverlay : MaskableGraphic
    {
        private Vector2[] measured, projected, locked;
        private int width = 1, height = 1;
        public void SetFrame(int w, int h, double[] result, double[] lockedPixels)
        {
            width = w; height = h;
            measured = result != null && result[0] == 1 ? Points(result, 15) : null;
            projected = result != null && result[0] == 1 ? Points(result, 23) : null;
            locked = lockedPixels != null ? Points(lockedPixels, 0) : null;
            SetVerticesDirty();
        }
        public void Clear() { measured = projected = locked = null; SetVerticesDirty(); }
        private static Vector2[] Points(double[] values, int start)
        {
            var points = new Vector2[4];
            for (int i = 0; i < 4; i++) points[i] = new Vector2((float)values[start + i * 2], (float)values[start + i * 2 + 1]);
            return points;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Outline(vh, locked, new Color(1, 0.65f, 0.05f), 2.5f);
            Outline(vh, measured, Color.green, 2.5f);
            Outline(vh, projected, Color.magenta, 1.1f);
        }
        private Vector2 Local(Vector2 p)
        {
            var rect = rectTransform.rect;
            return new Vector2(rect.xMin + p.x / width * rect.width, rect.yMax - p.y / height * rect.height);
        }
        private void Outline(VertexHelper vh, Vector2[] points, Color tint, float thickness)
        {
            if (points == null) return;
            for (int i = 0; i < 4; i++)
            {
                // Clamp remote projected corners to a generous bound before generating geometry.
                Vector2 a = Local(points[i]), b = Local(points[(i + 1) % 4]);
                float bound = Mathf.Max(rectTransform.rect.width, rectTransform.rect.height) * 8;
                a = Vector2.ClampMagnitude(a, bound); b = Vector2.ClampMagnitude(b, bound);
                Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * thickness * 0.5f;
                int first = vh.currentVertCount;
                vh.AddVert(a-n, tint, Vector2.zero); vh.AddVert(a+n, tint, Vector2.zero);
                vh.AddVert(b+n, tint, Vector2.zero); vh.AddVert(b-n, tint, Vector2.zero);
                vh.AddTriangle(first, first+1, first+2); vh.AddTriangle(first, first+2, first+3);
            }
        }
    }
}
