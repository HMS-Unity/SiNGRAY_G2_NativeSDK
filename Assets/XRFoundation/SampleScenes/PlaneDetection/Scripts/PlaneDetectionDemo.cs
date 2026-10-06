
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    public class PlaneDetectionDemo : MonoBehaviour
    {
        //[SerializeField]
        private XvPlaneManager xvPlaneManager;
        //[SerializeField]
        private XvPlaneMeshVisualizer xvPlaneMeshVisualizer;

        private TextMeshPro meshText;
        private TextMeshPro colliderText;
        private TextMeshPro meshrenderText;



        private void Awake()
        {
            if (xvPlaneManager == null)
            {
                xvPlaneManager = FindObjectOfType<XvPlaneManager>();

                if (xvPlaneManager==null) {
                    GameObject newObj = Instantiate(Resources.Load<GameObject>("XvPlaneManager"));

                    newObj.name = "XvPlaneManager";
                    xvPlaneManager = newObj.GetComponent<XvPlaneManager>();
                }
            }

            if (xvPlaneMeshVisualizer == null)
            {
                xvPlaneMeshVisualizer = FindObjectOfType<XvPlaneMeshVisualizer>();
            }

            meshText = transform.Find("UI/Canvas/PlaneDetection/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();
            colliderText = transform.Find("UI/Canvas/MeshCollider/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();
            meshrenderText = transform.Find("UI/Canvas/MeshRender/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();

        }

        private void Start()
        {
            if (xvPlaneManager.IsDetecting)
            {
                meshText.text = "StopDetction";
            }
            else
            {
                meshText.text = "StartDetction";
            }

            if (xvPlaneMeshVisualizer.EnableCollider)
            {

                colliderText.text = "DisableCollider";
            }
            else
            {
                colliderText.text = "EnableCollider";
            }

            if (xvPlaneMeshVisualizer.EnableRender)
            {

                meshrenderText.text = "DisableRender";
            }
            else
            {
                meshrenderText.text = "EnableRender";

            }
        }

        private void Update()
        {

            AddArrow();
            AddLine();

        }
        public void SetPlaneDetection() {
            if (xvPlaneManager.IsDetecting)
            {
                xvPlaneManager.StopPlaneDetection();

                meshText.text = "StartDetction";
            }
            else { 
            xvPlaneManager.StartPlaneDetction();
                meshText.text = "StopDetction";

            }

        }
       

       
        public void SetCollider()
        {
            if (xvPlaneMeshVisualizer.EnableCollider)
            {

                xvPlaneMeshVisualizer.SetCollider(false);
                colliderText.text = "EnableCollider";
            }
            else { 
                xvPlaneMeshVisualizer.SetCollider(true);
                colliderText.text = "DisableCollider";


            }
        }

        public void SetVisualizer()
        {
            if (xvPlaneMeshVisualizer.EnableRender)
            {

                xvPlaneMeshVisualizer.SetVisualizer(false);
                meshrenderText.text = "EnableRender";
            }
            else
            {
                xvPlaneMeshVisualizer.SetVisualizer(true);
                meshrenderText.text = "DisableRender";
            }
          
        }

        public Transform arrow;
        public Transform line;

        private List<Transform> arrowList = new List<Transform>();
        private List<Transform> lineList = new List<Transform>();

        private LineRenderer leftLineRenderer;
        private LineRenderer rightLineRenderer;
        private int markType=-1;


        public void btClick(GameObject bt)
        {
         
            switch (bt.name)
            {
                case "AddArrow":
                    markType = 0;
                    break;
                case "ClearArrow":
                    ClearArrow();
                    break;
                case "AddLine":
                    markType = 1;
                    break;
                case "ClearLine":
                    ClearLine();
                    break;
                default:
                    break;
            }

        }


        private void AddArrow()
        {
            if (markType != 0)
            {
                return;
            }

            if (HandInputManager.Instance.GetKeyDown(UnityEngine.XR.XRNode.LeftHand))
            {
                if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Left, out Ray ray))
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 10))
                    {
                        if (hit.transform.GetComponentInParent<Canvas>())
                        {
                            return;
                        }

                        Transform newArrow = Instantiate(arrow);
                        newArrow.position = hit.point;
                        newArrow.rotation = Quaternion.LookRotation(ray.direction);
                        arrowList.Add(newArrow);
                    }
                }
            }

            if (HandInputManager.Instance.GetKeyDown(UnityEngine.XR.XRNode.RightHand))
            {
                if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Right, out Ray ray))
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 10))
                    {
                        if (hit.transform.GetComponentInParent<Canvas>())
                        {
                            return;
                        }
                        Transform newArrow = Instantiate(arrow);
                        newArrow.position = hit.point;
                        newArrow.rotation = Quaternion.LookRotation(ray.direction);
                        arrowList.Add(newArrow);
                    }
                }
            }
        }
        private void ClearArrow()
        {
            while (arrowList.Count > 0)
            {

                Destroy(arrowList[0].gameObject);
                arrowList.RemoveAt(0);

            }
        }

        private void AddLine()
        {
            if (markType != 1)
            {
                return;
            }
            if (HandInputManager.Instance.GetKeyDown(UnityEngine.XR.XRNode.LeftHand))
            {

                if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Left, out Ray ray))
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 10))
                    {
                        if (hit.transform.GetComponentInParent<Canvas>())
                        {
                            return;
                        }
                        Transform newArrow = Instantiate(line);
                        leftLineRenderer = newArrow.GetComponent<LineRenderer>();
                        leftLineRenderer.positionCount = 1;
                        leftLineRenderer.SetPosition(leftLineRenderer.positionCount - 1, hit.point);
                        lineList.Add(newArrow);
                    }
                }
            }
            else
            {
                if (HandInputManager.Instance.GetKey(UnityEngine.XR.XRNode.LeftHand))
                {
                    if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Left, out Ray ray))
                    {
                        if (Physics.Raycast(ray, out RaycastHit hit, 10))
                        {
                            if (leftLineRenderer != null)
                            {
                                leftLineRenderer.positionCount += 1;
                                leftLineRenderer.SetPosition(leftLineRenderer.positionCount - 1, hit.point);
                            }

                        }
                    }
                }

                if (HandInputManager.Instance.GetKeyUp(UnityEngine.XR.XRNode.LeftHand))
                {
                    leftLineRenderer = null;
                }
            }



            if (HandInputManager.Instance.GetKeyDown(UnityEngine.XR.XRNode.RightHand))
            {
                if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Right, out Ray ray))
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 10))
                    {
                        if (hit.transform.GetComponentInParent<Canvas>())
                        {
                            return;
                        }

                        Transform newArrow = Instantiate(line);

                        rightLineRenderer = newArrow.GetComponent<LineRenderer>();
                        rightLineRenderer.positionCount = 1;
                        rightLineRenderer.SetPosition(rightLineRenderer.positionCount - 1, hit.point);
                        lineList.Add(newArrow);
                    }
                }
            }


            if (HandInputManager.Instance.GetKey(UnityEngine.XR.XRNode.RightHand))
            {
                if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Right, out Ray ray))
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 10))
                    {
                        if (rightLineRenderer != null)
                        {
                            rightLineRenderer.positionCount += 1;
                            rightLineRenderer.SetPosition(rightLineRenderer.positionCount - 1, hit.point);
                        }


                    }
                }
            }

            if (HandInputManager.Instance.GetKeyUp(UnityEngine.XR.XRNode.RightHand))
            {
                rightLineRenderer = null;
            }
        }

        private void ClearLine()
        {
            while (lineList.Count > 0)
            {
                Destroy(lineList[0].gameObject);
                lineList.RemoveAt(0);

            }
        }

    }
}


