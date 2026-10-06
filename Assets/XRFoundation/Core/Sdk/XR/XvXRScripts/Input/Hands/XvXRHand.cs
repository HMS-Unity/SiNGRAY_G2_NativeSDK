
namespace Singray
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    
    public class XvXRHand : MonoBehaviour
    {
        [SerializeField]
        private HandEnum m_HandEnum = HandEnum.None;

        public HandEnum HandEnum { get { return m_HandEnum; } }

        private void Awake()
        {
            if(m_HandEnum == HandEnum.None)
            {
                Debug.LogError("HandEnum Should Not Be None !");
                return;
            }
            XvXRInput.Hands.RegistHand(this);
        }

        private void OnDestroy()
        {
             XvXRInput.Hands.UnRegistHand(this);
        }

        public HandState GetHandState()
        {
             return XvXRInput.Hands.GetHandState(m_HandEnum);
           
        }
    }
}
