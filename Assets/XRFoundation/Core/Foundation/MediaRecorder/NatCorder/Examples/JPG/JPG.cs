using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class JPG : SingletonMonoBehaviour<JPG>
{
    [Header("GIF Settings")]
    [HideInInspector]
    internal int imageWidth = 640;
    internal int imageHeight = 480;

    private JPGRecorder recorder;
    private CameraInput cameraInput;
    [HideInInspector]
    internal Camera cam;
    private bool isRecording;
    public bool IsRecording
    {
        get { return isRecording; }
    }





    private UnityAction<string> callback;
    public void SaveScreenshot(UnityAction<string> callback)
    {
       
        if (isRecording)
        {
            MyDebugTool.LogError("Screenshot capture in progress...");
            return;
        }
        this.callback = callback;
        isRecording = true;

        recorder = new JPGRecorder(imageWidth, imageHeight);
        cameraInput = new CameraInput(recorder, new RealtimeClock(), cam);
        cameraInput.frameSkip = 1000;
        MyDebugTool.Log(" SaveScreenshot "+ imageWidth+"  "+ imageHeight);
       
        Invoke("StopRecording", 0.5f);
    }

    
    private async void StopRecording()
    {
        if (!isRecording)
        {
            return;
        }
        isRecording = false;

        cameraInput.Dispose();
        var path = await recorder.FinishWriting();
        MyDebugTool.Log($"Saved animated jpg image to: {path}");
        var prefix = Application.platform == RuntimePlatform.IPhonePlayer ? "file://" : "";





        // Application.OpenURL($"{prefix}{path}");


#if PLATFORM_ANDROID && !UNITY_EDITOR
try { 
        if (!Directory.Exists("/storage/emulated/0/DCIM/Screenshots"))
        {
        Directory.CreateDirectory("/storage/emulated/0/DCIM/Screenshots");
        }
        
        string path2 = "/storage/emulated/0/DCIM/Screenshots/" + getDate() + ".jpg";; 
        File.Copy(path+"/1.jpg", path2);
        callback?.Invoke(path2);
         MyDebugTool.LogError("Save completed");
         }catch ( Exception e ) { 
         MyDebugTool.LogError("Save Exception:"+e.Message);
        }
#endif



#if UNITY_EDITOR

        string dirPath = Path.GetFileName(path);

        string rootPath = Application.dataPath.Replace("Assets/", "");
        if (!Directory.Exists(rootPath + "/Screenshots"))
        {
            Directory.CreateDirectory(rootPath + "/Screenshots");
        }
        string path_configData = rootPath + "/Screenshots/" + dirPath + ".jpg";
       

        Debug.LogError(path_configData+"  "+ dirPath);
        File.Copy(path + "/1.jpg", path_configData);
        callback?.Invoke(path_configData);

#endif

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

        if (DateTime.Now.Second.ToString().Length == 1)
        {
            str += "0" + DateTime.Now.Second;
        }
        else
        {
            str += DateTime.Now.Second;
        }
        return str;
    }
}
