using System;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{
    public class XvStaticGestureDemo : MonoBehaviour
    {
        [SerializeField]
        private XvStaticGestureManager staticGestureManager;


        public XvStaticGestureManager XvStaticGestureManager
        {
            get
            {

                if (staticGestureManager == null)
                {
                    staticGestureManager = FindObjectOfType<XvStaticGestureManager>();
                }

                if (staticGestureManager == null)
                {
                    staticGestureManager = new GameObject("XvStaticGestureManager").AddComponent<XvStaticGestureManager>();
                }
                return staticGestureManager;

            }
        }

        public Text leftGesture;
        public Text rightGesture;

       



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            foreach (StaticGestureStatus value in Enum.GetValues(typeof(StaticGestureStatus)))
            {
                if (XvStaticGestureManager.GetKeyDown(value, HandType.Left))
                {
                    MyDebugTool.Log("Left GetKeyDown " + value);
                }

               

                if (XvStaticGestureManager.GetKeyUp(value,HandType.Left))
                {
                    MyDebugTool.Log("Left GetKeyUp " + value);
                }

                if (XvStaticGestureManager.GetKeyDown(value, HandType.Right))
                {
                    MyDebugTool.Log("Right GetKeyDown " + value);
                }

               

                if (XvStaticGestureManager.GetKeyUp(value, HandType.Right))
                {
                    MyDebugTool.Log("Right GetKeyUp " + value);
                }
            }


            
            leftGesture.text= XvStaticGestureManager.GetCurrentStaticGesture(HandType.Left).ToString();
            rightGesture.text = XvStaticGestureManager.GetCurrentStaticGesture(HandType.Right).ToString();

        }
    }
}
