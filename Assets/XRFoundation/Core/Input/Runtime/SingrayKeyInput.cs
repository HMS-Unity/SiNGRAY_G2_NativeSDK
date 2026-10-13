using System;
using System.Threading;
using Singray.Foundation;
using Singray.InputSystem.Internal;
using UnityEngine;
using UnityEngine.Events;

namespace Singray.InputSystem
{
    [Serializable] public sealed class SingrayKeyUnityEvent : UnityEvent<SingrayKeyEvent> { }
    [Serializable] public sealed class SingrayKnobUnityEvent : UnityEvent<int> { }

    /// <summary>Device keys only. Application actions remain under customer control.</summary>
    [AddComponentMenu("Singray/Input/Key Input")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class SingrayKeyInput : MonoBehaviour
    {
        [SerializeField] private SingrayKeyInputMode runtimeMode = SingrayKeyInputMode.Auto;
        [Tooltip("Optional explicit mapping. When empty, matching automatic profiles are tried before the NativeSDK default table.")]
        [SerializeField] private SingrayKeyMappingProfile mappingProfile;
        [SerializeField] private SingrayKeyMappingProfile[] automaticProfiles = Array.Empty<SingrayKeyMappingProfile>();
        [SerializeField] private bool debugLogging;

        [Header("Click Events")]
        [SerializeField] private UnityEvent onConfirmClick = new UnityEvent();
        [SerializeField] private UnityEvent onBackClick = new UnityEvent();
        [SerializeField] private UnityEvent onVolumeUpClick = new UnityEvent();
        [SerializeField] private UnityEvent onVolumeDownClick = new UnityEvent();
        [SerializeField] private UnityEvent onKnobButtonClick = new UnityEvent();
        [SerializeField] private UnityEvent onAdjustmentClick = new UnityEvent();
        [SerializeField] private SingrayKnobUnityEvent onKnobRotated = new SingrayKnobUnityEvent();
        [Header("Advanced Events")]
        [SerializeField] private SingrayKeyUnityEvent onKeyDown = new SingrayKeyUnityEvent();
        [SerializeField] private SingrayKeyUnityEvent onKeyUp = new SingrayKeyUnityEvent();
        [SerializeField] private SingrayKeyUnityEvent onKeyClick = new SingrayKeyUnityEvent();
        [SerializeField] private SingrayKeyUnityEvent onKeyCancelled = new SingrayKeyUnityEvent();

        public event Action<SingrayKeyEvent> KeyDown;
        public event Action<SingrayKeyEvent> KeyUp;
        public event Action<SingrayKeyEvent> KeyClick;
        public event Action<SingrayKeyEvent> KeyCancelled;
        public event Action<SingrayKnobEvent> KnobRotated;
        public event Action<SingrayRawInputEvent> RawEvent;
        public UnityEvent OnConfirmClick => onConfirmClick;
        public UnityEvent OnBackClick => onBackClick;
        public UnityEvent OnVolumeUpClick => onVolumeUpClick;
        public UnityEvent OnVolumeDownClick => onVolumeDownClick;
        public SingrayKnobUnityEvent OnKnobRotated => onKnobRotated;
        public bool IsSimulation { get; private set; }
        public bool HasReceivedEvents { get; private set; }
        public bool IsListening => subscription != null && (IsSimulation || DeviceEventHub.NativeStarted);
        public int LastNativeStartResult => DeviceEventHub.LastNativeStartResult;
        public string DeviceName { get; private set; } = "";
        public string ActiveMappingName => activeProfile ? activeProfile.name : "NativeSDK Default";
        public bool IsPressed(SingrayKeyCode key) => processor.IsPressed(key);

        private readonly KeyStateProcessor processor = new KeyStateProcessor();
        private SingrayKeyMappingProfile activeProfile;
        private IDisposable subscription;
        private int cancellationEpoch;
        private bool deviceMappingResolved;
        private static long simulationTimestamp;

        private void OnEnable()
        {
            IsSimulation = runtimeMode == SingrayKeyInputMode.Simulation || Application.platform != RuntimePlatform.Android || Application.isEditor;
            HasReceivedEvents = false;
            processor.KeyEvent += DispatchKey;
            processor.KnobEvent += DispatchKnob;
            try { SelectMapping(mappingProfile); }
            catch (Exception error) { Debug.LogException(error, this); processor.KeyEvent -= DispatchKey; processor.KnobEvent -= DispatchKnob; return; }
            deviceMappingResolved = IsSimulation;
            cancellationEpoch = DeviceEventHub.GetCancellationEpoch(IsSimulation);
            subscription = DeviceEventHub.Subscribe(Receive, !IsSimulation);
        }
        private void OnDisable()
        {
            subscription?.Dispose(); subscription = null;
            processor.Cancel(Time.realtimeSinceStartupAsDouble);
            processor.KeyEvent -= DispatchKey; processor.KnobEvent -= DispatchKnob;
        }
        private void Update() => SynchronizeDeviceState();
        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            processor.Cancel(Time.realtimeSinceStartupAsDouble);
            HasReceivedEvents = false;
        }

        private void SynchronizeDeviceState()
        {
            if (cancellationEpoch != DeviceEventHub.GetCancellationEpoch(IsSimulation))
            {
                cancellationEpoch = DeviceEventHub.GetCancellationEpoch(IsSimulation);
                HasReceivedEvents = false;
                processor.Cancel(Time.realtimeSinceStartupAsDouble);
                deviceMappingResolved = IsSimulation;
            }
            if (!IsSimulation && DeviceEventHub.NativeStarted && !deviceMappingResolved)
            {
                deviceMappingResolved = true;
#if UNITY_ANDROID && !UNITY_EDITOR
                try { DeviceName = Singray.SystemEvents.AndroidConnection.getBoxName() ?? ""; }
                catch (Exception error) { if (debugLogging) Debug.LogWarning("[Singray Input] Device name unavailable: " + error.Message, this); }
#endif
                if (!mappingProfile && automaticProfiles != null)
                    foreach (var candidate in automaticProfiles)
                        if (candidate && candidate.Matches(DeviceName)) { SelectMapping(candidate); break; }
                if (debugLogging) Debug.Log("[Singray Input] Mapping: " + ActiveMappingName + "; device: " + DeviceName, this);
            }
        }
        private void Receive(XvEvent data)
        {
            SynchronizeDeviceState();
            if (subscription == null || !isActiveAndEnabled) return;
            HasReceivedEvents = true;
            InvokeSafe(RawEvent, new SingrayRawInputEvent(data.type, data.state, data.hostTimestamp, data.edgeTimestampUs));
            if (subscription == null || !isActiveAndEnabled) return;
            if (debugLogging) Debug.Log($"[Singray Input] type={data.type} state={data.state} timestamp={data.edgeTimestampUs}", this);
            processor.Process(data, Time.realtimeSinceStartupAsDouble);
        }

        /// <summary>Change the explicit mapping on the main thread. Held keys are cancelled.</summary>
        public void SetMappingProfile(SingrayKeyMappingProfile profile)
        {
            if (profile && profile.ValidateMapping() != null) throw new ArgumentException(profile.ValidateMapping());
            mappingProfile = profile;
            SelectMapping(profile);
            deviceMappingResolved = IsSimulation;
        }
        private void SelectMapping(SingrayKeyMappingProfile profile)
        {
            if (profile && profile.ValidateMapping() != null) throw new ArgumentException(profile.ValidateMapping());
            processor.Cancel(Time.realtimeSinceStartupAsDouble);
            processor.Configure(profile); activeProfile = profile;
        }

        private void DispatchKey(SingrayKeyEvent e)
        {
            switch (e.Action)
            {
                case SingrayKeyAction.Down: InvokeSafe(KeyDown, e); InvokeSafe(() => onKeyDown.Invoke(e)); break;
                case SingrayKeyAction.Up: InvokeSafe(KeyUp, e); InvokeSafe(() => onKeyUp.Invoke(e)); break;
                case SingrayKeyAction.Cancelled: InvokeSafe(KeyCancelled, e); InvokeSafe(() => onKeyCancelled.Invoke(e)); break;
                case SingrayKeyAction.Click:
                    InvokeSafe(KeyClick, e); InvokeSafe(() => onKeyClick.Invoke(e));
                    UnityEvent simple = null;
                    switch (e.Key)
                    {
                        case SingrayKeyCode.Confirm: simple = onConfirmClick; break;
                        case SingrayKeyCode.Back: simple = onBackClick; break;
                        case SingrayKeyCode.VolumeUp: simple = onVolumeUpClick; break;
                        case SingrayKeyCode.VolumeDown: simple = onVolumeDownClick; break;
                        case SingrayKeyCode.KnobButton: simple = onKnobButtonClick; break;
                        case SingrayKeyCode.Adjustment: simple = onAdjustmentClick; break;
                    }
                    if (simple != null) InvokeSafe(simple.Invoke);
                    break;
            }
        }
        private void DispatchKnob(SingrayKnobEvent e) { InvokeSafe(KnobRotated, e); InvokeSafe(() => onKnobRotated.Invoke(e.Steps)); }
        private void InvokeSafe(Action action) { try { action(); } catch (Exception error) { Debug.LogException(error, this); } }
        private void InvokeSafe<T>(Action<T> action, T value)
        {
            if (action == null) return;
            foreach (Action<T> handler in action.GetInvocationList()) InvokeSafe(() => handler(value));
        }

        /// <summary>Simulation follows the same queue and state processor as hardware input.</summary>
        public void SimulateKeyDown(SingrayKeyCode key) => SimulateKey(key, true);
        public void SimulateKeyUp(SingrayKeyCode key) => SimulateKey(key, false);
        public void SimulateKeyClick(SingrayKeyCode key) { SimulateKeyDown(key); SimulateKeyUp(key); }
        private void SimulateKey(SingrayKeyCode key, bool down)
        {
            if (!CanSimulate()) return;
            var bindings = activeProfile ? activeProfile.bindings : SingrayKeyMappingProfile.DefaultBindings();
            foreach (var binding in bindings)
                if (binding.key == key)
                {
                    QueueSimulation(binding.rawType, down ? (activeProfile ? activeProfile.downState : 254) : (activeProfile ? activeProfile.upState : 255));
                    return;
                }
            Debug.LogWarning("[Singray Input] No binding for " + key, this);
        }
        public void SimulateKnob(int steps)
        {
            if (!CanSimulate()) return;
            int count = (int)Math.Min(128, Math.Abs((long)steps));
            for (int i = 0; i < count; i++) QueueSimulation(activeProfile ? activeProfile.knobType : 17,
                steps > 0 ? (activeProfile ? activeProfile.clockwiseState : 99) : (activeProfile ? activeProfile.anticlockwiseState : 101));
        }
        private bool CanSimulate() => Application.isPlaying && isActiveAndEnabled && IsSimulation && subscription != null;
        private static void QueueSimulation(int type, int state)
        {
            double time = Time.realtimeSinceStartupAsDouble;
            long previous, timestamp;
            do
            {
                previous = Volatile.Read(ref simulationTimestamp);
                timestamp = Math.Max(previous + 1, (long)(time * 1000000));
            }
            while (Interlocked.CompareExchange(ref simulationTimestamp, timestamp, previous) != previous);
            DeviceEventHub.Simulate(new XvEvent { type = type, state = state, hostTimestamp = time, edgeTimestampUs = timestamp });
        }
    }
}
