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
            EditorGUILayout.HelpBox("Bind an object and method with + under an event. Click events fire once after a matching press and release. No application actions are assigned by default.", MessageType.Info);
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            for (bool enterChildren = true; property.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
                string help = EventHelp(property.propertyPath);
                if (help != null) EditorGUILayout.LabelField(help, EditorStyles.wordWrappedMiniLabel);
            }
            serializedObject.ApplyModifiedProperties();
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

        private static string EventHelp(string name)
        {
            switch (name)
            {
                case "onConfirmClick": return "Confirm / select. Assign your selection action.";
                case "onBackClick": return "Back / navigation. No automatic exit. Review the rig's backHome setting if your app handles Back.";
                case "onVolumeUpClick": return "Volume Up key. This event does not adjust system volume by itself.";
                case "onVolumeDownClick": return "Volume Down key. This event does not adjust system volume by itself.";
                case "onKnobButtonClick": return "Knob press. Requires a custom KnobButton mapping; not mapped by default.";
                case "onAdjustmentClick": return "Adjustment key. Requires a custom Adjustment mapping; no automatic IPD action.";
                case "onKnobRotated": return "Signed step delta (+ / -). Bind a Dynamic int method. Confirm physical direction on your device.";
                case "onKeyDown": return "First press; repeated Down packets are filtered. The argument identifies the key.";
                case "onKeyUp": return "Release of a held key; normally followed by a click.";
                case "onKeyClick": return "Click for any mapped key. Bind each business action once, here or in its per-key event.";
                case "onKeyCancelled": return "Held input cancelled by lifecycle/device/mapping changes or queue overflow. No click is generated.";
                default: return null;
            }
        }
    }
}
