using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Movement.PathFindig;
using Burmuruk.RPGStarterTemplate.UI.Samples;
using System.IO;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Saving.Samples
{
    public class JsonSavingWrapperSample : JsonSavingWrapper
    {
        protected override void LoadNavigationMap()
        {
#if UNITY_EDITOR
            NavSaver.Restart();
            // Resolve relative to this imported sample, including its versioned folder.
            string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(
                UnityEditor.MonoScript.FromMonoBehaviour(this));
            string assetsSamplePath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(scriptPath), "..", "..", "NavigationMaps"));

            if (!Directory.Exists(assetsSamplePath)) return;

            NavSaver.LoadNavMesh(assetsSamplePath);
            FindAnyObjectByType<LevelManager>()?.SetPaths(); 
#endif
        }

        protected override void LoadFinalElements(SlotData data)
        {
            base.LoadFinalElements(data);
        }
    }
}
