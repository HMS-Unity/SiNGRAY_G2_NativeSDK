using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singray.Foundation;

public class ViewerAutoControl : MonoBehaviour
{
    public XvCameraDemo xvCameraDemo;
    private bool ifReady = false;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    private void openRgb()
    {
        xvCameraDemo.StartARCamera();
    }
    private void openTof()
    {
        xvCameraDemo.StartTofCamera();
    }
    private void openLeftStereo()
    {
        xvCameraDemo.StartLeftStereoCamera();
    }
    private void openRightStereo()
    {
        xvCameraDemo.StartRightStereoCamera();
    }

    // Update is called once per frame
    void Update()
    {
        if (API.xslam_ready() == true && ifReady == false)
        {
            ifReady = true;
            Invoke("openRgb", 1);
            Invoke("openLeftStereo", 2);
            Invoke("openRightStereo", 3);
            Invoke("openTof", 5);
        }
    }
}
