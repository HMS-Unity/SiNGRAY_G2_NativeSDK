using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShowFPS : MonoBehaviour
{
    //Update interval
    public float UpdateInterval = 0.5F;
    //Previous update time
    private float _lastInterval;
    //Intermediate frame counter
    private int _frames = 0;
    //Current frame rate
    private float _fps;

    private TextMeshPro _text;

    int count = 0;

    //private int _temp = 0;

    void Start()
    {
        //Application.targetFrameRate=60;

        //UpdateInterval = Time.realtimeSinceStartup;

        _frames = 0;
        _text = GameObject.Find("FPS").GetComponent<TextMeshPro>();

        InvokeRepeating("GetAllObjects", 1, 1);
    }

    void OnGUI()
    {
        if (_text != null)
        {
            //_text.text = "FPS:" + _fps.ToString("f2") + "\nverts:" + verts + "\ntris:" + tris;
            _text.text = "FPS:" + _fps.ToString("f2");
        }
        //_text.text = "temp:" + _temp;
    }


    public static int verts;
    public static int tris;

    void GetAllObjects()
    {
        verts = 0;
        tris = 0;
        GameObject[] ob = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        foreach (GameObject obj in ob)
        {
            GetAllVertsAndTris(obj);
        }
    }

    //Get triangle and vertex counts
    void GetAllVertsAndTris(GameObject obj)
    {
        Component[] filters;
        filters = obj.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter f in filters)
        {
            tris += f.sharedMesh.triangles.Length / 3;
            verts += f.sharedMesh.vertexCount;
        }
    }


    void Update()
    {
        ++_frames;

        if (Time.realtimeSinceStartup > _lastInterval + UpdateInterval)
        {
            _fps = _frames / (Time.realtimeSinceStartup - _lastInterval);

            _frames = 0;
            count++;
            _lastInterval = Time.realtimeSinceStartup;
            if (count % 10 == 0)
            {
                MyDebugTool.Log("fps is:" + _fps);
            }

        }

        //if (_temp == 0) {
        //	Debug.Log( "gettemp" );
        //    byte[] cmd = {0x02, 0xde, 0x78};
        //    byte[] rdata = API.HidWriteAndRead(cmd, cmd.Length);
        //    if (rdata != null) {
        //        _temp = rdata[3];
        //        Debug.Log( "gettemp" + _temp );
        //    }
        //}
    }
}
