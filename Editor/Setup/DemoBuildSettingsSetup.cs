#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.EditorSetup
{
    public static class DemoBuildSettingsSetup
    {
        private static readonly string[] DemoSceneNames =
        {
            "MainMenuSample",
            "MenuCharactersScene",
            "OutdoorsScene",
            "Station"
        };

        [MenuItem("RPGTemplate/Setup/Add Demo Scenes to Build", priority = 31)]
        public static void AddDemoScenesToBuild()
        {
            List<EditorBuildSettingsScene> buildScenes, addedScenes;
            DisableCurrentScenes(out buildScenes, out addedScenes);

            if (!GetDemoScenes(addedScenes)) return;

            buildScenes = SortScenes(buildScenes, addedScenes);

            EditorBuildSettings.scenes = buildScenes.ToArray();

            Debug.Log("Demo scenes added to Build Settings using scene names and fixed order.");
        }

        private static List<EditorBuildSettingsScene> SortScenes(List<EditorBuildSettingsScene> buildScenes, List<EditorBuildSettingsScene> addedScenes)
        {
            foreach (var scene in addedScenes)
            {
                var existing = buildScenes.FirstOrDefault(s => s.path == scene.path);
                if (existing != null)
                {
                    existing.enabled = true;
                }
                else
                {
                    buildScenes.Add(scene);
                }
            }

            buildScenes = buildScenes
                .OrderByDescending(s => s.enabled)
                .ThenBy(s =>
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(s.path);
                    var index = System.Array.IndexOf(DemoSceneNames, name);
                    return index < 0 ? int.MaxValue : index;
                })
                .ToList();
            return buildScenes;
        }

        private static bool GetDemoScenes(List<EditorBuildSettingsScene> addedScenes)
        {
            foreach (var sceneName in DemoSceneNames)
            {
                var guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");

                if (guids.Length == 0)
                {
                    Debug.LogWarning($"Scene '{sceneName}' was not found in the project.");
                    continue;
                }

                if (guids.Length > 1)
                {
                    Debug.LogWarning(
                        $"Multiple scenes named '{sceneName}' found. Using the first one."
                    );
                }

                var path = AssetDatabase.GUIDToAssetPath(guids[0]);

                addedScenes.Add(new EditorBuildSettingsScene(path, true));
            }

            if (addedScenes.Count == 0)
            {
                Debug.LogError("No demo scenes could be added to Build Settings.");
                return false;
            }

            return true;
        }

        private static void DisableCurrentScenes(out List<EditorBuildSettingsScene> buildScenes, out List<EditorBuildSettingsScene> addedScenes)
        {
            buildScenes = EditorBuildSettings.scenes.ToList();
            foreach (var scene in buildScenes)
            {
                scene.enabled = false;
            }

            addedScenes = new List<EditorBuildSettingsScene>();
        }
    }
}
#endif
