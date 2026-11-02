using System;
using UnityEngine;
using UnityEngine.UI;
namespace Singray.Foundation.SampleScenes
{
    // Retained compatibility type for existing customer scripts and serialized references.
    public class XvStaticGestureDemo : MonoBehaviour
    {
        [SerializeField] private XvStaticGestureManager staticGestureManager;
        public XvStaticGestureManager XvStaticGestureManager
        {
            get
            {
                if (staticGestureManager == null) staticGestureManager = GetComponentInChildren<XvStaticGestureManager>();
                if (staticGestureManager == null) staticGestureManager = FindObjectOfType<XvStaticGestureManager>();
                if (staticGestureManager == null)
                {
                    var owned = new GameObject("XvStaticGestureManager");owned.transform.SetParent(transform, false);
                    staticGestureManager = owned.AddComponent<XvStaticGestureManager>();
                }
                return staticGestureManager;
            }
        }
        public Text leftGesture;
        public Text rightGesture;
        private static readonly StaticGestureStatus[] gestures = (StaticGestureStatus[])Enum.GetValues(typeof(StaticGestureStatus));
        private StaticGestureStatus lastLeft = StaticGestureStatus.UNKONOW, lastRight = StaticGestureStatus.UNKONOW;
        private float nextLog;
        protected virtual void OnEnable() => Debug.Log("[GESTURE-LIFE] sample enabled");
        protected virtual void OnDisable() => Debug.Log("[GESTURE-LIFE] sample disabled");
        protected virtual void Update()
        {
            var manager = XvStaticGestureManager;
            foreach (var gesture in gestures)
            {
                if (manager.GetKeyDown(gesture, HandType.Left)) Debug.Log("[GESTURE-LIFE] hand=Left down=" + gesture);
                if (manager.GetKeyUp(gesture, HandType.Left)) Debug.Log("[GESTURE-LIFE] hand=Left up=" + gesture);
                if (manager.GetKeyDown(gesture, HandType.Right)) Debug.Log("[GESTURE-LIFE] hand=Right down=" + gesture);
                if (manager.GetKeyUp(gesture, HandType.Right)) Debug.Log("[GESTURE-LIFE] hand=Right up=" + gesture);
            }
            var left = manager.GetCurrentStaticGesture(HandType.Left);var right = manager.GetCurrentStaticGesture(HandType.Right);
            if (leftGesture != null && leftGesture.text != left.ToString()) leftGesture.text = left.ToString();
            if (rightGesture != null && rightGesture.text != right.ToString()) rightGesture.text = right.ToString();
            if (left != lastLeft || right != lastRight || Time.unscaledTime >= nextLog)
            {
                Debug.Log($"[GESTURE-LIFE] left={left} right={right}");
                lastLeft = left; lastRight = right; nextLog = Time.unscaledTime + 5f;
            }
        }
    }
}
