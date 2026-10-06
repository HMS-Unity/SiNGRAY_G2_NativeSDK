using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrameRateCounter : MonoBehaviour
{
    public Text display;
    private int frames;
    private float duration;
    [SerializeField,Range(0.1f,2f)]
    private float sampleDuration = 1;

    private float bestDuration = float.MaxValue;
    private float worstDuration=0;
    public enum DisplayMode { FPS,MS}

    [SerializeField]
    private DisplayMode displayMode=DisplayMode.FPS;

    // Update is called once per frame
    void Update()
    {
        float framDuration = Time.unscaledDeltaTime;
        frames += 1;
        duration += framDuration;

        if (framDuration<bestDuration) {
            bestDuration= framDuration;
        }
        if (framDuration>worstDuration) {
          
            worstDuration= framDuration;
        }

        if (duration> sampleDuration) {

            if (displayMode == DisplayMode.FPS)
            {
                display.text = string.Format("FPS\nBest: {0:0}\nAverage: {1:0}\nWorst: {2:0}", 1f / bestDuration, frames / duration, 1f / worstDuration);
            }
            else { 
                display.text = string.Format("MS\nBest: {0:F2}\nAverage: {1:F2}\nWorst: {2:F2}", 1000 * bestDuration, 1000* duration /frames , 1000* worstDuration);
            }
            frames = 0;
            duration = 0;
            bestDuration = float.MaxValue;
            worstDuration = 0;
        }  
    }
}
