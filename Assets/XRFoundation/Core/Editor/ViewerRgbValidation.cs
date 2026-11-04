using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Singray.Foundation
{
    public static class ViewerRgbValidation
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Singray/Validation/Viewer RGB Controls")]
        public static void Run()
        {
            const string output = "Builds/ViewerRgbValidation";
            Directory.CreateDirectory(output);
            Scene scene = EditorSceneManager.NewPreviewScene();
            Texture2D frame = null, screenshot = null;
            RenderTexture target = null;
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/XRFoundation/SampleScenes/Viewer/Prefabs/CameraDemo.prefab");
                var root = UnityEngine.Object.Instantiate(source);
                root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, scene);
                var demo = root.GetComponent<CameraDemo>();
                var manager = root.AddComponent<XvCameraManager>();
                manager.XvARCameraParameter.rgbResolution = RgbResolution.RGB_1280x720;
                var panel = root.GetComponent<XvRgbResolutionPanel>() ?? root.AddComponent<XvRgbResolutionPanel>();
                Invoke(panel, "Initialize", demo, manager);
                Invoke(panel, "Initialize", demo, manager);
                var controls = (GameObject)typeof(XvRgbResolutionPanel).GetField("controls", Instance).GetValue(panel);
                var buttons = controls.GetComponentsInChildren<Interactable>(true);
                Require(buttons.Length == 6, "Expected six RGB resolution buttons without duplicate setup.");
                string[] labels = { "320x240", "640x480", "1280x720", "1920x1080", "2560x1920", "3840x2160" };
                foreach (string label in labels)
                    Require(buttons.Any(button => button.name == "RGB " + label), "Missing RGB mode " + label);
                Require(buttons.All(button => button.OnClick.GetPersistentEventCount() == 0),
                    "RGB buttons retained a cloned Start/Stop persistent callback.");
                var images = new[] { demo.arCameraImage, demo.tofCameraImage, demo.tofIRCameraImage,
                    demo.leftStereoCameraImage, demo.rightStereoCameraImage, demo.webCameraImage, demo.mrVideoImage,
                    demo.fisheyeCamera2Image, demo.fisheyeCamera3Image };
                Require(images.All(image => image != null), "A four-camera Viewer image reference is missing.");
                Require(images.Where(image => image != demo.webCameraImage)
                    .All(image => image.enabled && image.gameObject.activeSelf), "A retained camera tile was hidden.");
                Require(!demo.webCameraImage.gameObject.activeSelf, "Compute Unit preview should be hidden.");
                Require(demo.tofCameraImage.uvRect == new Rect(1, 0, -1, 1)
                    && demo.tofIRCameraImage.uvRect == new Rect(1, 0, -1, 1),
                    "ToF depth and IR previews should be mirrored horizontally through UV sampling.");
                var original = source.GetComponent<CameraDemo>();
                var originals = new[] { original.arCameraImage, original.tofCameraImage, original.tofIRCameraImage,
                    original.leftStereoCameraImage, original.rightStereoCameraImage, original.webCameraImage, original.mrVideoImage,
                    original.fisheyeCamera2Image, original.fisheyeCamera3Image };
                for (int i = 0; i < images.Length; i++)
                    Require(images[i].rectTransform.anchoredPosition == originals[i].rectTransform.anchoredPosition
                        && images[i].rectTransform.sizeDelta == originals[i].rectTransform.sizeDelta
                        && images[i].rectTransform.localScale == originals[i].rectTransform.localScale,
                        "Original camera tile anchors, size or scale changed.");
                Require(buttons.All(button => button.transform.localPosition.x > demo.arCameraImage.rectTransform.rect.xMax + 100), "RGB controls overlap the image.");
                string[] Bindings(GameObject item) => item.GetComponentsInChildren<Interactable>(true)
                    .SelectMany(button => Enumerable.Range(0, button.OnClick.GetPersistentEventCount())
                        .Select(index => button.OnClick.GetPersistentMethodName(index))).OrderBy(name => name).ToArray();
                Require(Bindings(root).SequenceEqual(Bindings(source)), "Camera Start/Stop bindings changed during layout.");
                var status = (TMP_Text)typeof(XvRgbResolutionPanel).GetField("status", Instance).GetValue(panel);
                Invoke(panel, "NotifyStarted");
                frame = new Texture2D(16, 16);
                Invoke(panel, "OnFrame", new cameraData { tex = frame, texWidth = 640, texHeight = 480,
                    parameter = new CameraParameter { timeStamp = 1 } });
                Require(status.text.Contains("1280x720") && status.text.Contains("640x480") && status.text.Contains("SIZE MISMATCH"),
                    "Requested/actual resolution mismatch was hidden.");
                Invoke(panel, "NotifyStarted");
                typeof(XvRgbResolutionPanel).GetField("startedAt", Instance).SetValue(panel, Time.realtimeSinceStartup - 11f);
                Invoke(panel, "Update");
                Require(status.text.Contains("No RGB frame after 10s"), "Missing no-frame diagnostic.");
                Invoke(panel, "OnFrame", new cameraData { tex = frame, texWidth = 1280, texHeight = 720,
                    parameter = new CameraParameter { timeStamp = 2 } });
                Require(!status.text.Contains("SIZE MISMATCH") && status.text.Contains("Actual: 1280x720"),
                    "Supported-mode preview status did not recover.");
                Invoke(panel, "NotifyStopped");
                Require(status.text.Contains("RGB stopped"), "Stop did not reset Viewer status.");

                var switchProbeObject = new GameObject("RGB switch guard probe");
                SceneManager.MoveGameObjectToScene(switchProbeObject, scene);
                var switchProbe = switchProbeObject.AddComponent<XvRgbResolutionPanel>();
                typeof(XvRgbResolutionPanel).GetField("owner", Instance).SetValue(switchProbe, demo);
                typeof(XvRgbResolutionPanel).GetField("cameras", Instance).SetValue(switchProbe, manager);
                typeof(XvRgbResolutionPanel).GetField("status", Instance).SetValue(switchProbe, status);
                var otherOwner = new object();
                var lease = typeof(XvCameraManager).Assembly.GetType("Singray.Foundation.XvCaptureLease", true);
                const BindingFlags leaseFlags = BindingFlags.Static | BindingFlags.NonPublic;
                object LeaseCall(string method, object owner) => lease.GetMethod(method, leaseFlags)
                    .Invoke(null, new[] { (object)XvCameraStreamType.ARCameraStream, owner });
                LeaseCall("Register", manager);
                LeaseCall("Register", otherOwner);
                try
                {
                    Invoke(switchProbe, "SelectResolution", RgbResolution.RGB_3840x2160);
                    Require(status.text.Contains("in use by another feature") &&
                        manager.XvARCameraParameter.rgbResolution == RgbResolution.RGB_1280x720 &&
                        (bool)LeaseCall("HasOtherOwners", otherOwner),
                        "Shared RGB switch changed the mode or released the Viewer lease.");
                }
                finally
                {
                    LeaseCall("Release", otherOwner);
                    LeaseCall("Release", manager);
                }

                var canvasObject = demo.arCameraImage.GetComponentInParent<Canvas>(true).gameObject;
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rgb = demo.arCameraImage;
                var preview = canvas.transform;
                preview.SetParent(null, false);
                preview.localPosition = Vector3.zero;
                preview.localRotation = Quaternion.identity;
                preview.localScale = Vector3.one;
                rgb.color = new Color(0.15f, 0.2f, 0.25f);
                foreach (var image in images) image.color = new Color(0.15f,0.2f,0.25f);
                rgb.gameObject.SetActive(true);
                controls.SetActive(true);
                var cameraObject = new GameObject("RGB preview camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.cameraType = UnityEngine.CameraType.Game;
                camera.orthographic = true;
                camera.orthographicSize = 2900;
                camera.transform.position = new Vector3(1500, 0, -10000);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20000;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                canvas.worldCamera = camera;
                target = new RenderTexture(1600, 900, 24);
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    screenshot = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    screenshot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    screenshot.Apply();
                    Require(screenshot.GetPixels32().Any(pixel => pixel.r > 20 || pixel.g > 20 || pixel.b > 20),
                        "RGB control preview rendered blank.");
                    File.WriteAllBytes(output + "/rgb-controls.png", screenshot.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; }
                File.WriteAllText(output + "/validation.txt", "PASS: six mode controls; idempotent setup; cloned callbacks removed; " +
                    "requested/actual mismatch; no-frame timeout; recovery; stop status; shared RGB switch guard; all original tile anchors/sizes/scales preserved; " +
                    "eight previews including four fisheyes enabled; Compute Unit hidden; ToF depth/IR horizontal UV mirroring. Grid preview rendered.\n");
                Debug.Log("Viewer RGB control validation PASSED.");
            }
            finally
            {
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (frame != null) UnityEngine.Object.DestroyImmediate(frame);
                if (screenshot != null) UnityEngine.Object.DestroyImmediate(screenshot);
                EditorSceneManager.ClosePreviewScene(scene);
                EditorUtility.ClearProgressBar();
            }
        }

        private static object Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Instance).Invoke(target, args);
        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
