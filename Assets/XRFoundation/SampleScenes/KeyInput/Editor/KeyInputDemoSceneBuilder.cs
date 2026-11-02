using System;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Singray.InputSystem;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Singray.Samples.Editor
{
    public static class KeyInputDemoSceneBuilder
    {
        public const string ScenePath = "Assets/XRFoundation/SampleScenes/KeyInput/Scenes/KeyInput.unity";
        public const string PrefabPath = "Assets/XRFoundation/SampleScenes/KeyInput/Prefabs/KeyInputDemo.prefab";
        const string ButtonPath = "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2_32x96.prefab";
        static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.gameObject.layer=5;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        static TMP_Text Text(Transform parent,string name,string message,Vector2 position,Vector2 size,float fontSize,Color color)
        {
            var rect=Rect(parent,name,position,size);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=TMP_Settings.defaultFontAsset;text.text=message;text.fontSize=fontSize;text.color=color;
            text.alignment=TextAlignmentOptions.TopLeft;text.enableWordWrapping=true;text.raycastTarget=false;
            text.overflowMode=TextOverflowModes.Ellipsis;return text;
        }
        static Interactable Button(Transform parent,string label,Vector3 position,float width=0.18f)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);if(!asset)throw new FileNotFoundException(ButtonPath);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);go.name=label;go.transform.localPosition=position;
            go.transform.localScale=new Vector3(width/0.10f,1.1f,1.1f);
            var config=go.GetComponentInChildren<ButtonConfigHelper>(true);config.MainLabelText=label;config.SeeItSayItLabelEnabled=false;
            config.IconStyle=ButtonIconStyle.None;
            var labelProperty=new SerializedObject(config).FindProperty("mainLabelText");
            if(labelProperty.objectReferenceValue is TMP_Text originalLabel)originalLabel.gameObject.SetActive(false);
            // Keep the MRTK press geometry, with readable unscaled world-space labels.
            var labelCanvas=new GameObject(label+" Label",typeof(RectTransform),typeof(Canvas));labelCanvas.layer=5;
            labelCanvas.transform.SetParent(parent,false);labelCanvas.transform.localPosition=position+new Vector3(0,0,-0.001f);labelCanvas.transform.localScale=Vector3.one*0.001f;
            labelCanvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;labelCanvas.GetComponent<Canvas>().sortingOrder=20;
            ((RectTransform)labelCanvas.transform).sizeDelta=new Vector2(width*1000,44);
            var readable=Text(labelCanvas.transform,"Text",label,Vector2.zero,new Vector2(width*1000-10,38),18,Color.white);readable.alignment=TextAlignmentOptions.Center;readable.enableWordWrapping=false;
            var button=go.GetComponentInChildren<Interactable>(true);Singray.UI.MrtkClickGate.Install(button);return button;
        }
        static void Ref(SerializedObject so,string name,UnityEngine.Object value)=>so.FindProperty(name).objectReferenceValue=value;
        static void CreatePrefab()
        {
            var root=new GameObject("KeyInputDemo");
            try
            {
                root.transform.position=new Vector3(0,0,1.2f);var input=root.AddComponent<SingrayKeyInput>();var demo=root.AddComponent<KeyInputDemo>();
                var canvasGO=new GameObject("Visible Key Input Panel",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                canvasGO.layer=5;canvasGO.transform.SetParent(root.transform,false);canvasGO.transform.localScale=Vector3.one*0.001f;
                var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=10;
                var canvasRect=(RectTransform)canvas.transform;canvasRect.sizeDelta=new Vector2(1000,780);
                var bg=Rect(canvas.transform,"Backplate",Vector2.zero,new Vector2(1000,780)).gameObject.AddComponent<Image>();bg.color=new Color(0.025f,0.045f,0.075f,0.98f);bg.raycastTarget=false;
                Text(canvas.transform,"Title","KEY INPUT",new Vector2(0,350),new Vector2(920,48),34,Color.white);
                Text(canvas.transform,"Description","Hardware keys and rotary control | SDK event monitor",new Vector2(0,302),new Vector2(920,30),19,new Color(0.52f,0.82f,1));
                var status=Text(canvas.transform,"Connection Status","AUTO MODE  |  Listening starts in Play Mode\nMapping and device status appear here.",new Vector2(0,248),new Vector2(920,65),19,Color.white);
                var labels=new TMP_Text[6];
                for(int i=0;i<6;i++)
                {
                    var pos=new Vector2(i%2==0?-235:235,165-(i/2)*60);
                    var tile=Rect(canvas.transform,"Key State "+((SingrayKeyCode)(i+1)),pos,new Vector2(450,54));var image=tile.gameObject.AddComponent<Image>();image.color=new Color(0.07f,0.115f,0.17f,1);image.raycastTarget=false;
                    labels[i]=Text(tile,"State",((SingrayKeyCode)(i+1))+"   Released\nLast: --    Clicks: 0",Vector2.zero,new Vector2(430,48),18,Color.white);
                }
                Text(canvas.transform,"Events Heading","RECENT EVENTS",new Vector2(0,-10),new Vector2(920,28),20,new Color(0.52f,0.82f,1));
                var log=Text(canvas.transform,"Recent Events","No events yet. Press hardware keys after launch.\nIn the Editor, use the simulation buttons below.",new Vector2(0,-115),new Vector2(920,180),16,Color.white);
                Text(canvas.transform,"Safety Note","This demo observes input only. No app-exit or system-volume action is requested by this demo.",new Vector2(0,-218),new Vector2(920,30),16,new Color(0.6f,0.7f,0.8f));
                var sim=new GameObject("Editor Simulation Controls");sim.transform.SetParent(root.transform,false);
                for(int i=0;i<4;i++)
                {
                    var button=Button(sim.transform,((SingrayKeyCode)(i+1)).ToString(),new Vector3(-0.35f+i*0.235f,-0.28f,-0.02f),0.22f);
                    UnityEventTools.AddIntPersistentListener(button.OnClick,demo.SimulateClick,i+1);
                }
                var minus=Button(sim.transform,"Knob -1",new Vector3(-0.12f,-0.335f,-0.02f),0.21f);UnityEventTools.AddIntPersistentListener(minus.OnClick,demo.SimulateKnob,-1);
                var plus=Button(sim.transform,"Knob +1",new Vector3(0.12f,-0.335f,-0.02f),0.21f);UnityEventTools.AddIntPersistentListener(plus.OnClick,demo.SimulateKnob,1);
                var clear=Button(root.transform,"Clear Log",new Vector3(0.37f,0.33f,-0.02f),0.16f);UnityEventTools.AddPersistentListener(clear.OnClick,demo.ClearLog);
                var so=new SerializedObject(demo);Ref(so,"input",input);Ref(so,"statusText",status);Ref(so,"logText",log);Ref(so,"simulationControls",sim);
                var array=so.FindProperty("keyStates");array.arraySize=6;for(int i=0;i<6;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=labels[i];so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [MenuItem("Singray/Input/Build Standard Key Input Demo")]
        public static void BuildSample()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before building the sample.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));AssetDatabase.Refresh();
            var original=SceneManager.GetActiveScene();
            bool additive=!string.IsNullOrEmpty(original.path)&&original.path!=ScenePath;
            if(!Application.isBatchMode&&((string.IsNullOrEmpty(original.path)&&original.rootCount>0)||(!additive&&original.isDirty)))
                throw new InvalidOperationException("Save your active scene before generating the KeyInput demo.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,additive?NewSceneMode.Additive:NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                CreatePrefab();
                foreach(var name in new[]{"MixedRealityToolkit","XvXRInput","XvXRManager"})
                {
                    var path="Assets/XRFoundation/Core/Sdk/XR/Resources/"+name+".prefab";
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new FileNotFoundException(path);PrefabUtility.InstantiatePrefab(prefab,scene);
                }
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save the key-input sample.");
            }
            finally{if(additive){EditorSceneManager.CloseScene(scene,true);if(original.IsValid())SceneManager.SetActiveScene(original);}}
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Singray/Toolkit/Build Scenes/KeyInput")]
        public static void BuildApk()
        {
            var output=Path.GetFullPath("build/KeyInput_Demo_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.Android,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("KeyInput APK build failed: "+report.summary.result);
            Debug.Log("KEY INPUT APK BUILT: "+output);
        }
    }
}
