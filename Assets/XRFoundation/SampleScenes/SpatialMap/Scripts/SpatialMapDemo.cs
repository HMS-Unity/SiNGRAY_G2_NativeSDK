using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{

    public class MapContentInfo
    {
        public string mapPath;
        public List<GameObjectInfo> gameObjectInfos;
    }
    public class GameObjectInfo
    {
        public float t_x = 0;
        public float t_y = 0;
        public float t_z = 0;
        public string o_name = "";
        public float r_x = 0;
        public float r_y = 0;
        public float r_z = 0;
        public float s_x = 0;
        public float s_y = 0;
        public float s_z = 0;
    }

    public class SpatialMapDemo : MonoBehaviour
    {
        private List<MapContentInfo> mapContentInfos = new List<MapContentInfo>();

        [SerializeField]
        private XvSpatialMapManager xvSpatialMapManager;
        [SerializeField]
        private List<GameObject> saveGameObject;
        [SerializeField]
        private ParticleSystem featurePointParticle;


        [SerializeField, Tooltip("Show feature points")]
        private bool showFeaturePoint;

        private TextMeshPro featurePointText;

        private Text similarityText;
        private Text logText;
        private ParticleSystem.Particle[] allParticles;
        private string map_Path;

        public Button mapItem;

        private void Awake()
        {
            similarityText = transform.Find("UI/Canvas/Similarity").GetComponent<Text>();
            logText = transform.Find("UI/Canvas/LogText").GetComponent<Text>();
            featurePointText=transform.Find("UI/Canvas/FeaturePoint/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();

            if (xvSpatialMapManager == null)
            {
                xvSpatialMapManager = FindObjectOfType<XvSpatialMapManager>();
            }

            if (xvSpatialMapManager == null)
            {
                MyDebugTool.LogError("xvSpatialMapManager==null");
                return;
            }

            if (!xvSpatialMapManager.ShowFeaturePoint && showFeaturePoint)
            {
                xvSpatialMapManager.SwitchFeaturePointState();
                featurePointText.text = "HideFeaturePoint";
                featurePointParticle.gameObject.SetActive(true);

            }
            else
            {
                featurePointText.text = "ShowFeaturePoint";
                featurePointParticle.gameObject.SetActive(false);


            }


        }
        private void Start()
        {
          

                try
                {
                  

                    string data = GetText(Application.persistentDataPath, "CslamMap.txt");

                    mapContentInfos = JsonConvert.DeserializeObject<List<MapContentInfo>>(data);
                    if (mapContentInfos != null)
                    {
                        MyDebugTool.Log("cslam map data:" + data + "  " + mapContentInfos.Count);
                       
                        for (int i= mapContentInfos.Count - 1; i >= 0; i--)
                        {

                            FileInfo file_info = new FileInfo(mapContentInfos[i].mapPath);
                            if (!file_info.Exists)
                            {
                                MyDebugTool.LogError("delete：" + mapContentInfos[i].mapPath);
                                mapContentInfos.RemoveAt(i);
                            }
                            else
                            {

                            }

                        }
                        RefrashUI();
                    }
                    else {
                        MyDebugTool.LogError("mapContentInfos is null");
                    }


                }
                catch (Exception e)
                {

                    MyDebugTool.LogError("MapContentInfo Exception:" + e.Message);
                }


           


        }

        private void OnEnable()
        {
            XvSpatialMapManager.onMapSaveCompleteEvent.AddListener(onSaveComplete);
            XvSpatialMapManager.onMapLoadCompleteEvent.AddListener(onLoadComplete);
            XvSpatialMapManager.onMapMatchingEvent.AddListener(onMapSimilarity);
        }

        private void OnDisable()
        {
            XvSpatialMapManager.onMapSaveCompleteEvent.RemoveListener(onSaveComplete);
            XvSpatialMapManager.onMapLoadCompleteEvent.RemoveListener(onLoadComplete);
            XvSpatialMapManager.onMapMatchingEvent.RemoveListener(onMapSimilarity);
        }

        private void Update()
        {
            if (xvSpatialMapManager.ShowFeaturePoint)
            {
                if (Time.frameCount % 5 == 0)
                {
                    if (xvSpatialMapManager != null)
                    {
                        List<Vector3> pointList = xvSpatialMapManager.GetFeaturePoint();

                        if (pointList != null)
                        {
                            DrawPointCloud(pointList);
                        }
                    }

                }
            }

           


        }


        public void CreateMap()
        {
            xvSpatialMapManager.StartSlamMap();
            Log ("Scan map");
        }

        public void SaveMap()
        {
            map_Path = xvSpatialMapManager.SaveSlamMap();

            Log("Saving map");
        }

        public void SwitchFeaturePointState() {
          
            xvSpatialMapManager.SwitchFeaturePointState();

            if (xvSpatialMapManager.ShowFeaturePoint)
            {
                featurePointText.text = "HideFeaturePoint";
                featurePointParticle.gameObject.SetActive(true);

            }
            else { 
                featurePointText.text = "ShowFeaturePoint";
                featurePointParticle.gameObject.SetActive(false);


            }
        }
        private void Log(string msg) {
            logText.text = "Log:" + msg;
        }
        private void DrawPointCloud(List<Vector3> drawList)
        {
            featurePointParticle.gameObject.SetActive(true);
            if (featurePointParticle != null)
            {
                featurePointParticle.Clear();
                // particleSystem = null;
                System.GC.Collect();
            }

            var main = featurePointParticle.main;
            main.startSpeed = 0.0f;                           // Set the initial particle velocity to zero
            main.startLifetime = 1000000.0f;

            var pointCount = drawList.Count;
            allParticles = new ParticleSystem.Particle[pointCount];
            main.maxParticles = pointCount;
            featurePointParticle.Emit(pointCount);
            featurePointParticle.GetParticles(allParticles);

            for (int i = 0; i < pointCount; i++)
            {
                allParticles[i].position = (Vector3)drawList[i];

            }

            featurePointParticle.SetParticles(allParticles, pointCount);      // Load the point cloud into the particle system

        }


        private void onSaveComplete(int status_of_saved_map, int map_quality)
        {
            Log("Map saved successfully");
            MyDebugTool.Log("Map saved successfully");
            SaveGameobject();

            RefrashUI();
          
        }

        private void onLoadComplete(int map_quality)
        {
            MyDebugTool.Log("Map loading completed");
            Log("Map loading completed");

            LoadGameobject();

        }
        private void onMapSimilarity(float similarity)
        {

            similarityText.text = "similarity：" + similarity;


        }
        private void SaveGameobject()
        {



            List<GameObjectInfo> gameObjectInfoList = new List<GameObjectInfo>();
            for (int i = 0; i < saveGameObject.Count; i++)
            {
                GameObjectInfo objInfo = new GameObjectInfo();
                objInfo.t_x = saveGameObject[i].transform.position.x;
                objInfo.t_y = saveGameObject[i].transform.position.y;
                objInfo.t_z = saveGameObject[i].transform.position.z;


                objInfo.r_x = saveGameObject[i].transform.eulerAngles.x;
                objInfo.r_y = saveGameObject[i].transform.eulerAngles.y;
                objInfo.r_z = saveGameObject[i].transform.eulerAngles.z;

                objInfo.s_x = saveGameObject[i].transform.localScale.x;
                objInfo.s_y = saveGameObject[i].transform.localScale.y;
                objInfo.s_z = saveGameObject[i].transform.localScale.z;

                objInfo.o_name = saveGameObject[i].name;
                gameObjectInfoList.Add(objInfo);
            }


            MapContentInfo mapContentInfo = new MapContentInfo();
            mapContentInfo.mapPath = map_Path;
            mapContentInfo.gameObjectInfos = gameObjectInfoList;


            if (mapContentInfos == null)
            {
                MyDebugTool.Log("mapContentInfos==null");

                mapContentInfos = new List<MapContentInfo>();
            }
            mapContentInfos.Add(mapContentInfo);



            try
            {
                string data = JsonConvert.SerializeObject(mapContentInfos);

                MyDebugTool.Log("  data==" + data);
                WriteText(Application.persistentDataPath, "CslamMap.txt", data);
            }
            catch (Exception ex)
            {
                MyDebugTool.LogError("Save Gameobject Exception" + ex.Message);

            }



        }


        private void LoadGameobject()
        {

            MapContentInfo mapContentInfo = null;
            if (mapContentInfos != null)
            {
                for (int i = 0; i < mapContentInfos.Count; i++)
                {
                    if (mapContentInfos[i].mapPath == map_Path)
                    {
                        mapContentInfo = mapContentInfos[i];
                        break;
                    }
                }
            }
            else
            {
                MyDebugTool.Log("mapContentInfos==null");
            }

            if (mapContentInfo != null)
            {

                for (int i = 0; i < mapContentInfo.gameObjectInfos.Count; i++)
                {
                    for (int j = 0; j < saveGameObject.Count; j++)
                    {

                        if (saveGameObject[j].name == mapContentInfo.gameObjectInfos[i].o_name)
                        {

                            saveGameObject[j].transform.position = new Vector3(mapContentInfo.gameObjectInfos[i].t_x, mapContentInfo.gameObjectInfos[i].t_y, mapContentInfo.gameObjectInfos[i].t_z);
                            saveGameObject[j].transform.eulerAngles = new Vector3(mapContentInfo.gameObjectInfos[i].r_x, mapContentInfo.gameObjectInfos[i].r_y, mapContentInfo.gameObjectInfos[i].r_z);
                            saveGameObject[j].transform.localScale = new Vector3(mapContentInfo.gameObjectInfos[i].s_x, mapContentInfo.gameObjectInfos[i].s_y, mapContentInfo.gameObjectInfos[i].s_z);

                            continue;
                        }

                    }

                }

            }

            else
            {
                MyDebugTool.Log("mapContentInfo==null");

            }
        }




        public void WriteText(string file_path, string file_name, string str_info)
        {
            MyDebugTool.LogError(file_path + "//" + file_name);
            StreamWriter sw;
            FileInfo file_info = new FileInfo(file_path + "//" + file_name);
            if (!file_info.Exists)
            {
                sw = file_info.CreateText();
            }
            else
            {
                sw = file_info.CreateText();
            }
            sw.Write(str_info);
            sw.Close();
            sw.Dispose();
        }

        private string GetText(string file_path, string file_name)
        {
            string str_info = "";
            StreamWriter sw;
            FileInfo file_info = new FileInfo(file_path + "//" + file_name);
            if (!file_info.Exists)
            {
                sw = file_info.CreateText();
                sw.Write(str_info);
                sw.Close();
                sw.Dispose();
            }

            string result = string.Empty;
            try
            {
                FileInfo file = new FileInfo(file_path + "//" + file_name);

                StreamReader streamReader = file.OpenText();
                result = streamReader.ReadToEnd();

                streamReader.Close();
                streamReader.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }



        private List<Button> mapItemList = new List<Button>();
        private void RefrashUI()
        {

            for (int i = 0; i < mapItemList.Count; i++)
            {
                Destroy(mapItemList[i].gameObject);
            }
            mapItemList.Clear();

            if (mapContentInfos != null)
            {


                for (int i = 0; i < mapContentInfos.Count; i++)
                {

                    if (File.Exists(mapContentInfos[i].mapPath))
                    {
                        MyDebugTool.Log("RefrashUI" + mapContentInfos.Count);
                        Button button = Instantiate(mapItem.gameObject, mapItem.transform.parent).GetComponent<Button>();
                        string map = mapContentInfos[i].mapPath;
                        button.onClick.AddListener(() =>
                        {

                            map_Path = map;
                            xvSpatialMapManager.LoadSlamMap(map_Path);
                            Log("loading map：" + map_Path);


                        });
                        button.transform.GetComponentInChildren<Text>().text = mapContentInfos[i].mapPath;
                        button.gameObject.SetActive(true);
                        mapItemList.Add(button);
                        MyDebugTool.Log("RefrashUI" + mapContentInfos.Count);
                    }

                }
            }
        }



    }


}
