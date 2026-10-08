using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public static class AssetReferenceProvider
    {
        private const string BUILTIN_GUID =
            "0000000000000000f000000000000000";

        public static string GetReference(
            UnityEngine.Object asset)
        {
            if (asset == null)
                return null;

            string assetPath =
                AssetDatabase.GetAssetPath(asset);

            string guid =
                AssetDatabase.AssetPathToGUID(assetPath);

            if (guid == BUILTIN_GUID)
                return assetPath + "|" + asset.name;

            return guid;
        }

        public static T GetAsset<T>(
            string reference)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(reference))
                return null;

            if (reference.Contains("|"))
            {
                string[] slices = reference.Split('|');

                if (slices.Length < 2)
                    return null;

                string path = slices[0];
                string name = slices[1];

                return AssetDatabase
                    .LoadAllAssetsAtPath(path)
                    .OfType<T>()
                    .FirstOrDefault(
                        asset => asset.name.Contains(name));
            }

            string assetPath =
                AssetDatabase.GUIDToAssetPath(reference);

            return AssetDatabase.LoadAssetAtPath<T>(
                assetPath);
        }
    }
}