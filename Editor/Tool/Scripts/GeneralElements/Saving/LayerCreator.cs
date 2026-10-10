namespace Burmuruk.RPGStarterTemplate.Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class LayerCreator
    {
        public static int? EnsureLayer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new System.ArgumentException("Invalid name.");

            int existing = LayerMask.NameToLayer(name);

            if (existing >= 0)
                return existing;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");

            var settings = new SerializedObject(assets[0]);
            SerializedProperty layers = settings.FindProperty("layers");

            for (int i = 10; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);

                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;

                layer.stringValue = name;
                settings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }

            Debug.LogError($"No space available for layer '{name}'.");
            return null;
        }

        [MenuItem("Tools/Layers/Create Enemy Layer")]
        private static void CreateEnemyLayer()
        {
            EnsureLayer("Enemy");
        }
    }

    public static class TagCreator
    {
        public static void CreateTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag))
                return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Checks if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(newTag))
                    return; // Already exists
            }

            // Adds new tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = newTag;

            tagManager.ApplyModifiedProperties();
            Debug.Log($"Tag \"{newTag}\" added successfully.");
        }

        public static void GetTags()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");
            var settings = new SerializedObject(assets[0]);
            SerializedProperty tags = settings.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty tag = tags.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(tag.stringValue))
                    Debug.Log($"Tag {i}: {tag.stringValue}");
            }

        }
    }
}
