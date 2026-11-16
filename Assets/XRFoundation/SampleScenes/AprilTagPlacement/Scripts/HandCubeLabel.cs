using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>Keeps the hand-cube label above its actual mesh, independently of scale.</summary>
    public sealed class HandCubeLabel : MonoBehaviour
    {
        [SerializeField] private Renderer target;
        private void LateUpdate()
        {
            if (target == null) return;
            var bounds = target.bounds;
            transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.045f, bounds.center.z);
            var camera = Camera.main;
            if (camera == null) return;
            var forward = transform.position - camera.transform.position;
            if (forward.sqrMagnitude > 0.00001f) transform.rotation = Quaternion.LookRotation(forward, camera.transform.up);
        }
    }
}
