using UnityEngine;
namespace Singray.Foundation.SampleScenes
{
    public class XvTagRecognizerDemo : MonoBehaviour
    {
        public void OnFoundEvent(XvTagRecognizerBehavior xvTagRecognizerBehavior)
        {
            Transform TagCenter = xvTagRecognizerBehavior.transform.Find("TagCenter");
            string qrCodeContent= System.Text.Encoding.UTF8.GetString(xvTagRecognizerBehavior.TagDetection.qrcode);
            TagCenter.Find("msg").GetComponent<TextMesh>().text = qrCodeContent;
        }
        public void OnLostEvent(XvTagRecognizerBehavior xvTagRecognizerBehavior)
        {

        }
    }
}
