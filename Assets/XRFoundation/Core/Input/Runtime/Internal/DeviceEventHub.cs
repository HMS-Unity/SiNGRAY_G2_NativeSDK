using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AOT;
using Singray.Foundation;
using UnityEngine;

namespace Singray.InputSystem.Internal
{
    /// <summary>One native registration shared by all SDK event consumers.</summary>
    internal static class DeviceEventHub
    {
        private static readonly object Gate = new object();
        private static readonly List<Subscription> Listeners = new List<Subscription>();
        private static Subscription[] rawListeners = Array.Empty<Subscription>();
        private static readonly ConcurrentQueue<Packet> Pending = new ConcurrentQueue<Packet>();
        private static readonly ConcurrentQueue<Exception> Errors = new ConcurrentQueue<Exception>();
        private static readonly XvSystemSetting.device_stream_callback NativeCallback = Receive;
        private static DeviceEventPump pump;
        private static int generation, simulationGeneration, queued, overflow, errorCount;
        private static bool paused, quitting;
        private static double nextAttempt;
        public static bool NativeStarted { get; private set; }
        public static int CancellationEpoch { get; private set; }
        private static int simulationCancellationEpoch;
        public static int GetCancellationEpoch(bool simulation) => simulation ? simulationCancellationEpoch : CancellationEpoch;
        public static int LastNativeStartResult { get; private set; }
        // Injectable internally for deterministic validation; never exposed to customers.
        internal static Func<bool> Ready = NativeReady;
        internal static Func<XvSystemSetting.device_stream_callback, int> Start = NativeStart;
        internal static Action Stop = NativeStop;

        private struct Packet { public XvEvent data; public int generation; public bool simulation; }
        private sealed class Subscription : IDisposable
        {
            internal Action<XvEvent> callback;
            internal bool native, rawThread;
            internal int active = 1;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref active, 0) == 0) return;
                lock (Gate) { Listeners.Remove(this); UpdateRawListeners(); }
            }
        }

        // Subscribe/dispose from Unity's main thread. Legacy callbacks retain their native-thread contract.
        public static IDisposable Subscribe(Action<XvEvent> callback, bool requireNative, bool rawThread = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (quitting) throw new InvalidOperationException("The application is shutting down.");
            if (pump == null && Application.isPlaying)
            {
                var go = new GameObject("Singray Device Events");
                UnityEngine.Object.DontDestroyOnLoad(go);
                pump = go.AddComponent<DeviceEventPump>();
            }
            var subscription = new Subscription { callback = callback, native = requireNative, rawThread = rawThread };
            lock (Gate) { Listeners.Add(subscription); UpdateRawListeners(); }
            return subscription;
        }

        private static void UpdateRawListeners() => Volatile.Write(ref rawListeners, Listeners.Where(s => s.native && s.rawThread).ToArray());

        [MonoPInvokeCallback(typeof(XvSystemSetting.device_stream_callback))]
        private static void Receive(XvEvent data)
        {
            if (Volatile.Read(ref paused) || Volatile.Read(ref quitting)) return;
            int epoch = Volatile.Read(ref generation);
            var raw = Volatile.Read(ref rawListeners);
            foreach (var listener in raw)
                if (Volatile.Read(ref listener.active) != 0)
                    try { listener.callback(data); }
                    catch (Exception error)
                    {
                        if (Interlocked.Increment(ref errorCount) <= 16) Errors.Enqueue(error);
                        else Interlocked.Decrement(ref errorCount);
                    }
            Enqueue(data, false, epoch);
        }

        public static void Simulate(XvEvent data) => Enqueue(data, true, Volatile.Read(ref simulationGeneration));
        private static void Enqueue(XvEvent data, bool simulation, int epoch)
        {
            if (Interlocked.Increment(ref queued) > 2048)
            {
                Interlocked.Decrement(ref queued);
                Interlocked.Exchange(ref overflow, 1);
                return;
            }
            Pending.Enqueue(new Packet { data = data, simulation = simulation, generation = epoch });
        }

        public static void Tick(double now)
        {
            Subscription[] snapshot;
            lock (Gate) snapshot = Listeners.ToArray();
            bool wanted = !paused && !quitting && snapshot.Any(s => s.native && Volatile.Read(ref s.active) != 0);
            bool ready = false;
            if (wanted)
                try { ready = Ready(); } catch (Exception error) { if (now >= nextAttempt) { Debug.LogException(error); nextAttempt = now + 5; } }
            if (NativeStarted && (!wanted || !ready)) StopStream();
            if (wanted && ready && !NativeStarted && now >= nextAttempt)
            {
                nextAttempt = now + 1;
                try
                {
                    // The vendor return value is retained, not guessed to be an ID or boolean.
                    LastNativeStartResult = Start(NativeCallback);
                    NativeStarted = true;
                }
                catch (Exception error) { Debug.LogException(error); nextAttempt = now + 5; }
            }
            if (Interlocked.Exchange(ref overflow, 0) != 0)
            {
                Invalidate(true);
                Debug.LogWarning("[Singray Input] Device event queue overflowed; pressed keys were cancelled.");
            }
            while (Errors.TryDequeue(out var error)) { Interlocked.Decrement(ref errorCount); Debug.LogException(error); }
            int budget = 2048;
            while (budget-- > 0 && Pending.TryDequeue(out var packet))
            {
                Interlocked.Decrement(ref queued);
                if (packet.generation != (packet.simulation ? Volatile.Read(ref simulationGeneration) : Volatile.Read(ref generation)) || paused) continue;
                foreach (var listener in snapshot)
                {
                    if (listener.rawThread || listener.native == packet.simulation || Volatile.Read(ref listener.active) == 0) continue;
                    try { listener.callback(packet.data); } catch (Exception error) { Debug.LogException(error); }
                }
            }
        }

        public static void Pause(bool value)
        {
            Volatile.Write(ref paused, value);
            if (value) { if (NativeStarted) StopStream(); Invalidate(true); }
            else nextAttempt = 0;
        }
        private static void StopStream()
        {
            NativeStarted = false;
            try { Stop(); } catch (Exception error) { Debug.LogException(error); }
            Invalidate();
        }
        private static void Invalidate(bool includeSimulation = false)
        {
            Interlocked.Increment(ref generation);
            CancellationEpoch++;
            if (!includeSimulation) return;
            Interlocked.Increment(ref simulationGeneration);
            simulationCancellationEpoch++;
            int budget = 2048;
            while (budget-- > 0 && Pending.TryDequeue(out _)) Interlocked.Decrement(ref queued);
        }

        public static void Shutdown()
        {
            Volatile.Write(ref quitting, true);
            if (NativeStarted) StopStream();
            Invalidate(true);
            lock (Gate) { foreach (var listener in Listeners) Interlocked.Exchange(ref listener.active, 0); Listeners.Clear(); UpdateRawListeners(); }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Shutdown();
            pump = null; quitting = paused = false; nextAttempt = 0;
            while (Errors.TryDequeue(out _)) Interlocked.Decrement(ref errorCount);
            Ready = NativeReady; Start = NativeStart; Stop = NativeStop;
        }
        private static bool NativeReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }
        private static int NativeStart(XvSystemSetting.device_stream_callback callback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return XvSystemSetting.xslam_start_event_stream(callback);
#else
            throw new PlatformNotSupportedException("Native device events require an Android device.");
#endif
        }
        private static void NativeStop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            XvSystemSetting.xslam_stop_event_stream();
#endif
        }
    }
}
