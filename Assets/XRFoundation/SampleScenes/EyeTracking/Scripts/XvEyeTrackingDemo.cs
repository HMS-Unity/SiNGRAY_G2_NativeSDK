using System.Collections.Generic;
using UnityEngine;
namespace Singray.Foundation.SampleScenes
{
    /// <summary>
    /// Demonstrates eye tracking
    /// </summary>
    public class XvEyeTrackingDemo : MonoBehaviour
    {
        public bool is0dofMode = false;

        //[SerializeField]
        private XvEyeTrackingManager xvEyeTrackingManager;

        public List<Transform> transformList=new List<Transform>();

        private Transform lastGaze;
        private float initScale = 0.6f;
        public Transform point;
       
       // public TextMesh eyeDataText;

        private void Awake()
        {
            if (xvEyeTrackingManager==null) {
                xvEyeTrackingManager=FindObjectOfType<XvEyeTrackingManager>();

                if (xvEyeTrackingManager==null) {
                    xvEyeTrackingManager=new GameObject("XvEyeTrackingManager").AddComponent<XvEyeTrackingManager>();
                }
            }
        }
        private void OnEnable()
        {
            xvEyeTrackingManager.StartGaze();
        }

        private void OnDisable()
        {
            xvEyeTrackingManager.StopGaze();
        }

        private void Update()
        {
            if (!xvEyeTrackingManager.Tracking)
            {
                return;
            }

            #region
            //if (eyeDataText != null)
            //{
            //    eyeDataText.text =
            //        $"timestamp:{xvEyeTrackingManager.EyeData.timestamp}\n" +
            //        $"recomGaze gazePoint:{xvEyeTrackingManager.EyeData.recomGaze.gazePoint.x / 1000},{-xvEyeTrackingManager.EyeData.recomGaze.gazePoint.y / 1000},{xvEyeTrackingManager.EyeData.recomGaze.gazePoint.z / 1000}\n" +
            //        $"recomGaze gazeOrigin:{xvEyeTrackingManager.EyeData.recomGaze.gazeOrigin.x / 1000},{-xvEyeTrackingManager.EyeData.recomGaze.gazeOrigin.y / 1000},{xvEyeTrackingManager.EyeData.recomGaze.gazeOrigin.z / 1000}\n" +
            //        $"recomGaze gazeDirection:{xvEyeTrackingManager.EyeData.recomGaze.gazeDirection.x},{-xvEyeTrackingManager.EyeData.recomGaze.gazeDirection.y},{xvEyeTrackingManager.EyeData.recomGaze.gazeDirection.z}\n" +
            //        $"recomGaze re:{xvEyeTrackingManager.EyeData.recomGaze.re}\n" +
            //        $"leftGaze gazePoint:{xvEyeTrackingManager.EyeData.leftGaze.gazePoint.x / 1000},{-xvEyeTrackingManager.EyeData.leftGaze.gazePoint.y / 1000},{xvEyeTrackingManager.EyeData.leftGaze.gazePoint.z / 1000}\n" +
            //        $"leftGaze gazeOrigin:{xvEyeTrackingManager.EyeData.leftGaze.gazeOrigin.x / 1000},{-xvEyeTrackingManager.EyeData.leftGaze.gazeOrigin.y / 1000},{xvEyeTrackingManager.EyeData.leftGaze.gazeOrigin.z / 1000}\n" +
            //        $"leftGaze gazeDirection:{xvEyeTrackingManager.EyeData.leftGaze.gazeDirection.x},{-xvEyeTrackingManager.EyeData.leftGaze.gazeDirection.y},{xvEyeTrackingManager.EyeData.leftGaze.gazeDirection.z}\n" +
            //        $"leftGaze re:{xvEyeTrackingManager.EyeData.leftGaze.re}\n" +
            //        $"rightGaze gazePoint:{xvEyeTrackingManager.EyeData.rightGaze.gazePoint.x / 1000},{-xvEyeTrackingManager.EyeData.rightGaze.gazePoint.y / 1000},{xvEyeTrackingManager.EyeData.rightGaze.gazePoint.z / 1000}\n" +
            //        $"rightGaze gazeOrigin:{xvEyeTrackingManager.EyeData.rightGaze.gazeOrigin.x / 1000},{-xvEyeTrackingManager.EyeData.rightGaze.gazeOrigin.y / 1000},{xvEyeTrackingManager.EyeData.rightGaze.gazeOrigin.z / 1000}\n" +
            //        $"rightGaze gazeDirection:{xvEyeTrackingManager.EyeData.rightGaze.gazeDirection.x},{-xvEyeTrackingManager.EyeData.rightGaze.gazeDirection.y},{xvEyeTrackingManager.EyeData.rightGaze.gazeDirection.z}\n" +
            //        $"rightGaze re:{xvEyeTrackingManager.EyeData.rightGaze.re}\n" +
            //        $"leftPupil pupilCenter:{xvEyeTrackingManager.EyeData.leftPupil.pupilCenter.x},{xvEyeTrackingManager.EyeData.leftPupil.pupilCenter.y}\n" +
            //        $"rightPupil pupilCenter:{xvEyeTrackingManager.EyeData.rightPupil.pupilCenter.x},{xvEyeTrackingManager.EyeData.rightPupil.pupilCenter.y}\n" +
            //        $"ipd:{xvEyeTrackingManager.EyeData.ipd}\n" +
            //        $"leftEyeMove:{xvEyeTrackingManager.EyeData.leftEyeMove}\n" +
            //        $"rightEyeMove:{xvEyeTrackingManager.EyeData.rightEyeMove}";

            //      MyDebugTool.Log($"XvFeatureGetGazeCallback:" + eyeDataText.text);
            //}
            #endregion

            if (point != null)
            {

#if UNITY_EDITOR
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#else
                Ray ray = new Ray(xvEyeTrackingManager.GazeOrigin, xvEyeTrackingManager.GazeDirection);
#endif


                if (Physics.Raycast(ray, out RaycastHit raycastHit))
                {
                    ScaleCube(raycastHit.transform);

                    point.position = raycastHit.point;
                }
                else
                {

                    MyDebugTool.Log(xvEyeTrackingManager.GazeOrigin+"     "+Camera.main.transform.position);
#if UNITY_EDITOR
                  
#else
                 point.position = xvEyeTrackingManager.GazeOrigin + (xvEyeTrackingManager.GazeDirection.normalized) * 10;
#endif


                }
            }

            if (is0dofMode)
            {
                transform.position = Camera.main.transform.position;
                transform.rotation = Camera.main.transform.rotation;
            }
            
        }

        private void ScaleCube(Transform tran) {
            for (int i = 0; i < transformList.Count; i++)
            {

                if (transformList[i]== tran) {


                    if (lastGaze!=null) {
                        lastGaze.localScale = Vector3.one * initScale;
                    }

                    lastGaze = tran;
                    tran.localScale = Vector3.one * (initScale) * 1.2f;
                }

            }
        }
    }
}
