using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singray.Foundation;
using TMPro;
using System.Runtime.InteropServices;

public class SlamConfidenceControl : MonoBehaviour
{
    public TextMeshPro info;
    private double c = 0;

    [DllImport("xslam-unity-wrapper")]
    public static extern bool xslam_get_pose_confidence(ref double confidence);

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("getConfidence",5,1);
    }

    private void getConfidence()
    {
        bool r = xslam_get_pose_confidence(ref c);
        info.text = c.ToString();
        Debug.LogError("confidence === " + r);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
