using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes.Editor
{
    /// <summary>Internal native/synthetic lifecycle checks. No G2 accuracy or physical-input claim.</summary>
    public static class RgbTagAlignmentValidation
    {
        private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        private static readonly List<string> checks=new List<string>();
        [Serializable] private sealed class CameraFixture { public double[] calibration; }
        private static void Require(bool pass,string text) { if(!pass) throw new InvalidOperationException(text);checks.Add(text); }
        private static object Get(object o,string name)=>o.GetType().GetField(name,Instance).GetValue(o);
        private static void Set(object o,string name,object value)=>o.GetType().GetField(name,Instance).SetValue(o,value);
        private static object Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Instance).Invoke(o,args);
        private static void Report(string state,Exception failure=null)
        { Directory.CreateDirectory(RgbTagAlignmentBuilder.Output);File.WriteAllText(RgbTagAlignmentBuilder.Output+"/editor-validation.txt",state+"\nChecks: "+checks.Count+"\n"+string.Join("\n",checks)+(failure==null?"":"\n"+failure)); }
        private static object Frame(Type type,byte[] rgba,double[] k,double[] result,Matrix4x4 worldFromRgb,bool tracking,int generation)
        {
            var f=Activator.CreateInstance(type,true);Set(f,"pixels",rgba);Set(f,"width",1280);Set(f,"height",720);Set(f,"timestamp",123.456);
            Set(f,"generation",generation);Set(f,"capturedAt",Time.realtimeSinceStartup);Set(f,"k",k);Set(f,"result",result);
            Set(f,"worldFromRgb",worldFromRgb);Set(f,"hasTracking",tracking);return f;
        }
        [MenuItem("Singray/RGB Tag Alignment/Validate Independent Demo")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait for Edit Mode and compilation before validation.");
            checks.Clear();var scene=EditorSceneManager.OpenScene(RgbTagAlignmentBuilder.ScenePath,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var demo=roots.SelectMany(r=>r.GetComponentsInChildren<RgbTagAlignmentDemo>(true)).Single();
                Require(roots.Length==4,"Three existing XR hosts plus independent Demo root.");
                Require(roots.SelectMany(r=>r.GetComponentsInChildren<XvTagRecognizerManager>(true)).Count()==0,"No vendor AprilTag manager in new scene.");
                Require(roots.SelectMany(r=>r.GetComponentsInChildren<XvRgbdManager>(true)).Count()==0,"No RGBD manager in new scene.");
                foreach(var go in roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject)==0,"No missing script: "+go.name);
                var so=new SerializedObject(demo);
                foreach(var field in new[]{"cameraManager","controls","worldPlane","preview","overlay","statusText","valuesText"})
                    Require(so.FindProperty(field).objectReferenceValue!=null,"Saved reference: "+field);
                Require(so.FindProperty("correctMirroredRgbX").boolValue,"G2 horizontal input correction is saved enabled.");
                Require(demo.GetComponentsInChildren<Interactable>(true).Length==7,"Six detection controls plus one recording button.");
                foreach(var button in (Interactable[])Get(demo,"buttons"))
                    Require(button.OnClick.GetPersistentEventCount()==1 && button.OnClick.GetPersistentTarget(0)==demo,"One saved action per button: "+button.name);
                var recording=demo.GetComponent<RgbTagPalmRecording>();
                Require(recording!=null && recording.transform==demo.transform,"Recorder controller remains outside hidden palm content.");
                var rec=new SerializedObject(recording);
                foreach(var field in new[]{"recorderPrefab","recordButton","buttonText","palmStatus","statusText"})
                    Require(rec.FindProperty(field).objectReferenceValue!=null,"Saved recorder reference: "+field);
                var recordButton=(Interactable)Get(recording,"recordButton");
                Require(recordButton.OnClick.GetPersistentEventCount()==1 && recordButton.OnClick.GetPersistentTarget(0)==recording && recordButton.OnClick.GetPersistentMethodName(0)=="ToggleRecording","One saved recording action.");
                var palm=demo.GetComponentInChildren<Microsoft.MixedReality.Toolkit.Utilities.Solvers.HandConstraintPalmUp>(true);
                var solver=palm.GetComponent<Microsoft.MixedReality.Toolkit.Utilities.Solvers.SolverHandler>();
                Require(solver.TrackedHandedness==Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left,"Recording menu tracks the left hand.");
                Require(!palm.transform.Find("Menu Content").gameObject.activeSelf,"Palm content starts hidden.");
                recording.ToggleRecording();
                Require(Get(recording,"recorder")==null && !recording.IsPreparing,"Unready SDK cannot queue capture or instantiate an encoder.");
                Require(!demo.WorldPlane.gameObject.activeSelf,"World Plane starts hidden.");
                Require((demo.WorldPlane.localScale-Vector3.one*.08f).sqrMagnitude<1e-9,"Saved Plane is 80mm with unit Quad mesh.");
                Require((demo.WorldPlane.GetComponent<MeshFilter>().sharedMesh.bounds.size-new Vector3(1,1,0)).sqrMagnitude<1e-9,"Quad geometry has unit XY edges.");
                var preview=(RawImage)Get(demo,"preview");Require(preview.uvRect==new Rect(0,1,1,-1),"Native top-row convention represented explicitly in preview.");
                demo.StartDetection();Require(!demo.IsRunning && !(bool)Get(demo,"acquired"),"Editor/unready SDK Start cannot queue an unowned capture.");
                Require(preview.GetComponent<AspectRatioFitter>()!=null,"Preview retains image aspect ratio.");
                Require(((RgbTagOverlay)Get(demo,"overlay")).GetComponent<CanvasRenderer>()!=null,"Overlay has required CanvasRenderer.");
                foreach(var text in demo.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.gameObject.activeInHierarchy))
                { text.ForceMeshUpdate(true);Require(text.text.All(c=>c<128) && !text.isTextOverflowing,"English labels fit: "+text.name); }
                var abi=typeof(RgbTagAlignmentDemo).Assembly.GetType("Singray.Foundation.SampleScenes.RgbTagNative");
                abi.GetMethod("CheckAbi",Static).Invoke(null,null);Require(true,"Actual Windows native bridge ABI loaded through Unity P/Invoke.");
                byte[] rgba=File.ReadAllBytes(RgbTagAlignmentBuilder.Output+"/synthetic-rgb.rgba");
                double[] k={1100,1090,640,360,0,0,0,0,0};var result=new double[31];
                int status=(int)abi.GetMethod("Detect",Static).Invoke(null,new object[]{rgba,1280,720,k,result});
                Require(status==1 && result[1]<1,"Actual native 36h11 ID0 detector and PnP succeed on synthetic frame.");
                var matrix=(Matrix4x4)typeof(RgbTagAlignmentDemo).GetMethod("CameraFromTag",Static).Invoke(null,new object[]{result});
                Require((bool)typeof(RgbTagAlignmentDemo).GetMethod("Rigid",Static).Invoke(null,new object[]{matrix}),"OpenCV-to-Unity basis change produces proper rigid transform.");
                var pixels=new double[8];Require((int)abi.GetMethod("Project",Static).Invoke(null,new object[]{k,matrix,pixels})==1,"Unity-to-OpenCV reverse projection succeeds.");
                Require(Enumerable.Range(0,8).All(i=>Math.Abs(pixels[i]-result[23+i])<.002),"Tag coordinate basis and corner order round-trip within 0.002px.");
                var frameType=typeof(RgbTagAlignmentDemo).GetNestedType("Frame",BindingFlags.NonPublic);
                var mirror=typeof(RgbTagAlignmentDemo).GetMethod("CorrectHorizontalReflection",Static);
                var odd=Enumerable.Range(0,3*2*4).Select(i=>(byte)i).ToArray();var original=(byte[])odd.Clone();
                mirror.Invoke(null,new object[]{odd,3,2});
                Require(Enumerable.Range(0,24).All(i=>odd[i]==original[((i/12)*3+2-(i/4)%3)*4+i%4]),"RGBA mirror preserves rows/channels and odd-width centre pixel.");
                mirror.Invoke(null,new object[]{odd,3,2});Require(odd.SequenceEqual(original),"Two horizontal reflections recover exact original bytes.");
                string deviceFixtures=RgbTagAlignmentBuilder.Output+"/DeviceTriage";
                if(Directory.Exists(deviceFixtures) && File.Exists(deviceFixtures+"/calibration-fixture.json")) foreach(string file in Directory.GetFiles(deviceFixtures,"*.rgba"))
                {
                    var raw=File.ReadAllBytes(file);double[] deviceK=JsonUtility.FromJson<CameraFixture>(File.ReadAllText(deviceFixtures+"/calibration-fixture.json")).calibration;
                    Require(deviceK!=null && deviceK.Length==9,"Private replay calibration is loaded from ignored fixture data.");
                    var rawFrame=Frame(frameType,(byte[])raw.Clone(),deviceK,new double[31],Matrix4x4.identity,false,0);
                    typeof(RgbTagAlignmentDemo).GetMethod("Process",Static).Invoke(null,new[]{rawFrame});
                    Require(((double[])Get(rawFrame,"result"))[0]==0,"Original G2 reflected frame reproduces absent ID0: "+Path.GetFileName(file));
                    var corrected=Frame(frameType,raw,deviceK,new double[31],Matrix4x4.identity,false,0);Set(corrected,"horizontalCorrectionApplied",true);
                    typeof(RgbTagAlignmentDemo).GetMethod("Process",Static).Invoke(null,new[]{corrected});var correctedResult=(double[])Get(corrected,"result");
                    Require(correctedResult[0]==1 && correctedResult[1]<1,"Corrected G2 frame independently detects ID0: "+Path.GetFileName(file));
                    var checkProjection=new double[8];var correctedMatrix=(Matrix4x4)typeof(RgbTagAlignmentDemo).GetMethod("CameraFromTag",Static).Invoke(null,new object[]{correctedResult});
                    abi.GetMethod("Project",Static).Invoke(null,new object[]{deviceK,correctedMatrix,checkProjection});
                    Require(Enumerable.Range(0,8).All(i=>Math.Abs(checkProjection[i]-correctedResult[23+i])<.002),"Corrected-frame coordinate basis/reprojection remain consistent: "+Path.GetFileName(file));
                }
                Set(demo,"running",true);int generation=(int)Get(demo,"generation");
                var cameraWorld=Matrix4x4.TRS(new Vector3(.2f,.1f,.3f),Quaternion.Euler(4,25,2),Vector3.one);
                var frame=Frame(frameType,rgba,k,result,cameraWorld,true,generation);Invoke(demo,"Display",frame);
                Require(demo.WorldPlane.gameObject.activeSelf,"Valid timestamped fixture displays Plane.");
                var expected=cameraWorld*matrix;Require(Vector3.Distance(demo.WorldPlane.position,expected.GetColumn(3))<1e-6,"World composition rotates camera-relative translation.");
                demo.LockPlane();Require(demo.IsLocked,"Fresh valid Plane can be locked.");var locked=demo.WorldPlane.localToWorldMatrix;
                var shiftedResult=(double[])result.Clone();shiftedResult[12]+=.025;
                Invoke(demo,"Display",Frame(frameType,rgba,k,shiftedResult,cameraWorld,true,generation));
                Require(Vector3.Distance(demo.WorldPlane.position,locked.GetColumn(3))<1e-6,"Locked world position unchanged by new detections.");
                Require(Quaternion.Angle(demo.WorldPlane.rotation,locked.rotation)<.001,"Locked orientation unchanged by new detections.");
                var label=(TMP_Text)Get(demo,"valuesText");Require(label.text.Contains("Live vs lock:"),"Live-to-lock millimetre/angle comparison shown.");
                demo.RecenterPanel();Require(Vector3.Distance(demo.WorldPlane.position,locked.GetColumn(3))<1e-6,"Recenter UI does not move locked Plane.");
                Invoke(demo,"Display",Frame(frameType,rgba,k,new double[31],cameraWorld,true,generation));
                Require(demo.IsLocked && demo.WorldPlane.gameObject.activeSelf,"Tag loss retains locked reference.");
                demo.UnlockPlane();Require(!demo.IsLocked && !demo.WorldPlane.gameObject.activeSelf,"Unlock on lost target hides live Plane.");
                Invoke(demo,"Display",Frame(frameType,rgba,k,result,cameraWorld,false,generation));
                Require(!demo.WorldPlane.gameObject.activeSelf,"Missing timestamped tracking rejects world placement.");
                demo.LockPlane();Require(!demo.IsLocked,"Cannot lock unavailable world pose.");
                Invoke(demo,"Display",Frame(frameType,rgba,k,result,cameraWorld,true,generation));
                float timeout=(float)Get(demo,"frameTimeoutSeconds");
                // Fresh batch Editors can have uptime below two seconds: avoid a negative sentinel.
                Set(demo,"frameTimeoutSeconds",0.000001f);Set(demo,"lastDisplayAt",0f);
                demo.LockPlane();Require(!demo.IsLocked,"Cannot lock stale frame.");
                Invoke(demo,"Update");Require(preview.texture==null && !demo.WorldPlane.gameObject.activeSelf,"Stale live image and Plane are cleared.");
                Set(demo,"frameTimeoutSeconds",timeout);
                var old=Frame(frameType,rgba,k,result,cameraWorld,true,generation-1);
                var task=typeof(Task).GetMethod("FromResult").MakeGenericMethod(frameType).Invoke(null,new[]{old});
                Set(demo,"pending",task);Set(demo,"lastDisplayAt",-1f);Invoke(demo,"Update");
                Require(preview.texture==null,"Late worker from previous generation cannot restore old frame.");
                Invoke(demo,"Display",Frame(frameType,rgba,k,result,cameraWorld,true,generation));demo.LockPlane();
                Require(label.text.Contains("0.0 mm"),"Lock immediately updates zero-baseline readout.");
                Invoke(recording,"SetStatus","Recording");
                if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                    Render(demo); // REC/UI evidence is synthetic, not a running encoder or G2.
                else Debug.Log("[RGB-TAG] Camera.Render skipped: headless graphics device; no new visual evidence.");
                Invoke(recording,"SetStatus","Ready");
                var directory=Path.Combine(Application.persistentDataPath,"RgbTagAlignment");
                var before=Directory.Exists(directory)?Directory.GetFiles(directory):new string[0];
                demo.SaveSnapshot();var exports=Directory.GetFiles(directory).Except(before).ToArray();
                Require(exports.Length==2 && exports.Any(p=>p.EndsWith(".png")) && exports.Any(p=>p.EndsWith(".json")),"Save emits exactly one PNG/JSON pair.");
                string json=File.ReadAllText(exports.Single(p=>p.EndsWith(".json")));
                Require(json.Contains("\"worldPoseValid\": true") && json.Contains("\"timestamp\": 123.456"),"Saved validity/timestamp correspond to displayed frame.");
                var exported=new Texture2D(2,2);try
                {
                    exported.LoadImage(File.ReadAllBytes(exports.Single(p=>p.EndsWith(".png"))));
                    Require(exported.width==1280 && exported.height==720,"PNG preserves native dimensions.");
                    var colors=exported.GetPixels32();bool equal=true;
                    for(int i=0;i<1280*720;i+=137)
                    { int source=((719-i/1280)*1280+i%1280)*4;var c=colors[i];equal &= c.r==rgba[source] && c.g==rgba[source+1] && c.b==rgba[source+2]; }
                    Require(equal,"PNG rows preserve native top-left pixel coordinates.");
                }
                finally { UnityEngine.Object.DestroyImmediate(exported); }
                foreach(var file in exports) File.Move(file,RgbTagAlignmentBuilder.Output+"/fixture-"+Path.GetFileName(file));
                demo.StopDetection();Require(!demo.IsRunning && !demo.IsLocked && !demo.WorldPlane.gameObject.activeSelf,"Stop clears Plane and lock.");
                demo.StopDetection();Require(!demo.IsRunning,"Repeated stop is safe.");
                Report("PASS - synthetic/Editor only");Debug.Log("[RGB-TAG] Validation PASS: "+checks.Count);
            }
            catch(Exception e) { Report("FAIL",e);throw; }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        private static void Render(RgbTagAlignmentDemo demo)
        {
            var go=new GameObject("RGB Tag Validation Camera");var camera=go.AddComponent<Camera>();
            var controls=(Transform)Get(demo,"controls");camera.transform.position=controls.TransformPoint(new Vector3(0,0,-1.3f));
            camera.transform.rotation=controls.rotation;camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.fieldOfView=48;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.11f,.14f);camera.cullingMask=~0;
            var layers=demo.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t.gameObject,t=>t.gameObject.layer);
            foreach(var item in layers) item.Key.layer=31;
            camera.cullingMask=1<<31;
            var target=new RenderTexture(1100,1300,24);var image=new Texture2D(1100,1300,TextureFormat.RGB24,false);
            var active=RenderTexture.active;
            try
            {
                foreach(var canvas in demo.GetComponentsInChildren<Canvas>(true)) canvas.worldCamera=camera;
                Canvas.ForceUpdateCanvases();camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1100,1300),0,0);image.Apply();File.WriteAllBytes(RgbTagAlignmentBuilder.Output+"/editor-preview.png",image.EncodeToPNG());
                Require(true,"Regular Camera.Render evidence saved using synthetic RGB and native result.");
            }
            finally { foreach(var item in layers) item.Key.layer=item.Value;RenderTexture.active=active;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
