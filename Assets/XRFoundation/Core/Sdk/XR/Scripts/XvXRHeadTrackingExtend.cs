using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Singray.Engine;
using Singray.SystemEvents;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Mouse Look")]
public class XvXRHeadTrackingExtend : MonoBehaviour {

	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 1F;
	public float sensitivityY = 1F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -45F;
	public float maximumY = 45F;

	float rotationY = 0F;

	//bool isEnable = false;
	float currentTouchTime = 0;
	bool startTouch = false;
	bool isFirstTouch = true;
	bool isFristLoad = true;
	void Update ()
	{
		
		if (!Singray.Engine.XvXRManager.SDK.IsVRMode && startTouch && Input.GetTouch(0).phase == TouchPhase.Moved)
		{
			Cursor.visible = false;

			currentTouchTime+= Time.deltaTime;
			if (currentTouchTime < 0.1)
			{
				return;
			}
			else
			{
				currentTouchTime = 0;
			}

			if (axes == RotationAxes.MouseXAndY)
			{
				//float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

				//rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
				//rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

				//transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
			}
			else if (axes == RotationAxes.MouseX)
			{
				if (!isFirstTouch)
				{
					float currentX = Input.GetAxis("Mouse X");

					transform.Rotate(0, -currentX * sensitivityX, 0);
				}
				else
				{
					isFirstTouch = false;
				}
					
			
			}
			else
			{
				//rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
				//rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

				//transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
			}

			
		}
		if (Input.GetMouseButtonDown(0))
		{
			startTouch = true;
			isFirstTouch = true;
			currentTouchTime = 0;
			if (XvXRManager.SDK.IsVRMode)
			{
				AndroidConnection.VrShowRecenter();
			}
		}

		if (Input.GetMouseButtonUp(0))
		{
			startTouch = false;
			isFirstTouch = false;
			currentTouchTime = 0;
		}
		if (isFristLoad)
		{
			isFristLoad = false;
			SDK_OnVrModeChangeDelegate(Singray.Engine.XvXRManager.SDK.IsVRMode);
		}

	}
	void Start ()
	{
		// Make the rigid body not change rotation
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
		Singray.Engine.XvXRManager.SDK.ReCenterOnClick += SDK_ReCenterOnClick;
		Singray.Engine.XvXRManager.SDK.OnVrModeChangeDelegate += SDK_OnVrModeChangeDelegate;

		Singray.Engine.XvXRManager.SDK.ReCenter();

	}

	private void SDK_OnVrModeChangeDelegate(bool isVrMode)
	{
		
		if (isVrMode)
		{
			transform.localPosition = new Vector3(0, 0, 0);
		}
		else
		{
			transform.localRotation = new Quaternion(0, 0, 0, 1);
		}
	}

	private void SDK_ReCenterOnClick()
	{
		if (Singray.Engine.XvXRManager.SDK.IsVRMode)
		{
			Singray.Engine.XvXRManager.SDK.ReCenter();
		}
		else
		{
			transform.localRotation = new Quaternion(0, 0, 0, 1);
			rotationY = 0f;
		}
		
	}
}