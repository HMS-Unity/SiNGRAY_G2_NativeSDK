using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        private static XslamSurface[] objdata;
        public static int meshSurfaceId;
        private bool isCreatMesh = false;
       
        public static event Action<NowXslamSurface> meshChanged;


        private bool isDetecting;
        public bool IsDetecting
        {
            get { return isDetecting; }
        }


        /// <summary>
        /// Start spatial mesh scanning
        /// </summary>
        public void StartMeshDetection()
        {
            if (!isDetecting)
            {
                isDetecting = true;

#if UNITY_EDITOR
                return;
#endif

          

                while (!API.xslam_ready())
                {
                    MyDebugTool.Log("xslam_ready==false");
                }

                MyDebugTool.Log("xslam_ready==true");


                if (isCreatMesh)
                {
                    API.xslam_enable_surface_reconstruction(true);
                }
                else
                {
                    // CameraManager.StartCapture(XvCameraStreamType.TofDepthCameraStream);
                    meshSurfaceId = API.xslam_start_surface_callback(true, false, OnStartSurfaceCallback);//Call the Create Mesh API
                    Debug.Log($"xslam_start_surface_callback return id:{meshSurfaceId}");
                    // API.xslam_reset_slam();
                    isCreatMesh = true;
                }

            }
           
        }

        /// <summary>
        /// Stop spatial mesh scanning
        /// </summary>
        public void StopMeshDetection()
        {
            if (isDetecting)
            {
                isDetecting = false;
                API.xslam_enable_surface_reconstruction(false);
            }
        }

       

        /// <summary>
        /// Callback when a mesh is detected
        /// </summary>
        /// <param name="surfaces"></param>
        /// <param name="size"></param>
        [MonoPInvokeCallback(typeof(API.xslam_surface_callback))]
        public static void OnStartSurfaceCallback(IntPtr surfaces, int size)
        {
//#if UNITY_EDITOR
//            return;
//#endif

           // MyDebugTool.Log($"OnStartSurfaceCallback"+ size);
            objdata = new XslamSurface[size];
            for (int i = 0; i < size; i++)
            {
                /*try
                {*/
                objdata[i] = new XslamSurface();

                IntPtr ptr = surfaces + i * Marshal.SizeOf(typeof(XslamSurface));
                if (ptr == IntPtr.Zero)
                    return;

                objdata[i] = (XslamSurface)Marshal.PtrToStructure(ptr, typeof(XslamSurface));


                IntPtr intptr_kp = objdata[i].vertices;
                if (intptr_kp == IntPtr.Zero)
                    return;
                long a = intptr_kp.ToInt64();

                IntPtr intptr_nor = objdata[i].vertexNormals;
                if (intptr_nor == IntPtr.Zero)
                    return;
                long a_n = intptr_nor.ToInt64();

                IntPtr intptr_t = objdata[i].triangles;
                if (intptr_t == IntPtr.Zero)
                    return;
                long a_t = intptr_t.ToInt64();

                List<Vector3> viList = new List<Vector3>();
                List<Vector3> vi_nList = new List<Vector3>();
                List<Vector3uint> trianglesList = new List<Vector3uint>();


                //All points of each detected object
                for (int n = 0; n < objdata[i].verticesSize; n++)
                {
                    IntPtr ptr_vi = (IntPtr)((a + n * Marshal.SizeOf(typeof(Vector3))));
                    if (ptr_vi == IntPtr.Zero)
                        return;
                    Vector3 vi = (Vector3)Marshal.PtrToStructure(ptr_vi, typeof(Vector3));
                    viList.Add(vi);

                    IntPtr ptr_vi_n = (IntPtr)((a_n + n * Marshal.SizeOf(typeof(Vector3))));
                    if (ptr_vi_n == IntPtr.Zero)
                        return;
                    Vector3 vi_n = (Vector3)Marshal.PtrToStructure(ptr_vi_n, typeof(Vector3));
                    vi_nList.Add(vi_n);

                    Marshal.DestroyStructure(ptr_vi, typeof(Vector3));
                    Marshal.DestroyStructure(ptr_vi_n, typeof(Vector3));
                }

                for (int n = 0; n < objdata[i].trianglesSize; n++)
                {
                    //Debug
                    IntPtr ptr_vi_t = (IntPtr)((a_t + n * Marshal.SizeOf(typeof(Vector3uint))));
                    if (ptr_vi_t == IntPtr.Zero)
                    {
                        return;
                    }
                    Vector3uint vi_t = (Vector3uint)Marshal.PtrToStructure(ptr_vi_t, typeof(Vector3uint));
                    trianglesList.Add(vi_t);

                    Marshal.DestroyStructure(ptr_vi_t, typeof(Vector3uint));
                }


              //  MyDebugTool.Log($"Mesh callback");
               
                getCreatMeshData(viList, vi_nList, trianglesList, objdata[i].mapId.ToString());
                Marshal.DestroyStructure(ptr, typeof(XslamSurface));
            }
        }


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
