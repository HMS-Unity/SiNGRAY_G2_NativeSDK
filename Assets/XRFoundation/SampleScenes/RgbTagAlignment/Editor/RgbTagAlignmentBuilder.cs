using System;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes.Editor
{
    public static class RgbTagAlignmentBuilder
    {
        public const string Folder="Assets/XRFoundation/SampleScenes/RgbTagAlignment";
        public const string ScenePath=Folder+"/Scenes/RgbTagAlignment.unity";
        public const string ContentPath=Folder+"/Prefabs/RgbTagAlignmentDemo.prefab";
        public const string Output="Builds/RgbTagAlignment_20261116";
        private const string ButtonPath="Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2_32x96.prefab";
        private static void Ref(UnityEngine.Object obj,string field,UnityEngine.Object value)
        { var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
        private static TMP_Text Label(Transform parent,string name,string text,Vector3 position,Vector2 size,float font=20)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));go.layer=5;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=Vector3.one*0.001f;
            ((RectTransform)go.transform).sizeDelta=size;go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var child=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));child.layer=5;child.transform.SetParent(go.transform,false);
            var label=child.GetComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.text=text;
            label.fontSize=font;label.alignment=TextAlignmentOptions.Center;label.enableWordWrapping=false;
            label.raycastTarget=false;label.rectTransform.sizeDelta=size;return label;
        }
        private static Interactable Button(Transform parent,string name,Vector3 position,UnityAction action,float width=0.192f)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath),parent);
            go.name=name;go.transform.localPosition=position;go.transform.localScale=new Vector3(width/0.096f,1.3f,1);
            var helper=go.GetComponent<ButtonConfigHelper>();helper.MainLabelText=name;helper.SeeItSayItLabelEnabled=false;helper.IconStyle=ButtonIconStyle.None;
            var so=new SerializedObject(helper);var label=so.FindProperty("mainLabelText").objectReferenceValue as TMP_Text;
            if(label!=null) label.gameObject.SetActive(false);
            Label(parent,name+" Label",name,position+new Vector3(0,0,-0.004f),new Vector2(180,36),20);
            var button=go.GetComponent<Interactable>();button.OnClick=new UnityEvent();UnityEventTools.AddPersistentListener(button.OnClick,action);
            Singray.UI.MrtkClickGate.Install(button);return button;
        }
        private static Material Material(string name,Shader shader,Color color,Texture texture=null)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(shader);AssetDatabase.CreateAsset(material,path); }
            material.color=color;if(texture!=null) material.mainTexture=texture;EditorUtility.SetDirty(material);return material;
        }
        private static void Plugins()
        {
            foreach(string path in new[] { Folder+"/Plugins/Android/arm64-v8a/libsingray_rgb_tag.so",Folder+"/Plugins/x86_64/singray_rgb_tag.dll" })
            {
                var importer=AssetImporter.GetAtPath(path) as PluginImporter;
                if(importer==null) throw new FileNotFoundException("Build native bridge first: "+path);
                importer.SetCompatibleWithAnyPlatform(false);bool android=path.EndsWith(".so");
                foreach(var target in new[]{BuildTarget.StandaloneLinux64,BuildTarget.StandaloneOSX,BuildTarget.StandaloneWindows,BuildTarget.iOS,BuildTarget.WebGL,BuildTarget.WSAPlayer})
                    importer.SetCompatibleWithPlatform(target,false);
                importer.SetCompatibleWithEditor(!android);
                importer.SetCompatibleWithPlatform(BuildTarget.Android,android);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64,!android);
                if(android) importer.SetPlatformData(BuildTarget.Android,"CPU","ARM64");
                else { importer.SetEditorData("CPU","x86_64");importer.SetEditorData("OS","Windows");importer.SetPlatformData(BuildTarget.StandaloneWindows64,"CPU","x86_64"); }
                importer.SaveAndReimport();
            }
        }
        [MenuItem("Singray/RGB Tag Alignment/Prepare Independent Demo")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var original=SceneManager.GetActiveScene();
            if(original.isDirty) throw new InvalidOperationException("Save the active scene first.");
            foreach(var path in new[]{"Scenes","Prefabs","Materials"}) Directory.CreateDirectory(Folder+"/"+path);
            AssetDatabase.Refresh();Plugins();
            var markerPath=Folder+"/Markers/AprilTag36h11_ID0.png";
            var ti=(TextureImporter)AssetImporter.GetAtPath(markerPath);ti.textureType=TextureImporterType.Default;
            ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
            var marker=AssetDatabase.LoadAssetAtPath<Texture2D>(markerPath);
            var tagMaterial=Material("TagPlane",Shader.Find("Singray/Samples/RGB Tag Plane"),new Color(0.35f,1,1,0.45f),marker);
            var panelMaterial=Material("Panel",Shader.Find("Unlit/Color"),new Color(0.025f,0.045f,0.07f));
            var borderMaterial=Material("PlaneOutline",Shader.Find("Unlit/Color"),new Color(1,0.65f,0.03f));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var root=new GameObject("RGB Tag Alignment");var demo=root.AddComponent<RgbTagAlignmentDemo>();
                var manager=new GameObject("RGB Capture").AddComponent<XvCameraManager>();manager.transform.SetParent(root.transform,false);
                manager.XvARCameraParameter.rgbResolution=RgbResolution.RGB_1280x720;
                var panel=new GameObject("Controls");panel.transform.SetParent(root.transform,false);panel.transform.localPosition=new Vector3(-0.52f,-0.03f,1.25f);
                var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="Backplate";plate.transform.SetParent(panel.transform,false);
                plate.transform.localPosition=new Vector3(0,0,0.024f);plate.transform.localScale=new Vector3(0.78f,1,0.012f);
                UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());plate.GetComponent<Renderer>().sharedMaterial=panelMaterial;
                Label(panel.transform,"Title","RGB TAG ALIGNMENT",new Vector3(0,0.445f,0),new Vector2(740,45),30);
                var canvas=new GameObject("Preview Canvas",typeof(RectTransform),typeof(Canvas));canvas.layer=5;
                canvas.transform.SetParent(panel.transform,false);canvas.transform.localPosition=new Vector3(0,0.19f,0);
                canvas.transform.localScale=Vector3.one*0.001f;canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(720,405);
                var image=new GameObject("Processed RGB",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter),typeof(RectMask2D));image.layer=5;image.transform.SetParent(canvas.transform,false);
                var raw=image.GetComponent<RawImage>();raw.enabled=false;raw.raycastTarget=false;raw.uvRect=new Rect(0,1,1,-1);
                var aspect=image.GetComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=16f/9;
                var graphic=new GameObject("Pixel Outlines",typeof(RectTransform),typeof(CanvasRenderer),typeof(RgbTagOverlay));graphic.layer=5;graphic.transform.SetParent(image.transform,false);
                var rect=(RectTransform)graphic.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var overlay=graphic.GetComponent<RgbTagOverlay>();overlay.raycastTarget=false;
                var state=Label(panel.transform,"Status","Ready - RGB only",new Vector3(0,-0.05f,0),new Vector2(745,46),18);
                var values=Label(panel.transform,"Values","36h11 / ID 0 / 80 mm\nGreen: detected   Magenta: reprojected\nAmber: locked Plane projected into RGB",new Vector3(0,-0.18f,0),new Vector2(735,174),20);
                var plane=GameObject.CreatePrimitive(PrimitiveType.Quad);plane.name="World Tag Plane - 80mm";plane.transform.SetParent(root.transform,false);
                UnityEngine.Object.DestroyImmediate(plane.GetComponent<Collider>());plane.GetComponent<Renderer>().sharedMaterial=tagMaterial;plane.transform.localScale=Vector3.one*0.08f;
                var outline=plane.AddComponent<LineRenderer>();outline.useWorldSpace=false;outline.loop=true;outline.positionCount=4;
                outline.SetPositions(new[]{new Vector3(-0.5f,0.5f,-0.001f),new Vector3(0.5f,0.5f,-0.001f),new Vector3(0.5f,-0.5f,-0.001f),new Vector3(-0.5f,-0.5f,-0.001f)});
                outline.widthMultiplier=0.0125f;outline.sharedMaterial=borderMaterial;plane.SetActive(false);
                Ref(demo,"cameraManager",manager);Ref(demo,"controls",panel.transform);Ref(demo,"worldPlane",plane.transform);
                Ref(demo,"preview",raw);Ref(demo,"overlay",overlay);Ref(demo,"statusText",state);Ref(demo,"valuesText",values);
                var buttons=new[]{Button(panel.transform,"Start",new Vector3(-0.24f,-0.32f,0),demo.StartDetection),
                    Button(panel.transform,"Stop",new Vector3(0,-0.32f,0),demo.StopDetection),
                    Button(panel.transform,"Lock",new Vector3(0.24f,-0.32f,0),demo.LockPlane),
                    Button(panel.transform,"Unlock",new Vector3(-0.24f,-0.40f,0),demo.UnlockPlane),
                    Button(panel.transform,"Save",new Vector3(0,-0.40f,0),demo.SaveSnapshot),
                    Button(panel.transform,"Recenter UI",new Vector3(0.24f,-0.40f,0),demo.RecenterPanel)};
                var serialized=new SerializedObject(demo);var array=serialized.FindProperty("buttons");array.arraySize=buttons.Length;
                for(int i=0;i<buttons.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];serialized.ApplyModifiedPropertiesWithoutUndo();
                PrepareRecording(root);
                foreach(var c in root.GetComponentsInChildren<Component>(true)) if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                PrefabUtility.SaveAsPrefabAsset(root,ContentPath);UnityEngine.Object.DestroyImmediate(root);
                foreach(var name in new[]{"MixedRealityToolkit","XvXRInput","XvXRManager"})
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/XRFoundation/Core/Sdk/XR/Resources/"+name+".prefab"),scene);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ContentPath),scene);
                if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Cannot save independent scene.");
                AssetDatabase.SaveAssets();Debug.Log("[RGB-TAG] Prepared "+ScenePath);
            }
            finally { EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original); }
        }
        private static void PrepareRecording(GameObject root)
        {
            if (root.GetComponent<RgbTagPalmRecording>() != null) return;
            const string hubPath = "Assets/XRFoundation/SampleScenes/SDKSampleHub/Scenes/SDKSamples.unity";
            var source = SceneManager.GetSceneByPath(hubPath);
            bool opened = !source.IsValid() || !source.isLoaded;
            if (opened) source = EditorSceneManager.OpenScene(hubPath, OpenSceneMode.Additive);
            GameObject menu;
            try
            {
                var original = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HandConstraintPalmUp>(true))
                    .Single(m => m.name == "Left Hand SDK Menu");
                menu = UnityEngine.Object.Instantiate(original.gameObject); menu.name = "Left Hand Recording";
                if (PrefabUtility.IsPartOfPrefabInstance(menu)) PrefabUtility.UnpackPrefabInstance(menu, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                menu.transform.SetParent(root.transform, false); menu.transform.localPosition = Vector3.zero; menu.transform.localRotation = Quaternion.identity;
            }
            finally { if (opened) EditorSceneManager.CloseScene(source, true); }
            // Keep the SDK's left-palm solver and its remapped visibility events.
            // Only this new copy gets a compact recording-only menu.
            var content = menu.transform.Find("Menu Content");
            foreach (var child in content.Cast<Transform>().ToArray())
                if (child.name != "MRTK Backplate") UnityEngine.Object.DestroyImmediate(child.gameObject);
            var plate = content.Find("MRTK Backplate");
            plate.localPosition = new Vector3(0, -0.018f, 0.014f); plate.localScale = new Vector3(0.22f, 0.12f, 0.01f);
            var control = root.AddComponent<RgbTagPalmRecording>();
            var button = Button(content, "Record", new Vector3(0, 0, -0.012f), control.ToggleRecording, 0.18f);
            var buttonText = content.Find("Record Label").GetComponentInChildren<TMP_Text>(true);
            var palmStatus = Label(content, "Recording State", "Ready", new Vector3(0, -0.056f, -0.012f), new Vector2(205, 28), 16);
            var visibleStatus = Label(root.transform.Find("Controls"), "Recording Status", "", new Vector3(0.25f, 0.411f, -0.008f), new Vector2(150, 22), 18);
            Ref(control, "recorderPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/XRFoundation/Core/Foundation/MediaRecorder/Resources/XvMediaRecorderManager.prefab"));
            Ref(control, "recordButton", button); Ref(control, "buttonText", buttonText); Ref(control, "palmStatus", palmStatus); Ref(control, "statusText", visibleStatus);
            content.gameObject.SetActive(false);
        }

        [MenuItem("Singray/RGB Tag Alignment/Add Left-Palm Recording")]
        public static void AddRecording()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(ContentPath);
            try { PrepareRecording(root);
                foreach (var c in root.GetComponentsInChildren<Component>(true))
                    if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                PrefabUtility.SaveAsPrefabAsset(root, ContentPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Singray/RGB Tag Alignment/Apply G2 RGB Correction")]
        public static void ApplyRgbCorrection()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait for Edit Mode and compilation before updating the prefab.");
            var root=PrefabUtility.LoadPrefabContents(ContentPath);
            try
            {
                var serialized=new SerializedObject(root.GetComponent<RgbTagAlignmentDemo>());
                serialized.FindProperty("correctMirroredRgbX").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,ContentPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Singray/RGB Tag Alignment/Build G2 APK")]
        public static void BuildG2()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait for Edit Mode and compilation before building.");
            Directory.CreateDirectory(Output);string output=Path.GetFullPath(Output+"/G2_RgbTagAlignment_1.2_Recording.apk");
            byte[] settings=File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");
            string product=PlayerSettings.productName,id=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android),version=PlayerSettings.bundleVersion;
            int code=PlayerSettings.Android.bundleVersionCode;
            try
            {
                PlayerSettings.productName="G2 RGB Tag Alignment";PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.singray.rgbtagalignment");
                PlayerSettings.bundleVersion="1.2";PlayerSettings.Android.bundleVersionCode=3;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},target=BuildTarget.Android,locationPathName=output,options=BuildOptions.None});
                File.WriteAllText(Output+"/build.txt",$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nTime: {report.summary.totalTime}\n");
                if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("RGB Tag build failed.");
            }
            finally
            {
                PlayerSettings.productName=product;PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,id);PlayerSettings.bundleVersion=version;PlayerSettings.Android.bundleVersionCode=code;
                AssetDatabase.SaveAssets();File.WriteAllBytes("ProjectSettings/ProjectSettings.asset",settings);
            }
        }
    }
}
