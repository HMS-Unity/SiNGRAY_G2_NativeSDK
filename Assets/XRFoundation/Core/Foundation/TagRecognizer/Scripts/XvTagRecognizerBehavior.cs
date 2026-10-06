using UnityEngine;
using UnityEngine.Events;
namespace Singray.Foundation
{

    /// <summary>
    /// Provides callbacks for successful recognition
    /// </summary>

    public class XvTagRecognizerBehavior : MonoBehaviour
    {

        [Tooltip("Apriltag ID")]

        public int id=-1;
        [Tooltip("Use QR-code text as the ID")]
        public string qrcodeID ;

        public bool ifCheck = true;

        public UnityEvent OnFoundEvent = new UnityEvent();
        public UnityEvent OnLostEvent = new UnityEvent();

        private XvTagRecognizerManager aprilTagManager;


        private TagDetection tagDetection;
        public TagDetection TagDetection
        {
            get { return tagDetection; }
        }

        public void btnClick(GameObject btn)
        {
            switch (btn.name)
            {
                case "StopBtn":
                    ifCheck = false;
                    break;
                case "StartBtn":
                    ifCheck = true;
                    break;
            }
        }


        private void Awake()
        {
       
            aprilTagManager = FindAnyObjectByType<XvTagRecognizerManager>();

            if (id==-1) {
                int.TryParse(gameObject.name, out id);
            }
           // 
        }

        private void OnEnable()
        {
            if (aprilTagManager != null)
            {
                aprilTagManager.OnDetectedAprilTagEvent.AddListener(OnDetectedAprilTagEvent);
            }
        }

        private void OnDisable()
        {
            if (aprilTagManager != null)
            {
                aprilTagManager.OnDetectedAprilTagEvent.RemoveListener(OnDetectedAprilTagEvent);
            }
        }
        // Start is called before the first frame update
        void Start()
        {
            if (OnFoundEvent == null)
            {
                OnFoundEvent = new UnityEvent();
            }
            if (OnLostEvent == null)
            {
                OnLostEvent = new UnityEvent();
            }
            OnLostEvent?.Invoke();

        }



        private void OnDetectedAprilTagEvent(TagDetection[] tagDetections)
        {
            bool isFound = false;
            if (ifCheck == false)
            {
                return;
            }

            if (tagDetections == null)
            {
                isFound = false;
                MyDebugTool.Log("tagDetection == null");
            }
            else
            {
                //Recognition succeeded
                for (int i = 0; i < tagDetections.Length; i++)
                {
                    MyDebugTool.Log("detected：" + tagDetections[i].id + "   " + tagDetections[i].confidence + "  " + aprilTagManager.Confidence);

                    if (tagDetections[i].confidence >= aprilTagManager.Confidence)
                    {
                        if (aprilTagManager.TagGroupName == "36h11")
                        {
                            if (tagDetections[i].id == id)
                            {
                                isFound = true;
                                Vector3 position = tagDetections[i].translation;
                                Quaternion rotation = new Quaternion(tagDetections[i].quaternion[0], tagDetections[i].quaternion[1], tagDetections[i].quaternion[2], tagDetections[i].quaternion[3]);
                                transform.position = position;
                                transform.rotation = rotation;
                                tagDetection = tagDetections[i];
                                break;
                            }
                        }
                        else {
                            MyDebugTool.Log("Parsing QR-code contents: ");

                            string qrText = System.Text.Encoding.UTF8.GetString(tagDetections[i].qrcode).Trim();

                            MyDebugTool.Log("QR-code contents: "+ tagDetections[i].qrcode+"  "+ qrText);
                          
                            if (qrText .Contains(qrcodeID) )
                            {
                                MyDebugTool.Log("qrText:" + qrText);
                                MyDebugTool.Log("qrTextID:" + qrcodeID);
                                isFound = true;
                                Vector3 position = tagDetections[i].translation;
                                Quaternion rotation = new Quaternion(tagDetections[i].quaternion[0], tagDetections[i].quaternion[1], tagDetections[i].quaternion[2], tagDetections[i].quaternion[3]);
                                transform.position = position;
                                transform.rotation = rotation;
                                tagDetection = tagDetections[i];
                                break;
                            }
                        }
                        
                        
                    }
                }
            }

            if (isFound)
            {
                MyDebugTool.Log("detected");

                transform.localScale = Vector3.one * (float)aprilTagManager.Size;
                OnFoundEvent?.Invoke();
            }
            else
            {
                //Debug.Log("Tracking lost");

                OnLostEvent?.Invoke();

            }
        }

    }
}
