using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundleEditor : Editor
{

	[MenuItem("Assetbundle/MakeCubeForWin64")]
	public static void MakeCubeForWin64()
	{

		// AssetBundle output directory
		string path = Path.Combine(Application.streamingAssetsPath, "Assetbundles/win64");

		// Check whether the directory exists
		if (!Directory.Exists(path))
		{
			System.IO.Directory.CreateDirectory(path);
		}

		// Build AssetBundles from prefabs with AssetBundle labels assigned in the Inspector
		// Arguments: 1 = output location, 2 = compression mode, 3 = target platform
		BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
	}

	[MenuItem("Assetbundle/MakeCubeForAndroid")]
	public static void MakeCubeForAndroid()
	{

		// AssetBundle output directory
		string path = Path.Combine(Application.streamingAssetsPath, "Assetbundles/android");

		// Check whether the directory exists
		if (!Directory.Exists(path))
		{
			System.IO.Directory.CreateDirectory(path);
		}
		// Build AssetBundles from prefabs with AssetBundle labels assigned in the Inspector
		// Arguments: 1 = output location, 2 = compression mode, 3 = target platform
		BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.Android);
	}
}
