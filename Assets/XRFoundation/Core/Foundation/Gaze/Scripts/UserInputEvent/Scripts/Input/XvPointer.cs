

namespace Singray.UI.Input
{
    using UnityEngine;
    public class XvPointer : MonoBehaviour
    {
        protected Canvas canvas;
        private RectTransform canvasRect;

        public Vector3 ScreenPosition {
            get {
                return UpdatePointer();
            }
            
        }
       
         protected virtual void Awake()
        {
            if (canvas==null) {

                canvas = GetComponentInParent<Canvas>();      
            }

            if (canvas!=null) {
                canvasRect = canvas.GetComponent<RectTransform>();
            
            }

        }

        protected virtual Vector3 UpdatePointer() {
            if (canvas==null) {
                return Vector3.zero;
            }
            Vector3 screenPosition=Input.mousePosition;


            // Debug.Assert(canvas);
            Vector2 localPosition;
            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screenPosition,null,out localPosition);
                    break;
                case RenderMode.ScreenSpaceCamera:
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvas.worldCamera, out localPosition);

                    break;
                case RenderMode.WorldSpace:
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvas.worldCamera, out localPosition);

                    break;
                default:
                    localPosition = Vector2.zero;
                    break;
            }

            //Debug.LogError(localPosition);

            screenPosition.x = localPosition.x;
            screenPosition.y = localPosition.y;
            screenPosition.z = 0;
            transform.localPosition = screenPosition;

            return Input.mousePosition;

        }
    }
}
