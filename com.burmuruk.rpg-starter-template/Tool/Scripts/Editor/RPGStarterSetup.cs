using UnityEditor;
using UnityEngine;
using System.IO;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    [InitializeOnLoad]
    public static class RPGStarterSetup
    {
        static readonly string sourcePath = "Packages/com.Burmuruk.RPG-Starter-Template/GameArchitecture";
        static readonly string targetPath = "Assets/GameArchitecture";
        const string copyPref = "CopyRPGFiles";

        static RPGStarterSetup()
        {
            if (!AssetDatabase.IsValidFolder("Assets/RPG-Results"))
            {
                if (!EditorPrefs.HasKey(copyPref) || !EditorPrefs.GetBool(copyPref))
                    return;

                AssetDatabase.CreateFolder("Assets/", "RPG-Results");
                AssetDatabase.Refresh();
            }

            if (!Directory.Exists(targetPath))
            {
                if (!EditorPrefs.HasKey(copyPref) || !EditorPrefs.GetBool(copyPref))
                    return;

                CopyFiles();
            }
        }

        static void CopyFiles()
        {
            if (EditorUtility.DisplayDialog("RPG Starter Template",
                    "Do you want to copy the base files to Assets/GameArchitecture?",
                    "yes, copy", "No"))
            {
                FileUtil.CopyFileOrDirectoryFollowSymlinks(sourcePath, targetPath);
                AssetDatabase.Refresh();
                Debug.Log("RPG Starter Template: GameArchitecture copied to Assets/");
                EditorPrefs.SetBool(copyPref, true);
            }
            else
            {
                EditorPrefs.SetBool(copyPref, false);
            }
        }
    }
}
