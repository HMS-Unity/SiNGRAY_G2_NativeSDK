using UnityEngine;
using UnityEngine.UI;
using Singray.Foundation;

namespace Singray.Foundation.SampleScenes
{


    public class XvSpeechVoiceDemo : MonoBehaviour
    {
        public XvSpeechVoiceManager xvSpeechVoiceManager;
        private bool isRotate;
        public Transform obj;
        public AudioSource audioSource;
        public AudioClip voiceWake;
        public AudioClip voiceEnd;
        public GameObject StopCurrent;
        public Text modeText;

        [SerializeField]
        [Tooltip("Enable wake-word detection")]

        private bool isAvw = true;

        [SerializeField]
        [Tooltip("Start automatically")]

        private bool runAwake = true;

        private void Awake()
        {
            if (xvSpeechVoiceManager == null)
            {
                xvSpeechVoiceManager = GameObject.FindObjectOfType<XvSpeechVoiceManager>();
            }
          

            StopCurrent.SetActive(isAvw);

            if (runAwake) { 
            Invoke("StartSpeechRecognition",3);

            }
        }

        private void Update()
        {
            if (isRotate)
            {
                obj.rotation *= Quaternion.Euler(Vector3.up * Time.deltaTime * 50);
            }


        }
        public void StartRotate()
        {
            isRotate = true;
        }
        public void StopRotate()
        {
            isRotate = false;
        }
        public void ScaleUp()
        {

            obj.localScale *= 1.1f;
        }
        public void ScaleDown()
        {
            obj.localScale *= 0.9f;
        }

      
        public void StartSpeechRecognition()
        {
            if (!xvSpeechVoiceManager.IsOn) {
               
                modeText.text = isAvw ? "Start wake-word recognition" : "Start command recognition";

                if (isAvw)
                {

                    xvSpeechVoiceManager.StartAVW(5000, 500);
                }
                else
                {
                    xvSpeechVoiceManager.StartASR(5000, 500);
                }
            }
        }

        /// <summary>
        /// Stop speech recognition
        /// </summary>
        public void StopSpeechRecognition()
        {
            if (xvSpeechVoiceManager.IsOn) {
                modeText.text = isAvw ? "Stop wake-word recognition" : "Stop command recognition";
                if (isAvw)
                {
                    //Stop wake-word and command recognition
                    xvSpeechVoiceManager.StopAVW();
                }
                else
                {
                    //Stop command recognition
                    xvSpeechVoiceManager.StopASR();
                }
            }
        }

        /// <summary>
        /// Stop command recognition only; in wake-word mode, keep wake-word detection active.
        /// </summary>
        public void StopCurrentSpeechRecognition()
        {
            if (xvSpeechVoiceManager.IsOn)
            {
                modeText.text = isAvw ? "Stop wake-word recognition" : "Stop command recognition";

                xvSpeechVoiceManager.StopASR();
               
            }
        }


        public void PlayVoiceWake()
        {
            if (audioSource != null && voiceWake != null)
            {
                audioSource.PlayOneShot(voiceWake);
            }
        }


      
        public void PlayVoiceEnd()
        {
            if (audioSource != null && voiceEnd != null)
            {
                audioSource.PlayOneShot(voiceEnd);
            }
        }


        public void OnRecognizedStatus(result result)
        {

            MyDebugTool.Log("OnRecognizedStatus:" + result.word);

        }


    }
}