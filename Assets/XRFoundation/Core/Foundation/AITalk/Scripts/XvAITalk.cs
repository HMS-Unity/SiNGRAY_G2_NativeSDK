using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Singray.Foundation {

    public class XvAITalk :SingleTon<XvAITalk>
    {

        public UnityAction<string, string, string> onTxtResult;
        public UnityAction<string, string, string> onVoiceResult;

        AndroidJavaObject GetUnityActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return unityActivity;
        }
        public class GptDataCallback : AndroidJavaProxy
        {
            public GptDataCallback() : base("com.k2fsa.sherpa.onnx.GptDataCallback") { }

            void onTxtResult(string id,string result,string status) {
                MyDebugTool.Log("onTxtResult:"+ id+"  "+ result+"  "+ status);
                XvAITalk.Instance.onTxtResult?.Invoke(id, result, status);
            }

            void onVoiceResult(string id, string result, string status) {
                MyDebugTool.Log("onVoiceResult:" + id + "  " + result + "  " + status);

                XvAITalk.Instance.onVoiceResult?.Invoke(id, result, status);

            }

        }
        private AndroidJavaObject unityInterface;



        GptDataCallback resultCallback;
        
        public void InitGptClient( string ipAddress) {

            if(unityInterface==null){
                AndroidJavaObject unityActivity = GetUnityActivity();
                AndroidJavaClass clazz = new AndroidJavaClass("com.k2fsa.sherpa.onnx.UnityInterface");
                unityInterface = clazz.CallStatic<AndroidJavaObject>("getInstance", unityActivity);
                resultCallback = new GptDataCallback();
                unityInterface.Call("initGptClient", resultCallback, ipAddress);
            }
           
           }



        /// <summary>
        /// Start speech input
        /// </summary>
        public void startAsrRecord()
        {
            if (unityInterface!=null) {
            unityInterface.Call("startAsrRecord");

            }


        }
        /// <summary>
        /// Stop speech input
        /// </summary>
        public void pauseAsrRecord()
        {
            if (unityInterface != null)
            {
                unityInterface.Call("pauseAsrRecord");

            }


        }
    }

}

