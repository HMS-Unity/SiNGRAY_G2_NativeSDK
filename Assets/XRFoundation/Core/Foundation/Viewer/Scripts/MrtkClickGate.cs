using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Singray.UI
{
    /// <summary>Delivers one action per physical touch cycle, including delayed input-up events.</summary>
    public sealed class MrtkClickGate : MonoBehaviour
    {
        private PressableButton physical;
        private UnityEvent action;
        private bool touchingCycle;
        private bool reachedPress;
        private bool delivered;
        private int endedFrame = -1;

        public static void Install(Interactable button)
        {
            if (button == null) return;
            foreach (var router in button.GetComponents<PhysicalPressEventRouter>())
                router.InteractableOnClick = PhysicalPressEventRouter.PhysicalPressEventBehavior.EventOnClickCompletion;
            // Runtime listeners must never replace serialized actions while saving a scene.
            if (!Application.isPlaying) return;
            var gate = button.GetComponent<MrtkClickGate>() ?? button.gameObject.AddComponent<MrtkClickGate>();
            if (gate.action != null) return;
            gate.physical = button.GetComponent<PressableButton>();
            gate.action = button.OnClick;
            button.OnClick = new UnityEvent();
            button.OnClick.AddListener(gate.Deliver);
            if (gate.physical != null)
            {
                gate.physical.TouchBegin.AddListener(gate.BeginTouch);
                gate.physical.ButtonPressed.AddListener(gate.ReachedPress);
                gate.physical.TouchEnd.AddListener(gate.EndTouch);
            }
        }

        private void BeginTouch()
        {
            if (touchingCycle && endedFrame < 0) return;
            touchingCycle = true;
            reachedPress = false;
            delivered = false;
            endedFrame = -1;
        }

        private void ReachedPress() { reachedPress = true; }
        private void EndTouch() { endedFrame = Time.frameCount; }

        private void Deliver()
        {
            if (touchingCycle)
            {
                // A pinch/input-up alongside a poke must not fire while the button is held.
                if (!reachedPress || delivered || (physical != null && physical.IsPressing)) return;
                delivered = true;
            }
            action?.Invoke();
        }

        private void LateUpdate()
        {
            // Keep the latch through touch-end and queued release callbacks.
            if (endedFrame >= 0 && Time.frameCount > endedFrame + 2 && (physical == null || !physical.IsTouching))
                ResetCycle();
        }

        private void ResetCycle()
        {
            touchingCycle = reachedPress = delivered = false;
            endedFrame = -1;
        }
        private void OnDisable() { ResetCycle(); }
    }
}
