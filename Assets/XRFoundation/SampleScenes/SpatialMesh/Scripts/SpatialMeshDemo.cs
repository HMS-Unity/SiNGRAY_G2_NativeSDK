
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using TMPro;
using UnityEngine;


namespace Singray.Foundation.SampleScenes
{
    public class SpatialMeshDemo : MonoBehaviour
    {
        // [SerializeField]
        private XvSpatialMeshManager xvSpatialMeshManager;
        //[SerializeField]
        private XvSpatialMeshVisualizer xvSpatialMeshVisualizer;

        private TextMeshPro meshText;
        private TextMeshPro colliderText;
        private TextMeshPro meshrenderText;

        private void Awake()
        {
            if (xvSpatialMeshManager == null)
            {
                xvSpatialMeshManager = FindObjectOfType<XvSpatialMeshManager>();

                if (xvSpatialMeshManager == null)
                {
                    GameObject newObj = Instantiate(Resources.Load<GameObject>("XvSpatialMeshManager"));

                    newObj.name = "XvSpatialMeshManager";
                    xvSpatialMeshManager = newObj.GetComponent<XvSpatialMeshManager>();
                }


            }

            if (xvSpatialMeshVisualizer == null)
            {
                xvSpatialMeshVisualizer = FindObjectOfType<XvSpatialMeshVisualizer>();
            }

            meshText = transform.Find("UI/Canvas/MeshDetection/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();
            colliderText = transform.Find("UI/Canvas/MeshCollider/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();
            meshrenderText = transform.Find("UI/Canvas/MeshRender/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();

        }

        private void Start()
        {
            if (xvSpatialMeshManager.IsDetecting)
            {
                meshText.text = "StopDetction";
            }
            else
            {
                meshText.text = "StartDetction";
            }

            if (xvSpatialMeshVisualizer.EnableCollider)
            {

                colliderText.text = "DisableCollider";
            }
            else
            {
                colliderText.text = "EnableCollider";
            }

            if (xvSpatialMeshVisualizer.EnableRender)
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

        public void SetMeshDetection()
        {
            if (xvSpatialMeshManager.IsDetecting)
            {
                xvSpatialMeshManager.StopMeshDetection();

                meshText.text = "StartDetection";
            }
            else
            {
                xvSpatialMeshManager.StartMeshDetection();
                meshText.text = "StopDetection";

            }

        }



        public void SetCollider()
        {
            if (xvSpatialMeshVisualizer.EnableCollider)
            {

                xvSpatialMeshVisualizer.SetCollider(false);
                colliderText.text = "EnableCollider";
            }
            else
            {
                xvSpatialMeshVisualizer.SetCollider(true);
                colliderText.text = "DisableCollider";


            }
        }

        public void SetVisualizer()
        {
            if (xvSpatialMeshVisualizer.EnableRender)
            {

                xvSpatialMeshVisualizer.SetVisualizer(false);
                meshrenderText.text = "EnableRender";
            }
            else
            {
                xvSpatialMeshVisualizer.SetVisualizer(true);
                meshrenderText.text = "DisableRender";
            }

        }

        public Transform arrow;
        public Transform line;

        private List<Transform> arrowList=new List<Transform>();
        private List<Transform> lineList = new List<Transform>();
        
        private LineRenderer leftLineRenderer;
        private LineRenderer rightLineRenderer;
        private int markType=-1;


        public void  btClick(GameObject bt) {
          
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


        private void AddArrow() {
            if (markType!=0) {
                return;
            }

            if (HandInputManager.Instance.GetKeyUp(UnityEngine.XR.XRNode.LeftHand)) {
                if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Left, out Ray ray))
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 10))
                    {
                        if (hit.transform.GetComponentInParent<Canvas>()) {
                            return;
                        }

                        Transform newArrow=  Instantiate(arrow);
                        newArrow.position = hit.point;
                        newArrow.rotation = Quaternion.LookRotation(ray.direction);
                        arrowList.Add(newArrow);
                    }
                }
            }

            if (HandInputManager.Instance.GetKeyUp(UnityEngine.XR.XRNode.RightHand))
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
        private void ClearArrow() { 
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
                        leftLineRenderer.SetPosition(leftLineRenderer.positionCount - 1 , hit.point);
                        lineList.Add(newArrow);
                    }
                }
            }
            else {
                if (HandInputManager.Instance.GetKey(UnityEngine.XR.XRNode.LeftHand))
                {
                    if (InputRayUtils.TryGetRay(InputSourceType.Hand, Handedness.Left, out Ray ray))
                    {
                        if (Physics.Raycast(ray, out RaycastHit hit, 10))
                        {
                            if (leftLineRenderer!=null) {
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
                        if (rightLineRenderer!=null) {
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

        private void ClearLine() {
           while (lineList.Count > 0)
            {
                Destroy(lineList[0].gameObject);
                lineList.RemoveAt(0);

            }
        }

    }
}
