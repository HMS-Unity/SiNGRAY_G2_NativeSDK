using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class LoadHandleController : MonoBehaviour
{
    private long currInput;
    private long currInputTimestamp;
    private DateTime currInputTime;

    void Start()
    {
    }
    
    void Update()
    {
        int type = 0;
        int state = 0;
        if( API.xslam_get_event(ref type, ref state, ref currInputTimestamp) ){
            if (type == 8) {
                currInput = state;
                currInputTime = DateTime.UtcNow;
            }
        }

        TimeSpan ts = DateTime.UtcNow - currInputTime;
        if (ts.TotalMilliseconds > 1000) {
            currInput = 0;
        }
    }

    void OnGUI()
    {
        if (currInput != 0) {
            string msg = "    ";
            switch(currInput) {
                case 1:
                    msg = "A down";
                    break;
                case 2:
                    msg = "A up";
                    break;
                case 3:
                    msg = "B down";
                    break;
                case 4:
                    msg = "B up";
                    break;
                default:
                    break;
            }
            GUIStyle style = new GUIStyle();
            style.normal.background = null;
            style.normal.textColor = new Color(0.0f, 1.0f, 1.0f);
            style.fontSize = 80;
            GUI.Label(new Rect(100, 800, 800, 50), msg, style);
        }
    }

}
