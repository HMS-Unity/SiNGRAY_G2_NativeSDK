using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{
    public class XvTagRecognizerUIController : MonoBehaviour
    {
        public XvTagRecognizerManager tagManager;
      


        public Text recognizerTex;
        

        public Text tagSize;
       

        private void Awake()
        {
            if (tagManager == null)
            {
                tagManager = FindAnyObjectByType<XvTagRecognizerManager>();
            }
            if (tagManager == null)
            {

                Debug.LogError("AprilTag:Manager==null");
                return;
            }

            switch (tagManager.RecognizerMode)
            {
                case RecognizerMode.None:
                    recognizerTex.text = "None";

                    break;
                case RecognizerMode.RGB_QRCode:
                    recognizerTex.text = "RGB_QRCode";

                    break;
                case RecognizerMode.RGB_Apriltag:
                    recognizerTex.text = "RGB_Apriltag";

                    break;
                case RecognizerMode.FishEye_Apriltag:
                    recognizerTex.text = "FishEye_Apriltag";

                    break;
                default:
                    break;
            }

            tagSize.text= tagManager.Size.ToString();



        }
        public void btnClick(GameObject btn)
        {

            if (tagManager == null)
            {

                Debug.LogError("AprilTag:Manager==null");
                return;
            }

            switch (btn.name)
            {
               
                case "4cmBtn":
                    tagManager.Size = 0.04;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "6cmBtn":
                    tagManager.Size = 0.06;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "8cmBtn":
                    tagManager.Size = 0.08;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "10cmBtn":
                    tagManager.Size = 0.1;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "12cmBtn":
                    tagManager.Size = 0.12;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "14cmBtn":
                    tagManager.Size = 0.14;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "16cmBtn":
                    tagManager.Size = 0.16;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "18cmBtn":
                    tagManager.Size = 0.18;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "30cmBtn":
                    tagManager.Size = 0.3;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "1cmBtn":
                    tagManager.Size = 0.01;
                    tagSize.text = tagManager.Size.ToString();
                    break;
                case "SizeBtn":
                    if (tagSize != null)
                    {
                        tagManager.Size = float.Parse(tagSize.text);
                    }
                    break;
                case "RGB_Apriltag":

                    recognizerTex.text = "RGB_Apriltag";
                    tagManager.StartTagDetector(RecognizerMode.RGB_Apriltag);
                    break;
                case "FishEye_Apriltag":


                    recognizerTex.text = "FishEye_Apriltag";
                    tagManager.StartTagDetector(RecognizerMode.FishEye_Apriltag);

                    break;

                case "RGB_QRCode":
                    recognizerTex.text = "RGB_QRCode";
                    tagManager.StartTagDetector(RecognizerMode.RGB_QRCode);

                    break;

                case "StopTagDetector":
                    recognizerTex.text = "None";
                    tagManager.StopTagDetector();

                    break;

                    
            }
        }



    }
}
