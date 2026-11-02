using UnityEngine;
namespace Singray.Foundation.SampleScenes
{
    // Compatibility type retained for customer scripts and serialized UnityEvent method references.
    public class XvTagRecognizerDemo : MonoBehaviour
    {
        public void OnFoundEvent(XvTagRecognizerBehavior xvTagRecognizerBehavior)
        {
            if (xvTagRecognizerBehavior == null || xvTagRecognizerBehavior.TagDetection == null) return;
            var center = xvTagRecognizerBehavior.transform.Find("TagCenter");
            var message = center != null ? center.Find("msg") : null;
            var text = message != null ? message.GetComponent<TextMesh>() : null;
            if (text != null) text.text = XvAprilTag.QrText(xvTagRecognizerBehavior.TagDetection.qrcode);
        }
        public void OnLostEvent(XvTagRecognizerBehavior xvTagRecognizerBehavior) { }
    }
}
