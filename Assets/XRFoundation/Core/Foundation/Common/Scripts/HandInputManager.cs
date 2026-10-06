using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
namespace Singray.Foundation
{
    public class HandState
    {

        public XRNode handNode;
        private Handedness handedness;

        private bool keyDown;
        private bool key;
        private bool keyUp;
        public HandState(XRNode xRNode)
        {
            handNode = xRNode;

            if (xRNode == XRNode.LeftHand)
            {
                handedness = Handedness.Left;
            }
            else if (xRNode == XRNode.RightHand)
            {
                handedness = Handedness.Right;
            }
        }

        public void Update()
        {

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out var leftIndex) &&
                  HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out var leftThumb))
            {

                bool isPinched = Vector3.Distance(leftIndex.Position, leftThumb.Position) < 0.06f;
                //Debug.Log("isPinched===" + isPinched);
                if (isPinched)
                {

                    if (!keyDown && !key)
                    {
                        keyDown = true;
                    }
                    else
                    {
                        keyDown = false;
                        key = true;
                    }

                }
                else
                {
                    if (key)
                    {
                        keyUp = true;
                        key = false;
                        keyDown = false;
                    }
                    else
                    {
                        keyUp = false;

                    }
                }


            }



        }

        public bool GetKeyDown()
        {
            return keyDown;
        }
        public bool GetKey()
        {
            return key;
        }
        public bool GetKeyUp()
        {
            return keyUp;
        }
    }

    public class HandInputManager : MonoBehaviour
    {
        private static HandInputManager instance;


        public static HandInputManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<HandInputManager>();

                    if (instance == null)
                    {
                        instance = new GameObject().AddComponent<HandInputManager>();
                    }

                }

                return instance;

            }
        }

        private Dictionary<XRNode, HandState> handInputSource = new Dictionary<XRNode, HandState>();
        private void Awake()
        {
          //  handInputSource = new Dictionary<XRNode, HandState>();
            handInputSource.Add(XRNode.LeftHand, new HandState(XRNode.LeftHand));
            handInputSource.Add(XRNode.RightHand, new HandState(XRNode.RightHand));

        }

        private void Update()
        {
            foreach (var item in handInputSource.Values)
            {
                item.Update();
            }

            //if (GetKeyDown(XRNode.LeftHand))
            //{
            //    Debug.Log("LeftHand:GetKeyDown");

            //}
            //if (GetKey(XRNode.LeftHand))
            //{
            //    Debug.Log("LeftHand:GetKey");

            //}
            //if (GetKeyUp(XRNode.LeftHand))
            //{
            //    Debug.Log("LeftHand:GetKeyUp");

            //}

            //if (GetKeyDown(XRNode.RightHand))
            //{
            //    Debug.Log("RightHand:GetKeyDown");

            //}
            //if (GetKey(XRNode.RightHand))
            //{
            //    Debug.Log("RightHand:GetKey");

            //}
            //if (GetKeyUp(XRNode.RightHand))
            //{
            //    Debug.Log("RightHand:GetKeyUp");

            //}

        }

        public bool GetKeyDown(XRNode xRNode)
        {
            if (handInputSource.TryGetValue(xRNode, out HandState state))
            {
                return state.GetKeyDown();
            }
            return false;
        }

        public bool GetKey(XRNode xRNode)
        {
            if (handInputSource.TryGetValue(xRNode, out HandState state))
            {
                return state.GetKey();
            }
            return false;
        }
        public bool GetKeyUp(XRNode xRNode)
        {
            if (handInputSource.TryGetValue(xRNode, out HandState state))
            {
                return state.GetKeyUp();
            }
            return false;
        }
    }
}