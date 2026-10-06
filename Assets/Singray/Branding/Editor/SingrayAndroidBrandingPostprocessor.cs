#if UNITY_EDITOR && UNITY_ANDROID
using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Singray.Branding.Editor
{
    /// <summary>
    /// Copies Singray-owned Android resources into the generated unityLibrary
    /// module. Resources in unityLibrary override resources with the same name
    /// from vendor AAR dependencies without modifying those AAR files.
    /// </summary>
    public sealed class SingrayAndroidBrandingPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string BrandingSourceRelativePath = "Singray/Branding/Android";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string sourceRoot = Path.Combine(Application.dataPath, BrandingSourceRelativePath);
            string unityLibraryRoot = ResolveUnityLibraryRoot(path);
            string resourceRoot = Path.Combine(unityLibraryRoot, "src", "main", "res", "mipmap");

            CopyBrandingResource(sourceRoot, resourceRoot, "xvlogo.png");
            CopyBrandingResource(sourceRoot, resourceRoot, "xvwait.jpg");

            Debug.Log($"Singray Android branding resources applied to: {resourceRoot}");
        }

        private static string ResolveUnityLibraryRoot(string path)
        {
            if (Directory.Exists(Path.Combine(path, "src", "main")))
            {
                return path;
            }

            string unityLibraryPath = Path.Combine(path, "unityLibrary");
            if (Directory.Exists(Path.Combine(unityLibraryPath, "src", "main")))
            {
                return unityLibraryPath;
            }

            throw new DirectoryNotFoundException(
                $"Unable to locate the generated unityLibrary module from '{path}'.");
        }

        private static void CopyBrandingResource(string sourceRoot, string destinationRoot, string fileName)
        {
            string sourcePath = Path.Combine(sourceRoot, fileName);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    $"Singray Android branding resource is missing: {sourcePath}",
                    sourcePath);
            }

            Directory.CreateDirectory(destinationRoot);
            File.Copy(sourcePath, Path.Combine(destinationRoot, fileName), true);
        }
    }
}
#endif
