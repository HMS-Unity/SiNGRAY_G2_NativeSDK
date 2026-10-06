using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singray.utils;
using System.Text;

using UnityEngine.UI;

using UnityEditor;

using System;
using System.Runtime.InteropServices;
public class ReadFisheyesCalibration : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    public void ReadStereoFisheyesCalibration(){
        MyDebugTool.Log("ReadStereoFisheyesCalibration start xxxx");
        Singray.Engine.XvDeviceManager.Manager.ReadStereoFisheyesCalibration();
    }

}
