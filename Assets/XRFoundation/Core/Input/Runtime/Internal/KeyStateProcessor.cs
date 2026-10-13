using System;
using System.Collections.Generic;
using Singray.Foundation;

namespace Singray.InputSystem.Internal
{
    internal sealed class KeyStateProcessor
    {
        private readonly Dictionary<int, SingrayKeyCode> bindings = new Dictionary<int, SingrayKeyCode>();
        private readonly Dictionary<SingrayKeyCode, Press> pressed = new Dictionary<SingrayKeyCode, Press>();
        private readonly HashSet<PacketId> recentPackets = new HashSet<PacketId>();
        private readonly Queue<PacketId> packetOrder = new Queue<PacketId>();
        private int down = 254, up = 255, knob = 17, clockwise = 99, anticlockwise = 101;
        private struct Press { public double time, host; public long device; public int rawType; }
        private readonly struct PacketId : IEquatable<PacketId>
        {
            private readonly int type, state;
            private readonly long edge;
            private readonly double host;
            public PacketId(XvEvent e) { type = e.type; state = e.state; edge = e.edgeTimestampUs; host = edge > 0 ? 0 : e.hostTimestamp; }
            public bool Equals(PacketId other) => type == other.type && state == other.state && edge == other.edge && host.Equals(other.host);
            public override bool Equals(object obj) => obj is PacketId other && Equals(other);
            public override int GetHashCode() => unchecked(((type * 397 ^ state) * 397 ^ edge.GetHashCode()) * 397 ^ host.GetHashCode());
        }

        public event Action<SingrayKeyEvent> KeyEvent;
        public event Action<SingrayKnobEvent> KnobEvent;
        public bool IsPressed(SingrayKeyCode key) => pressed.ContainsKey(key);

        public void Configure(SingrayKeyMappingProfile profile)
        {
            string error = profile == null ? null : profile.ValidateMapping();
            if (error != null) throw new ArgumentException(error);
            bindings.Clear();
            foreach (var b in profile == null ? SingrayKeyMappingProfile.DefaultBindings() : profile.bindings) bindings.Add(b.rawType, b.key);
            down = profile ? profile.downState : 254; up = profile ? profile.upState : 255;
            knob = profile ? profile.knobType : 17; clockwise = profile ? profile.clockwiseState : 99; anticlockwise = profile ? profile.anticlockwiseState : 101;
            recentPackets.Clear(); packetOrder.Clear();
        }

        public void Process(XvEvent raw, double now)
        {
            // Timestamped duplicate packets can be separated by other key events.
            // Untimestamped rotary ticks remain distinct; never debounce by Unity frame.
            if (raw.edgeTimestampUs > 0 || (raw.hostTimestamp > 0 && !double.IsNaN(raw.hostTimestamp) && !double.IsInfinity(raw.hostTimestamp)))
            {
                var id = new PacketId(raw);
                if (!recentPackets.Add(id)) return;
                packetOrder.Enqueue(id);
                if (packetOrder.Count > 256) recentPackets.Remove(packetOrder.Dequeue());
            }
            if (raw.type == knob && (raw.state == clockwise || raw.state == anticlockwise))
            {
                KnobEvent?.Invoke(new SingrayKnobEvent(raw.state == clockwise ? 1 : -1, now, raw.edgeTimestampUs));
                return;
            }
            if (!bindings.TryGetValue(raw.type, out var key)) return;
            if (raw.state == down)
            {
                if (pressed.ContainsKey(key)) return;
                pressed.Add(key, new Press { time = now, host = raw.hostTimestamp, device = raw.edgeTimestampUs, rawType = raw.type });
                Emit(key, SingrayKeyAction.Down, raw, now, 0);
            }
            else if (raw.state == up && pressed.TryGetValue(key, out var press))
            {
                pressed.Remove(key);
                float held = (float)Math.Max(0, now - press.time);
                if (press.device > 0 && raw.edgeTimestampUs >= press.device)
                    held = (float)((raw.edgeTimestampUs - press.device) / 1000000d);
                else if (press.host > 0 && raw.hostTimestamp >= press.host && !double.IsInfinity(raw.hostTimestamp) && !double.IsNaN(raw.hostTimestamp))
                    held = (float)(raw.hostTimestamp - press.host);
                int releaseVersion = dispatchVersion;
                Emit(key, SingrayKeyAction.Up, raw, now, held);
                // A subscriber can disable/reset the component during KeyUp.
                if (dispatchVersion == releaseVersion) Emit(key, SingrayKeyAction.Click, raw, now, held);
            }
        }

        private int dispatchVersion;
        private void Emit(SingrayKeyCode key, SingrayKeyAction action, XvEvent raw, double now, float held)
        {
            KeyEvent?.Invoke(new SingrayKeyEvent(key, action, now, raw.edgeTimestampUs, held, raw.type, raw.state));
        }

        public void Cancel(double now)
        {
            dispatchVersion++;
            var snapshot = new List<KeyValuePair<SingrayKeyCode, Press>>(pressed);
            pressed.Clear(); recentPackets.Clear(); packetOrder.Clear();
            foreach (var item in snapshot)
                Emit(item.Key, SingrayKeyAction.Cancelled, new XvEvent { type = item.Value.rawType }, now, (float)Math.Max(0, now - item.Value.time));
        }
    }
}
