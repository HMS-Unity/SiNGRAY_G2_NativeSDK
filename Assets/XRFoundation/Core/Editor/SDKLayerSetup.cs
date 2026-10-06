#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SDKLayerSetup : AssetPostprocessor
{
    private static readonly string[] TargetLayers = { "BGVideo", "XvBGVideo" };

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool sdkImported = false;
        foreach (string asset in importedAssets)
        {
            if (asset.Contains("XRFoundation") || asset.Contains("XvXRFoundation"))
            {
                sdkImported = true;
                break;
            }
        }

        if (sdkImported)
        {
            AddLayersIfMissing();
        }
    }

    public static void AddLayersIfMissing()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        foreach (string targetLayer in TargetLayers)
        {
            AddLayerIfMissing(tagManager, layers, targetLayer);
        }
    }

    static void AddLayerIfMissing(SerializedObject tagManager, SerializedProperty layers, string targetLayer)
    {
        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
            if (layerProp.stringValue == targetLayer)
            {
                return;
            }
        }

        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = targetLayer;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[SDK] Created layer: {targetLayer} at index {i}");
                return;
            }
        }

        Debug.LogError($"[SDK] Failed to create layer {targetLayer}! All user layers are in use.");
    }
}
#endif
