using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class Sound : MonoBehaviour
{
    //[Tooltip("Exist sound file")]
    //public UnityEngine.Object soundFile;

    private const string soundFilePath = "sikabuluo_48k_16bit_stereo.wav";

    private const string recordFilePath = "/sdcard/xvrecord.wav";
    private static byte[] recordData = new byte[256*1024];
    private static byte[] recordDataToFlush;
    private static bool toFlush = false;
    private static int recordLen = 0;
    private static BinaryWriter bw;

    private static TextAsset soundAsset = null;
    private GCHandle dataHandle;
    private bool recording = false;
    private bool playing = false; 
    private bool playingExist = false;
    private int delay = 0;

    private bool loop = false;

    static API.DataCallback micCallback;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (bw != null && toFlush)
        {
            //flush record
            try
            {
                toFlush = false;
                bw.Write(recordDataToFlush);
                bw.Flush();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "Cannot write to file.");
            }
        }

        if (playing || playingExist) {
            delay++;
        }
        // add delay to wait play really start
        if (delay > 10) {
            bool r = API.xslam_is_playing();
            if (!r && playing) {
                GameObject.Find("ButtonPlay").GetComponentInChildren<Text>().text= "Start Play";
                playing = false;
                if (loop) PlayRecord();
            }
            if (!r && playingExist) {
                GameObject.Find("ButtonPlayExist").GetComponentInChildren<Text>().text= "Start Play Exist";
                playingExist = false;
                if (loop) PlayExist();
            }
        }
    }

    static void OnMicData( IntPtr data, int len)
    {
        MyDebugTool.Log("OnMicData: " + len);

        Marshal.Copy(data, recordData, recordLen, len);
        recordLen += len;

        if (recordLen > 220*1024)
        {
            recordDataToFlush = new byte[recordLen];
            Array.Copy(recordData, recordDataToFlush, recordLen);
            recordLen = 0;
            toFlush = true;
        }
    }

	public void Record()
	{
        MyDebugTool.Log("On Record");

        if (playing)
            return;

        // not play and record at same time
        //if (playingExist)
        //    return;

        AndroidJNI.AttachCurrentThread();

        if (!recording) {
            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(recordFilePath, FileMode.Create));
            }
            catch (IOException e)
            {
                MyDebugTool.Log(e.Message + "Cannot create file " + recordFilePath);
                return;
            }

            recordLen = 0;
            API.xslam_set_mic_callback(OnMicData);
            GameObject.Find("ButtonMic").GetComponentInChildren<Text>().text= "Stop Record";
            recording = true;
        } else {
            API.xslam_unset_mic_callback();
            GameObject.Find("ButtonMic").GetComponentInChildren<Text>().text= "Start Record";
            recording = false;

            try
            {
                if (toFlush)
                {
                    toFlush = false;
                    bw.Write(recordDataToFlush);
                }
                if (recordLen > 0)
                {
                    bw.Write(recordData, 0, recordLen);
                    recordLen = 0;
                }
                bw.Close();
            }
            catch (IOException e)
            {
                MyDebugTool.Log(e.Message + "Cannot close file " + recordFilePath);
                return;
            }
            bw = null;
        }
	}

    private void playRecord()
    {
        MyDebugTool.Log("start playRecord " + recordFilePath);

#if False
        try {
            dataHandle.Free();
        } catch {}
        dataHandle = GCHandle.Alloc(recordData, GCHandleType.Pinned);

        bool r = API.xslam_play_sound(dataHandle.AddrOfPinnedObject(), recordLen);
        if (!r) {
            Debug.Log("play record failed, len: " + recordLen);
            return;
        }
#else
        bool r = API.xslam_play_sound_file(recordFilePath);
        if (!r) {
            MyDebugTool.Log("play record failed, file: " + recordFilePath);
            return;
        }
#endif

        GameObject.Find("ButtonPlay").GetComponentInChildren<Text>().text= "Stop Play";
        playing = true;
        delay = 0;
    }

	public void PlayRecord()
	{
        AndroidJNI.AttachCurrentThread();
        MyDebugTool.Log("On PlayRecord");
        if (recording || playingExist)
            return;

        if (!playing || recording) {
            loop = true;
            playRecord();
        } else {
            MyDebugTool.Log("stop PlayRecord");
            API.xslam_stop_play();
            loop = false;
        }
	}

	private void playExist()
	{
        MyDebugTool.Log("start Load " + soundFilePath);
        if (soundAsset == null) {
            soundAsset = Resources.Load(soundFilePath) as TextAsset;
        }

        byte[] soundData = soundAsset.bytes;
        MyDebugTool.Log("start playExist, len=" + soundData.Length);

        AndroidJNI.AttachCurrentThread();

        try {
            dataHandle.Free();
        } catch {}
        dataHandle = GCHandle.Alloc(soundData, GCHandleType.Pinned);

        bool r = API.xslam_play_sound(dataHandle.AddrOfPinnedObject(), soundData.Length);
        if (!r) {
            MyDebugTool.Log("play sound file failed, path: " + soundFilePath);
            return;
        }
        GameObject.Find("ButtonPlayExist").GetComponentInChildren<Text>().text= "Stop Play Exist";
        playingExist = true;
        delay = 0;
    }

	public void PlayExist()
	{
        MyDebugTool.Log("On PlayExist");
        if (playing)
            return;

        // not play and record at same time
        //if (recording)
        //    return;

        AndroidJNI.AttachCurrentThread();

        if (!playingExist) {
            loop = true;
            playExist();
        } else {
            MyDebugTool.Log("stop PlayExist");
            API.xslam_stop_play();
            loop = false;
        }
	}

    void OnApplicationQuit()
    {
        if (playing || playingExist)
            API.xslam_stop_play();

        if (recording)
            API.xslam_unset_mic_callback();
    }

}
