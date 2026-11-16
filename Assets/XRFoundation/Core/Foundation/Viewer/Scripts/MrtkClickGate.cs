using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;

namespace Singray.UI
{
    /// <summary>Delivers one action per physical touch cycle, including delayed input-up events.</summary>
    public sealed class MrtkClickGate : MonoBehaviour, IMixedRealityInputHandler, IMixedRealityPointerHandler
    {
        private PressableButton physical;
        private UnityEvent action;
        private bool touchingCycle;
        private bool reachedPress;
        private bool delivered;
        private int endedFrame = -1;
        private Interactable interactable;
        private UnityEvent forwarded;
        private bool needsFreshPress;

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
            gate.interactable = button;
            gate.action = button.OnClick;
            button.OnClick = new UnityEvent();
            gate.forwarded = button.OnClick;
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
            if (!isActiveAndEnabled) return;
            needsFreshPress = false;
            if (touchingCycle && endedFrame < 0) return;
            touchingCycle = true;
            reachedPress = false;
            delivered = false;
            endedFrame = -1;
        }

        private void ReachedPress() { if (isActiveAndEnabled) reachedPress = true; }
        private void EndTouch() { if (isActiveAndEnabled) endedFrame = Time.frameCount; }

        private void Deliver()
        {
            if (!isActiveAndEnabled || needsFreshPress || interactable == null || !interactable.isActiveAndEnabled || !interactable.IsEnabled) return;
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
        internal void CancelPending() { needsFreshPress = true; ResetCycle(); }
        private void OnDisable() { CancelPending(); }
        public void OnInputDown(InputEventData eventData) { if (isActiveAndEnabled) needsFreshPress = false; }
        public void OnInputUp(InputEventData eventData) { }
        public void OnPointerDown(MixedRealityPointerEventData eventData) { if (isActiveAndEnabled) needsFreshPress = false; }
        public void OnPointerUp(MixedRealityPointerEventData eventData) { }
        public void OnPointerClicked(MixedRealityPointerEventData eventData) { }
        public void OnPointerDragged(MixedRealityPointerEventData eventData) { }
        private void OnDestroy()
        {
            if (physical != null)
            {
                physical.TouchBegin.RemoveListener(BeginTouch);
                physical.ButtonPressed.RemoveListener(ReachedPress);
                physical.TouchEnd.RemoveListener(EndTouch);
            }
            if (forwarded != null)
            {
                forwarded.RemoveListener(Deliver);
                // Preserve listeners added after installation, as well as the original actions.
                if (action != null) forwarded.AddListener(action.Invoke);
            }
        }
    }
}
