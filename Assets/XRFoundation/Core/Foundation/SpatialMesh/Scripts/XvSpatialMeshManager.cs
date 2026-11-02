using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
namespace Singray.Foundation
{
    /// <summary>
    /// Scans spatial meshes
    /// </summary>
    public sealed class XvSpatialMeshManager : MonoBehaviour
    {
        private XvSpatialMeshManager() { }
        [SerializeField]
        private XvCameraManager cameraManager;


        public XvCameraManager CameraManager
        {
            get
            {

                if (cameraManager == null)
                {
                    cameraManager = FindObjectOfType<XvCameraManager>();
                }

                if (cameraManager == null)
                {
                    cameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
                }
                return cameraManager;

            }
        }

        public static int meshSurfaceId;
        private static bool isCreatMesh;
        private static readonly HashSet<XvSpatialMeshManager> detectionOwners = new HashSet<XvSpatialMeshManager>();
        private static int activeOwners;
        private static Func<bool> readyNative = NativeReady;
        private static Func<int> startNative = () => API.xslam_start_surface_callback(true, false, OnStartSurfaceCallback);
        private static Action<bool> enableNative = API.xslam_enable_surface_reconstruction;
       
        public static event Action<NowXslamSurface> meshChanged;


        private bool isDetecting;
        private Coroutine startRoutine;
        internal bool IsStarting => startRoutine != null;
        public bool IsDetecting
        {
            get { return isDetecting; }
        }


        /// <summary>
        /// Start spatial mesh scanning
        /// </summary>
        public void StartMeshDetection()
        {
            if (isActiveAndEnabled && !isDetecting && startRoutine == null)
            {
                Debug.Log("[MESH-LIFE] start requested; waiting for SDK readiness.");
                startRoutine = StartCoroutine(StartDetectionWhenReady(Time.realtimeSinceStartup + 10f));
            }
        }

        private static bool NativeReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }

        private IEnumerator StartDetectionWhenReady(float deadline)
        {
            // Yield before assigning startRoutine so even an already-ready SDK is cancellable.
            yield return null;
            try
            {
                while (!readyNative())
                {
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        Debug.LogWarning("SpatialMesh: SDK readiness timeout.");
                        yield break;
                    }
                    yield return null;
                }
                detectionOwners.Add(this);
                Volatile.Write(ref activeOwners, detectionOwners.Count);
                if (detectionOwners.Count == 1 && isCreatMesh)
                {
                    enableNative(true);
                }
                else if (!isCreatMesh)
                {
                    // CameraManager.StartCapture(XvCameraStreamType.TofDepthCameraStream);
                    meshSurfaceId = startNative();//Call the Create Mesh API
                    Debug.Log($"xslam_start_surface_callback return id:{meshSurfaceId}");
                    // API.xslam_reset_slam();
                    isCreatMesh = true;
                }
                isDetecting = true;
                Debug.Log($"[MESH-LIFE] scanning id={meshSurfaceId} owners={detectionOwners.Count}");
            }
            // A failing native entry must not leave a phantom owner keeping the stream alive.
            finally
            {
                if (!isDetecting)
                {
                    detectionOwners.Remove(this);
                    Volatile.Write(ref activeOwners, detectionOwners.Count);
                }
                startRoutine = null;
            }
        }

        /// <summary>
        /// Stop spatial mesh scanning
        /// </summary>
        public void StopMeshDetection()
        {
            if (startRoutine != null) StopCoroutine(startRoutine);
            startRoutine = null;
            if (isDetecting)
            {
                isDetecting = false;
                detectionOwners.Remove(this);
                Volatile.Write(ref activeOwners, detectionOwners.Count);
                if (detectionOwners.Count == 0) enableNative(false);
                Debug.Log($"[MESH-LIFE] stopped owners={detectionOwners.Count}");
            }
        }

        private void OnDisable() => StopMeshDetection();

       

        /// <summary>
        /// Callback when a mesh is detected
        /// </summary>
        /// <param name="surfaces"></param>
        /// <param name="size"></param>
        [MonoPInvokeCallback(typeof(API.xslam_surface_callback))]
        public static void OnStartSurfaceCallback(IntPtr surfaces, int size)
        {
            // Native memory is borrowed for this callback only. Never destroy or retain it.
            if (surfaces == IntPtr.Zero || size <= 0 || size > 4096 || Volatile.Read(ref activeOwners) == 0)
                return;
            try
            {
                int stride = Marshal.SizeOf(typeof(XslamSurface));
                for (int i = 0; i < size; i++)
                {
                    var item = (XslamSurface)Marshal.PtrToStructure(IntPtr.Add(surfaces, checked(i * stride)), typeof(XslamSurface));
                    if (item.verticesSize > 1000000 || item.trianglesSize > 2000000 ||
                        (item.verticesSize > 0 && (item.vertices == IntPtr.Zero || item.vertexNormals == IntPtr.Zero)) ||
                        (item.trianglesSize > 0 && item.triangles == IntPtr.Zero))
                        continue;
                    var vertices = new List<Vector3>((int)item.verticesSize);
                    var normals = new List<Vector3>((int)item.verticesSize);
                    var triangles = new List<Vector3uint>((int)item.trianglesSize);
                    bool valid = true;
                    for (int n = 0; n < item.verticesSize; n++)
                    {
                        var vertex = (Vector3)Marshal.PtrToStructure(IntPtr.Add(item.vertices, n * 12), typeof(Vector3));
                        var normal = (Vector3)Marshal.PtrToStructure(IntPtr.Add(item.vertexNormals, n * 12), typeof(Vector3));
                        if (!Finite(vertex) || !Finite(normal)) { valid = false; break; }
                        vertices.Add(vertex); normals.Add(normal);
                    }
                    if (!valid) continue;
                    for (int n = 0; n < item.trianglesSize; n++)
                    {
                        var triangle = (Vector3uint)Marshal.PtrToStructure(IntPtr.Add(item.triangles, n * 12), typeof(Vector3uint));
                        if (triangle.x >= item.verticesSize || triangle.y >= item.verticesSize || triangle.z >= item.verticesSize)
                        { valid = false; break; }
                        triangles.Add(triangle);
                    }
                    if (valid && Volatile.Read(ref activeOwners) > 0)
                        getCreatMeshData(vertices, normals, triangles, item.mapId.ToString());
                }
            }
            catch (Exception e)
            {
                // Do not propagate managed subscriber/copy failures through the native ABI.
                Debug.LogWarning("[MESH-LIFE] callback rejected: " + e.Message);
            }
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static void getCreatMeshData(List<Vector3> vList0, List<Vector3> vList1, List<Vector3uint> tList1, string mapID)
        {
           // MyDebugTool.Log($"getCreatMeshData{vList0.Count}   {vList1.Count}   {tList1.Count}   {mapID}");

            //for (int i = 0; i < tList1.Count; i++)
            //{
            //    Debug.Log($"tList1[{i}]:{tList1[i].x},{tList1[i].y},{tList1[i].z}");
            //}

            if (mapID == "")
            {
                return;
            }
            NowXslamSurface surface = new NowXslamSurface();
            surface.vList0_t = vList0;
            surface.vList1_t = vList1;
            surface.vListt_t = tList1;
            surface.mapID = mapID;

            meshChanged?.Invoke(surface);
           
            //infoTxt.text += mapID + " | ";
            //infoTxt.text = nowSurfaceList.Count + "";
        }

        
        private void OnDestroy()
        {
            StopMeshDetection();
        }

        private void OnApplicationQuit()
        {
            StopMeshDetection();

        }
    }

    public class NowXslamSurface
    {
        public List<Vector3> vList0_t;//Vertex coordinates
        public List<Vector3> vList1_t;//Normals
        public List<Vector3uint> vListt_t;//Triangle indices
        public string mapID;
    }
    public struct Vector3uint
    {
        public uint x;
        public uint y;
        public uint z;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct XslamSurface
    {
        public uint mapId;
        public uint version;
        public uint id;

        public uint verticesSize;

        public IntPtr vertices;

        public IntPtr vertexNormals;

        public uint trianglesSize;

        public IntPtr triangles;

        public IntPtr textureCoordinates;
        public uint textureWidth;
        public uint textureHeight;
        //[MarshalAs(UnmanagedType.LPArray)] 
        //public byte[] textureRgba;
    };
}
