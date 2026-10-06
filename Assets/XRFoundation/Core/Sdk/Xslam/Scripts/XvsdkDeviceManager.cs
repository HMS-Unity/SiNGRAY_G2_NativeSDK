using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AOT;
using System.Runtime.InteropServices;
using UnityEngine.UI;

namespace XvSdk
{

class XvsdkDeviceManager : MonoBehaviour
{
public static XvsdkDeviceManager Manager
{

	get
	{
		if (manager == null)
		{
			manager = UnityEngine.Object.FindObjectOfType<XvsdkDeviceManager>();
		}
		if (manager == null)
		{
                    MyDebugTool.Log("Creating XvsdkDeviceManager object");
			var go = new GameObject("XvsdkDeviceManager");
			manager = go.AddComponent<XvsdkDeviceManager>();
			go.transform.localPosition = Vector3.zero;
		}
		return manager;
	}
}


private static XvsdkDeviceManager manager = null;

void Start()
{

  
   

}

double[]  _poseData = new double[7];
long timestamp= 0;
double prediction = 0.016;

void Update()
{
    if(API.xslam_ready()){
      if(API.xslam_get_pose_prediction(_poseData,ref timestamp,prediction)){

                gameObject.transform.localRotation = new Quaternion(-(float)_poseData[0],(float)_poseData[1], -(float)_poseData[2], (float)_poseData[3]);
                gameObject.transform.localPosition = new Vector3((float)_poseData[4], -(float)_poseData[5], (float)_poseData[6]) ;
 
      }
    }else{
        API.xslam_init();
        
    }
}



    public void OnDestory()
    {

    }


}
}
