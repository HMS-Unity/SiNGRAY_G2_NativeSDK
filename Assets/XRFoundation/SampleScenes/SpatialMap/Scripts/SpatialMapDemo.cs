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
        private bool pendingSave, pendingLoad;

        public Button mapItem;

        private void Awake()
        {
            similarityText = transform.Find("UI/Canvas/Similarity").GetComponent<Text>();
            logText = transform.Find("UI/Canvas/LogText").GetComponent<Text>();
            featurePointText=transform.Find("UI/Canvas/FeaturePoint/IconAndText/TextMeshPro").GetComponent<TextMeshPro>();

            if (xvSpatialMapManager == null)
            {
                xvSpatialMapManager = GetComponentInChildren<XvSpatialMapManager>(true);
                if(xvSpatialMapManager==null) xvSpatialMapManager = FindObjectOfType<XvSpatialMapManager>();
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
                            if (!file_info.Exists || file_info.Length==0)
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
                        mapContentInfos = new List<MapContentInfo>();
                        MyDebugTool.Log("No saved spatial maps yet.");
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
            pendingSave=pendingLoad=false;
            if(featurePointParticle!=null){featurePointParticle.Clear();featurePointParticle.Stop();featurePointParticle.gameObject.SetActive(false);}
            allParticles=null;
        }

        private string lastResetStatus;
        private void Update()
        {
            if(xvSpatialMapManager==null) return;
            if (lastResetStatus != xvSpatialMapManager.OperationStatus)
            {
                lastResetStatus = xvSpatialMapManager.OperationStatus;
                Log(lastResetStatus);
            }
            if (xvSpatialMapManager.IsBusy)
            {
                Log(xvSpatialMapManager.OperationStatus);
                return;
            }
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
            if(xvSpatialMapManager==null) return;
            xvSpatialMapManager.StartSlamMap();
            if (xvSpatialMapManager.IsResetting)
            {
                if (featurePointParticle != null) { featurePointParticle.Clear(); featurePointParticle.gameObject.SetActive(false); }
                if (featurePointText != null) featurePointText.text = "ShowFeaturePoint";
            }
            Log(xvSpatialMapManager.OperationStatus);
        }

        public void SaveMap()
        {
            if(xvSpatialMapManager==null) return;
            if (xvSpatialMapManager.IsBusy) { Log(xvSpatialMapManager.OperationStatus); return; }
            string requestedPath=xvSpatialMapManager.SaveSlamMap();
            if(string.IsNullOrEmpty(requestedPath)){Log(xvSpatialMapManager.OperationStatus);return;}
            map_Path=requestedPath;pendingSave=true;pendingLoad=false;
            Log("Saving map; waiting for native callback");
        }

        public void SwitchFeaturePointState() {
            if(xvSpatialMapManager==null) return;
          
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
            if(featurePointParticle==null || drawList==null) return;
            featurePointParticle.gameObject.SetActive(true);
            var main=featurePointParticle.main;
            int count=drawList.Count;
            if(allParticles==null || allParticles.Length<count) allParticles=new ParticleSystem.Particle[count];
            if(count>0 && main.maxParticles!=count) main.maxParticles=count;
            float lifetime=Mathf.Max(1,main.startLifetime.constantMax);
            var color=main.startColor.color;float size=main.startSize.constantMax;
            for(int i=0;i<count;i++) allParticles[i]=new ParticleSystem.Particle{position=drawList[i],startColor=color,startSize=size,startLifetime=lifetime,remainingLifetime=lifetime};
            if(count>0 && !featurePointParticle.isPaused){featurePointParticle.Play(false);featurePointParticle.Pause(false);}
            featurePointParticle.SetParticles(allParticles,count);
        }

        private void onSaveComplete(int status_of_saved_map, int map_quality)
        {
            if(!pendingSave || xvSpatialMapManager==null || xvSpatialMapManager.CompletedSavePath!=map_Path) return;
            pendingSave=false;
            lastResetStatus=xvSpatialMapManager.OperationStatus;
            if(string.IsNullOrEmpty(map_Path)||!File.Exists(map_Path)||new FileInfo(map_Path).Length==0)
            {Log("Save callback received, but map file is missing or empty. status="+status_of_saved_map+" quality="+map_quality);return;}
            Log("Map file ready. Native status="+status_of_saved_map+" quality="+map_quality);
            SaveGameobject();

            RefrashUI();
          
        }

        private void onLoadComplete(int map_quality)
        {
            if(!pendingLoad || xvSpatialMapManager==null || xvSpatialMapManager.CompletedLoadPath!=map_Path) return;
            pendingLoad=false;
            lastResetStatus=xvSpatialMapManager.OperationStatus;
            MyDebugTool.Log("Map loading completed");
            Log("Load callback received. Native quality="+map_quality+"; matching is reported separately.");

            LoadGameobject();

        }
        private void onMapSimilarity(float similarity)
        {

            similarityText.text = "similarity：" + similarity;


        }
        private void SaveGameobject()
        {



            List<GameObjectInfo> gameObjectInfoList = new List<GameObjectInfo>();
            for (int i = 0; saveGameObject!=null && i < saveGameObject.Count; i++)
            {
                if(saveGameObject[i]==null) continue;
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

            if (mapContentInfo != null && mapContentInfo.gameObjectInfos!=null && saveGameObject!=null)
            {

                for (int i = 0; i < mapContentInfo.gameObjectInfos.Count; i++)
                {
                    for (int j = 0; j < saveGameObject.Count; j++)
                    {
                        if(saveGameObject[j]==null) continue;

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

                    if (File.Exists(mapContentInfos[i].mapPath) && new FileInfo(mapContentInfos[i].mapPath).Length>0)
                    {
                        MyDebugTool.Log("RefrashUI" + mapContentInfos.Count);
                        Button button = Instantiate(mapItem.gameObject, mapItem.transform.parent).GetComponent<Button>();
                        string map = mapContentInfos[i].mapPath;
                        button.onClick.AddListener(() =>
                        {

                            if(xvSpatialMapManager==null) return;
                            if(xvSpatialMapManager.IsBusy){Log(xvSpatialMapManager.OperationStatus);return;}
                            if(!File.Exists(map)||new FileInfo(map).Length==0){Log("Map file is missing or empty.");return;}
                            map_Path = map;pendingLoad=true;pendingSave=false;
                            xvSpatialMapManager.LoadSlamMap(map_Path);
                            pendingLoad=xvSpatialMapManager.IsBusy;
                            Log(xvSpatialMapManager.OperationStatus);


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
