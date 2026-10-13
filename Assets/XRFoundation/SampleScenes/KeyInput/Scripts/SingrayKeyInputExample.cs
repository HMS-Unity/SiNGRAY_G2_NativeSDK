using System.Collections.Generic;
using Singray.InputSystem;
using UnityEngine;

namespace Singray.Samples
{
    /// <summary>Optional diagnostic sample. No application actions are performed by the input module.</summary>
    [RequireComponent(typeof(SingrayKeyInput))]
    public sealed class SingrayKeyInputExample : MonoBehaviour
    {
        private SingrayKeyInput input;
        private readonly Queue<string> lines = new Queue<string>();
        private void Awake() => input = GetComponent<SingrayKeyInput>();
        private void OnEnable()
        {
            if (!input) input = GetComponent<SingrayKeyInput>();
            input.KeyDown += OnKey; input.KeyUp += OnKey; input.KeyClick += OnKey; input.KeyCancelled += OnKey; input.KnobRotated += OnKnob;
        }
        private void OnDisable()
        {
            if (!input) return;
            input.KeyDown -= OnKey; input.KeyUp -= OnKey; input.KeyClick -= OnKey; input.KeyCancelled -= OnKey; input.KnobRotated -= OnKnob;
        }
        private void OnKey(SingrayKeyEvent e) => Add($"{e.Key} {e.Action}; held={e.HeldSeconds:F3}s; raw={e.RawType}/{e.RawState}");
        private void OnKnob(SingrayKnobEvent e) => Add("Knob steps=" + e.Steps);
        private void Add(string text) { lines.Enqueue(text); while (lines.Count > 12) lines.Dequeue(); Debug.Log("[Singray Key Sample] " + text); }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, Mathf.Min(Screen.width - 40, 700), Screen.height - 40), GUI.skin.box);
            GUILayout.Label("Singray NativeSDK Key Input");
            GUILayout.Label("Listening: " + input.IsListening + "; mapping: " + input.ActiveMappingName);
            if (input.IsSimulation)
            {
                foreach (var key in new[] { SingrayKeyCode.Confirm, SingrayKeyCode.Back, SingrayKeyCode.VolumeUp, SingrayKeyCode.VolumeDown })
                    if (GUILayout.Button(key.ToString())) input.SimulateKeyClick(key);
                if (GUILayout.Button("Knob -1")) input.SimulateKnob(-1);
                if (GUILayout.Button("Knob +1")) input.SimulateKnob(1);
            }
            foreach (var line in lines) GUILayout.Label(line);
            GUILayout.EndArea();
        }
    }
}
