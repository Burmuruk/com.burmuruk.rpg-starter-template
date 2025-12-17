using UnityEditor;
using UnityEngine;
using System.IO;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class RPGStarterSetupMenu : EditorWindow
    {
        [MenuItem("RPGTemplate/Copy Core Assets", priority = 15)]
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
        static string SourcePath
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RPGStarterSetup).Assembly);
                return Path.Combine(package.assetPath, "CoreAssets");
            }
        }

        public static string TargetPath { get => Path.Combine(Application.dataPath, "CoreAssets"); }

        static RPGStarterSetup()
        {
            Debug.Log("Initialized");

            if (!Directory.Exists(TargetPath) && (!EditorPrefs.HasKey(copyPref) || !EditorPrefs.GetBool(copyPref)))
            {
                EditorApplication.delayCall += StartCopy;
            }

        }

        private static void StartCopy()
        {
            if (!Directory.Exists(TargetPath) && (!EditorPrefs.HasKey(copyPref) || !EditorPrefs.GetBool(copyPref)))
            {
                CopyFiles();
                EditorPrefs.SetBool(copyPref, true);
            }
            else
                EditorApplication.delayCall -= StartCopy;
        }

        public static void CopyFiles()
        {
            if (EditorUtility.DisplayDialog("RPG Starter Template",
                    "Do you want to copy the base files to Assets/CoreAssets?",
                    "yes, copy", "No"))
            {
                FileUtil.CopyFileOrDirectoryFollowSymlinks(SourcePath, TargetPath);
                AssetDatabase.Refresh();
                Debug.Log("RPG Starter Template: CoreAssets copied to Assets/");
            }
        }
    }
}
