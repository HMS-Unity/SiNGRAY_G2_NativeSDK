using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Singray.InputSystem;
using Singray.UI;
using TMPro;
using UnityEngine;
namespace Singray.Samples
{
    /// <summary>Visible optional key-input example for an initialized SDK XR scene.</summary>
    [RequireComponent(typeof(SingrayKeyInput))]
    public sealed class KeyInputDemo : MonoBehaviour
    {
        [SerializeField] private SingrayKeyInput input;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private TMP_Text[] keyStates;
        [SerializeField] private GameObject simulationControls;
        private readonly Queue<string> lines = new Queue<string>();
        private readonly int[] clicks = new int[7];
        private readonly string[] lastAction = new string[7];
        private int rawPackets, knobSteps;
        private bool dirty = true;
        private void Awake()
        {
            if (!input) input = GetComponent<SingrayKeyInput>();
            foreach (var button in GetComponentsInChildren<Interactable>(true)) MrtkClickGate.Install(button);
        }
        private void OnEnable()
        {
            if (!input) input = GetComponent<SingrayKeyInput>();
            input.KeyDown += OnKey; input.KeyUp += OnKey; input.KeyClick += OnKey; input.KeyCancelled += OnKey;
            input.KnobRotated += OnKnob; input.RawEvent += OnRaw;
            dirty = true;
        }
        private void OnDisable()
        {
            if (input)
            {
                input.KeyDown -= OnKey; input.KeyUp -= OnKey; input.KeyClick -= OnKey; input.KeyCancelled -= OnKey;
                input.KnobRotated -= OnKnob; input.RawEvent -= OnRaw;
            }
            lines.Clear();for (int i = 0; i < lastAction.Length; i++) lastAction[i] = null;
            dirty = true;
        }
        private void OnRaw(SingrayRawInputEvent value) { rawPackets++; dirty = true; }
        private void OnKey(SingrayKeyEvent value)
        {
            int index = (int)value.Key;
            if (index >= 0 && index < lastAction.Length)
            {
                lastAction[index] = value.Action.ToString();
                if (value.Action == SingrayKeyAction.Click) clicks[index]++;
            }
            Add($"{value.Key}  {value.Action}  held {value.HeldSeconds:F3}s  raw {value.RawType}/{value.RawState}");
        }
        private void OnKnob(SingrayKnobEvent value) { knobSteps += value.Steps; Add("Knob steps " + value.Steps + "  total " + knobSteps); }
        private void Add(string message)
        {
            lines.Enqueue(message);while (lines.Count > 10) lines.Dequeue();dirty = true;
            Debug.Log("[KEY-DEMO] " + message);
        }
        private void Update()
        {
            if (!input) return;
            if (simulationControls && simulationControls.activeSelf != input.IsSimulation) simulationControls.SetActive(input.IsSimulation);
            string state = (input.IsSimulation ? "EDITOR SIMULATION" : "DEVICE INPUT") + "  |  " +
                (input.IsListening ? "Listening" : "Waiting for SDK") + "\nMapping: " + input.ActiveMappingName +
                "  |  packets: " + rawPackets + "  |  knob total: " + knobSteps;
            if (statusText && statusText.text != state) statusText.text = state;
            if (!dirty) return;
            if (logText) logText.text = lines.Count == 0 ? "No events yet. Press a hardware key or use the Editor simulation buttons." : string.Join("\n", lines);
            if (keyStates != null) for (int i = 0; i < Mathf.Min(keyStates.Length, 6); i++)
            {
                var key = (SingrayKeyCode)(i + 1);
                if (keyStates[i] && i >= 4 && lastAction[i + 1] == null)
                { keyStates[i].text = key + " (optional binding)\nNo event received"; continue; }
                if (keyStates[i]) keyStates[i].text = key + "   " + (input.IsPressed(key) ? "HELD" : "Released") +
                    "\nLast: " + (lastAction[i + 1] ?? "--") + "    Clicks: " + clicks[i + 1];
            }
            dirty = false;
        }
        public void SimulateClick(int key)
        {
            if (input && input.IsSimulation && key >= 1 && key <= 6) input.SimulateKeyClick((SingrayKeyCode)key);
        }
        public void SimulateKnob(int steps) { if (input && input.IsSimulation) input.SimulateKnob(steps); }
        public void ClearLog() { lines.Clear(); dirty = true; }
    }
}
