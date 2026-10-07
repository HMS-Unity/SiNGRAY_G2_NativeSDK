using AOT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

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

        private static bool NativeReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }

        private void Update()
        {
            while (pendingCallbacks.TryDequeue(out Action callback)) callback();
        }

        private void OnDisable()
        {
            if (showFeaturePoint && NativeReady()) API.xslam_stop_map();
            showFeaturePoint = false;
            while (pendingCallbacks.TryDequeue(out _)) { }
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
            if (!NativeReady()) { Debug.LogWarning("SpatialMap: SDK is not ready."); return; }

            MyDebugTool.Log("Scan map 1");
#if UNITY_ANDROID && !UNITY_EDITOR
         API.xslam_reset_slam();
#endif
            MyDebugTool.Log("Scan map 2");

        }

        /// <summary>
        /// Save the scanned map
        /// </summary>
        /// <returns></returns>
        public string SaveSlamMap()
        {
            if (!NativeReady()) { Debug.LogWarning("SpatialMap: SDK is not ready."); return null; }
            MyDebugTool.Log("Save map 1: " );

            string cslamName = GetNowStamp() + "_map.bin";
            string mapPath = Application.persistentDataPath + "/" + cslamName;
            API.xslam_save_map_and_switch_to_cslam(mapPath, savedCallback, saveLocalizedCallback);
            MyDebugTool.Log("Save map 2: "+ mapPath);

            return mapPath;
        }

        /// <summary>
        /// Load an existing map
        /// </summary>
        /// <param name="mapPath"></param>
        public void LoadSlamMap(string mapPath)
        {
            if (!NativeReady()) { Debug.LogWarning("SpatialMap: SDK is not ready."); return; }
            MyDebugTool.Log("Load map 1: " + mapPath);

            API.xslam_reset_slam();         
            API.xslam_load_map_and_switch_to_cslam(mapPath, switchedCallback, loadLocalizedCallback);
            MyDebugTool.Log("Load map 2: " + mapPath);

        }


        private bool showFeaturePoint;
        public bool ShowFeaturePoint { 
           get { return showFeaturePoint; } 
           
        }
        public void SwitchFeaturePointState() {
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
                if (!NativeReady() || !ShowFeaturePoint)
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
          
            pendingCallbacks.Enqueue(() => onMapSaveCompleteEvent?.Invoke(status_of_saved_map, map_quality));

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
            pendingCallbacks.Enqueue(() => onMapLoadCompleteEvent?.Invoke(map_quality));
        }

        /// <summary>
        /// Loaded-map localization quality callback implementation
        /// </summary>
        /// <param name="percentc">0~1</param>
        [MonoPInvokeCallback(typeof(API.detectLocalized_callback))]
        static void OnLoadLocalized(float percentc)
        {
            similarity = percentc;
            pendingCallbacks.Enqueue(() => onMapMatchingEvent?.Invoke(percentc));
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
