

namespace Singray.UI.Input
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    public class CustomEventData : PointerEventData
    {
        #region  Avoid direct access to prevent unintended side effects

        public bool pressPrecessed;//Record press and release states

        public float screenPositionDeltadelta;//Movement delta

        private XvInputControllerBase inputControllerBase;



        #endregion

        public CustomEventData(EventSystem eventSystem, XvInputControllerBase inputControllerBase) : base(eventSystem)
        {
            this.inputControllerBase = inputControllerBase;
        }



        public RaycastHit hover3DRaycastHit;//Selected 3D object
        public static Vector3 ScreenCenterPoint { get { return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); } }

        internal bool GetKeyPress()
        {
            return inputControllerBase.GetKeyPress();

        }
    }

  
    

    public abstract class XvInputControllerBase : MonoBehaviour
    {
      
        internal XvRaycaster customRaycaster
        {
            get;
            private set;
        }
        private CustomEventData customEventData;
        public CustomEventData CustomEventData
        {
            get
            {

                if (customEventData == null)
                {
                    customEventData = new CustomEventData(EventSystem.current, this);
                }
                return customEventData;
            }
        }



        //Whether this is 3D ray input
        internal bool Is3DInput
        {
            get
            {
                return customRaycaster != null;
            }
        }

        /// <summary>
        /// Get the current custom screen point
        /// </summary>
        internal Vector3 screenPosition
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the movement between the previous two frames
        /// </summary>
        internal Vector3 PositionDeltadelta
        {
            get
            {
                return positionDeltadelta;
            }
            private set
            {
                positionDeltadelta = value;
            }
        }

        private Vector3 lastPosition;

        private Vector3 positionDeltadelta;


        /// <summary>
        /// Get screen input coordinates
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetInputPosition()
        {

            if (Is3DInput)
            {
                return transform.forward;
            }
            else
            {
                return Input.mousePosition;
            }

        }

        public virtual Vector2 GetScrollDelta()
        {
            return Vector3.zero;
        }

        public virtual bool GetKeyPress()
        {
           
            return Input.GetMouseButton(0);
        }
        protected virtual void Awake()
        {
            Initialized();
        }

        protected virtual void Update()
        {
            UpdateInputPosition();
        }

        /// <summary>
        /// Initialize the custom input module
        /// </summary>
        private void Initialized()
        {
            screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

            customRaycaster = GetComponent<XvRaycaster>();
            if (EventSystem.current.gameObject.GetComponent<XvXRInputModule>() == null)
            {
                Debug.LogError("Execution count");
                Destroy(EventSystem.current.gameObject.GetComponent<StandaloneInputModule>());
                EventSystem.current.gameObject.AddComponent<XvXRInputModule>();
            }
        }

        /// <summary>
        /// Update input coordinates
        /// </summary>
        private void UpdateInputPosition()
        {
            if (Is3DInput)
            {
                positionDeltadelta = (GetInputPosition() - lastPosition) * 1000;
                lastPosition = GetInputPosition();
            }
            else
            {
                screenPosition = GetInputPosition();
                positionDeltadelta = screenPosition - lastPosition;
                lastPosition = screenPosition;
            }
        }
    }
}
