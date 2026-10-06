using UnityEngine;
using System.Collections;

namespace Singray.Engine
{

[RequireComponent(typeof(Camera))]
public class XvXRPostRender : MonoBehaviour {


	new public Camera camera { get; private set; }


	void Reset() {
		camera.clearFlags = CameraClearFlags.Depth;
		camera.backgroundColor = Color.black;
		camera.orthographic = true;
		camera.orthographicSize = 0.5f;
		camera.cullingMask = 0;
		camera.useOcclusionCulling = false;
		camera.depth = 100;
	}
	
	void Awake() {
		camera = GetComponent<Camera>();
		Reset();

	}

	
	void OnPreCull() {
		if (XvXRSdkConfig.XvXR_PLATFORM == XvXRSdkConfig.PLATFORM.XvXR_UNITY_EDITOR) {
			float aspectComparison;
			float realAspect = (float)Screen.width / Screen.height;
			float fakeAspect = XvXRManager.SDK.Info.screen.width / XvXRManager.SDK.Info.screen.height;
			aspectComparison = fakeAspect / realAspect;
				if (!float.IsNaN(aspectComparison))
					camera.orthographicSize = 0.5f * Mathf.Max(1, aspectComparison);
			}

	}

	
	void OnRenderObject() {
		if (Camera.current != camera)
			return;
		// The source pose was frozen when XvXRHeadTracking applied it to the
		// camera hierarchy. Sampling tracking again here would associate the
		// completed textures with a newer, incorrect pose.
		var correction = XvXRManager.SDK.DistortionCorrection;
		RenderTexture[] stereoScreen = XvXRManager.SDK.StereoScreen;
		if (stereoScreen == null ||stereoScreen.Length!=2|| correction == XvXRManager.DistortionCorrectionMethod.None) {
			return;
		}
		if (correction == XvXRManager.DistortionCorrectionMethod.Native
		    && XvXRManager.SDK.NativeDistortionCorrectionSupported) {
			XvXRManager.SDK.PostRender();
		} else {

		}
		if(stereoScreen[0] != null)
			{
				stereoScreen[0].DiscardContents();
			}
			if (stereoScreen[1] != null)
			{
				stereoScreen[1].DiscardContents();
			}
    }

}

}
