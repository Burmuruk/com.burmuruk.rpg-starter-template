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
            EditorPrefs.SetBool(RPGStarterSetup.copyPref, true);
        }
    }

    [InitializeOnLoad]
    public static class RPGStarterSetup
    {
        public const string copyPref = "CopyRPGFiles";
        static readonly string sourcePath = "Packages/com.Burmuruk.RPG-Starter-Template/GameArchitecture";

        public static string TargetPath { get => Path.Combine(Application.dataPath, "GameArchitecture"); }

        static RPGStarterSetup()
        {
            Debug.Log("Initialized");

            if (!Directory.Exists(TargetPath))
            {
                EditorApplication.delayCall += CopyFiles;
            }

        }

        public static void CopyFiles()
        {
            if (EditorPrefs.HasKey(copyPref))
            {
                EditorPrefs.DeleteKey(copyPref);
                Debug.Log("RPG Starter Template: Copy preference reset. You can copy the files again from the RPGTemplate menu.");
            }

            if (EditorUtility.DisplayDialog("RPG Starter Template",
                    "Do you want to copy the base files to Assets/GameArchitecture?",
                    "yes, copy", "No"))
            {
                FileUtil.CopyFileOrDirectoryFollowSymlinks(sourcePath, TargetPath);
                AssetDatabase.Refresh();
                EditorPrefs.SetBool(copyPref, true);
                Debug.Log("RPG Starter Template: GameArchitecture copied to Assets/");
            }
            else
            {
                EditorPrefs.SetBool(copyPref, false);
            }
        }
    }
}
