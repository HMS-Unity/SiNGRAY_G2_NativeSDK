using System;

namespace Singray.InputSystem
{
    public enum SingrayKeyCode { Unknown, Confirm, Back, VolumeUp, VolumeDown, KnobButton, Adjustment }
    public enum SingrayKeyAction { Down, Up, Click, Cancelled }
    public enum SingrayKeyInputMode { Auto, Simulation }

    /// <summary>Unmodified event values for diagnostics and device extensions.</summary>
    public readonly struct SingrayRawInputEvent
    {
        public int Type { get; }
        public int State { get; }
        public double HostTimestamp { get; }
        public long DeviceTimestampUs { get; }
        internal SingrayRawInputEvent(int type, int state, double host, long device)
        { Type = type; State = state; HostTimestamp = host; DeviceTimestampUs = device; }
    }

    /// <summary>A semantic key event delivered on the Unity main thread.</summary>
    [Serializable]
    public readonly struct SingrayKeyEvent
    {
        public SingrayKeyCode Key { get; }
        public SingrayKeyAction Action { get; }
        public double Time { get; }
        public long DeviceTimestampUs { get; }
        public float HeldSeconds { get; }
        public int RawType { get; }
        public int RawState { get; }
        internal SingrayKeyEvent(SingrayKeyCode key, SingrayKeyAction action, double time, long timestamp, float held, int type, int state)
        { Key = key; Action = action; Time = time; DeviceTimestampUs = timestamp; HeldSeconds = held; RawType = type; RawState = state; }
    }

    [Serializable]
    public readonly struct SingrayKnobEvent
    {
        public int Steps { get; }
        public double Time { get; }
        public long DeviceTimestampUs { get; }
        internal SingrayKnobEvent(int steps, double time, long timestamp)
        { Steps = steps; Time = time; DeviceTimestampUs = timestamp; }
    }
}
