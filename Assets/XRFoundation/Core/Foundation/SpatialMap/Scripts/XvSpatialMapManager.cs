using AOT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.IO;

namespace Singray.Foundation
{
   /// <summary>
   /// Provides CSLAM scanning and map-file generation; saved maps support multi-user collaboration
   /// </summary>
    public sealed class XvSpatialMapManager : MonoBehaviour
    {
        private XvSpatialMapManager() { }
        private static readonly ConcurrentQueue<Action> pendingCallbacks = new ConcurrentQueue<Action>();
        private static readonly API.detectCslamSaved_callback savedCallback = OnCslamSaved;
        private static readonly API.detectLocalized_callback saveLocalizedCallback = OnSaveLocalized;
        private static readonly API.detectSwitched_callback switchedCallback = OnCslamSwitched;
        private static readonly API.detectLocalized_callback loadLocalizedCallback = OnLoadLocalized;
        private static Task<bool> nativeResetTask;
        private Task<bool> resetTask;
        private float resetDeadline;
        private bool resetTimedOut;
        private bool resetReturned;
        private float resetStartedAt;
        private long poseBeforeReset;
        private Action afterReset;
        private static Func<bool> resetNative = () => API.xslam_reset_slam();
        private static Func<long> poseTimestamp = ReadPoseTimestamp;
        private enum MapOperation { None, Saving, Loading }
        private static MapOperation activeOperation;
        private static XvSpatialMapManager operationOwner;
        private static string operationPath;
        private static float operationStartedAt;
        private static bool operationSubmitted, operationWarned;
        private static long lastSaveStamp;
        private static Func<string, API.detectCslamSaved_callback, API.detectLocalized_callback, bool> saveNative = API.xslam_save_map_and_switch_to_cslam;
        private static Func<string, API.detectSwitched_callback, API.detectLocalized_callback, bool> loadNative = API.xslam_load_map_and_switch_to_cslam;
        internal bool IsBusy => IsResetting || activeOperation != MapOperation.None || (nativeResetTask != null && !nativeResetTask.IsCompleted);
        internal string OperationStatus => activeOperation == MapOperation.None ? ResetStatus :
            (operationWarned ? activeOperation + " timed out; native completion still pending." : activeOperation + " map; waiting for native completion...");
        internal string CompletedSavePath { get; private set; }
        internal string CompletedLoadPath { get; private set; }
        private void SetOperation(MapOperation kind, string path)
        {
            activeOperation=kind; operationOwner=this; operationPath=path;
            operationStartedAt=Time.realtimeSinceStartup; operationSubmitted=operationWarned=false;
        }
        private static void ClearOperation()
        { activeOperation=MapOperation.None; operationOwner=null; operationPath=null; operationSubmitted=operationWarned=false; }
        internal bool IsResetting => resetTask != null;
        internal string ResetStatus { get; private set; } = "Ready";

        private static long ReadPoseTimestamp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Vector3 position = Vector3.zero, rotation = Vector3.zero; long timestamp = 0;
            return API.xslam_get_6dof(ref position, ref rotation, ref timestamp) ? timestamp : 0;
#else
            return 0;
#endif
        }

        private static Func<bool> readyNative = PlatformReady;
        private static bool NativeReady() => (nativeResetTask == null || nativeResetTask.IsCompleted) && readyNative();
        private static bool PlatformReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }

        private void Update()
        {
            PollReset();
            while (pendingCallbacks.TryDequeue(out Action callback))
            { try { callback(); } catch (Exception error) { Debug.LogException(error); } }
            if(activeOperation!=MapOperation.None&&!operationWarned&&Time.realtimeSinceStartup-operationStartedAt>30f)
            { operationWarned=true;Debug.LogWarning("[MAP-LIFE] "+OperationStatus+" Request gate is retained; do not overlap native operations."); }
        }

        private void BeginReset(Action continuation)
        {
            if(activeOperation!=MapOperation.None && !(activeOperation==MapOperation.Loading && operationOwner==this && continuation!=null))
            { ResetStatus="Create Map was not started: another map operation is pending.";Debug.LogWarning("[MAP-LIFE] "+ResetStatus);return; }
            if (IsResetting || (nativeResetTask != null && !nativeResetTask.IsCompleted))
            { Debug.LogWarning("[MAP-LIFE] reset already pending; duplicate request ignored."); return; }
            if (!NativeReady()) { ResetStatus="Map reset was not started: SDK is not ready.";Debug.LogWarning(ResetStatus);return; }
            if (showFeaturePoint) API.xslam_stop_map();
            showFeaturePoint = false;
            poseBeforeReset = poseTimestamp();
            resetStartedAt = Time.realtimeSinceStartup;
            resetDeadline = resetStartedAt + 10;
            resetTimedOut = false;
            resetReturned = false;
            afterReset = continuation;
            ResetStatus = "Creating map: resetting tracking...";
            Debug.Log("[MAP-LIFE] phase=reset-begin poseBefore=" + poseBeforeReset);
            // The supplier reset stops gesture/tracking internally. Never block Unity's render/input thread on it.
            resetTask = nativeResetTask = Task.Run(resetNative);
        }

        private void PollReset()
        {
            if (resetTask == null) return;
            if (resetTask.IsCompleted)
            {
                if (resetTask.IsFaulted || resetTask.IsCanceled || !resetTask.Result)
                {
                    ResetStatus = "Map reset failed. See log.";
                    Debug.LogError("[MAP-LIFE] phase=reset-failed " + resetTask.Exception);
                    resetTask = null; afterReset = null;
                    if(activeOperation==MapOperation.Loading && operationOwner==this) ClearOperation();
                    return;
                }
                if (!resetReturned)
                {
                    resetReturned = true;
                    ResetStatus = "Creating map: waiting for fresh tracking pose...";
                    Debug.Log("[MAP-LIFE] phase=reset-native-returned elapsedMs=" +
                        ((Time.realtimeSinceStartup - resetStartedAt) * 1000).ToString("F0"));
                }
                long timestamp = NativeReady() ? poseTimestamp() : 0;
                if (timestamp > poseBeforeReset)
                {
                    ResetStatus = "Scanning map";
                    Debug.Log("[MAP-LIFE] phase=reset-ready newPose=" + timestamp);
                    resetTask = null;
                    Action continuation = afterReset; afterReset = null;
                    continuation?.Invoke();
                    return;
                }
            }
            if (!resetTimedOut && Time.realtimeSinceStartup > resetDeadline)
            {
                resetTimedOut = true;
                ResetStatus = "Tracking recovery timed out. Waiting for pending operation...";
                Debug.LogError("[MAP-LIFE] phase=reset-timeout nativeCompleted=" + resetTask.IsCompleted);
            }
        }

        private void OnDisable()
        {
            // A native operation cannot safely be aborted; discard this scene's continuation, retain the global gate.
            afterReset = null;
            resetTask = null;
            if (showFeaturePoint && NativeReady()) API.xslam_stop_map();
            showFeaturePoint = false;
            if(activeOperation==MapOperation.Loading && operationOwner==this && !operationSubmitted) ClearOperation();
            // The callback queue is shared; another active manager must still be able to finish an in-flight operation.
        }



       

        /// <summary>
        /// The first parameter is the map save status
        /// The second parameter is map quality
        /// </summary>
        /// 
        public static UnityEvent<int,int> onMapSaveCompleteEvent=new UnityEvent<int, int>();
        /// <summary>
        /// The first parameter is map quality
        /// </summary>
        public static UnityEvent<int > onMapLoadCompleteEvent=new UnityEvent<int>();

        public static UnityEvent< float> onMapMatchingEvent = new UnityEvent< float>();

        


        /// <summary>
        /// Start map scanning
        /// </summary>
        public void StartSlamMap()
        {
            BeginReset(null);
        }

        /// <summary>
        /// Save the scanned map
        /// </summary>
        /// <returns></returns>
        public string SaveSlamMap()
        {
            if (IsBusy) { Debug.LogWarning("[MAP-LIFE] save ignored while another map operation is pending."); return null; }
            if (!NativeReady()) { ResetStatus="Map save was not started: SDK is not ready.";Debug.LogWarning(ResetStatus);return null; }
            MyDebugTool.Log("Save map 1: " );

            long stamp=Math.Max(GetNowStamp(),lastSaveStamp+1);lastSaveStamp=stamp;
            string cslamName = stamp + "_map.bin";
            string mapPath = Application.persistentDataPath + "/" + cslamName;
            CompletedSavePath=null;SetOperation(MapOperation.Saving,mapPath);
            try
            {
                operationSubmitted=saveNative(mapPath,savedCallback,saveLocalizedCallback);
                if(!operationSubmitted){ClearOperation();ResetStatus="Map save rejected by native SDK.";Debug.LogWarning("[MAP-LIFE] "+ResetStatus);return null;}
            }
            catch{ClearOperation();throw;}
            MyDebugTool.Log("Save map 2: "+ mapPath);

            return mapPath;
        }

        /// <summary>
        /// Load an existing map
        /// </summary>
        /// <param name="mapPath"></param>
        public void LoadSlamMap(string mapPath)
        {
            if (IsBusy) { Debug.LogWarning("[MAP-LIFE] load ignored while another map operation is pending."); return; }
            if (!NativeReady()) { ResetStatus="Map load was not started: SDK is not ready.";Debug.LogWarning(ResetStatus);return; }
            if(string.IsNullOrEmpty(mapPath)||!File.Exists(mapPath)||new FileInfo(mapPath).Length==0)
            { ResetStatus="Map file is missing or empty.";Debug.LogWarning("[MAP-LIFE] "+ResetStatus);return; }
            MyDebugTool.Log("Load map 1: " + mapPath);

            CompletedLoadPath=null;SetOperation(MapOperation.Loading,mapPath);
            BeginReset(() =>
            {
                try
                {
                    operationSubmitted=loadNative(mapPath,switchedCallback,loadLocalizedCallback);
                    if(!operationSubmitted){ClearOperation();ResetStatus="Map load rejected by native SDK.";Debug.LogWarning("[MAP-LIFE] "+ResetStatus);return;}
                }
                catch{ClearOperation();throw;}
                MyDebugTool.Log("Load map 2: " + mapPath);
            });
            if(resetTask==null && activeOperation==MapOperation.Loading && operationOwner==this) ClearOperation();

        }


        private bool showFeaturePoint;
        public bool ShowFeaturePoint { 
           get { return showFeaturePoint; } 
           
        }
        public void SwitchFeaturePointState() {
            if (IsBusy) return;
            if (!NativeReady()) { Debug.LogWarning("SpatialMap: SDK is not ready."); return; }

          
            if (!showFeaturePoint)
            {
                
                Debug.Log("SpatialMap: starting native feature-point stream.");
                showFeaturePoint = API.xslam_start_map();

                MyDebugTool.Log("Enable feature points");
            }
            else if(showFeaturePoint)
            {
                API.xslam_stop_map();
                showFeaturePoint = false;
                MyDebugTool.Log("Disable feature points");

            }
        }

      

      
        /// <summary>
        /// Poll at intervals rather than calling too frequently
        /// </summary>
        /// <returns>Feature-point positions in space</returns>
        public List<Vector3> GetFeaturePoint()
        {
                if (!NativeReady() || !ShowFeaturePoint || IsBusy)
                {
                    return null;
                }
           
           
                int count = 0;
                IntPtr pt = API.xslam_get_slam_map(ref count);
                if (pt == IntPtr.Zero || count <= 0 || count > 1000000) return null;
                API.SlamMap[] objdata = new API.SlamMap[count];

                List<Vector3> pointList = new List<Vector3>();
                for (int i = 0; i < count; i++)
                {
                    IntPtr ptr = pt + i * Marshal.SizeOf(typeof(API.SlamMap));

                    objdata[i] = (API.SlamMap)Marshal.PtrToStructure(ptr, typeof(API.SlamMap));

                    Vector3 xyz = new Vector3(objdata[i].vertices[0], -objdata[i].vertices[1], objdata[i].vertices[2]);
                    pointList.Add(xyz);
                    // The SDK owns this memory; do not destroy or free it.
                }
                return pointList;

            

           

        }

        
        //private static float savePercent;
        //private static int save_map_quality;


        private static float similarity;
        private static int matchingQueued;
        private static int load_map_quality;

        //private static int status_of_saved_mapq;


      


        /// <summary>
        /// Map-save callback implementation
        /// </summary>
        /// <param name="status_of_saved_map"></param>
        /// <param name="map_quality"></param>
        
        [MonoPInvokeCallback(typeof(API.detectCslamSaved_callback))]
        static void OnCslamSaved(int status_of_saved_map, int map_quality)
        {
          
            pendingCallbacks.Enqueue(() =>
            {
                if(activeOperation==MapOperation.Saving)
                {
                    if(operationOwner!=null){operationOwner.CompletedSavePath=operationPath;operationOwner.ResetStatus="Save callback received: status="+status_of_saved_map+" quality="+map_quality;}
                    ClearOperation();
                }
                onMapSaveCompleteEvent?.Invoke(status_of_saved_map,map_quality);
            });

            MyDebugTool.Log("Save completed: status_of_saved_map:"+ status_of_saved_map+ "   map_quality:" + map_quality);
        }

        /// <summary>
        /// Saved-map localization quality callback implementation
        /// </summary>
        /// <param name="percentc"></param>
        [MonoPInvokeCallback(typeof(API.detectLocalized_callback))]
        static void OnSaveLocalized(float percentc)
        {
            //savePercent = percentc;
            //MyDebugTool.Log("Saved map localization percent: " + percentc);
        }

        /// <summary>
        /// Map-load callback implementation
        /// </summary>
        /// <param name="map_quality"></param>
        [MonoPInvokeCallback(typeof(API.detectSwitched_callback))]
        static void OnCslamSwitched(int map_quality)
        {
            MyDebugTool.Log("Map loading completed: " + map_quality);

            load_map_quality = map_quality;
            pendingCallbacks.Enqueue(() =>
            {
                if(activeOperation==MapOperation.Loading)
                {
                    if(operationOwner!=null){operationOwner.CompletedLoadPath=operationPath;operationOwner.ResetStatus="Load callback received: quality="+map_quality;}
                    ClearOperation();
                }
                onMapLoadCompleteEvent?.Invoke(map_quality);
            });
        }

        /// <summary>
        /// Loaded-map localization quality callback implementation
        /// </summary>
        /// <param name="percentc">0~1</param>
        [MonoPInvokeCallback(typeof(API.detectLocalized_callback))]
        static void OnLoadLocalized(float percentc)
        {
            similarity = percentc;
            if (System.Threading.Interlocked.Exchange(ref matchingQueued,1)==0)
                pendingCallbacks.Enqueue(DispatchMatching);
        }

        private static void DispatchMatching()
        {
            System.Threading.Interlocked.Exchange(ref matchingQueued,0);
            onMapMatchingEvent?.Invoke(similarity);
        }


        /// <summary>
        /// Convert DateTime to a timestamp
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public long ConvertDateTimeTotTmeStamp(System.DateTime time)
        {
            System.DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(new System.DateTime(1970, 1, 1, 0, 0, 0, 0), TimeZoneInfo.Local);
            long t = (time.Ticks - startTime.Ticks) / 10000;  //Divide by 10000 to produce a 13-digit timestamp
            return t;
        }

        /// <summary>
        /// Get the current timestamp
        /// </summary>
        /// <returns></returns>
        public long GetNowStamp()
        {
            return ConvertDateTimeTotTmeStamp(DateTime.Now);
        }
    }
}
