using System.Collections.Generic;
using UnityEngine;
namespace Singray.Foundation
{
    public class XvStaticGestureManager : MonoBehaviour
    {
        private XvStaticGestureManager () { }
        private GestureKeyState left;
        private GestureKeyState right;
        void Start()
        {
            left = new GestureKeyState(0);
            right = new GestureKeyState(1);
        }

        //private void Update()
        //{


        //    if (GetKeyDown(StaticGestureStatus.Point, HandType.Left))
        //    {
        //        MyDebugTool.Log("GetKeyDown");
        //    }

        //    if (GetKey(StaticGestureStatus.Point, HandType.Left))
        //    {
        //        MyDebugTool.Log("GetKey");
        //    }

        //    if (GetKeyUp(StaticGestureStatus.Point, HandType.Left))
        //    {
        //        MyDebugTool.Log("GetKeyUp");
        //    }
        //}

        /// <summary>
        /// Get the current gesture
        /// </summary>
        /// <param name="handType">Left or right hand</param>
        /// <returns></returns>
        public StaticGestureStatus GetCurrentStaticGesture(HandType handType)
        {
            switch (handType)
            {
                case HandType.None:
                    break;
                case HandType.Left:
                    return (StaticGestureStatus)XvXRInput.xvSkeleton.status[0];

                case HandType.Right:
                    return (StaticGestureStatus)XvXRInput.xvSkeleton.status[1];

                default:
                    break;
            }

            return StaticGestureStatus.UNKONOW;
        }

        public bool GetKeyDown(StaticGestureStatus gestureStatus, HandType handType)
        {
            switch (handType)
            {
                case HandType.None:
                    break;
                case HandType.Left:
                    return left.GetKeyDown(gestureStatus);

                case HandType.Right:
                    return right.GetKeyDown(gestureStatus);

                default:
                    break;
            }
            return false;

        }
        public bool GetKeyUp(StaticGestureStatus buttonKey, HandType handType)
        {
            switch (handType)
            {
                case HandType.None:
                    break;
                case HandType.Left:
                    return left.GetKeyUp(buttonKey);

                case HandType.Right:
                    return right.GetKeyUp(buttonKey);

                default:
                    break;
            }
            return false;

        }

        public bool GetKey(StaticGestureStatus buttonKey, HandType handType)
        {

            switch (handType)
            {
                case HandType.None:
                    break;
                case HandType.Left:
                    return left.GetKey(buttonKey);

                case HandType.Right:
                    return right.GetKey(buttonKey);

                default:
                    break;
            }
            return false;


        }

        private void LateUpdate()
        {
            left.LateUpdate();
            right.LateUpdate();
        }
    }
    public enum StaticGestureStatus {
        UNKONOW=-1,
        GRAB=0,
        Point,
        VICTORY,
        Three,
        Four,
        OpenHand,
        Call,
        Rock,
        Thumb,
        Pinch,
    }
    public enum HandType
    {
        None,
        Left,
        Right,
    }

    public class GestureState
    {

        
        private StaticGestureStatus buttonKey;
        private bool IsPress;
        private KeyCode keyCode;

        private int  handIndex;
        public GestureState(StaticGestureStatus buttonKey, int handIndex)
        {
            this.buttonKey = buttonKey;
            this.handIndex = handIndex;
        }



        private bool IsTriggerPress;
        private float timer;
        private bool isKeyDown;


        private bool keyDown;
        private bool keyUp;
        private bool key;
        private bool doubleClick;



        public void Update()
        {
            keyDown = false;
            keyUp = false;

            doubleClick = false;


            switch (buttonKey)
            {
                case StaticGestureStatus.UNKONOW:
                    break;
                case StaticGestureStatus.GRAB:
           
                    IsPress = XvXRInput.xvSkeleton.status[handIndex]==0;
                    keyCode = KeyCode.Alpha0;
                    break;
                case StaticGestureStatus.Point:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 1;
                    keyCode = KeyCode.Alpha1;

                    break;
                case StaticGestureStatus.VICTORY:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 2;
                    keyCode = KeyCode.Alpha2;

                    break;
                case StaticGestureStatus.Three:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 3;
                    keyCode = KeyCode.Alpha3;

                    break;
                case StaticGestureStatus.Four:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 4;
                    keyCode = KeyCode.Alpha4;


                    break;
                case StaticGestureStatus.OpenHand:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 5;
                    keyCode = KeyCode.Alpha5;


                    break;
                case StaticGestureStatus.Call:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 6;
                    keyCode = KeyCode.Alpha6;


                    break;
                case StaticGestureStatus.Rock:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 7;
                    keyCode = KeyCode.Alpha7;


                    break;
                case StaticGestureStatus.Thumb:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 8;
                    keyCode = KeyCode.Alpha8;


                    break;
                case StaticGestureStatus.Pinch:
                    IsPress = XvXRInput.xvSkeleton.status[handIndex] == 9;
                    keyCode = KeyCode.Alpha9;


                    break;
                default:
                    break;
            }


            
            if (IsPress || Input.GetKey(keyCode))
            {
                if (!IsTriggerPress)
                {
                    keyDown = true;
                    //MyDebugTool.Log("OnTriggerKeyDown:   " + buttonKey);
                    IsTriggerPress = true;

                    if (isKeyDown)
                    {

                        if (timer < 0.2f)
                        {
                            doubleClick = true;
                            //MyDebugTool.Log("OnTriggerDoubleClick:" + buttonKey);

                        }
                        isKeyDown = false;
                        timer = 0;
                    }
                    else
                    {

                        isKeyDown = true;
                    }
                }
                else
                {


                    key = true;
                    // MyDebugTool.Log("OnTriggerKey");

                }

            }
            else
            {
                if (isKeyDown)
                {
                    timer += Time.deltaTime;
                    if (timer > 0.3f)
                    {
                        timer = 0;
                        isKeyDown = false;
                    }
                }
                if (IsTriggerPress)
                {
                    keyUp = true;

                    //MyDebugTool.Log("OnTriggerKeyUp:" + buttonKey);

                    key = false;
                    IsTriggerPress = false;
                }
            }

        }




        public bool GetKeyDown()
        {
            return keyDown;

        }
        public bool GetKeyUp()
        {
            return keyUp;

        }

        public bool GetKey()
        {
            return key;

        }
        public bool GetDoubleClick()
        {
            return doubleClick;

        }


    }
    public class GestureKeyState
    {
        public Dictionary<StaticGestureStatus, GestureState> buttonKeyDic = new Dictionary<StaticGestureStatus, GestureState>();
        public GestureKeyState(int hanIndex)
        {

            buttonKeyDic.Add(StaticGestureStatus.GRAB, new GestureState(StaticGestureStatus.GRAB, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Point, new GestureState(StaticGestureStatus.Point, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.VICTORY, new GestureState(StaticGestureStatus.VICTORY, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Three, new GestureState(StaticGestureStatus.Three, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Four, new GestureState(StaticGestureStatus.Four, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.OpenHand, new GestureState(StaticGestureStatus.Four, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Call, new GestureState(StaticGestureStatus.Four, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Rock, new GestureState(StaticGestureStatus.Four, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Thumb, new GestureState(StaticGestureStatus.Four, hanIndex));
            buttonKeyDic.Add(StaticGestureStatus.Pinch, new GestureState(StaticGestureStatus.Four, hanIndex));


        }

      

        public void LateUpdate()
        {
            foreach (var item in buttonKeyDic.Values)
            {
                item.Update();
            }
        }

        public bool GetKeyDown(StaticGestureStatus buttonKey)
        {
            if (buttonKeyDic.TryGetValue(buttonKey, out GestureState keyState)) { 
            


                return keyState.GetKeyDown();

            }

            return false;

        }
        public bool GetKeyUp(StaticGestureStatus buttonKey)
        {
            if (buttonKeyDic.TryGetValue(buttonKey, out GestureState keyState))
            {

            return keyState.GetKeyUp();
            }
            return false;

        }

        public bool GetKey(StaticGestureStatus buttonKey)
        {
            if (buttonKeyDic.TryGetValue(buttonKey, out GestureState keyState)) { 
            return keyState.GetKey();
            }
            return false;

        }
        public bool GetDoubleClick(StaticGestureStatus buttonKey)
        {

            if (buttonKeyDic.TryGetValue(buttonKey, out GestureState keyState)) { 
            return keyState.GetDoubleClick();
            }
            return false;

        }
    }

    

}