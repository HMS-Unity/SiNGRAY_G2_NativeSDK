using System.Collections.Generic;
using UnityEngine;



namespace Singray.Foundation
{
    public class XvHandleController : MonoBehaviour
    {
      
        [SerializeField]
        private string serialNumber;

        public string SerialNumber { 
            get { return serialNumber; } 
        
        }


      


        protected  void LateUpdate()
        {
            UpdatePose();
        }

        protected virtual void UpdatePose()
        {
           
        }


        public virtual Vector3 GetPosition()
        {
            return Vector3.zero;

        }
        public virtual Quaternion GetRotation()
        {
            return Quaternion.identity;
        }

        public virtual  bool IsConnected() {

          

            return false;
        }

        public virtual   JoystickData GetJoystickData()
        {
            return null;
        }



        public virtual int GetBattery()
        {

          
            return 0;
        }

        public virtual int GetTemperature()
        {
           
            return 0;
        }

        public virtual API.WirelessControllerDeviceInfo GetDeviceInfo( )
        {

            return default;
        }


    }
}