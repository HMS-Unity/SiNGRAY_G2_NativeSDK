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
        get { return isRecording || finishing; }
    }





    private UnityAction<string> callback;
    private bool finishing;
    private int generation;
    public void SaveScreenshot(UnityAction<string> callback)
    {
        if (isRecording || finishing || !isActiveAndEnabled || cam == null)
        { callback?.Invoke(null); return; }
        this.callback = callback;
        isRecording = true;
        try
        {
            recorder = new JPGRecorder(imageWidth, imageHeight);
            cameraInput = new CameraInput(recorder, new RealtimeClock(), cam);
            cameraInput.frameSkip = 1000;
            Invoke(nameof(StopRecording), 0.5f);
        }
        catch (Exception error)
        {
            Debug.LogError("[MR-PHOTO] capture failed: " + error.Message);
            CancelScreenshot();
        }
    }
    private async void StopRecording() => await FinishScreenshot(true);
    internal void CancelScreenshot()
    {
        generation++;
        CancelInvoke(nameof(StopRecording));
        _ = FinishScreenshot(false);
    }
    private async System.Threading.Tasks.Task FinishScreenshot(bool save)
    {
        if (!isRecording) return;
        isRecording = false; finishing = true;
        int requestGeneration = generation;
        var input = cameraInput; cameraInput = null;
        var current = recorder; recorder = null;
        var complete = callback; callback = null;
        string result = null;
        try
        {
            string path = null;
            try { input?.Dispose(); }
            finally { if (current != null) path = await current.FinishWriting(); }
            if (save && this != null && isActiveAndEnabled && generation == requestGeneration && path != null)
            {
                string directory = Application.platform == RuntimePlatform.Android
                    ? "/storage/emulated/0/DCIM/Screenshots"
                    : Path.Combine(Path.GetDirectoryName(Application.dataPath), "Screenshots");
                string destination = Path.Combine(directory, getDate() + "_" + Guid.NewGuid().ToString("N").Substring(0,8) + ".jpg");
                await System.Threading.Tasks.Task.Run(() =>
                {
                    Directory.CreateDirectory(directory);
                    File.Copy(Path.Combine(path, "1.jpg"), destination);
                });
                if (this != null && isActiveAndEnabled && generation == requestGeneration) result = destination;
                MyDebugTool.LogDiagnostic("MR-PHOTO", "saved", "saved=" + destination, 0f);
            }
        }
        catch (Exception error) { Debug.LogError("[MR-PHOTO] save failed: " + error.Message); }
        finally { finishing = false; complete?.Invoke(result); }
    }
    private void OnDisable() => CancelScreenshot();
    private void OnDestroy() => CancelScreenshot();

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
