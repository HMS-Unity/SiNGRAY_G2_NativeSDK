using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Singray.G2.Validation.Editor
{
    public static class SingrayG2ValidationSceneBuilder
    {
        public const string ScenePath = "Assets/XRFoundation/SampleScenes/SingrayG2Validation/Scenes/SingrayG2Validation.unity";
        public const string Mrtk2ScenePath = "Assets/XRFoundation/SampleScenes/MRTK2/Scenes/MRTK2.unity";
        private const string BuildPathArgument = "-codexBuildPath";
        private const string Mrtk2BuildPathArgument = "-codexMrtk2BuildPath";



        [MenuItem("Singray/G2/Regenerate Validation Scene")]
        public static void CreateScene()
        {
            string directory = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";

            GameObject validationObject = new GameObject("Singray G2 Validation");
            validationObject.AddComponent<SingrayG2ValidationController>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save Singray G2 validation scene.");
            }

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Singray G2 validation scene generated: " + ScenePath);
        }

        [MenuItem("Singray/G2/Build Android Validation APK")]
        public static void BuildAndroid()
        {
            CreateScene();

            string outputPath = GetCommandLineValue(BuildPathArgument);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "build", "Singray-G2-Validation.apk"));
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Singray G2 validation build result={summary.result}, size={summary.totalSize}, output={outputPath}");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Singray G2 validation build failed: {summary.result}, errors={summary.totalErrors}");
            }
        }

        [MenuItem("Singray/G2/Build Android MRTK2 Interaction APK")]
        public static void BuildAndroidMrtk2Interaction()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Mrtk2ScenePath) == null)
            {
                throw new FileNotFoundException("MRTK2 interaction scene was not found.", Mrtk2ScenePath);
            }

            string outputPath = GetCommandLineValue(Mrtk2BuildPathArgument);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "build",
                    "SDK.apk"));
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string previousCompanyName = PlayerSettings.companyName;
            string previousProductName = PlayerSettings.productName;
            string previousApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            bool previousOverrideDefaultApplicationIdentifier = GetOverrideDefaultApplicationIdentifier();
            string previousBundleVersion = PlayerSettings.bundleVersion;
            int previousBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            AndroidSdkVersions previousMinSdkVersion = PlayerSettings.Android.minSdkVersion;
            bool previousAllowUnsafeCode = PlayerSettings.allowUnsafeCode;
            bool previousShowSplashScreen = PlayerSettings.SplashScreen.show;
            InsecureHttpOption previousInsecureHttpOption = PlayerSettings.insecureHttpOption;

            try
            {
                // Build the integrated SDK using Singray's own application identity.
                // Vendor package names are intentionally not used here.
                PlayerSettings.companyName = "singray";
                PlayerSettings.productName = "SDK";
                PlayerSettings.SetApplicationIdentifier(
                    BuildTargetGroup.Android,
                    "com.singray.SDK");
                SetOverrideDefaultApplicationIdentifier(true);
                PlayerSettings.bundleVersion = "0.1";
                PlayerSettings.Android.bundleVersionCode = 1;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
                PlayerSettings.allowUnsafeCode = false;
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    // Build exactly one dedicated MRTK2 scene. It contains XvXRManager,
                    // gesture input, hand rays, near/far press and one/two-hand manipulation.
                    scenes = new[] { Mrtk2ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.CompressWithLz4
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                Debug.Log($"Singray G2 MRTK2 interaction build result={summary.result}, size={summary.totalSize}, output={outputPath}");
                if (summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Singray G2 MRTK2 interaction build failed: {summary.result}, errors={summary.totalErrors}");
                }
            }
            finally
            {
                PlayerSettings.companyName = previousCompanyName;
                PlayerSettings.productName = previousProductName;
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, previousApplicationIdentifier);
                SetOverrideDefaultApplicationIdentifier(previousOverrideDefaultApplicationIdentifier);
                PlayerSettings.bundleVersion = previousBundleVersion;
                PlayerSettings.Android.bundleVersionCode = previousBundleVersionCode;
                PlayerSettings.Android.minSdkVersion = previousMinSdkVersion;
                PlayerSettings.allowUnsafeCode = previousAllowUnsafeCode;
                PlayerSettings.SplashScreen.show = previousShowSplashScreen;
                PlayerSettings.insecureHttpOption = previousInsecureHttpOption;
                AssetDatabase.SaveAssets();
            }
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(item => string.Equals(item.path, ScenePath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            EditorBuildSettings.scenes = scenes
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, false) })
                .ToArray();
        }

        private static string GetCommandLineValue(string argumentName)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; ++i)
            {
                if (string.Equals(arguments[i], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        private static bool GetOverrideDefaultApplicationIdentifier()
        {
            UnityEngine.Object playerSettingsObject = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .FirstOrDefault();
            if (playerSettingsObject == null)
            {
                return false;
            }

            SerializedProperty property = new SerializedObject(playerSettingsObject)
                .FindProperty("overrideDefaultApplicationIdentifier");
            return property != null && property.boolValue;
        }

        private static void SetOverrideDefaultApplicationIdentifier(bool value)
        {
            UnityEngine.Object playerSettingsObject = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .FirstOrDefault();
            if (playerSettingsObject == null)
            {
                return;
            }

            SerializedObject serializedPlayerSettings = new SerializedObject(playerSettingsObject);
            SerializedProperty property = serializedPlayerSettings
                .FindProperty("overrideDefaultApplicationIdentifier");
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serializedPlayerSettings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
