using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Singray.UI.Keyboard;
namespace Singray.Foundation.SampleScenes {


public class XvAITalkDemo : MonoBehaviour
{
        public CustomInputField customInput;
        public Button send;

    void Start()
    {

            XvAITalk.Instance.InitGptClient("ws://copilot.xvisio.net:8000/realtime");
            send.onClick.AddListener(() => { 
            


            });

            XvAITalk.Instance.onTxtResult += (id, result, status) =>
            {


            };
            XvAITalk.Instance.startAsrRecord();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
}
