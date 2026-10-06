namespace Singray
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Runtime.InteropServices;


    public struct HandData
    {
        public Pose[] poses ;
        public long dataFetchTimeMs;
        public long dataTimeStampMs;

        public bool isTracked;
    }
    public enum HandEnum
    {
        None = -1,
        RightHand,
        LeftHand
    }

    public enum HandGesture
    {
        None = 0,
        OpenHand,
        Point,
        Grab,
        Victory
    }

    public enum HandJointID
    {
        Invalid = -1,

        Wrist,
        Palm,
        ThumbMetacarpalJoint,
        ThumbProximalJoint,
        ThumbDistalJoint,
        ThumbTip,
        IndexMetacarpal,
        IndexKnuckle,
        IndexMiddleJoint,
        IndexDistalJoint,
        IndexTip,
        MiddleMetacarpal,
        MiddleKnuckle,
        MiddleMiddleJoint,
        MiddleDistalJoint,
        MiddleTip,
        RingMetacarpal,
        RingKnuckle,
        RingMiddleJoint,
        RingDistalJoint,
        RingTip,
        PinkyMetacarpal,
        PinkyKnuckle,
        PinkyMiddleJoint,
        PinkyDistalJoint,
        PinkyTip,

        Max = PinkyTip + 1

        //ThumbMetacarpal = 0,
        //ThumbProximal,
        //ThumbDistal,
        //ThumbTip,
        //IndexProximal,//4
        //IndexMiddle,
        //IndexDistal,
        //IndexTip,
        //MiddleProximal,//8
        //MiddleMiddle,
        //MiddleDistal,
        //MiddleTip,
        //RingProximal,
        //RingMiddle,
        //RingDistal,
        //RingTip,
        //PinkyMetacarpal,
        //PinkyProximal,
        //PinkyMiddle,
        //PinkyDistal,
        //PinkyTip,
        //MiddleMetacarpal=21,
        //Wrist = 22,

        //Palm = 24,

        //Max = Palm + 1


    }

    /// <summary> Contains the details of current hand tracking info of left/right hand. </summary>
    public class HandState
    {
        public readonly HandEnum handEnum;
        public bool isTracked;
        public Pose pointerPose;
        public bool pointerPoseValid;
        public bool isPinching;
        public float pinchStrength;
        public HandGesture currentGesture;
        public readonly Dictionary<HandJointID, Pose> jointsPoseDict = new Dictionary<HandJointID, Pose>();

        public HandState(HandEnum handEnum)
        {
            this.handEnum = handEnum;
            Reset();
        }

        /// <summary> Reset the hand state to default. </summary>
        public void Reset()
        {
            isTracked = false;
            pointerPose = Pose.identity;
            pointerPoseValid = false;
            isPinching = false;
            pinchStrength = 0f;
            currentGesture = HandGesture.None;
            jointsPoseDict.Clear();
        }

        /// <summary>
        /// Returns the pose of the hand joint ID of this hand state.
        /// </summary>
        /// <param name="handJointID"></param>
        /// <returns></returns>
        public Pose GetJointPose(HandJointID handJointID)
        {
            Pose pose = Pose.identity;
            jointsPoseDict.TryGetValue(handJointID, out pose);
            return pose;
        }

        public  void UpdateHandState(HandData handData){
            if(handData.poses!=null && handData.poses.Length>0){
                for(int i=0;i<handData.poses.Length;i++){
                    if(i>=(int)HandJointID.Max){
                        break;
                    }
                    jointsPoseDict[(HandJointID)i] = handData.poses[i];
                }
               

            }
             isTracked = handData.isTracked;
        }
    }
}
