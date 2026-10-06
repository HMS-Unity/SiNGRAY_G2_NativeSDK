using UnityEngine;
using System.Collections;
using System;

namespace Singray.SystemEvents
{
	

	internal class AndroidEvent  {

		private static GameObject bvrEventMoudle = null;

		private static AndroidInterface androidInterface;

		private static AndroidInterface Interface{
			get{
				if(androidInterface==null){
					InitEventObject();
				}
				return androidInterface;
			}
		}

		public static void Init(){
			InitEventObject ();
		}

       

        private static void InitEventObject(){
			if (androidInterface == null) {
				bvrEventMoudle = new GameObject("XvXREventModule");
				androidInterface =bvrEventMoudle.AddComponent<AndroidInterface>();   
			}
		}


		public static Quaternion GetSensorQuaternion(){
			float[] quaternion = Interface.GetQuaternion ();
			Quaternion newQ = Quaternion.identity;
			newQ=new Quaternion(quaternion[0], quaternion[1], quaternion[2], quaternion[3]);
			return newQ;
		}

		public static float[] GetXvXRConfigInfo(){
			return Interface.GetUnityXvXRConfigInfo ();
		}

		/// <summary>
		/// Call GetXvXROpticalParameter in the Java library
		/// </summary>
		/// <returns></returns>
		internal static float[] GetXvXROpticalParameter()
        {
			return Interface.GetXvXROpticalParameter();

		}

		internal static void UpdateDevicePose(Quaternion quaternion, Vector3 postion)
        {
			Interface.UpdateDevicePoseData(quaternion.x,quaternion.y,quaternion.z,quaternion.w,postion.x,postion.y,postion.z);
        }

        internal static int GetFd()
        {
			return Interface.GetFd();
        }
    }
}