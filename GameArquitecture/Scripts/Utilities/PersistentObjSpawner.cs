using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentObjSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> persistentObjectsPref;
    [SerializeField] private string _id = "";
    //static Dictionary<string, object> neverDelete = new();


    public void TrySpawnObjects()
    {
        //if (neverDelete.ContainsKey(_id) && (bool)neverDelete[_id] == true) return;

        SpawnObjects();

        //neverDelete[_id] = true;
    }

    private void SpawnObjects()
    {
        foreach (var obj in persistentObjectsPref)
        {
            var newObj = Instantiate(obj);
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
        foreach (var go in objects)
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

