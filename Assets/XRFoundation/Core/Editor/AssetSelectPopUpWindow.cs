using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;


public class AssetSelectPopUpWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> items = null;
    //Whether to export scripts
    public static bool exportWithScript = false;
    private bool[] selectionStates;


    #region Editor menu


    [MenuItem("Assets/Tools/Export Unity Package")]
    public static void ExportWithoutScript()
    {
        exportWithScript = false;
        ShowWindow();
    }


    [MenuItem("Assets/Tools/Export Unity Package (Include Scripts)")]
    public static void ExportWithScript()
    {
        exportWithScript = true;
        ShowWindow();
    }

    public static void ShowWindow()
    {
        AssetSelectPopUpWindow wnd = GetWindow<AssetSelectPopUpWindow>();
        wnd.titleContent = new GUIContent("Asset Export");
        wnd.minSize = new Vector2(450, 200);
        wnd.maxSize = new Vector2(1920, 720);
        wnd.Show();
    }
    #endregion




    public void GetAllFiles(bool withScript)
    {
        //Get all selected files
        Object[] selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
        List<string> assetPathNames = new List<string>();
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            string directoryPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
            if (directoryPath != null)
            {
                //For a folder, enumerate all assets inside it
                if (Directory.Exists(directoryPath))
                {
                    string[] folders = Directory.GetFiles(directoryPath);
                    for (int j = 0; j < folders.Length; j++)
                    {
                        //Exclude .meta files
                        if (!folders[j].EndsWith(".meta"))
                        {
                            assetPathNames.Add(folders[j]);
                        }
                    }
                }
                else
                {
                    assetPathNames.Add(directoryPath);
                }
            }
        }

        items = new List<string>();

        for (int i = 0; i < assetPathNames.Count; i++)
        {
            var depends = AssetDatabase.GetDependencies(assetPathNames[i], true);
            for (int j = 0; j < depends.Length; j++)
            {
                AddFiles(withScript, depends[j]);
            }
        }


        items.Sort();
        selectionStates = new bool[items.Count];
        //Select all by default
        SelectAllItems();

    }


    private void OnEnable()
    {
        GetAllFiles(exportWithScript);
    }

    //Print all selected files
    private void ShowFiles()
    {
        for (int i = 0; i < items.Count; i++)
        {
            Debug.Log($"all Files is {items[i]}");
        }
    }
    private void AddFiles(bool withScript, string filePath)
    {
        //Skip excluded directories
        if (filePath.StartsWith("Packages/"))
        {
            return;
        }
        if (withScript || !filePath.EndsWith(".cs"))
        {
            if (!items.Contains(filePath))
            {
                items.Add(filePath);
            }
        }
    }
    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // Scroll view
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
        {
            scrollPosition = scrollView.scrollPosition;

            for (int i = 0; i < items.Count; i++)
            {
                selectionStates[i] = EditorGUILayout.ToggleLeft(items[i], selectionStates[i]);
            }
        }

        GUILayout.Space(10);

        // Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            SelectAllItems();
        }
        if (GUILayout.Button("Deselect All"))
        {
            DeselectAllItems();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Export"))
        {
            OutputSelectedItems();

        }
    }
    #region Button event
    private void SelectAllItems()
    {
        for (int i = 0; i < selectionStates.Length; i++)
        {
            selectionStates[i] = true;
        }
    }

    private void DeselectAllItems()
    {
        for (int i = 0; i < selectionStates.Length; i++)
        {
            selectionStates[i] = false;
        }
    }

    private void OutputSelectedItems()
    {
        List<string> exportItems = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            if (selectionStates[i])
            {
                exportItems.Add(items[i]);
            }
        }
        if (exportItems.Count == 0)
        {
            EditorUtility.DisplayDialog("No Assets Selected", "Select the assets to export.", "OK");
            return;
        }
        var path = EditorUtility.SaveFilePanel("Export Package", "", "", "unitypackage");
        if (path == "")
            return;

        var flag = ExportPackageOptions.Interactive | ExportPackageOptions.Recurse;
        //Add ExportPackageOptions.IncludeDependencies to export referenced assets as well
        //if (exportWithScript)
        //{
        //    flag = flag | ExportPackageOptions.IncludeDependencies;
        //}
        AssetDatabase.ExportPackage(exportItems.ToArray(), path, flag);
        Close();

    }
    #endregion



}