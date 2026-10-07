using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Singray.Foundation
{
    /// <summary>
    /// Detects horizontal and vertical planes in space
    /// </summary>

    public sealed class XvPlaneManager : MonoBehaviour
    {
        private XvPlaneManager() { }
        public event Action<plane[]> planesChanged;

        private bool isDetecting;
        public bool IsDetecting
        {
            get { return isDetecting; }
        }


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

        private float gapTime;

        /// <summary>
        /// 
        /// </summary>
        private Coroutine startRoutine;
        public void StartPlaneDetction()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!isDetecting && startRoutine == null)
                startRoutine = StartCoroutine(StartDetectionWhenReady());
#endif
        }

        private IEnumerator StartDetectionWhenReady()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!API.xslam_ready())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogWarning("PlaneDetection: SDK readiness timeout.");
                    startRoutine = null;
                    yield break;
                }
                yield return null;
            }
            Debug.Log("PlaneDetection: starting native ToF plane detection.");
            API.xslam_tof_set_framerate(5);
            isDetecting = API.xslam_start_detect_plane_from_tof_nosurface();
            startRoutine = null;
            Debug.Log("PlaneDetection: native start result=" + isDetecting);
        }

        public void StopPlaneDetection()
        {
            if (startRoutine != null) StopCoroutine(startRoutine);
            startRoutine = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (isDetecting) API.xslam_stop_detect_plane_from_tof();
#endif
            isDetecting = false;
        }

        private void OnDisable() => StopPlaneDetection();

        private void Update()
        {
#if !PLATFORM_ANDROID || UNITY_EDITOR
            return;
#endif   
            if (isDetecting)
            {
                gapTime += Time.deltaTime;

                if (gapTime >= 1)
                {
                    plane[] planes = GetPlane();
                    if (planes != null)
                    {
                        planesChanged?.Invoke(planes);
                    }

                    gapTime = 0;
                }

            }
        }





        private plane[] GetPlane()
        {
            int len = 1024 * 64;
            byte[] rdata = new byte[len];
            GCHandle rh = GCHandle.Alloc(rdata, GCHandleType.Pinned);
            bool ret;
            try { ret = API.xslam_get_plane_from_tof(rh.AddrOfPinnedObject(), ref len); }
            finally { rh.Free(); }
            if (ret)
                return ParsePlane(rdata, len);
            else
                return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rdata">Plane data</param>
        /// <param name="len">Total data length</param>
        /// <returns></returns>
        private static plane[] ParsePlane(byte[] rdata, int len)
        {
            if (rdata == null || len < 4 || len > rdata.Length) return null;
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(rdata, 0, len)))
                {
                    int count = reader.ReadInt32();
                    if (count < 0 || count > (len - 4) / 40) return null;
                    var planes = new plane[count];
                    for (int i = 0; i < count; i++)
                    {
                        int points = reader.ReadInt32();
                        long remaining = len - reader.BaseStream.Position;
                        if (points < 0 || remaining < 36 || points > (remaining - 36) / 24) return null;
                        var item = new plane { points = new List<Vector3D>(points) };
                        for (int j = 0; j < points; j++)
                            item.points.Add(new Vector3D { x = reader.ReadDouble(), y = reader.ReadDouble(), z = reader.ReadDouble() });
                        item.normal = new Vector3D { x = reader.ReadDouble(), y = reader.ReadDouble(), z = reader.ReadDouble() };
                        item.d = reader.ReadDouble();
                        int idLength = reader.ReadInt32();
                        if (idLength < 0 || idLength > len - reader.BaseStream.Position) return null;
                        item.id = BitConverter.ToString(reader.ReadBytes(idLength));
                        planes[i] = item;
                    }
                    return planes;
                }
            }
            catch (EndOfStreamException) { return null; }
        }


        private void OnDestroy()
        {
            StopPlaneDetection();
        }

        private void OnApplicationQuit()
        {
            StopPlaneDetection();
        }
    }
    public struct Vector3D
    {
        public double x;
        public double y;
        public double z;
    };


    public class plane
    {
        public List<Vector3D> points;//Plane vertex coordinates
        public Vector3D normal;//Plane normal
        public double d;
        public string id;//Plane ID
    };
}
