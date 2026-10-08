using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentObjSpawner : MonoBehaviour
{
    private static readonly Dictionary<GameObject, GameObject> spawnedObjects = new();
    [SerializeField] List<GameObject> persistentObjectsPref;
    [SerializeField] private string _id = "";

    public void TrySpawnObjects()
    {
        SpawnObjects();
    }

    private void SpawnObjects()
    {
        foreach (GameObject obj in persistentObjectsPref)
        {
            if (obj == null)
                continue;

            if (spawnedObjects.TryGetValue(obj, out GameObject existing) && existing != null)
                continue;

            if (obj.GetComponentInChildren<Burmuruk.RPGStarterTemplate.Control.GameManager>(true) != null &&
                Burmuruk.RPGStarterTemplate.Control.GameManager.Instance != null)
            {
                spawnedObjects[obj] = Burmuruk.RPGStarterTemplate.Control.GameManager.Instance.transform.root.gameObject;
                continue;
            }

            GameObject newObj = Instantiate(obj);
            spawnedObjects[obj] = newObj;
            DontDestroyOnLoad(newObj);
        }
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(_id))
            _id = Guid.NewGuid().ToString();

        //SerializedObject serializedObject = new(this);
        //SerializedProperty property = serializedObject.FindProperty("_id");
        //property.intValue = _id;
        //serializedObject.ApplyModifiedProperties(); 
#endif
    }

    public void OnAfterDeserialize() { }
}

public static class PersistentObjects
{
    private static List<GameObject> objects = new();

    public static void Register(GameObject go)
    {
        objects.Add(go);
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    public static void ClearAll()
    {
        foreach (GameObject go in objects)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }

        objects.Clear();
    }

    public static void ClearAndChangeScene(int idx)
    {
        ClearAll();
        SceneManager.LoadScene(idx);
    }

    public static void ClearAndChangeScene(string name)
    {
        ClearAll();
        SceneManager.LoadScene(name);
    }
}

