using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Threading.Tasks;


namespace Singray.Foundation.SampleScenes
{
    public class XvParticlesCloudPoint : MonoBehaviour
    {
        public ParticleSystem ps;
        ParticleSystem.Particle[] allParticles;
        private static string _saveFolderPath;
        List<Vector3> FilterVec = new List<Vector3>();
        List<Vector3> t_FilterVec = new List<Vector3>();

        Matrix4x4 P_tofpoint_world = Matrix4x4.identity;

        Matrix4x4 P_tofpoint_tofcam = Matrix4x4.identity;//Original ToF point cloud
        Matrix4x4 P_tofcam_glassImu = Matrix4x4.identity;//ToF extrinsics
        Matrix4x4 P_glassImu_world = Matrix4x4.identity;//Glasses 6DoF pose

        public Material[] Mat_alpha;
        public List<GameObject> planeList = new List<GameObject>();



        void Start()
        {
            //InvokeRepeating("savePointCloud",3,1);
        }

        void Update()
        {
            
        }

        // Static state: create the directory only once
        private string _saveFolder = "";
        // Static state: filename for the current second
        private string _currentSecondFile;
        // Static state: current second
        private int _lastSecond = -1;

        private StreamWriter _currentWriter;
        public string folderName = "";

        /// <summary>
        /// Save List<Vector3> data, creating a new file each second
        /// </summary>
        private void SaveArray(List<Vector3> FilterVec)
        {
            // Return immediately for empty data
            if (FilterVec == null || FilterVec.Count == 0)
                return;

            // ==================== Create the root directory on first use ====================
            if (string.IsNullOrEmpty(_saveFolder))
            {
                // Android SD card root directory
                string rootPath = Path.Combine("/sdcard", "CollectData");

                // Create a timestamped subdirectory
                //folderName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                _saveFolder = Path.Combine(rootPath, folderName);

                if (!Directory.Exists(_saveFolder))
                    Directory.CreateDirectory(_saveFolder);
            }

            // ==================== Switch files every second ====================
            DateTime now = DateTime.Now;
            int currentSecond = now.Second;

            if (currentSecond != _lastSecond || string.IsNullOrEmpty(_currentSecondFile))
            {
                // Close the previous second's file stream
                _currentWriter?.Flush();
                _currentWriter?.Close();
                _currentWriter?.Dispose();

                _lastSecond = currentSecond;
                string timeStamp = now.ToString("yyyy-MM-dd-HH-mm-ss");
                _currentSecondFile = Path.Combine(_saveFolder, $"{timeStamp}.asc");

                // Open a new stream once per second to reduce overhead
                _currentWriter = new StreamWriter(_currentSecondFile, append: true);
            }

            // ==================== Write List<Vector3> in batches ====================
            foreach (Vector3 v3 in FilterVec)
            {
                // Filter out points with x > 100
                if (v3.x > 100)
                    continue;

                _currentWriter.WriteLine($"{v3.x:F6} {v3.y:F6} {v3.z:F6}");

                // Write JSON
                //string json = JsonUtility.ToJson(v3, prettyPrint: false);
                //_currentWriter.WriteLine(json);
            }
            t_FilterVec.Clear();
            // Optional: flush immediately; buffering improves performance
            // _currentWriter.Flush();
        }

        public bool ifCreatFold = false;

        public void savePointCloud()
        {
            if (ifCreatFold == true)
            {
                folderName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                ifCreatFold = false;
                _saveFolder = "";
                /*if (t_FilterVec != null)
                {
                    t_FilterVec.Clear();
                    t_FilterVec = null;
                }*/
            }
            SaveArray(t_FilterVec);
        }

        public void StartDraw(Vector3[] vs)
        {
            Debug.LogError("wayland cloud " + vs.Length);
            FilterVec.Clear();

            /*for (int i = 0; i < Mathf.FloorToInt(vs.Length); i++)
            {
                //int random = UnityEngine.Random.Range(i*10, i*10 + 99);

                #region Transform ToF point-cloud coordinates
                ////Transform the ToF point cloud from ToF camera coordinates to world coordinates
                //P_tofpoint_tofcam.SetTRS(new Vector3(vs[i].x, -vs[i].y, vs[i].z), new Quaternion(0, 0, 0, 1), Vector3.one);
                //P_tofcam_glassImu.SetTRS(TofCloudManager.tofpos, TofCloudManager.tofQua, Vector3.one);
                //P_glassImu_world.SetTRS(XvXRManager.SDK.HeadPose.Position, XvXRManager.SDK.HeadPose.Orientation, Vector3.one);

                //P_tofpoint_world = P_glassImu_world * P_tofcam_glassImu * P_tofpoint_tofcam;

                //FilterVec.Add(P_tofpoint_world.GetColumn(3));
                #endregion

                //Use the point-cloud data returned by the API directly
                //FilterVec.Add(vs[i]);
            }*/
           
            //Filter
            for (int i = 0; i < vs.Length; i++)
            {
                /*int randomIndex = UnityEngine.Random.Range(0, 10);
                if (randomIndex > 28)
                {
                    FilterVec.Add(vs[i]);
                }*/
                if (i % 5 == 0)
                {
                    FilterVec.Add(vs[i]);
                    t_FilterVec.Add(vs[i]);
                }
            }


            var main = ps.main;

            var pointCount = FilterVec.Count;
            allParticles = new ParticleSystem.Particle[pointCount];
            main.maxParticles = pointCount;
            //ps.maxParticles = pointCount;
            ////Debug.Log("Rendered point count " + main.maxParticles);
            ps.Emit(pointCount);
            ps.GetParticles(allParticles);
            for (int i = 0; i < pointCount; i++)
            {
                allParticles[i].position = (Vector3)FilterVec[i];    // Set each point's position
                allParticles[i].startColor = Color.blue;    // Set each point's RGB color
                allParticles[i].startSize = 0.015f;
            }


            ps.SetParticles(allParticles, pointCount);      // Load the point cloud into the particle system
            /*
            //Render the mesh
            if (isCreateMesh)
            {
                CreateMesh(FilterVec);
                isCreateMesh = false;
            }

            */

            //Debug.Log($"XVTof finish");
        }
        public void ClearPointCloud()
        {
            if (ps != null)
            {
                ps.Clear();          // Clear all particles
                ps.Stop();           // Stop emission
                ps.Clear();          // Clear again to ensure no particles remain
            }

            // Clear the array (optional)
            allParticles = null;
        }

        void CreateMesh(List<Vector3> FilterVec)
        {
            for (int i = 0; i < planeList.Count; i++)
            {
                Destroy(planeList[i]);
            }
            planeList = new List<GameObject>();

            Vector3[] Verts = new Vector3[FilterVec.Count];
            for (int i = 0; i < FilterVec.Count; i++)
            {
                Verts[i] = FilterVec[i];
            }
            DoCreatPloygonMesh(Verts);
        }

        /// <summary>
        /// Generate a custom polygon
        /// </summary>
        /// <param name="s_Vertives">Custom vertex array</param>
        public void DoCreatPloygonMesh(Vector3[] s_Vertives)
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
            tPolygon.GetComponent<Renderer>().materials = Mat_alpha;
            tPolygon.AddComponent<MeshCollider>();

            planeList.Add(tPolygon);
        }
    }
}