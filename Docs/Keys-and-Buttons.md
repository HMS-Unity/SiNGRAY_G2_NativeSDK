# SiNGRAY Native SDK: Keys and MRTK Buttons

This guide covers the `dev/4.2` SDK device-key component and MRTK 2.8.3 hand-menu buttons. Device keys deliver events for customer actions. MRTK buttons use a physical press-cycle guard to prevent repeated actions from overlapping touch and input-release callbacks.

## Device keys

1. Open an SDK scene containing an initialized device rig.
2. Add **Singray > Input > Key Input** to a GameObject, or use `Assets/XRFoundation/Core/Input/Prefabs/SingrayKeyInput.prefab`.
3. Keep Runtime Mode at **Auto**: Android players use native input; the Editor uses simulation.
4. Bind the component's **Click Events** in the Inspector, or subscribe to its C# events.

The default hardware mapping is:

| Raw type / state | Input |
| --- | --- |
| 1 / 254, 255 | Confirm down, up |
| 3 / 254, 255 | Back down, up |
| 10 / 254, 255 | Volume Up down, up |
| 11 / 254, 255 | Volume Down down, up |
| 17 / 99 | Knob clockwise, +1 step |
| 17 / 101 | Knob anticlockwise, -1 step |

Confirm is the default semantic name for raw type 1. To expose that input as `KnobButton`, use a mapping profile that replaces the Confirm binding; do not map one raw type to two keys. Verify the table and rotation direction on the target firmware.

`KeyDown` reports the first press. Repeated down packets do not create additional presses. A matching release emits `KeyUp`, then `KeyClick`; an unmatched release does not click. `KeyCancelled` reports interrupted holds, including component disable and device loss. `KnobRotated` reports signed steps. Use `IsPressed(key)` for held state. These component callbacks run on Unity's main thread.

Bind a business action once: subscribing to both `KeyClick` and the corresponding Inspector click event with the same action will execute that action twice. For an action intended to start immediately on press, subscribe to `KeyDown` instead of binding it again to `KeyClick`.

The component does not automatically click UI, quit, change volume, or adjust IPD. Applications choose those actions. If the application owns Back navigation, review the device rig's `backHome` setting to avoid competing Back handling.

For C# integration, automatic device profiles, shared native-event ownership, and simulation APIs, see the [Key Input API Guide](../Assets/XRFoundation/Core/Input/README.md).

## MRTK hand-menu buttons

`Singray.UI.MrtkClickGate` preserves the button's existing `OnClick` action and routes delivery through a press-cycle guard. It configures `PhysicalPressEventRouter.InteractableOnClick` to `EventOnClickCompletion`. During a physical touch cycle, an action is eligible only after the press threshold has been reached and the button is no longer pressing. Once delivered, subsequent callbacks in that cycle are suppressed.

The latch remains through touch-end and queued release callbacks, then resets after more than two frames once the button is no longer touching. This is a per-button physical-cycle guard; it does not impose a general cooldown on independent far-pointer or programmatic clicks outside a physical touch cycle.

The SDK recording hand menu and SDK Samples Hub recording/RTSP controls already install the guard. For a custom MRTK button, configure its action first and install the guard on the `Interactable`:

```csharp
using Microsoft.MixedReality.Toolkit.UI;
using Singray.UI;
using UnityEngine;

public sealed class CustomerButton : MonoBehaviour
{
    [SerializeField] private Interactable button;

    private void Start()
    {
        button.OnClick.AddListener(HandleClick);
        MrtkClickGate.Install(button);
    }

    private void HandleClick()
    {
        // Perform the customer action here.
    }
}
```

Use a standard MRTK pressable button with `Interactable`, `PressableButton`, and `PhysicalPressEventRouter` on the same GameObject. Serialized Inspector actions are retained. Runtime wrapping happens only in Play Mode; calling `Install` outside Play Mode configures routing without replacing serialized actions. Install the guard on each newly created button after setting up its listeners.

After runtime installation, do not replace `button.OnClick` or bind business actions directly to the replacement event: doing so can bypass the guard. Keep business listeners on the original action before installation. Adding the component alone does not install the event routing; call `Install`.

## Verification

For device keys:

1. In Play Mode, use **Input Simulation** in the input component's Inspector. Confirm one down/up/click sequence per simulated click and signed rotary steps.
2. Use `Assets/XRFoundation/SampleScenes/KeyInput/KeyInput.unity` for event logging. This example has no initialized device rig; add the input prefab to an existing SDK device scene for hardware tests.
3. Run **Singray > Input > Validate Key Input** and **Singray > Input > Run Key Input Play Validation**. Reports are written under `Builds/KeyInput`.
4. On G2, check all key mappings, rapid knob rotation, simultaneous keys, pause/resume, and disconnect/reconnect while a key is held. Cancellation should not produce a click.

For MRTK buttons, test the SDK recording menu and Samples Hub on G2:

1. Touch without reaching the press threshold, then withdraw: no action.
2. Press and hold: overlapping input-release callbacks must not execute the action while the button remains pressed.
3. Complete the press and withdraw: one action, with no second action from delayed release callbacks.
4. Perform two separate presses: each complete cycle should execute once.
5. Test far-pointer/pinch interaction and menu recreation as well as near-touch input. For recording and RTSP controls, confirm one start/stop transition per completed click.

Source inspection and Editor checks do not confirm native key delivery or physical hand interaction on hardware. Record the firmware, scene, interaction type, and observed action count when reporting a failure.
