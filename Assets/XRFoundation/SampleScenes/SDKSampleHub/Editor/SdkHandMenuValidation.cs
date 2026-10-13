using System;
using System.IO;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.UI;
using Singray.Foundation.SampleScenes;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Singray.Foundation.SampleEditor
{
    public static class SdkHandMenuValidation
    {
        [MenuItem("Singray/XR/SDK Samples/Update and Validate Recording Menu")]
        public static void Run()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/XRFoundation/SampleScenes/SDKSampleHub/Scenes/SDKSamples.unity");
                var controller = UnityEngine.Object.FindObjectOfType<SdkSamplesHubController>();
                if (controller == null) throw new Exception("SDK hub controller is missing.");
                var setup = typeof(SdkSamplesHubController).GetMethod("SetupRecordingButton", BindingFlags.Instance | BindingFlags.NonPublic);
                setup.Invoke(controller, null);
                setup.Invoke(controller, null);
                var menu = GameObject.Find("Left Hand SDK Menu").transform.Find("Menu Content");
                var record = menu.Find("MR Recording").GetComponentInChildren<Interactable>(true);
                var stream = menu.Find("RTSP Streaming").GetComponentInChildren<Interactable>(true);
                if (menu.childCount != 6 || record == null || stream == null)
                    throw new Exception("Expected five buttons and one backplate without duplicate controls.");
                if (record.GetComponentInChildren<ButtonConfigHelper>(true).MainLabelText != "Start Recording" ||
                    stream.GetComponentInChildren<ButtonConfigHelper>(true).MainLabelText != "Start Streaming")
                    throw new Exception("Incorrect initial button labels.");
                // Verify that cloning the old Hide control cannot hide the catalog.
                var canvasField = typeof(SdkSamplesHubController).GetField("canvasRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                var canvas = (GameObject)canvasField.GetValue(controller);
                canvas.SetActive(true);
                stream.OnClick.Invoke();
                if (!canvas.activeSelf) throw new Exception("Streaming control invoked the cloned Hide action.");
                record.OnClick = new UnityEngine.Events.UnityEvent();
                stream.OnClick = new UnityEngine.Events.UnityEvent();
                UnityEventTools.AddPersistentListener(record.OnClick, controller.ToggleMrRecording);
                UnityEventTools.AddPersistentListener(stream.OnClick, controller.ToggleRtspStreaming);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Directory.CreateDirectory("Builds/HandMenu");
                var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
                {
                    target = BuildTarget.Android, group = BuildTargetGroup.Android,
                    options = ScriptCompilationOptions.None
                }, "Builds/HandMenu/AndroidScripts");
                if (result.assemblies == null || result.assemblies.Count == 0)
                    throw new Exception("Android script compilation produced no assemblies.");
                File.WriteAllText("Builds/HandMenu/validation.txt", "PASS: two recording/streaming controls; idempotent menu setup; no cloned Hide listener; persisted scene; Android script compilation.\n");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
