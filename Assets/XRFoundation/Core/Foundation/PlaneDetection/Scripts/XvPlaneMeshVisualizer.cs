using System.Collections.Generic;
using UnityEngine;
namespace Singray.Foundation
{
    /// <summary>
    /// Renders plane data
    /// </summary>
    public sealed class XvPlaneMeshVisualizer : MonoBehaviour
    {

        private XvPlaneMeshVisualizer() { }

        private List<GameObject> planeList = new List<GameObject>();
        private List<GameObject> PlaneList { 
        get { return planeList; }
        }

        [SerializeField]
        private XvPlaneManager xvPlaneManager;
        [SerializeField]
        private Material planeMat;

        [SerializeField]
        private bool enableCollider=true;

        public bool EnableCollider { 
        get { return enableCollider; } 
        }

        [SerializeField]
        private bool enableRender = true;

        public bool EnableRender { 
        get { return enableRender; }
        }

        [SerializeField]
        private bool autoDetection=true;


        public bool AutoDetection
        {
            get { return autoDetection; }
        }
        private void Awake()
        {
            if (xvPlaneManager == null)
            {
                xvPlaneManager = FindAnyObjectByType<XvPlaneManager>();
            }

            if (xvPlaneManager == null)
            {
                xvPlaneManager = new GameObject("XvPlaneManager").AddComponent<XvPlaneManager>();

            }
        }

        private void OnEnable()
        {
            if (xvPlaneManager != null)
            {

                xvPlaneManager.planesChanged += PlanesChanged;

                if (autoDetection) { 
                
                xvPlaneManager.StartPlaneDetction();
                }

            }
        }

        private void OnDisable()
        {
            if (xvPlaneManager != null)
            {
                xvPlaneManager.planesChanged -= PlanesChanged;
                if (autoDetection)
                {
                    xvPlaneManager.StopPlaneDetection();
                }
            }
        }
        private void OnDestroy()
        {
            for (int i = 0; i < planeList.Count; i++)
            {
                Destroy(planeList[i]);
            }

            if (xvPlaneManager.IsDetecting) {

                xvPlaneManager.StopPlaneDetection();
            }
        }


        private void PlanesChanged(plane[] planes)
        {
            for (int i = 0; i < planeList.Count; i++)
            {
                Destroy(planeList[i]);
            }
            planeList = new List<GameObject>();

            //Debug.LogError("planes === " + planes.Length);

            for (int i = 0; i < planes.Length; i++)
            {
                Vector3[] v = new Vector3[planes[i].points.Count];

                for (int f = 0; f < planes[i].points.Count; f++)
                {
                    v[f] = new Vector3((float)planes[i].points[f].x, -(float)planes[i].points[f].y, (float)planes[i].points[f].z);
                }

                DoCreatPloygonMesh(v);
            }
        }

        /// <summary>
        /// Generate a custom polygon
        /// </summary>
        /// <param name="s_Vertives">Custom vertex array</param>
        private void DoCreatPloygonMesh(Vector3[] s_Vertives)
        {
            //Create an empty GameObject to render the custom polygon
            GameObject tPolygon = new GameObject("tPolygon");

            //The two components required for rendering
            tPolygon.AddComponent<MeshFilter>();
            tPolygon.AddComponent<MeshRenderer>();

            //Create a new mesh
            Mesh tMesh = new Mesh();

            //Store all vertices
            Vector3[] tVertices = s_Vertives;

            //Store triangle vertex indices
            List<int> tTriangles = new List<int>();

            //Populate triangle indices from the vertices
            for (int i = 0; i < tVertices.Length - 1; i++)
            {
                tTriangles.Add(i);
                tTriangles.Add(i + 1);
                tTriangles.Add(tVertices.Length - i - 1);
            }

            //Assign polygon vertices
            tMesh.vertices = tVertices;

            //Assign triangle indices
            tMesh.triangles = tTriangles.ToArray();

            //Recalculate UVs and normals
            tMesh.RecalculateBounds();
            tMesh.RecalculateNormals();

            //Assign the generated mesh
            tPolygon.GetComponent<MeshFilter>().mesh = tMesh;
            tPolygon.GetComponent<Renderer>().material = planeMat;
            Collider collider= tPolygon.AddComponent<MeshCollider>();
            collider.enabled = enableCollider;

            tPolygon.GetComponent<MeshRenderer>().enabled = enableRender;
            planeList.Add(tPolygon);
        }


        public void SetCollider(bool enable)
        {
            enableCollider = enable;
            foreach (var item in planeList)
            {
                item.GetComponent<Collider>().enabled = enableCollider;
            }
        }

        public void SetVisualizer(bool enable)
        {
            this.enableRender = enable;
            foreach (var item in planeList)
            {
                item.GetComponent<MeshRenderer>().enabled = enableRender;
            }
        }

        
    }
}