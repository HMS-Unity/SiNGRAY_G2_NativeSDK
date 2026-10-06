using System;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEditor;
using UnityEngine;
using Singray.MixedReality.Toolkit.XvXR.Input;

namespace Singray.Foundation
{
    internal static class SingrayInputValidation
    {
        [MenuItem("Singray/Validation/Check MRTK Input Mappings")]
        public static void Run()
        {
            string[] paths = {
                "Assets/XRFoundation/Core/Sdk/MRTK/Profiles/XvXR Hand MixedRealityInputSystemProfile.asset",
                "Assets/XRFoundation/Core/Sdk/Joystick/XvXR Joystick MixedRealityInputSystemProfile.asset"
            };
            foreach (string path in paths)
            {
                var profile = AssetDatabase.LoadAssetAtPath<MixedRealityInputSystemProfile>(path);
                Require(profile != null && profile.ControllerMappingProfile != null, "Missing profile: " + path);
                var serialized = new SerializedObject(profile);
                Require(serialized.FindProperty("enableControllerMapping").boolValue, "Controller mapping disabled.");
                foreach (Type type in new[] { typeof(XvXRController), typeof(XvXRJoystickController) })
                foreach (Handedness side in new[] { Handedness.Left, Handedness.Right })
                {
                    var matches = profile.ControllerMappingProfile.MixedRealityControllerMappings
                        .Where(m => m.ControllerType.Type == type && m.Handedness == side).ToArray();
                    Require(matches.Length == 1, "Expected one mapping: " + type.FullName + " " + side);
                    var maps = matches[0].Interactions;
                    foreach (var map in maps)
                        Require(profile.InputActionsProfile.InputActions.Any(a => a == map.MixedRealityInputAction),
                            "Unresolved action: " + map.Description);
                    var pointer = maps.Single(m => m.InputType == DeviceInputType.SpatialPointer);
                    var grip = maps.Single(m => m.InputType == DeviceInputType.SpatialGrip);
                    Require(pointer.MixedRealityInputAction.Id == 4 && grip.MixedRealityInputAction.Id == 3,
                        "Pointer and grip actions must remain distinct.");
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/XRFoundation/Core/Foundation/MRTK2/Prefabs/ShellHandRayPointer.prefab");
            var synchronizer = prefab.GetComponent<ControllerPoseSynchronizer>();
            Require(synchronizer != null && !synchronizer.UseSourcePoseData && synchronizer.PoseAction.Id == 4,
                "Hand pointer must consume Pointer Pose action 4.");
            Debug.Log("Singray MRTK mapping validation PASSED: both profiles resolve left/right controller types and actions; pointer consumes action 4, distinct from grip action 3.");
        }
        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
