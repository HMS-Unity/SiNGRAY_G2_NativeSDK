
namespace Singray
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> A manager of hand states. </summary>
    public class HandsManager
    {
        private readonly Dictionary<HandEnum, XvXRHand> m_HandsDict;
        private readonly HandState[] m_HandStates; // index 0 represents right and index 1 represents left
        private bool m_Inited;

     

        public HandsManager()
        {
            m_HandsDict = new Dictionary<HandEnum, XvXRHand>();
            m_HandStates = new HandState[2] { new HandState(HandEnum.RightHand), new HandState(HandEnum.LeftHand) };
        }

        internal void RegistHand(XvXRHand hand)
        {
            if (hand == null || hand.HandEnum == HandEnum.None)
                return;
            var handEnum = hand.HandEnum;
            if (m_HandsDict.ContainsKey(handEnum))
            {
                m_HandsDict[handEnum] = hand;
            }
            else
            {
                m_HandsDict.Add(handEnum, hand);
            }
        }

        internal void UnRegistHand(XvXRHand hand)
        {
            if (hand == null)
                return;
            m_HandsDict.Remove(hand.HandEnum);
        }

   
        public HandState GetHandState(HandEnum handEnum)
        {
            switch (handEnum)
            {
                case HandEnum.RightHand:
                    return m_HandStates[0];
                case HandEnum.LeftHand:
                    return m_HandStates[1];
                default:
                    break;
            }
            return null;
        }

       
        public XvXRHand GetHand(HandEnum handEnum)
        {
            XvXRHand hand;
            if (m_HandsDict != null && m_HandsDict.TryGetValue(handEnum, out hand))
            {
                return hand;
            }
            return null;
        }

        private void ResetHandStates()
        {
            for (int i = 0; i < m_HandStates.Length; i++)
            {
                m_HandStates[i].Reset();
            }
        }

        private void UpdateHandPointer()
        {
            for (int i = 0; i < m_HandStates.Length; i++)
            {
                var handState = m_HandStates[i];
                if (handState == null)
                    continue;

                CaculatePointerPose(handState);
                CaculatePinchState(handState);
            }
        }

        private void CaculatePointerPose(HandState handState)
        {
            // if (handState.isTracked)
            // {
            //     var palmPose = handState.GetJointPose(HandJointID.Palm);
            //     var cameraTransform = NRInput.CameraCenter;
            //     handState.pointerPoseValid = Vector3.Angle(cameraTransform.forward, palmPose.forward) < 70f;

            //     if (handState.pointerPoseValid)
            //     {
            //         var cameraWorldUp = NRFrame.GetWorldMatrixFromUnityToNative().MultiplyVector(Vector3.up).normalized;
            //         var rayEndPoint = palmPose.position;
            //         var right = Vector3.Cross(rayEndPoint - cameraTransform.position, cameraWorldUp).normalized;
            //         var horizontalVec = right * (handState.handEnum == HandEnum.RightHand ? -1f : 1f);
            //         var rayStartPoint = cameraTransform.position + horizontalVec * 0.14f - cameraWorldUp * 0.16f;

            //         var foward = (rayEndPoint - rayStartPoint).normalized;
            //         var upwards = Vector3.Cross(right, foward);
            //         var pointerRotation = Quaternion.LookRotation(foward, upwards);
            //         var pointerPosition = rayEndPoint + cameraWorldUp * 0.01f + foward * 0.06f - horizontalVec * 0.03f;
            //         handState.pointerPose = new Pose(pointerPosition, pointerRotation);
            //     }
            // }
            // else
            // {
            //     handState.pointerPoseValid = false;
            // }
        }

        private void CaculatePinchState(HandState handState)//Pinch state
        {
         //   handState.pinchStrength = HandStateUtility.GetIndexFingerPinchStrength(handState);
          //  handState.isPinching = handState.pinchStrength > float.Epsilon;
        }

    }
}
