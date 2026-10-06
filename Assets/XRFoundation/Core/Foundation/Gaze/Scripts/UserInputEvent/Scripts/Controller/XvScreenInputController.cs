
 namespace Singray.UI.Input
{
    using UnityEngine;
    public class XvScreenInputController : XvInputControllerBase
    {
        public KeyCode keyCode = KeyCode.Q;
        public XvPointer pointer;

        public override bool GetKeyPress()
        {
            return Input.GetKey(keyCode);
        }

        public override Vector3 GetInputPosition()
        {
            if (pointer != null)
            {
                return pointer.ScreenPosition;
            }
            else
            {
                return Input.mousePosition;
            }
        }
    }
}
