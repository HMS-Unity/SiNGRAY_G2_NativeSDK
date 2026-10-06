using UnityEngine;
using System.Collections;
using System.IO;
public class Function
{

    //Get the project name
    public static string projectName
    {
        get
        {
            //Parse shell arguments here, including the project-$1 argument
            //Find the argument starting with project and return the substring after the hyphen
            //In this example, the resulting string is 91
            foreach (string arg in System.Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("project"))
                {
                    return arg.Split("-"[0])[1];
                }
            }
            return "test";
        }
    }

    public static void DeleteFolder(string dir)
    {
        FileInfo fi = new FileInfo(dir);
        File.Delete(dir);
    }

    public static void CopyDirectory(string sourcePath, string destinationPath)
    {
        DirectoryInfo info = new DirectoryInfo(sourcePath);
        Directory.CreateDirectory(destinationPath);
        foreach (FileSystemInfo fsi in info.GetFileSystemInfos())
        {
            string destName = Path.Combine(destinationPath, fsi.Name);
            if (fsi is System.IO.FileInfo)
                File.Copy(fsi.FullName, destName);
            else
            {
                Directory.CreateDirectory(destName);
                CopyDirectory(fsi.FullName, destName);
            }
        }
    }
}