using UnityEngine;
using System.Collections;
using System;
using Assets.XvXRScripts.Engine;

namespace Singray.Engine
{
    //cm
    [System.Serializable]
    public class XvXRConfigInfo {


	public XvXRConfigInfo Clone(){
		return new XvXRConfigInfo{
			screen=this.screen,
			device=this.device
		};
	}


	[System.Serializable]
	public struct Screen {
		public float width;   // The long edge of the phone.
		public float height;  // The short edge of the phone.
	}

	[System.Serializable]
	public struct Lenses {
		public float separation;     // Center to center. 
		public float screenDistance; // Distance from lens center to the phone screen.
		public float bottomOffset;         // Offset of lens center from top or bottom 

	}
	

	[System.Serializable]
	public struct MaxFOV {
		public float left;  
		public float right;  
		public float top;  
		public float bottom; 
	}


	[System.Serializable]
	public struct Distortion {
		public float k1;
		public float k2;
		
		public float distort(float r) {
			float r2 = r * r;
			return ((k2 * r2 + k1) * r2 + 1) * r;
		}
		
		public float distortInv(float radius) {
			// Secant method.
			float r0 = 0;
			float r1 = 1;
			float dr0 = radius - distort(r0);
			while (Mathf.Abs(r1 - r0) > 0.0001f) {
				float dr1 = radius - distort(r1);
				float r2 = r1 - dr1 * ((r1 - r0) / (dr1 - dr0));
				r0 = r1;
				r1 = r2;
				dr0 = dr1;
			}
			return r1;
		}
	}

	public float VerticalLensOffset{
		get{
			return (device.lenses.bottomOffset-screen.height/2);
		}
	}
	
	/// Information about a particular device, including specfications on its lenses, FOV,
	/// and distortion and inverse distortion coefficients.
	[System.Serializable]
	public struct Device {
		public Lenses lenses;
		public MaxFOV maxFOV;
		public Distortion distortionR;
		public Distortion distortionG;
		public Distortion distortionB;
	}




        public Screen screen;

		public Device device;

		public XvXROpticalParameter_t parameter;

		public void setParamter(XvXROpticalParameter_t param,float[] realFov)
		{
			parameter = param;


			device.lenses.separation = param.separation;
			device.lenses.screenDistance = param.screenDistance;
			device.lenses.bottomOffset = param.bottomOffset;



			if (realFov[0] != 0 && realFov.Length >= 4)
			{
				device.maxFOV.left = realFov[0];
				device.maxFOV.right = realFov[1];
				device.maxFOV.top = realFov[2];
				device.maxFOV.bottom = realFov[3];
			}
			else
			{
				device.maxFOV.left = param.fov_left;
				device.maxFOV.right = param.fov_right;
				device.maxFOV.top = param.fov_top;
				device.maxFOV.bottom = param.fov_bottom;
			}
			

			device.distortionR.k1 = param.red_coff[0];
			device.distortionR.k2 = param.red_coff[1];
			device.distortionG.k1 = param.green_coff[0];
			device.distortionG.k2 = param.green_coff[1];
			device.distortionB.k1 = param.blue_coff[0];
			device.distortionB.k2 = param.blue_coff[1];

			screen.width = param.screen_width_physics;
			screen.height = param.screen_height_physics;

		}


	public void SetDatas(float [] datas){

   

		device.lenses.separation = datas[0];
		device.lenses.screenDistance = datas[1];
		device.lenses.bottomOffset = datas[2];



        device.maxFOV.left = datas [3];
		device.maxFOV.right = datas [4];
		device.maxFOV.top = datas [5];
		device.maxFOV.bottom = datas [6];

		device.distortionR.k1 = datas [7];
		device.distortionR.k2 = datas [8];
		device.distortionG.k1 = datas [9];
		device.distortionG.k2 = datas [10];
		device.distortionB.k1 = datas [11];
		device.distortionB.k2 = datas [12];

		screen.width = datas [13];
		screen.height = datas [14];
	}

        public XvXRConfigInfo.Lenses GetEyeCenter()
        {
            return device.lenses;
        }

      

        public float[] GetEyeVisibleTanAngles(bool isLeft) {
			// Tan-angles from the max FOV.
			if (isLeft)
			{
				float fovLeft = Mathf.Tan(-device.maxFOV.left * Mathf.Deg2Rad);
				float fovTop = Mathf.Tan(device.maxFOV.top * Mathf.Deg2Rad);
				float fovRight = Mathf.Tan(device.maxFOV.right * Mathf.Deg2Rad);
				float fovBottom = Mathf.Tan(-device.maxFOV.bottom * Mathf.Deg2Rad);

				return new float[] { fovLeft, fovTop, fovRight, fovBottom };
			}
			else
			{
				float fovLeft = Mathf.Tan(-device.maxFOV.right * Mathf.Deg2Rad);
				float fovTop = Mathf.Tan(device.maxFOV.top * Mathf.Deg2Rad);
				float fovRight = Mathf.Tan(device.maxFOV.left * Mathf.Deg2Rad);
				float fovBottom = Mathf.Tan(-device.maxFOV.bottom * Mathf.Deg2Rad);

				return new float[] { fovLeft, fovTop, fovRight, fovBottom };
			}
	

	
	}



}
}
