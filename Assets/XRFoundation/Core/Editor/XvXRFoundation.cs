using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Singray.UI.Input;
using Singray.Engine;

namespace Singray.Foundation
{
    public class XvXRFoundation : MonoBehaviour
    {
        [MenuItem("GameObject/Singray XR/XvFoundation/XvCameraManager", false, 0)]
        static void CreateXvCameraManager()
        {

            XvCameraManager xvCameraManager = FindObjectOfType<XvCameraManager>();

            if (xvCameraManager == null)
            {
                xvCameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();

                Undo.RegisterCreatedObjectUndo(xvCameraManager.gameObject, xvCameraManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvCameraManager already exists in the scene");
            }
            Selection.activeObject = xvCameraManager.gameObject;


        }




        [MenuItem("GameObject/Singray XR/XvFoundation/XvMRVideoCaptureManager", false, 1)]
        static void CreateXvMRVideoCaptureManager()
        {
            CreateXvCameraManager();

            XvMRVideoCaptureManager xvMRVideoCaptureManager = FindObjectOfType<XvMRVideoCaptureManager>();

            if (xvMRVideoCaptureManager == null)
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvMRVideoCaptureManager"));

                newObj.name = "XvMRVideoCaptureManager";
                xvMRVideoCaptureManager = newObj.GetComponent<XvMRVideoCaptureManager>();

                Undo.RegisterCreatedObjectUndo(newObj.gameObject, newObj.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvMRVideoCaptureManager already exists in the scene");
            }
            Selection.activeObject = xvMRVideoCaptureManager.gameObject;

        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvMediaRecorderManager", false, 2)]

        static void CreateXvMediaRecorderManager()
        {
            CreateXvMRVideoCaptureManager();
            XvMediaRecorderManager xvMediaRecorderManager = FindObjectOfType<XvMediaRecorderManager>();

            if (xvMediaRecorderManager == null)
            {
                xvMediaRecorderManager = new GameObject("XvMediaRecorderManager").AddComponent<XvMediaRecorderManager>();

                Undo.RegisterCreatedObjectUndo(xvMediaRecorderManager.gameObject, xvMediaRecorderManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvMediaRecorderManager already exists in the scene");
            }

            Selection.activeObject = xvMediaRecorderManager.gameObject;
        }

        [MenuItem("GameObject/Singray XR/XvFoundation/XvMediaRecorder", false, 3)]

        static void CreateXvMediaRecorder()
        {
            CreateXvMediaRecorderManager();
            XvMediaRecorder xvMediaRecorder = FindObjectOfType<XvMediaRecorder>();

            if (xvMediaRecorder == null)
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvMediaRecorder"));

                newObj.name = "XvMediaRecorder";
                xvMediaRecorder = newObj.GetComponent<XvMediaRecorder>();

                Undo.RegisterCreatedObjectUndo(newObj.gameObject, newObj.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvMediaRecorder already exists in the scene");
            }

            Selection.activeObject = xvMediaRecorder.gameObject;

        }

        [MenuItem("GameObject/Singray XR/XvFoundation/XvTagRecognizerManager", false, 4)]

        static void CreateXvTagRecognizerManager()
        {

            XvTagRecognizerManager xvAprilTagManager = FindObjectOfType<XvTagRecognizerManager>();

            if (xvAprilTagManager == null)
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvTagRecognizerManager"));

                newObj.name = "XvTagRecognizerManager";
                xvAprilTagManager = newObj.GetComponent<XvTagRecognizerManager>();

                Undo.RegisterCreatedObjectUndo(newObj.gameObject, newObj.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvTagRecognizerManager already exists in the scene");
            }

            Selection.activeObject = xvAprilTagManager.gameObject;
        }

        [MenuItem("GameObject/Singray XR/XvFoundation/XvTagRecognizerBehavior", false, 4)]

        static void CreateXvTagRecognizerBehavior()
        {
            XvTagRecognizerManager xvAprilTagManager = FindObjectOfType<XvTagRecognizerManager>();
            if (xvAprilTagManager == null)
            {
                CreateXvTagRecognizerManager();

            }

            xvAprilTagManager = FindObjectOfType<XvTagRecognizerManager>();

            GameObject newObj = Instantiate(Resources.Load<GameObject>("XvTagRecognizerBehavior"));
            newObj.name = "XvTagRecognizerBehavior";
            Undo.RegisterCreatedObjectUndo(newObj.gameObject, newObj.gameObject.name);
            newObj.transform.SetParent(xvAprilTagManager.transform);
            Selection.activeObject = newObj;
        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvRgbdManager", false, 5)]

        static void CreateXvRgbdManager()
        {

            XvRgbdManager xvRgbdManager = FindObjectOfType<XvRgbdManager>();

            if (xvRgbdManager == null)
            {
                xvRgbdManager = new GameObject("XvRgbdManager").AddComponent<XvRgbdManager>();

                Undo.RegisterCreatedObjectUndo(xvRgbdManager.gameObject, xvRgbdManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvRgbdManager already exists in the scene");
            }

            Selection.activeObject = xvRgbdManager.gameObject;

        }

        [MenuItem("GameObject/Singray XR/XvFoundation/XvRTSPStreamerManager", false,6)]

        static void CreateXvRTSPStreamerManager()
        {

            CreateXvMRVideoCaptureManager();
            XvRTSPStreamerManager xvRTSPStreamerManager = FindObjectOfType<XvRTSPStreamerManager>();

            if (xvRTSPStreamerManager == null)
            {
                xvRTSPStreamerManager = new GameObject("XvRTSPStreamerManager").AddComponent<XvRTSPStreamerManager>();

                Undo.RegisterCreatedObjectUndo(xvRTSPStreamerManager.gameObject, xvRTSPStreamerManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvRTSPStreamerManager already exists in the scene");
            }

            Selection.activeObject = xvRTSPStreamerManager.gameObject;

        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvPlaneManager", false, 7)]

        static void CreateXvPlaneManager()
        {
            XvPlaneManager xvPlaneManager = FindObjectOfType<XvPlaneManager>();

            if (xvPlaneManager == null)
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvPlaneManager"));

                newObj.name = "XvPlaneManager";
                xvPlaneManager = newObj.GetComponent<XvPlaneManager>();

                Undo.RegisterCreatedObjectUndo(newObj, newObj.name);

            }
            else
            {
                Debug.LogWarning("XvPlaneManager already exists in the scene");
            }

            Selection.activeObject = xvPlaneManager.gameObject;

        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvSpatialMapManager", false, 8)]

        static void CreateXvSpatialMapManager()
        {
            XvSpatialMapManager xvSpatialMapManager = FindObjectOfType<XvSpatialMapManager>();

         

            if (xvSpatialMapManager == null)
            {
                xvSpatialMapManager = new GameObject("XvSpatialMapManager").AddComponent<XvSpatialMapManager>();

                Undo.RegisterCreatedObjectUndo(xvSpatialMapManager.gameObject, xvSpatialMapManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvRTSPStreamerManager already exists in the scene");
            }

            Selection.activeObject = xvSpatialMapManager.gameObject;

        }

        [MenuItem("GameObject/Singray XR/XvFoundation/XvSpatialMeshManager", false, 9)]

        static void CreateXvSpatialMeshManager()
        {
            XvSpatialMeshManager xvSpatialMeshManager = FindObjectOfType<XvSpatialMeshManager>();

            if (xvSpatialMeshManager == null)
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvSpatialMeshManager"));

                newObj.name = "XvSpatialMeshManager";
                xvSpatialMeshManager = newObj.GetComponent<XvSpatialMeshManager>();

                Undo.RegisterCreatedObjectUndo(newObj, newObj.name);

            }
            else
            {
                Debug.LogWarning("XvPlaneManager already exists in the scene");
            }

            Selection.activeObject = xvSpatialMeshManager.gameObject;

        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvSpeechVoiceManager", false, 10)]

        static void CreateXvSpeechVoiceManager()
        {
            XvSpeechVoiceManager xvSpeechVoiceManager = FindObjectOfType<XvSpeechVoiceManager>();



            if (xvSpeechVoiceManager == null)
            {
                xvSpeechVoiceManager = new GameObject("XvSpeechVoiceManager").AddComponent<XvSpeechVoiceManager>();

                Undo.RegisterCreatedObjectUndo(xvSpeechVoiceManager.gameObject, xvSpeechVoiceManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvRTSPStreamerManager already exists in the scene");
            }

            Selection.activeObject = xvSpeechVoiceManager.gameObject;

        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvEyeTrackingManager", false, 11)]

        static void CreateXvEyeTrackingManager()
        {
            XvEyeTrackingManager xvEyeTrackingManager = FindObjectOfType<XvEyeTrackingManager>();



            if (xvEyeTrackingManager == null)
            {
                xvEyeTrackingManager = new GameObject("XvEyeTrackingManager").AddComponent<XvEyeTrackingManager>();

                Undo.RegisterCreatedObjectUndo(xvEyeTrackingManager.gameObject, xvEyeTrackingManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvEyeTrackingManager already exists in the scene");
            }

            Selection.activeObject = xvEyeTrackingManager.gameObject;

        }


        [MenuItem("GameObject/Singray XR/XvFoundation/XvStaticGestureManager", false, 0)]
        static void CreateXvStaticGestureManager()
        {

            XvStaticGestureManager xvStaticGestureManager = FindObjectOfType<XvStaticGestureManager>();

            if (xvStaticGestureManager == null)
            {
                xvStaticGestureManager = new GameObject("XvStaticGestureManager").AddComponent<XvStaticGestureManager>();

                Undo.RegisterCreatedObjectUndo(xvStaticGestureManager.gameObject, xvStaticGestureManager.gameObject.name);

            }
            else
            {
                Debug.LogWarning("XvStaticGestureManager already exists in the scene");
            }
            Selection.activeObject = xvStaticGestureManager.gameObject;


        }

        [MenuItem("GameObject/Singray XR/XvFoundation/XvJoystickManager", false, 0)]
        static void CreateXvJoystickManager()
        {

            XvJoystickManager xvJoystickManager = FindObjectOfType<XvJoystickManager>();

            if (xvJoystickManager == null)
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvJoystickManager"));

                newObj.name = "XvJoystickManager";
                xvJoystickManager = newObj.GetComponent<XvJoystickManager>();

                Undo.RegisterCreatedObjectUndo(newObj, newObj.name);

            }
            else
            {
                Debug.LogWarning("XvJoystickManager already exists in the scene");
            }

            Selection.activeObject = xvJoystickManager.gameObject;


        }
        

        [MenuItem("GameObject/Singray XR/XvFoundation/XvHeadGazeInputController", false, 12)]

        static void CreateXvHeadGazeInputController()
        {
          GameObject head=  GameObject.Find("XvXRManager/Head");
            if (head != null)
            {
                XvHeadGazeInputController xvHeadGazeInputController = FindObjectOfType<XvHeadGazeInputController>();

                if (xvHeadGazeInputController == null)
                {
                    xvHeadGazeInputController = Instantiate(Resources.Load<GameObject>("XvHeadGazeInputController")).GetComponent<XvHeadGazeInputController>();
                    xvHeadGazeInputController.gameObject.name = "XvHeadGazeInputController";
                    Undo.RegisterCreatedObjectUndo(xvHeadGazeInputController.gameObject, xvHeadGazeInputController.gameObject.name);
                }
                else
                {
                    Debug.LogWarning("XvHeadGazeInputController already exists in the scene");
                }

                Selection.activeObject = xvHeadGazeInputController.gameObject;

                xvHeadGazeInputController.transform.parent = head.transform;
                xvHeadGazeInputController.transform.localPosition = Vector3.zero;
                xvHeadGazeInputController.transform.localRotation = Quaternion.identity;

              GameObject XvXRCamera=  GameObject.Find("XvXRManager/Head/XvXRCamera");

                if (!XvXRCamera.GetComponent<XvXRInputModule>()) {
                    MixedRealityInputModule mixedRealityInputModule = XvXRCamera.GetComponent<MixedRealityInputModule>();

                    if (mixedRealityInputModule != null)
                    {

                        DestroyImmediate(mixedRealityInputModule);
                    }
                    XvXRCamera.AddComponent<XvXRInputModule>();
                }
            }
            else {
                MyDebugTool.LogError("Please first build the basic scene");
            }




        }


        

        [MenuItem("Singray/Toolkit/Add To Scene and Configure", false, 98)]

        static void ConfigProject()
        {
            

            if (!FindObjectOfType<MixedRealityToolkit>())
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("MixedRealityToolkit"));

                newObj.name = "MixedRealityToolkit";
                Undo.RegisterCreatedObjectUndo(newObj, newObj.name);

            }

            if (!FindObjectOfType<XvXRInput>())
            {

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvXRInput"));


                
                newObj.name = "XvXRInput";
                Undo.RegisterCreatedObjectUndo(newObj, newObj.name);
            }

            if (!FindObjectOfType<XvXRManager>())
            {

                if (FindObjectOfType<Camera>())
                {
                    DestroyImmediate(FindObjectOfType<Camera>().gameObject);
                }
                if (FindObjectOfType<EventSystem>())
                {

                    DestroyImmediate(FindObjectOfType<EventSystem>().gameObject);
                }

                GameObject newObj = Instantiate(Resources.Load<GameObject>("XvXRManager"));

                newObj.name = "XvXRManager";
                Undo.RegisterCreatedObjectUndo(newObj, newObj.name);

                newObj.transform.Find("Head/XvXRCamera").GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;

            }
            // Debug.Log("");

        }


        [MenuItem("GameObject/Singray XR/Add To Scene and Configure", false, 98)]

        static void ConfigProjectA()
        {
            ConfigProject();
        }
    }
}
