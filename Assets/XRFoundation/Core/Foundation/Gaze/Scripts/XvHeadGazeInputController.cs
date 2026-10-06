
using UnityEngine;
namespace Singray.UI.Input
{
    [DisallowMultipleComponent]
    public sealed class XvHeadGazeInputController : XvInputControllerBase
    {

        private XvHeadGazeInputController (){}
        public static XvHeadGazeInputController Instance;
        public KeyCode keyCode = KeyCode.Q;

        public Transform pointer;

        private Vector3 targetPosition = Vector3.forward * 3;
        public float scaleFactor = 1;

        private Transform cameraTran;
        private bool enable=true;

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
            cameraTran = Camera.main.transform;
        }

        public void ShowOrHidePointer(bool isShow)
        {
            enable=isShow;
            pointer.gameObject.SetActive(enable);
        }


        public Vector3 GetPointer3DPoint()
        {
            return pointer.position;

        }
        protected override void Update()
        {
            base.Update();
            Control();


        }

        private void Control()
        {
            if (cameraTran != null)
            {
                transform.position = cameraTran.position;
                cameraTran.rotation = cameraTran.rotation;
            }
            if (pointer != null)
            {
                if (CustomEventData.hover3DRaycastHit.transform?.gameObject != null && CustomEventData.hover3DRaycastHit.distance < 5)
                {
                    targetPosition.z = CustomEventData.hover3DRaycastHit.distance;
                    pointer.localScale = Vector3.one * CustomEventData.hover3DRaycastHit.distance * scaleFactor;
                    pointer.localPosition = targetPosition;

                    //MyDebugTool.Log("Selected 3D object: " + CustomEventData.hover3DRaycastHit.transform?.gameObject);
                }
                else
                {
                    if (CustomEventData.pointerCurrentRaycast.gameObject != null)
                    {
                        targetPosition.z = CustomEventData.pointerCurrentRaycast.distance;

                        pointer.localScale = Vector3.one * CustomEventData.pointerCurrentRaycast.distance * scaleFactor;
                        pointer.localPosition = targetPosition;
                       // MyDebugTool.Log("Selected UI object");

                    }
                    else
                    {
                        pointer.localScale = Vector3.one * 3 * scaleFactor;
                        pointer.transform.localPosition = Vector3.forward * 3;
                       // MyDebugTool.Log("No object selected");

                    }
                }
            }
            if (!Is3DInput)
            {
                return;
            }

#if UNITY_EDITOR

            if (UnityEngine.Input.GetMouseButton(1))
            {
                float x = UnityEngine.Input.GetAxis("Mouse X");
                float y = UnityEngine.Input.GetAxis("Mouse Y");


                transform.localRotation *= Quaternion.Euler(new Vector3(-y, x, 0));

            }

            //transform.localPosition += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * 10;



#endif

        }

        public override Vector3 GetInputPosition()
        {
            return transform.forward;
        }

        public override bool GetKeyPress()
        {
           

            bool press = UnityEngine.Input.GetKey(keyCode); ;

            bool OK = UnityEngine.Input.GetKey(KeyCode.Return) || UnityEngine.Input.GetMouseButton(0);
            // Debug.Log("OK;" + OK);
            //bool OK = Input.GetKeyDown(KeyCode.Return);
            //bool LeftArrow = Input.GetKeyDown(KeyCode.LeftArrow);
            //bool RightArrow = Input.GetKeyDown(KeyCode.RightArrow);
            //bool UpArrow = Input.GetKeyDown(KeyCode.UpArrow);
            //bool DownArrow = Input.GetKeyDown(KeyCode.DownArrow);

            return (press || OK)&& enable;
        }
    }
}



