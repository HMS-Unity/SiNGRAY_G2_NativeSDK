using UnityEngine;
using System.Collections;

namespace Singray.Engine
{
    [RequireComponent(typeof(Camera))]
    public class XvXRPreRender : MonoBehaviour {

	

	new public Camera camera { get; private set; }

	void Awake() {
		camera = GetComponent<Camera> ();
	}
	
	void Reset() {
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = Color.black;
		camera.cullingMask = 0;
		camera.useOcclusionCulling = false;
		camera.depth = -100;
	}
	
	void OnPreCull() {
			XvXRManager.SDK.UpdateState();

			camera.clearFlags = CameraClearFlags.SolidColor;
		var stereoScreen = XvXRManager.SDK.StereoScreen;
		if (stereoScreen != null&&stereoScreen.Length==2) {
            if (stereoScreen[0]!=null&&!stereoScreen[0].IsCreated()) { 
			    stereoScreen[0].Create();
            }

            if (stereoScreen[1] != null && !stereoScreen[1].IsCreated())
            {
                stereoScreen[1].Create();
            }
		}
			//XvXRManager.SDK.OnCameraPreCull();
		}
	

}

}
