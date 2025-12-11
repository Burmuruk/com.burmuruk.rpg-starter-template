using UnityEditor;
using UnityEngine;
using System.IO;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class RPGStarterSetupMenu : EditorWindow
    {
        [MenuItem("RPGTemplate/Copy Architecture", priority = 15)]
        public static void ShowWindow()
        {
            RPGStarterSetup.CopyFiles();
        }
    }

    [InitializeOnLoad]
    public static class RPGStarterSetup
    {
        static readonly string sourcePath = "Packages/com.Burmuruk.RPG-Starter-Template/GameArchitecture";
        static readonly string targetPath = "Assets/GameArchitecture";
        const string copyPref = "CopyRPGFiles";

        static RPGStarterSetup()
        {
            Debug.Log("Initialized");
            if (!Directory.Exists(targetPath))
            {
                //if (!EditorPrefs.HasKey(copyPref) || !EditorPrefs.GetBool(copyPref))
                //    return;

                EditorPrefs.SetBool(copyPref, CopyFiles());
            }
        }

        public static bool CopyFiles()
        {
            if (EditorPrefs.HasKey(copyPref))
            {
                EditorPrefs.DeleteKey(copyPref);
                Debug.Log("RPG Starter Template: Copy preference reset. You can copy the files again from the RPGTemplate menu.");
                return true;
            }

            if (EditorUtility.DisplayDialog("RPG Starter Template",
                    "Do you want to copy the base files to Assets/GameArchitecture?",
                    "yes, copy", "No"))
            {
                FileUtil.CopyFileOrDirectoryFollowSymlinks(sourcePath, targetPath);
                AssetDatabase.Refresh();
                Debug.Log("RPG Starter Template: GameArchitecture copied to Assets/");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
