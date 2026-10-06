
namespace Singray.UI.Input
{
   
    using UnityEngine;
    public class XvHandInputController : XvInputControllerBase
    {
        public KeyCode keyCode = KeyCode.Q;

        public LineRenderer lineRenderer;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {

        }
        protected override void Update()
        {
            base.Update();
            Control();
        }

        private void Control()
        {
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, transform.position);

                if (CustomEventData.hover3DRaycastHit.transform?.gameObject != null)
                {
                    // Debug.LogError("Selected 3D object: " + CustomEventData.hover3DRaycastHit.transform.name);
                    lineRenderer.SetPosition(1, CustomEventData.hover3DRaycastHit.point);
                }
                else
                {
                    if (CustomEventData.pointerCurrentRaycast.gameObject != null)
                    {
                        // Debug.LogError("Selected UI");

                        lineRenderer.SetPosition(1, CustomEventData.pointerCurrentRaycast.worldPosition);
                    }
                    else
                    {
                         //Debug.LogError("No interactive object selected");
                        lineRenderer.SetPosition(1, transform.position + transform.forward * 1000);
                    }
                }
            }
            if (!Is3DInput)
            {
                return;
            }

            if (Input.GetMouseButton(1))
            {
                float x = Input.GetAxis("Mouse X") * 10;
                float y = Input.GetAxis("Mouse Y") * 10;


                transform.localRotation *= Quaternion.Euler(new Vector3(-y, x, 0));

            }

            //transform.localPosition += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * 10;

            if (Input.GetKey(KeyCode.H))
            {

                transform.localEulerAngles += Vector3.up * -0.04f;

            }
            else if (Input.GetKey(KeyCode.K))
            {
                transform.localEulerAngles += Vector3.up * 0.04f;

            }

            if (Input.GetKey(KeyCode.U))
            {

                transform.localEulerAngles += Vector3.right * -0.04f;

            }
            else if (Input.GetKey(KeyCode.J))
            {
                transform.localEulerAngles += Vector3.right * 0.04f;

            }

        }

        public override Vector3 GetInputPosition()
        {
            return transform.forward;
        }

        public override bool GetKeyPress()
        {
            bool press = Input.GetKey(keyCode);
            return press;
        }

    }
}