using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Linq;

public class LoadGesture : MonoBehaviour
{
	private List<EventInfo> events = new List<EventInfo>();
    private EventInfo currEvent;
    private DateTime currEventTime;
    private GestureInfo currGes;
    private Dictionary<string, Texture2D> gesTextures = new Dictionary<string, Texture2D>();
    private Texture2D texGes;
    private Texture2D texEv;

    void Start()
    {
        currEvent.id = -1;
        currEventTime = DateTime.UtcNow;
    }
    
    void Update()
    {
        if (!XvGesture.Ready())
            return;

        // Get gesture
        GestureInfo newGes = XvGesture.GetGesture();
        if (newGes.id != currGes.id && newGes.id > 0) {
            Debug.LogFormat("got new gesture! id:{0} x:{1} y:{2}", newGes.id, newGes.x, newGes.y );
        }
        currGes = newGes;

        // Get event
        GetCurrEvent();

        if (currGes.id != -1) {
            string path = string.Format("Gesture/g{0:D2}", currGes.id);
            if (gesTextures.ContainsKey(path)) {
                texGes = gesTextures[path];
            } else {
                texGes = Resources.Load<Texture2D>(path);
                gesTextures.Add(path, texGes);
            }
        }
        if (currEvent.id != -1) {
            string path = string.Format("Gesture/e{0:D2}", currEvent.id);
            if (gesTextures.ContainsKey(path)) {
                texEv = gesTextures[path];
            } else {
                texEv = Resources.Load<Texture2D>(path);
                gesTextures.Add(path, texEv);
            }
        }
    }

    void OnGUI()
    {
        if (currGes.id != -1) {
            GUI.Label(new Rect(100, 550, 200, 200), texGes);
        }
        if (currEvent.id != -1) {
            GUI.Label(new Rect(100, 750, 200, 200), texEv);
        }
    }

    private EventInfo GetCurrEvent()
    {
        // update
        foreach (EventInfo ev in XvGesture.GetEvents()) {
            events.Add(ev);
        }

        // unique list, then remove bad events
        // should not be uniqued if need x,y
        events = events.Distinct().ToList();
        for (int i = events.Count - 1; i >= 0; i--) {
            if (events[i].id == 21) { // ignore NODEF
                events.RemoveAt(i);
            }
        }

        // remove events too old
        for (int i = events.Count - 2; i >= 0; i--) {
            events.RemoveAt(i);
        }


        // no old event
        if (currEvent.id == -1) {
            if (events.Count == 0) {
                return currEvent;
            } else {
                currEvent = events[0];
                currEventTime = DateTime.UtcNow;
                events.RemoveAt(0);
                return currEvent;
            }
        }

        // if haven't showed for enough time, still show old event
        TimeSpan ts = DateTime.UtcNow - currEventTime;
        if (ts.TotalMilliseconds < 100) {
            return currEvent;
        }

        if (events.Count > 0) {
            currEvent = events[0];
            currEventTime = DateTime.UtcNow;
            events.RemoveAt(0);
            return currEvent;
        } else if (ts.TotalMilliseconds < 1000) {
            // still show old if no new event
            return currEvent;
        } else {
            // set event empty
            currEvent.id = -1;
            return currEvent;
        }
    }
}
