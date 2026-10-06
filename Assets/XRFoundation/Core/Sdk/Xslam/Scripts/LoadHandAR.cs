using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using UnityEditor;

using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;

public class LoadHandAR : MonoBehaviour
{

	[Header("Origin Settings")]
	[Tooltip("Position of the origin")]
	public Vector3 positionOrigin = new Vector3(0.0f, 0.0f, 0.0f);
	public static float frustumHeight;
	public static float frustumWidth;
	public Hand leftHand, rightHand;
	public Fire leftFire;
	private bool _initialized = false;
	public HandParams handParams;
	public GameObject fireL,Cube, fireR, shipin;
    private Dictionary<string, Texture2D> gesTextures = new Dictionary<string, Texture2D>();
    private Texture2D texGes;
    private Texture2D texEv;
    private int handtype = -1;
    static Vector3[] joints_ex = new Vector3[50];
    static Vector3 defultPoint = new Vector3(0, 0, 10000);

    void Start()
    {

    }
    
    void Update()
    {
        if (API.xslam_ready())
        {
			//Matrix4x4 mt = Matrix4x4.identity;
			//long ts = 0;
			//int status = 0;
			//if (!API.xslam_get_transform(ref mt, ref ts, ref status))
			//{
			//	mt = Matrix4x4.identity;
			//}
			//else
			//{
			
			//}
			//Debug.Log(mt);

			//draw hand
			if (!_initialized)
			{
                //var frameWidth = 1920;
                //var frameHeight = 1080;
                //frustumHeight = Camera.main.orthographicSize * 2;
                //frustumWidth = frustumHeight * frameWidth / frameHeight;
                leftHand = new Hand(xMult: -1);
          //      rightHand = new Hand( xMult: 1);
                Cube = GameObject.Find("Cube");//Magic fire 1
         //       fireL = GameObject.Find("Magic fire 1");//
                shipin = GameObject.Find("robotpos");//
            //    leftFire = new Fire(leftHand, fireL, shipin);


				_initialized = true;
			}

			Vector3[] handdata = new Vector3[42];
            float[] matrix = new float[16];
            Matrix4x4 mt = Matrix4x4.identity;


            if (true)// (API.xslam_get_hand_landmark_xyz(handdata, ref handtype, ref mt))//
            {
                Data data = new Data();
                data.dataL = new Data.HandData();

                //data.dataL.joints = new List<Vector3>(handdata);
                data.dataL.joints = new List<Vector3>(joints_ex);
                
                leftHand.Process(data.dataL);
                //		rightHand.Process(data.dataR);
                //Debug.Log("eddy handtype = " + handtype);
                //if (handtype != -1)
                //{
                //    string path = string.Format("Gesture/{0:D1}", handtype);
                //    if (gesTextures.ContainsKey(path))
                //    {
                //        texGes = gesTextures[path];
                //    }
                //    else
                //    {
                //        texGes = Resources.Load<Texture2D>(path);
                //        gesTextures.Add(path, texGes);
                //    }
                //}
               // if (handtype == 9 )
                // leftFire.Draw(handtype);
             //    leftFire.drawLine(handtype, mt);
            }

         
	

		}
		
	
	}
  
    void OnGUI()
    {
        if (handtype != -1)
        {
            GUI.Label(new Rect(200, 250, 260, 260), texGes);
        }
        //if (currEvent.id != -1)
        //{
        //    GUI.Label(new Rect(100, 750, 200, 200), texEv);
        //}
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        //    System.Environment.Exit(0);
        }
    }
    private void OnDestroy()
	{

	}


    [MonoPInvokeCallback(typeof(API.xslam_skeleton_callback))]
    public static void OnStartSkeletonCallback(API.XvXRSkeleton skeleton)
    {
        //Debug.Log("Xreadcalibration OnStartSkeletonCallback... count:"+skeleton.size+",[0]:"+skeleton.joints[0].x+",[41]:"+skeleton.joints[41].x);
        //if(skeleton.size>0){


        //for(int i=0;i<joints.Length;i++){
        //    if(Double.NaN==skeleton.joints[i].x||Double.NaN==skeleton.joints[i].y||Double.NaN==skeleton.joints[i].z){
        //    for(int i=0;i<joints.Length;i++){
        //         joints[i] = defultPoint;
        //    }
        //}


        MyDebugTool.Log("Xreadcalibration OnStartSkeletonCallback... count:" + skeleton.size + ",[0]:" + skeleton.joints_ex[0].x + ",[41]:" + skeleton.joints_ex[41].x);
        if (true)
        {
            for (int i = 0; i < joints_ex.Length; i++)
            {
                if (Double.NaN == skeleton.joints_ex[i].x || Double.NaN == skeleton.joints_ex[i].y || Double.NaN == skeleton.joints_ex[i].z)
                {
                    joints_ex[i] = defultPoint;
                }
                else
                {
                    joints_ex[i] = new Vector3(skeleton.joints_ex[i].x, skeleton.joints_ex[i].y, skeleton.joints_ex[i].z);
                }

            }
            if(joints_ex.Length == 50)
            {
                Vector3 tmp = joints_ex[23];
                joints_ex[23] = joints_ex[24];
                joints_ex[24] = tmp;

                tmp = joints_ex[48];
                joints_ex[48] = joints_ex[49];
                joints_ex[49] = tmp;
            }
            
        }
        else
        {
            for (int i = 0; i < joints_ex.Length; i++)
            {
                joints_ex[i] = defultPoint;
            }
        }
    }

    [MonoPInvokeCallback(typeof(API.xslam_gesture_callback))]
    public static void OnStartGestureCallback(API.GestureData gesture)
    {

        MyDebugTool.Log(" OnStartGestureCallback... index[0]:" + gesture.index[0] + ",index[1]:" + gesture.index[1]);
       
    }

}
