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
        // Retain recent samples for periodic ASC saving without growing forever in preview mode.
        private const int MaxBufferedPoints = 131072;
        private int bufferStart;
        [SerializeField] private bool colorByDistance = true;
        private const float NearColorDistance = 0.5f;
        private const float FarColorDistance = 5f;
        private static readonly Color32[] DistanceColors = CreateDistanceColors();

        private static Color32[] CreateDistanceColors()
        {
            var colors = new Color32[256];
            Color32 near = new Color32(255, 190, 72, 255);
            Color32 middle = new Color32(64, 225, 170, 255);
            Color32 far = new Color32(75, 155, 255, 255);
            for (int i = 0; i < colors.Length; i++)
            {
                // Index by squared distance: square roots and palette interpolation run only once.
                float distance = Mathf.Sqrt(Mathf.Lerp(NearColorDistance * NearColorDistance,
                    FarColorDistance * FarColorDistance, i / (float)(colors.Length - 1)));
                float t = Mathf.InverseLerp(NearColorDistance, FarColorDistance, distance);
                colors[i] = t < 0.5f ? Color32.Lerp(near, middle, t * 2) : Color32.Lerp(middle, far, t * 2 - 1);
            }
            return colors;
        }

        private static Color32 GetDistanceColor(float squaredDistance)
        {
            if (float.IsNaN(squaredDistance)) return DistanceColors[DistanceColors.Length - 1];
            float t = Mathf.InverseLerp(NearColorDistance * NearColorDistance,
                FarColorDistance * FarColorDistance, squaredDistance);
            return DistanceColors[Mathf.Clamp(Mathf.RoundToInt(t * (DistanceColors.Length - 1)), 0, DistanceColors.Length - 1)];
        }

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

        private void CloseWriter()
        {
            try { _currentWriter?.Dispose(); }
            finally
            {
                _currentWriter = null;
                _currentSecondFile = null;
                _lastSecond = -1;
            }
        }

        private void OnDisable() => ClearPointCloud();
        private void OnDestroy() => CloseWriter();

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
                CloseWriter();

                _lastSecond = currentSecond;
                string timeStamp = now.ToString("yyyy-MM-dd-HH-mm-ss");
                _currentSecondFile = Path.Combine(_saveFolder, $"{timeStamp}.asc");

                // Open a new stream once per second to reduce overhead
                _currentWriter = new StreamWriter(_currentSecondFile, append: true);
            }

            // ==================== Write List<Vector3> in batches ====================
            for (int i = 0; i < FilterVec.Count; i++)
            {
                Vector3 v3 = FilterVec[(bufferStart + i) % FilterVec.Count];
                // Filter out points with x > 100
                if (v3.x > 100)
                    continue;

                _currentWriter.WriteLine($"{v3.x:F6} {v3.y:F6} {v3.z:F6}");

                // Write JSON
                //string json = JsonUtility.ToJson(v3, prettyPrint: false);
                //_currentWriter.WriteLine(json);
            }
            t_FilterVec.Clear();
            bufferStart = 0;
            // Optional: flush immediately; buffering improves performance
            // _currentWriter.Flush();
        }

        public bool ifCreatFold = false;

        public void savePointCloud()
        {
            if (ifCreatFold == true)
            {
                CloseWriter();
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
            if (vs == null || ps == null) return;
            int pointCount = vs.Length / 5 + (vs.Length % 5 == 0 ? 0 : 1);
            if (allParticles == null || allParticles.Length < pointCount)
                allParticles = new ParticleSystem.Particle[pointCount];
            if (FilterVec.Capacity < pointCount) FilterVec.Capacity = pointCount;
            if (t_FilterVec.Capacity < MaxBufferedPoints) t_FilterVec.Capacity = MaxBufferedPoints;
            FilterVec.Clear();

            var main = ps.main;
            if (pointCount > 0 && main.maxParticles != pointCount) main.maxParticles = pointCount;
            // Explicitly initialize live particles; Emit/GetParticles unnecessarily generated and copied them first.
            float lifetime = Mathf.Max(1f, main.startLifetime.constantMax);
            var view = colorByDistance ? Camera.main : null;
            Vector3 viewPosition = view != null ? view.transform.position : Vector3.zero;
            Transform particleSpace = main.simulationSpace == ParticleSystemSimulationSpace.Local ? ps.transform :
                main.simulationSpace == ParticleSystemSimulationSpace.Custom ? main.customSimulationSpace : null;
            Matrix4x4 toWorld = particleSpace != null ? particleSpace.localToWorldMatrix : Matrix4x4.identity;
            for (int i = 0, source = 0; i < pointCount; i++, source += 5)
            {
                Vector3 point = vs[source];
                Vector3 worldPoint = particleSpace != null ? toWorld.MultiplyPoint3x4(point) : point;
                FilterVec.Add(point);
                if (t_FilterVec.Count < MaxBufferedPoints) t_FilterVec.Add(point);
                else
                {
                    t_FilterVec[bufferStart] = point;
                    bufferStart++;
                    if (bufferStart == MaxBufferedPoints) bufferStart = 0;
                }
                allParticles[i] = new ParticleSystem.Particle
                {
                    position = point,
                    startColor = colorByDistance ? GetDistanceColor((worldPoint - viewPosition).sqrMagnitude) : (Color32)Color.blue,
                    startSize = 0.015f,
                    startLifetime = lifetime,
                    remainingLifetime = lifetime
                };
            }
            // A cloud is a static snapshot, not an emitter: prevent extra particles or drift between updates.
            if (pointCount > 0 && !ps.isPaused)
            {
                ps.Play(false);
                ps.Pause(false);
            }
            ps.SetParticles(allParticles, pointCount);
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
            FilterVec.Clear();
            t_FilterVec.Clear();
            bufferStart = 0;
            CloseWriter();
            _saveFolder = "";
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
