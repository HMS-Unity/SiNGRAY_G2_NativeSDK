using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
namespace Singray.Foundation
{
    class ProjectBuild : Editor
    {
        // Sample builds use the customer's current identity, version and signing settings.
        internal static void ValidateScenes(string[] scenes)
        {
            if (scenes == null || scenes.Length == 0)
                throw new InvalidOperationException("No scenes were selected for the SDK build.");
            foreach (string scene in scenes)
            {
                if (string.IsNullOrEmpty(scene) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                    throw new InvalidOperationException("SDK build scene is missing: " + scene);
            }
        }

        private static void BuildScenes(string[] scenes, string outputName)
        {
            ValidateScenes(scenes);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "build"));
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, outputName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".apk");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("SDK build failed: " + report.summary.result);
            Debug.Log("SDK build completed: " + output);
        }

        //Return project scene paths as a string array; add filtering here to include only selected scenes
        static string[] GetBuildScenes()
        {
            List<string> names = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e == null)
                    continue;
                if (e.enabled)
                    names.Add(e.path);
            }
            return names.ToArray();
        }


        //[MenuItem("Make Page/Test", false, 0)]
        //static void Test()
        //{
        //    double aa = 77;
        //    aa -= 1;
        //    Debug.LogError(aa / 10);
        //    Debug.LogError(aa);
        //}
        [MenuItem("Singray/test", false, 0)]
        static void Test()
        {
            long t = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            Debug.LogError(t);
        }




        [MenuItem("Singray/Toolkit/Build Scenes/Wifi", false, 0)]
        static void Wifi()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Wifi/Scenes/Wifi.unity",
            }, "Wifi");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/AR301-3D", false, 0)]
        static void AR3013D()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/TofPointCloud/Scenes/AR301-3D.unity",
            }, "AR301-3D");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/Bluetooth", false, 0)]
        static void Bluetooth()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Bluetooth/Scenes/Bluetooth.unity",
            }, "Bluetooth");
        }


        



        [MenuItem("Singray/Toolkit/Build Scenes/Viewer", false, 0)]
        static void XvViewer()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Viewer/Scenes/Viewer.unity",
            }, "Viewer");
        }








        [MenuItem("Singray/Toolkit/Build Scenes/TagRecognizer", false, 0)]
        static void XvTagRecognizer()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/TagRecognizer/Scenes/TagRecognizer.unity",
            }, "TagRecognizer");
        }
        [MenuItem("Singray/Toolkit/Build Scenes/PlaneDetection", false, 0)]
        static void XvPlaneDetection()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/PlaneDetection/Scenes/PlaneDetection.unity",
            }, "PlaneDetection");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/SpatialMesh", false, 0)]
        static void SpatialMesh()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/SpatialMesh/Scenes/SpatialMesh.unity",
            }, "SpatialMesh");
        }


        [MenuItem("Singray/Toolkit/Build Scenes/SpatialMap", false, 0)]
        static void SpatialMap()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/SpatialMap/Scenes/SpatialMap.unity",
            }, "SpatialMap");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/SpeechVoice", false, 0)]
        static void SpeechVoice()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/SpeechVoice/Scenes/SpeechVoice.unity",
            }, "SpeechVoice");
        }
        [MenuItem("Singray/Toolkit/Build Scenes/MRVideoCapture", false, 0)]
        static void MRVideoCapture()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/MRVideoCapture/Scenes/MRVideoCapture.unity",
            }, "MRVideoCapture");
        }


        [MenuItem("Singray/Toolkit/Build Scenes/RTSPStreamer", false, 0)]
        static void RTSPStreamer()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/RTSPStreamer/Scenes/RTSPStreamer.unity",
            }, "RTSPStreamer");
        }


        [MenuItem("Singray/Toolkit/Build Scenes/Rgbd", false, 0)]
        static void Rgbd()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Rgbd/Scenes/Rgbd.unity",
            }, "Rgbd");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/MediaRecorder", false, 0)]
        static void MediaRecorder()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/MediaRecorder/Scenes/MediaRecorder.unity",
            }, "MediaRecorder");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/SystemSetting", false, 0)]
        static void SystemSetting()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/SystemSetting/Scenes/SystemSetting.unity",
            }, "SystemSetting");
        }
        [MenuItem("Singray/Toolkit/Build Scenes/MRTK2", false, 0)]
        static void MRTK2()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/MRTK2/Scenes/MRTK2.unity",
            }, "MRTK2");
        }
        [MenuItem("Singray/Toolkit/Build Scenes/EyeTrack", false, 0)]
        static void EyeTrack()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/EyeTracking/Scenes/EyeTracking.unity",
            }, "EyeTrack");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/EyeTrackNew", false, 0)]
        static void EyeTrackNew()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/EyeTrackingNew/Scenes/EyeTrackingNew.unity",
            }, "EyeTrackNew");
        }

        //[MenuItem("Singray/Toolkit/Build Scenes/EyeCalibration", false, 0)]
        //static void EyeCalibration()
        //{
        //    string[] scenes = { "Assets/XvXRFoundation/SampleScenes/EyeTracking/Scenes/EyeCalibration.unity" };
        //    string identifierName = "com.xv.EyeCalibration";
        //    string AppName = "EyeCalibration" +
        //        "";

        //    // init("Gesture");
        //    PlayerSettings.productName = AppName;
        //    PlayerSettings.applicationIdentifier = identifierName;

        //    string path = Application.dataPath + "/apk/" + AppName + "_" + getDate() + ".apk";
        //    path = path.Replace("Assets/", "");
        //    Debug.LogError(path);
        //   // PlayerSettings.Android.useCustomKeystore = false;
        //    BuildPipeline.BuildPlayer(scenes, path, BuildTarget.Android, BuildOptions.None);
        //}

        //[MenuItem("Singray/Toolkit/Build Scenes/EyeCalibrationXV", false, 0)]
        //static void EyeCalibrationXV()
        //{
        //    string[] scenes = { "Assets/XvXRFoundation/SampleScenes/EyeTrackingNew/Scenes/EyeTrackingNew.unity" };
        //    string identifierName = "com.xv.EyeCalibrationXv";
        //    string AppName = "EyeTracking";

        //    // init("Gesture");
        //    PlayerSettings.productName = AppName;
        //    PlayerSettings.applicationIdentifier = identifierName;

        //    string path = Application.dataPath + "/apk/" + AppName + "_" + getDate() + ".apk";
        //    path = path.Replace("Assets/", "");
        //    Debug.LogError(path);
        //    // PlayerSettings.Android.useCustomKeystore = false;
        //    BuildPipeline.BuildPlayer(scenes, path, BuildTarget.Android, BuildOptions.None);
        //}

        //[MenuItem("Singray/Toolkit/Build Scenes/EyeImage", false, 0)]
        //static void EyeImage()
        //{
        //    string[] scenes = { "Assets/XvXRFoundation/SampleScenes/EyeTracking/Scenes/EyeImage.unity" };
        //    string identifierName = "com.xv.EyeImage";
        //    string AppName = "EyeImage";

        //    // init("Gesture");
        //    PlayerSettings.productName = AppName;
        //    PlayerSettings.applicationIdentifier = identifierName;

        //    string path = Application.dataPath + "/apk/" + AppName + "_" + getDate() + ".apk";
        //    path = path.Replace("Assets/", "");
        //    Debug.LogError(path);
        //   // PlayerSettings.Android.useCustomKeystore = false;
        //    BuildPipeline.BuildPlayer(scenes, path, BuildTarget.Android, BuildOptions.None);
        //}

        [MenuItem("Singray/Toolkit/Build Scenes/Gaze", false, 0)]
        static void Gaze()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Gaze/Scenes/Gaze.unity",
            }, "Gaze");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/Keyboard", false, 0)]
        static void Keyboard()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Keyboard/Scenes/Keyboard.unity",
            }, "Keyboard");
        }


        [MenuItem("Singray/Toolkit/Build Scenes/StaticGesture", false, 0)]
        static void StaticGesture()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/StaticGesture/Scenes/StaticGesture.unity",
            }, "StaticGesture");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/SeerPadController", false, 0)]
        static void SeerPadController()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Joystick/Scenes/SeerPadController.unity",
            }, "SeerPadController");
        }


        [MenuItem("Singray/Toolkit/Build Scenes/Joystick", false, 0)]
        static void Joystick()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/Joystick/Scenes/Joystick.unity",
            }, "Joystick");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/TofPointCloud", false, 0)]
        static void TofPointCloud()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/TofPointCloud/Scenes/TofPointCloud.unity",
            }, "TofPointCloud");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/IRToWorld", false, 0)]
        static void IRToWorld()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/IRToWorld/Scenes/IRToWorld.unity",
            }, "IRToWorld");
        }
        [MenuItem("Singray/Toolkit/Build Scenes/XvAITalk", false, 0)]
        static void XvAITalk()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/XvAITalk/Scenes/XvAITalk.unity",
            }, "XvAITalk");
        }


        //[MenuItem("Singray/Toolkit/Build Scenes/InfraredTracked", false, 0)]
        //static void InfraredTracked()
        //{
        //    string[] scenes = { "Assets/XvXRFoundation/SampleScenes/IRToWorld/Scenes/InfraredTracked.unity" };
        //    string identifierName = "com.xv.InfraredTracked";
        //    string AppName = "InfraredTracked";

        //    // init("Gesture");
        //    PlayerSettings.productName = AppName;
        //    PlayerSettings.applicationIdentifier = identifierName;

        //    string path = Application.dataPath + "/apk/" + AppName + "_" + getDate() + ".apk";
        //    path = path.Replace("Assets/", "");
        //    Debug.LogError(path);
        //    //PlayerSettings.Android.useCustomKeystore = false;
        //    BuildPipeline.BuildPlayer(scenes, path, BuildTarget.Android, BuildOptions.None);
        //}






        [MenuItem("Singray/Toolkit/Build Scenes/BaseScene", false, 0)]
        static void BaseScene()
        {
            BuildScenes(new[]
            {
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/BaseScene.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/Viewer.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/MediaRecorder.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/MRTK2.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/MRVideoCapture.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/PlaneDetection.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/Rgbd.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/RTSPStreamer.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/SpatialMap.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/SpatialMesh.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/SpeechVoice.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/SystemSetting.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/TagRecognizer.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/EyeCalibration.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/EyeTracking.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/EyeImage.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/Keyboard.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/Gaze.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/StaticGesture.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/Joystick.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/TofPointCloud.unity",
                "Assets/XRFoundation/SampleScenes/SDKSamples/Scenes/IRToWorld.unity",
            }, "BaseScene");
        }

        [MenuItem("Singray/Toolkit/Build Scenes/SDKSamples", false, 100)]
        static void SDKSamples()
        {
            BuildScenes(new[] { "Assets/XRFoundation/SampleScenes/SDKSampleHub/Scenes/SDKSamples.unity" }, "SDKSamples");
        }
      
        static void init(string app = "Default")
        {

            SetDefaultIcon(app);

            //AssetDatabase.Refresh();
        }

        public static void SetDefaultIcon(string iconName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath(string.Format("Assets/Images/Icon/{0}.png", iconName),
                typeof(Texture2D)) as Texture2D;

            int[] iconSize = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            Texture2D[] textureArray = new Texture2D[iconSize.Length];
            for (int i = 0; i < textureArray.Length; i++)
            {
                textureArray[i] = texture;
            }
            textureArray[0] = texture;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, textureArray);
            AssetDatabase.SaveAssets();
        }

        private static string getDate()
        {
            string str = DateTime.Now.Year.ToString();
            if (DateTime.Now.Month.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Month;
            }
            else
            {
                str += DateTime.Now.Month;
            }
            if (DateTime.Now.Day.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Day;
            }
            else
            {
                str += DateTime.Now.Day;
            }
            if (DateTime.Now.Hour.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Hour;
            }
            else
            {
                str += DateTime.Now.Hour;
            }
            if (DateTime.Now.Minute.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Minute;
            }
            else
            {
                str += DateTime.Now.Minute;
            }
            return str;
        }

        public const string AndroidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";

        public static void OnPreprocessBuild(BuildTarget target)
        {
            if (target == BuildTarget.Android) ValidateScenes(GetBuildScenes());
        }

        private static string getVersion()
        {
            string str = "";
            if (DateTime.Now.Month.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Month;
            }
            else
            {
                str += DateTime.Now.Month;
            }
            if (DateTime.Now.Day.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Day;
            }
            else
            {
                str += DateTime.Now.Day;
            }
            if (DateTime.Now.Hour.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Hour;
            }
            else
            {
                str += DateTime.Now.Hour;
            }
            if (DateTime.Now.Minute.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Minute;
            }
            else
            {
                str += DateTime.Now.Minute;
            }

            return str;
        }

        private static void AddBuildToolsToPath()
        {
            string sdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
            string[] levels = Directory.GetDirectories(Path.Combine(sdkRoot, "build-tools"));
            // Prefer the newer SDK version
            System.Array.Reverse(levels);

#if UNITY_EDITOR_WIN
            string delimiter = ";";
#else
    string delimiter = ":";
#endif

            var name = "PATH";
            string PATH = System.Environment.GetEnvironmentVariable(name);
            var value = PATH + delimiter + string.Join(delimiter, levels);
            var target = System.EnvironmentVariableTarget.Process;
            System.Environment.SetEnvironmentVariable(name, value, target);
        }


    }
}