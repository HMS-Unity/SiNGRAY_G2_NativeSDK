# Singray NativeSDK Key Input

This module is part of NativeSDK 4.2. It exposes device keys without depending on MRTK, an application launcher, audio, scanning, or a demo controller. The public namespace is `Singray.InputSystem`; this avoids shadowing `UnityEngine.Input` in existing SDK namespaces.

## Quick start: Inspector

1. Open an existing NativeSDK scene containing its initialized device rig.
2. Add **Singray > Input > Key Input** to a GameObject, or drag `Prefabs/SingrayKeyInput.prefab` into the scene.
3. Leave Runtime Mode at **Auto**. Android players use the native device; the Editor and other platforms use simulation.
4. Assign your methods under **Click Events**. For example, bind Confirm to your selection method and Back to your navigation method.
5. Enter Play Mode and use **Input Simulation** in the Inspector. The customer component does not capture arbitrary keyboard shortcuts.

Enable/disable the component to subscribe/unsubscribe. No customer-owned singleton or native start/stop call is required. Multiple components may observe the same input; each has its own key state. Use one application input controller if only one business action should occur per click.

The key-input module never quits an application, changes volume or IPD, clicks a UI button, or plays a sound automatically. Assign those actions explicitly. An existing NativeSDK rig may independently handle the operating-system Back/Home keys; turn off that rig's `backHome` setting when your application owns Back navigation.

## C# integration

```csharp
using Singray.InputSystem;
using UnityEngine;

[RequireComponent(typeof(SingrayKeyInput))]
public sealed class CustomerKeys : MonoBehaviour
{
    private SingrayKeyInput input;
    private void Awake() => input = GetComponent<SingrayKeyInput>();
    private void OnEnable()
    {
        input.KeyClick += HandleClick;
        input.KnobRotated += HandleRotation;
    }
    private void OnDisable()
    {
        input.KeyClick -= HandleClick;
        input.KnobRotated -= HandleRotation;
    }
    private void HandleClick(SingrayKeyEvent e)
    {
        if (e.Key == SingrayKeyCode.Confirm) ConfirmSelection();
        if (e.Key == SingrayKeyCode.Back) GoBack();
    }
    private void HandleRotation(SingrayKnobEvent e) => AdjustValue(e.Steps);
    private void ConfirmSelection() { /* Customer action. */ }
    private void GoBack() { /* Customer action. */ }
    private void AdjustValue(int steps) { /* Customer action. */ }
}
```

Use `IsPressed(key)` for held state. Subscribe to `KeyDown`, `KeyUp`, `KeyClick`, `KeyCancelled`, and `KnobRotated` for complete transitions. `RawEvent` exposes unmodified event values, including unknown device extensions, wearing state, and ambient-light packets. All new component callbacks execute on the Unity main thread. `SetMappingProfile(profile)` changes mappings on the main thread and cancels held keys first.

`SingrayKeyEvent` includes the semantic key, action, delivery time in seconds, device timestamp in microseconds, held duration, and raw type/state. Held duration uses monotonic device timestamps when available, then host timestamps, then SDK receipt time. `SingrayKnobEvent.Steps` is positive for configured clockwise steps and negative for anticlockwise steps.

## Device mappings

The default table follows the existing NativeSDK application key listener:

| Raw type | Semantic input |
| --- | --- |
| 1 | Confirm |
| 3 | Back |
| 10 | Volume Up |
| 11 | Volume Down |
| 17 / state 99 | Knob +1 |
| 17 / state 101 | Knob -1 |

Press is state 254; release is state 255. These values are a default table, not a guarantee for every hardware/firmware variant. In particular, rotary direction must be checked on the target device.

Create a profile through **Assets > Create > Singray > Input > Key Mapping Profile**. `Profiles/NativeSDKDefault.asset` provides an editable starting point. Assign an explicit profile for a known device. Alternatively, add profiles to Automatic Profiles with device-name matches; the SDK compares those strings with the connected device name, ignoring case. The first match wins. With no match, the default table is used and Active Mapping Name remains `NativeSDK Default`. Unknown hardware is not automatically inferred.

Use a single semantic key for each physical input. For example, map type 1 to Confirm or KnobButton, not both. Bindings reject duplicate raw types/keys and conflicting rotation/press states. Mapping changes made through code should use `SetMappingProfile`; Inspector runtime-mode changes take effect when the component is re-enabled.

## Event contract

- First Down sets pressed state and emits KeyDown. Repeated Down does not generate another press.
- A matching Up clears pressed state, emits KeyUp, then KeyClick. A release without a press does not generate a click.
- Disabling or remapping during KeyUp suppresses the subsequent click.
- Disabling, device loss, application pause, and queue overflow cancel held keys. Cancellation never generates a click.
- Timestamped duplicate packets are filtered with a bounded recent-packet history. Distinct rotary ticks arriving in one frame are retained. Untimestamped rotary ticks are not discarded based on frame timing.
- There is no automatic long press, double click, or key repeat in this version.
- C# subscribers are invoked separately; a failing subscriber is logged and does not block others. Inspector UnityEvents retain Unity's own listener-dispatch behavior, while the SDK catches errors at each event boundary.

## Runtime architecture

```text
Native callback -> DeviceEventHub -> main-thread queue
                    |                     |
                    |                 KeyStateProcessor
                    |                     |
              legacy subscribers      SingrayKeyInput
              (native thread)         C# events / UnityEvents
                    |
              G2 subscribers also share the registration
              and receive events on the main thread
```

The hub owns one native registration and a pinned static AOT callback. A persistent internal pump is created on the first runtime subscription. The first native consumer starts registration when the SDK reports readiness; the final consumer releases it. Device loss stops registration and invalidates queued native packets. Reconnection retries registration. Native and simulated consumers are separated so native teardown does not discard independent simulation events.

The queue is limited to 2048 packets. Overflow invalidates pending events and cancels held keys rather than leaving a stuck press. No GameObject or customer code is accessed by the new key module on the native callback thread.

`IsListening` reports that the component owns an active subscription and a native registration has been attempted, or that simulation is active. `HasReceivedEvents` confirms actual callback delivery. `LastNativeStartResult` exposes the unmodified vendor return value. The current vendor API has no documented return-value contract in this repository; the implementation deliberately does not reinterpret it as an ID or success flag. Check callback delivery and that result on the target firmware before release.

The existing SDK must initialize the native device. This module does not replace the device rig or initialize SLAM itself. Subscriptions, simulation calls, and component/profile changes must be made from Unity's main thread.

## Compatibility

The legacy system-setting start method replaces only the subscription owned by that manager instance. Its callback still runs on the native callback thread. Its stop method releases that subscription, and does not stop other SDK consumers. Destroying the manager releases its subscription.

The G2 backend now acquires its own main-thread subscription instead of replacing the system-setting callback. Its public event API remains unchanged. Existing wearing and light events continue through that API.

Use the SDK subscription APIs; calling the underlying native event start/stop functions directly bypasses shared ownership and can interrupt other consumers.

## Example and validation

Open `Assets/XRFoundation/SampleScenes/KeyInput/KeyInput.unity` for a minimal simulation/event-log example. It has no initialized native device rig: for hardware testing, add the input prefab to an existing NativeSDK scene instead. The example is optional and outside the runtime module.

**Singray > Input > Validate Key Input** runs deterministic event/lifecycle checks and Android player-script compilation. Its report is `Builds/KeyInput/validation.txt`. **Singray > Input > Create Key Input Example Scene** recreates the example and prefab without replacing the active scene.

**Singray > Input > Run Key Input Play Validation** tests the actual runtime pump, C# and Inspector events, multiple components, cancellation, re-enable, and disable-on-release behavior. Save the active scene first. The test restores the original scene when complete; its report is `Builds/KeyInput/play-validation.txt`.

For device acceptance testing, verify the key table and rotation direction, press/release pairs, simultaneous keys, rapid rotation, disconnect/reconnect while held, application pause/resume, two independent listeners, and release of one listener while the other remains active. Editor tests do not establish hardware callback delivery or native return-value semantics.
