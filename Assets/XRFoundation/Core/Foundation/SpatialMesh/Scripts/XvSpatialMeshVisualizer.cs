using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Singray.Foundation
{
    /// <summary>Visualizes the latest spatial surface for each map ID.</summary>
    public sealed class XvSpatialMeshVisualizer : WorkQueue
    {
        private XvSpatialMeshVisualizer() { }
        private readonly Dictionary<string, GameObject> meshDic = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> MeshDic => meshDic;
        [SerializeField] private XvSpatialMeshManager xvSpatialMeshManager;
        [SerializeField] private Material meshMat;
        [SerializeField] private bool enableCollider = true;
        public bool EnableCollider => enableCollider;
        [SerializeField] private bool enableRender = true;
        public bool EnableRender => enableRender;
        [SerializeField] public bool autoDetection = true;
        public bool AutoDetection => autoDetection;
        private readonly object pendingLock = new object();
        private readonly Dictionary<string, NowXslamSurface> pending = new Dictionary<string, NowXslamSurface>();
        private readonly Queue<string> pendingIds = new Queue<string>();
        private readonly Dictionary<string, Mesh> ownedMeshes = new Dictionary<string, Mesh>();
        private readonly Dictionary<string, GameObject> ownedRoots = new Dictionary<string, GameObject>();
        private bool accepting;
        private int applied;
        private float nextLog;
        protected override void Awake()
        {
            base.Awake();
            if (xvSpatialMeshManager == null) xvSpatialMeshManager = GetComponent<XvSpatialMeshManager>();
            if (xvSpatialMeshManager == null) xvSpatialMeshManager = GetComponentInParent<XvSpatialMeshManager>();
            if (xvSpatialMeshManager == null) xvSpatialMeshManager = FindObjectOfType<XvSpatialMeshManager>();
            if (xvSpatialMeshManager == null) xvSpatialMeshManager = new GameObject("XvSpatialMeshManager").AddComponent<XvSpatialMeshManager>();
        }
        private void OnEnable()
        {
            lock (pendingLock) accepting = true;
            XvSpatialMeshManager.meshChanged += meshChanged;
            if (autoDetection) xvSpatialMeshManager.StartMeshDetection();
        }
        private void OnDisable()
        {
            XvSpatialMeshManager.meshChanged -= meshChanged;
            lock (pendingLock) { accepting = false; pending.Clear(); pendingIds.Clear(); }
            if (autoDetection && xvSpatialMeshManager != null) xvSpatialMeshManager.StopMeshDetection();
        }
        private void OnDestroy()
        {
            OnDisable();
            foreach (var mesh in ownedMeshes.Values) Release(mesh);
            foreach (var root in ownedRoots.Values) Release(root);
            ownedMeshes.Clear(); ownedRoots.Clear(); meshDic.Clear();
        }
        private static void Release(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
        }
        // Native callbacks may run off the main thread; replace stale updates for the same ID.
        private void meshChanged(NowXslamSurface data)
        {
            if (data == null || string.IsNullOrEmpty(data.mapID)) return;
            lock (pendingLock)
            {
                if (!accepting) return;
                if (!pending.ContainsKey(data.mapID))
                {
                    pendingIds.Enqueue(data.mapID);
                }
                pending[data.mapID] = data;
            }
        }
        protected override void Update()
        {
            base.Update();
            // Bound per-frame collision cooking and mesh uploads; retained IDs receive their latest update.
            for (int i = 0; i < 4; i++)
            {
                NowXslamSurface data;
                lock (pendingLock)
                {
                    if (pendingIds.Count == 0) break;
                    string id = pendingIds.Dequeue(); data = pending[id]; pending.Remove(id);
                }
                creatMesh(data);
            }
            if (applied > 0 && Time.unscaledTime >= nextLog)
            {
                int waiting; lock (pendingLock) waiting = pending.Count;
                Debug.Log($"[MESH-LIFE] surfaces={meshDic.Count} updates={applied} pending={waiting} render={enableRender} collider={enableCollider}");
                nextLog = Time.unscaledTime + 5f;
            }
        }
        private void creatMesh(NowXslamSurface data)
        {
            if (!Valid(data)) return;
            if (data.vList0_t.Count == 0 || data.vListt_t.Count == 0)
            {
                if (ownedMeshes.TryGetValue(data.mapID, out var oldMesh)) Release(oldMesh);
                if (ownedRoots.TryGetValue(data.mapID, out var oldRoot)) Release(oldRoot);
                ownedMeshes.Remove(data.mapID); ownedRoots.Remove(data.mapID); meshDic.Remove(data.mapID);
                return;
            }
            if (!meshDic.TryGetValue(data.mapID, out var obj) || obj == null)
            {
                if (ownedMeshes.TryGetValue(data.mapID, out var oldMesh)) Release(oldMesh);
                if (ownedRoots.TryGetValue(data.mapID, out var oldRoot)) Release(oldRoot);
                var root = new GameObject(data.mapID); root.transform.SetParent(transform, true);
                obj = new GameObject("tPolygon"); obj.transform.SetParent(root.transform, true);
                obj.AddComponent<MeshFilter>();
                var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = meshMat;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                obj.AddComponent<MeshCollider>(); obj.isStatic = true;
                var mesh = new Mesh { name = "SpatialMesh_" + data.mapID }; mesh.MarkDynamic();
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                meshDic[data.mapID] = obj; ownedMeshes[data.mapID] = mesh; ownedRoots[data.mapID] = root;
            }
            var target = ownedMeshes[data.mapID];
            var vertices = new Vector3[data.vList0_t.Count]; var normals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = data.vList0_t[i]; vertices[i] = new Vector3(v.x, -v.y, v.z);
                var n = data.vList1_t[i]; normals[i] = new Vector3(n.x, -n.y, n.z);
            }
            var indices = new int[data.vListt_t.Count * 3];
            for (int i = 0; i < data.vListt_t.Count; i++)
            {
                var t = data.vListt_t[i]; indices[i * 3] = (int)t.x; indices[i * 3 + 1] = (int)t.y; indices[i * 3 + 2] = (int)t.z;
            }
            Array.Reverse(indices);
            var collider = obj.GetComponent<MeshCollider>(); collider.sharedMesh = null;
            target.Clear(); target.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            target.vertices = vertices; target.normals = normals; target.triangles = indices;
            collider.enabled = enableCollider;
            if (enableCollider) collider.sharedMesh = target;
            obj.GetComponent<MeshRenderer>().enabled = enableRender;
            applied++;
        }
        private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.z));
        private static bool Valid(NowXslamSurface data)
        {
            if (data == null || string.IsNullOrEmpty(data.mapID) || data.vList0_t == null || data.vList1_t == null || data.vListt_t == null ||
                data.vList0_t.Count != data.vList1_t.Count || data.vListt_t.Count > int.MaxValue / 3) return false;
            for (int i = 0; i < data.vList0_t.Count; i++) if (!Finite(data.vList0_t[i]) || !Finite(data.vList1_t[i])) return false;
            foreach (var t in data.vListt_t) if (t.x >= data.vList0_t.Count || t.y >= data.vList0_t.Count || t.z >= data.vList0_t.Count) return false;
            return true;
        }
        public void SetCollider(bool enable)
        {
            enableCollider = enable;
            foreach (var item in meshDic.Values)
            {
                if (item == null) continue;
                var collider = item.GetComponent<MeshCollider>(); collider.enabled = enable;
                collider.sharedMesh = enable ? item.GetComponent<MeshFilter>().sharedMesh : null;
            }
            Debug.Log("[MESH-LIFE] collider=" + enable);
        }
        public void SetVisualizer(bool enable)
        {
            enableRender = enable;
            foreach (var item in meshDic.Values) if (item != null) item.GetComponent<MeshRenderer>().enabled = enable;
            Debug.Log("[MESH-LIFE] render=" + enable);
        }
    }
}
