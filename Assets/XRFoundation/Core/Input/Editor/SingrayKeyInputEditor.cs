using Singray.InputSystem;
using UnityEditor;
using UnityEngine;

namespace Singray.InputSystem.Editor
{
    [CustomEditor(typeof(SingrayKeyInput))]
    public sealed class SingrayKeyInputEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var input = (SingrayKeyInput)target;
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("Add this component to your SDK scene and bind Click Events. Enter Play Mode to simulate input.", MessageType.Info); return; }
            EditorGUILayout.LabelField("Listening", input.IsListening.ToString());
            EditorGUILayout.LabelField("Events Received", input.HasReceivedEvents.ToString());
            if (!input.IsSimulation) EditorGUILayout.IntField("Native Start Result", input.LastNativeStartResult);
            EditorGUILayout.LabelField("Mapping", input.ActiveMappingName);
            EditorGUILayout.LabelField("Device", input.DeviceName);
            if (!input.IsSimulation) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input Simulation", EditorStyles.boldLabel);
            foreach (var key in new[] { SingrayKeyCode.Confirm, SingrayKeyCode.Back, SingrayKeyCode.VolumeUp, SingrayKeyCode.VolumeDown })
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(key.ToString(), GUILayout.Width(100));
                if (GUILayout.Button("Down")) input.SimulateKeyDown(key);
                if (GUILayout.Button("Up")) input.SimulateKeyUp(key);
                if (GUILayout.Button("Click")) input.SimulateKeyClick(key);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Knob -1")) input.SimulateKnob(-1);
            if (GUILayout.Button("Knob +1")) input.SimulateKnob(1);
            EditorGUILayout.EndHorizontal();
            Repaint();
        }
    }
}
