using System;
using UnityEngine;

namespace Singray.InputSystem
{
    /// <summary>Hardware-specific values are configuration, not application behavior.</summary>
    [CreateAssetMenu(menuName = "Singray/Input/Key Mapping Profile", fileName = "SingrayKeyMapping")]
    public sealed class SingrayKeyMappingProfile : ScriptableObject
    {
        [Serializable]
        public struct Binding
        {
            public int rawType;
            public SingrayKeyCode key;
            public Binding(int type, SingrayKeyCode code) { rawType = type; key = code; }
        }

        [Tooltip("Optional device-name matches for automatic profile selection. Matching ignores case.")]
        public string[] deviceNameMatches = Array.Empty<string>();
        public Binding[] bindings = DefaultBindings();
        public int downState = 254;
        public int upState = 255;
        public int knobType = 17;
        public int clockwiseState = 99;
        public int anticlockwiseState = 101;

        internal static Binding[] DefaultBindings() => new[] {
            new Binding(1, SingrayKeyCode.Confirm), new Binding(3, SingrayKeyCode.Back),
            new Binding(10, SingrayKeyCode.VolumeUp), new Binding(11, SingrayKeyCode.VolumeDown) };

        internal bool Matches(string deviceName) => !string.IsNullOrEmpty(deviceName) && deviceNameMatches != null &&
            Array.Exists(deviceNameMatches, name => !string.IsNullOrWhiteSpace(name) && deviceName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

        internal string ValidateMapping()
        {
            if (downState == upState || clockwiseState == anticlockwiseState) return "Press/release and rotation states must be distinct.";
            if (bindings == null) return "Key bindings are missing.";
            var raw = new System.Collections.Generic.HashSet<int>();
            var keys = new System.Collections.Generic.HashSet<SingrayKeyCode>();
            foreach (var binding in bindings)
                if (binding.key == SingrayKeyCode.Unknown || !Enum.IsDefined(typeof(SingrayKeyCode), binding.key) || binding.rawType < 0 || !raw.Add(binding.rawType) || !keys.Add(binding.key))
                    return "Key bindings must have unique raw types and semantic keys.";
            if (raw.Contains(knobType) && (clockwiseState == downState || clockwiseState == upState || anticlockwiseState == downState || anticlockwiseState == upState))
                return "Rotation states must not overlap press/release states for the same raw type.";
            return null;
        }
    }
}
