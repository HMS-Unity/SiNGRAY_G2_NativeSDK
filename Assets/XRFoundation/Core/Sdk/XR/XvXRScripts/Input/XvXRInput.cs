using System;
using UnityEngine;
using AOT;


namespace Singray
{

    using utils;
    using static API;

    public partial class XvXRInput : SingletonBehaviour<XvXRInput>
    {
        public static XvXRSkeleton xvSkeleton = new XvXRSkeleton();

        public static HandsManager Hands = new HandsManager();
        public static HandData[] handDatas = new HandData[2];

        static Vector3 defultPoint = new Vector3(0,0,100);

        public const int PRE_COUNT = 26;//25 values per group

        public static int[] LRstaticGes = new int[2];
        

        private void Start()
        {
            xvSkeleton.scale = new float[2];
            xvSkeleton.status = new int[2];
        }


        [MonoPInvokeCallback(typeof(API.xslam_skeleton_callback))]
        public static void OnSkeletonCallback(API.XvXRSkeleton skeleton)
        {
            xvSkeleton = skeleton;
           // Debug.Log($"XvXRInput xvSkeleton scale[0]:{xvSkeleton.scale[0]},scale[1]:{xvSkeleton.scale[1]}");

            LRstaticGes[0] = xvSkeleton.status[0];
            LRstaticGes[1] = xvSkeleton.status[1];
            //Print the static gesture code
           // Debug.Log($"LRstaticGes[0]:{LRstaticGes[0]},LRstaticGes[1]:{LRstaticGes[1]},xvSkeleton.status[0]:{xvSkeleton.status[0]},xvSkeleton.status[1]:{xvSkeleton.status[1]}");

            //Debug.Log($"XvXRInput timestamp0:{skeleton.timestamp0},timestamp[1]:{skeleton.timestamp1};fisheye_timestamp:{skeleton.fisheye_timestamp};left interval:{skeleton.timestamp0- xvSkeleton.fisheye_timestamp},right interval:{skeleton.timestamp1 - xvSkeleton.fisheye_timestamp}");

            for (int i = 0;i<handDatas.Length;i++){
                if(handDatas[i].poses==null){
                    handDatas[i].poses = new Pose[26];
                }
                handDatas[i].isTracked = false;
            }

            
           // XvXRLog.LogInfo("XvXRInput OnSkeletonCallback...Gesture  count:" + skeleton.size + ",[0]:" + skeleton.joints_ex[0].x + ",[49]:" + skeleton.joints_ex[49].x);
           // StringBuilder str = new StringBuilder();
            for(int i= 0;i<skeleton.size;i++){
                int index = i/PRE_COUNT;
                int j = i%PRE_COUNT;
                if(index<2){
                   if (Double.NaN == skeleton.joints_ex[i].x || Double.NaN == skeleton.joints_ex[i].y || Double.NaN == skeleton.joints_ex[i].z)
                    {
                        handDatas[index].poses[j].position = defultPoint;
                    }
                    else
                    {
                        if (skeleton.joints_ex[i].x == 0 && skeleton.joints_ex[i].y == 0 && skeleton.joints_ex[i].z == 0)
                        {
                            handDatas[index].poses[j].position= defultPoint;
                        }
                        else
                        {
                            handDatas[index].isTracked = true;
                            /*
                            if (index == 0)
                            {
                                if (j == 0)
                                {
                                    handDatas[index].poses[j].position = new Vector3(skeleton.joints_ex[i].x, -skeleton.joints_ex[i].y - 0.08f, skeleton.joints_ex[i].z);
                                }
                                else
                                {
                                    handDatas[index].poses[j].position = new Vector3(skeleton.joints_ex[i].x, -skeleton.joints_ex[i].y, skeleton.joints_ex[i].z);
                                }
                            }
                            else
                            {
                                handDatas[index].poses[j].position = new Vector3(skeleton.joints_ex[i].x, -skeleton.joints_ex[i].y, skeleton.joints_ex[i].z);
                            }
                            */
                            handDatas[index].poses[j].position = new Vector3(skeleton.joints_ex[i].x, -skeleton.joints_ex[i].y, skeleton.joints_ex[i].z);
                        }
                    }
                    UnityEngine.Quaternion q = new UnityEngine.Quaternion(-0.707f, 0, 0, 0.707f); 
                    handDatas[index].poses[j].rotation = new UnityEngine.Quaternion(-skeleton.rotateData[i].x, skeleton.rotateData[i].y, -skeleton.rotateData[i].z, skeleton.rotateData[i].w);
                    handDatas[index].poses[j].rotation = handDatas[index].poses[j].rotation * q;
                    //handDatas[index].poses[j].rotation = new Quaternion(-skeleton.rotateData[i].x,skeleton.rotateData[i].y,-skeleton.rotateData[i].z,skeleton.rotateData[i].w);

                }
                //if(i%5==0){
                    //Debug.LogError("XvXRInput OnSkeletonCallback...Gesture."+index+","+j+"..(pose["+i+"]:("+skeleton.joints_ex[i].x+","+skeleton.joints_ex[i].y+","+skeleton.joints_ex[i].z+"),("+skeleton.rotateData[i].x+","+skeleton.rotateData[i].y+","+skeleton.rotateData[i].z+","+skeleton.rotateData[i].w+")");
               // }
            }
            handDatas[0].dataFetchTimeMs = handDatas[1].dataFetchTimeMs = skeleton.dataFetchTimeMs;
            handDatas[0].dataTimeStampMs = handDatas[1].dataTimeStampMs = skeleton.dataTimeStampMs;

            Hands.GetHandState(HandEnum.LeftHand)?.UpdateHandState(handDatas[0]);
            Hands.GetHandState(HandEnum.RightHand)?.UpdateHandState(handDatas[1]);

            // XvXRLog.LogInfo("XvXRInput OnSkeletonCallback...Gesture..."+str.ToString()); 

        }

         [MonoPInvokeCallback(typeof(API.xslam_gesture_callback))]
        public static void OnGestureCallback(API.GestureData gesture)
        {
        
            XvXRLog.LogInfo("XvXRInput OnGestureCallback... index[0]:" + gesture.index[0] + ",index[1]:" + gesture.index[1]);
        
        }
    }
}
