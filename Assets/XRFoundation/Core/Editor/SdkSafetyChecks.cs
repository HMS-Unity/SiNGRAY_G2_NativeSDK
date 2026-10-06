using System;
using UnityEditor;
using UnityEngine;

namespace Singray.Foundation
{
    // Editor-only regression checks; no native device or APK build is required.
    internal static class SdkSafetyChecks
    {
        [MenuItem("Singray/Validation/Run Build Validation Checks")]
        public static void Run()
        {
            string product = PlayerSettings.productName;
            string identifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string version = PlayerSettings.bundleVersion;
            int versionCode = PlayerSettings.Android.bundleVersionCode;
            bool signing = PlayerSettings.Android.useCustomKeystore;
            Expect<InvalidOperationException>(() => ProjectBuild.ValidateScenes(new string[0]));
            Expect<InvalidOperationException>(() => ProjectBuild.ValidateScenes(new[] { "Assets/__missing_safety_test__.unity" }));
            ProjectBuild.ValidateScenes(new[] { "Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer.unity" });
            Check(product == PlayerSettings.productName &&
                  identifier == PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) &&
                  version == PlayerSettings.bundleVersion &&
                  versionCode == PlayerSettings.Android.bundleVersionCode &&
                  signing == PlayerSettings.Android.useCustomKeystore,
                  "Scene validation changed project identity or signing settings.");

            Debug.Log("Singray build validation checks PASSED: scene validation and project settings preservation.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name);
        }
    }
}
