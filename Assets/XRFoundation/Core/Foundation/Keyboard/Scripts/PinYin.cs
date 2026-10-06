using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Singray.UI.Keyboard {
public sealed class PinYin
{
#if UNITY_EDITOR
    string path = Application.persistentDataPath + "/" + "pinyin.csv";


#else
string path = "sdcard/" + "pinyin.csv";
#endif


    private static PinYin instance;
    public static PinYin Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PinYin();
            }
            return instance;
        }
    }

    private Dictionary<string, StringBuilder> hanziDic = new Dictionary<string, StringBuilder>();


    private PinYin()
    {
        CSVFile cSVFile;
        if (File.Exists(path))
        {
            cSVFile = new CSVFile(path);
           
        }
        else
        {
            TextAsset textAsset = Resources.Load<TextAsset>("Files/pinyin");
            cSVFile = new CSVFile(textAsset.bytes, Encoding.UTF8);
            MyDebugTool.LogError(textAsset.bytes.Length);
        }
        for (int i = 0; i < cSVFile.RowCount; i++)
        {
            StringBuilder stringBuilder = new StringBuilder(cSVFile.GetValue(i, 1));
            hanziDic.Add(cSVFile.GetValue(i, 0), stringBuilder);
        }
    }

    private IEnumerator readeFile(string path) {
        WWW www = new WWW(path);
        yield return www;

        //www.bytes;
    
    }

    public string GetHanZi(string pinyin)
    {

        if (hanziDic.TryGetValue(pinyin, out StringBuilder hanzi))
        {

            return hanzi.ToString();
        }
        return null;
    }
    public void SaveHotWord()
    {
        if (!File.Exists(path))
        {
            MyDebugTool.Log("The file path does not exist" + path);
            File.Create(path).Close();
        }
        StringBuilder stringBuilder = new StringBuilder(4096);

        foreach (var item in hanziDic.Keys)
        {

            stringBuilder.AppendFormat("{0},{1}\n", item, hanziDic[item]);
        }


        File.WriteAllText(path, stringBuilder.ToString());
        stringBuilder.Clear();
        stringBuilder = null;
        MyDebugTool.Log("End of Save");
    }

    public void UpdateHotWord(string key, int index, string word)
    {
        if (index > 0)
        {
            if (hanziDic.TryGetValue(key, out StringBuilder hanzi))
            {
                hanzi.Remove(index, 1);
                hanzi.Insert(0, word);
            }
        }

    }

    public  void UpdateHotWord(char word)
    {
        string key=  NPinyin.Pinyin.GetPinyin(word);
     
        int index = 0;
        if (hanziDic.TryGetValue(key, out StringBuilder hanzi))
        {
            int i;
            for ( i = 0; i < hanzi.Length; i++)
            {
                if (hanzi[i]==(word))
                {
                   
                    index = i;
                    break;
                }
            }

            if (i >= hanzi.Length&& key != word.ToString())
            {
                hanzi.Insert(0, word);
            
            }
            else {
                if (index>0) {
                    hanzi.Remove(index, 1);
                    hanzi.Insert(0, word);
                }
               
            }
         
        }
    }

    public string GetPinYin(char word) {

        foreach (var item in hanziDic.Keys)
        {
            StringBuilder hanzi = hanziDic[item];


            for (int i = 0; i < hanzi.Length; i++)
            {
                if (hanzi[i] == (word))
                {

                    return item;
                 
                }
            }

        }

        MyDebugTool.Log("No Chinese characters found£º" + word);
        return null;

    }
}
}
