using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public static class SavingSystem
    {
        //const string DATA_PATH = "Assets/com.burmuruk.rpg-starter-template/Tool/Data";
        const string DATA_PATH = "Assets/com.burmuruk.rpg-starter-template/Tool/Data";
        public static CreationDatabase Data { get; private set; } = null;
        public static event Action<ModificationTypes, ElementType, string, CreationData> OnCreationModified;

        public static void Initialize()
        {
            //Data = (AssetDatabase.FindAssets("t:" + typeof(CreationDatabase).ToString(), new[] { DATA_PATH })
            //    .Select(guid => AssetDatabase.LoadAssetAtPath<CreationDatabase>(AssetDatabase.GUIDToAssetPath(guid)))
            //    .ToList().FirstOrDefault());

            //if (Data == null)
            //{
            //    Notify("No creation found", BorderColour.Error);

            //    Data = ScriptableObject.CreateInstance<CreationDatabase>();
            //    AssetDatabase.CreateAsset(Data, DATA_PATH);
            //    AssetDatabase.SaveAssets();
            //    AssetDatabase.Refresh();
            //}

            Data = new CreationDatabase()
            {
                creations = GetAllCreations()
            };

            OnCreationModified += (modification, type, id, data) =>
            {
                var info = new BaseCreationInfo(id, data?.Id, data);
                CreationScheduler.ChangeData(modification, type, id, info);
            };
        }

        private static Dictionary<ElementType, Dictionary<string, CreationData>> GetAllCreations()
        {
            if (!JsonWritter.ReadJson(out var json) || !json.ContainsKey("creations"))
                return new();

            return (from item in (IDictionary<string, JToken>)json["creations"]
                    where item.Value is JObject obj && obj.ContainsKey("type")
                    let type = (ElementType)item.Value["type"].ToObject<int>()
                    let obj = (JObject)item.Value
                    let creationData = new
                    {
                        Id = item.Key,
                        Instance = (CreationData)FormatterServices.GetUninitializedObject(Type.GetType(obj.Properties().First().Name)),
                        Data = (JObject)obj.Properties().First().Value
                    }
                    group creationData by type into g
                    select new
                    {
                        Type = g.Key,
                        Creations = g.ToDictionary(c => c.Id, c =>
                        {
                            c.Instance.RestoreFromJson(c.Data);
                            return (CreationData)c.Instance;
                        })
                    }).ToDictionary(x => x.Type, x => x.Creations);
        }

        private static bool TryLoadCreations()
        {
            bool result = false;

            foreach (var elements in Data.creations)
            {
                foreach (var creation in elements.Value)
                {
                    OnCreationModified?.Invoke(ModificationTypes.Add, elements.Key, creation.Key, creation.Value);
                    result = true;
                }
            }

            return result;
        }

        public static void LoadCreations()
        {
            //Data.SyncFromSerialized();

            if (TryLoadCreations())
                Notify("Creations loaded", BorderColour.Success);
        }

        public static CreationData GetCreation(ElementType type, string id)
        {
            return Data.creations[type][id];
        }

        public static bool SaveCreation(ElementType type, in string id, in CreationData data, ModificationTypes modificationType)
        {
            string newId = id;
            bool saved = Save_CreationData(type, ref newId, data);

            if (saved)
                OnCreationModified?.Invoke(modificationType, type, newId, data);
            return saved;
        }

        public static CreationData Load(ElementType type, string id)
        {
            if (!Data.creations.ContainsKey(type))
                return null;

            if (!LoadIdFromJson(id, out _, out _))
                return null;

            return Data.creations[type][id];
        }

        public static CreationData Load(string id)
        {
            ElementType type = ElementType.None;

            foreach (var creation in Data.creations)
            {
                if (creation.Value.ContainsKey(id))
                {
                    type = creation.Key;
                    break;
                }
            }

            if (type == ElementType.None) return null;

            if (!LoadIdFromJson(id, out _, out _))
                return null;

            return Data.creations[type][id];
        }

        private static bool LoadIdFromJson(string id, out CreationData data, out JObject jo)
        {
            data = null;
            if (!Editor.JsonWritter.ReadJson(out jo)) return false;

            var json = (IDictionary<string, JToken>)jo["creations"];

            if (json == null) return false;

            if (json.ContainsKey(id) && json[id] is JObject obj)
            {
                Type t = Type.GetType(obj.Properties().First().Name);
                data = (CreationData)FormatterServices.GetUninitializedObject(t);

                data.RestoreFromJson((JObject)obj[t.FullName]);
            }
            else
                return false;

            return true;
        }

        static bool Save_CreationData(ElementType type, ref string id, CreationData creationData, string newName = "")
        {
            string name = creationData.Id;

            if (string.IsNullOrEmpty(newName))
            {
                if (!name.VerifyName(NotificationType.Creation)) return false;

                newName = name;
            }
            else if (newName != name)
            {
                if (!newName.VerifyName(NotificationType.Creation))
                    return false;
            }

            if (!Data.creations.ContainsKey(type))
                Data.creations.Add(type, new Dictionary<string, CreationData>());

            if (!string.IsNullOrEmpty(id))
            {
                if (Data.creations[type].ContainsKey(id))
                {
                    Data.creations[type].Remove(id);
                }
            }
            else
                id = Guid.NewGuid().ToString();

            SaveJson(type, id, creationData);

            bool result = Data.creations[type].TryAdd(id, creationData);

            if (!result) return false;

            return true;
        }

        private static void SaveJson(ElementType type, string id, CreationData creationData)
        {
            var data = new JObject
            {
                [creationData.GetType().FullName] = creationData.GetJson(),
                ["type"] = ((int)type).ToString(),
            };

            if (LoadIdFromJson(id, out _, out var json))
            {
                json["creations"][id] = data;
            }
            else
            {
                if (!JsonWritter.JsonFileExists())
                    json = new JObject();

                if (json["creations"] == null)
                {
                    json["creations"] = new JObject
                    {
                        [id] = data
                    };
                }
                else
                {
                    json["creations"][id] = data;
                }
            }

            Editor.JsonWritter.WriteJson(json);
        }

        public static bool Remove(ElementType type, string id)
        {
            var lastData = Data.creations[type][id];
            Data.creations[type].Remove(id);

            if (LoadIdFromJson(id, out _, out var json))
            {
                (json["creations"] as JObject).Remove(id);
                Editor.JsonWritter.WriteJson(json);
            }

            OnCreationModified?.Invoke(ModificationTypes.Remove, type, id, lastData);

            return true;
        }

        public static string GetAssetReference(UnityEngine.Object asset)
        {
            if (asset == null) return null;

            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

            if (guid == "0000000000000000f000000000000000")
                return AssetDatabase.GetAssetPath(asset) + "|" + asset.name;
            else
                return guid;
        }

        public static T GetAsset<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (path.Contains("|"))
            {
                var slices = path.Split('|');
                var (newPath, name) = (slices[0], slices[1]);

                return (T)AssetDatabase.LoadAllAssetsAtPath(newPath).Where(a => a is T && a.name.Contains(name)).FirstOrDefault();
            }
            else
                return (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(path), typeof(T));
        }
    }

    public interface ISaveable : IDataVerifiable, IDataProvider
    {
        public bool Save();
        public CreationData Load(ElementType type, string id);
        public CreationData Load(string id);
    }

    public interface IDataProvider
    {
        public CreationData GetInfo();
        public void UpdateInfo(CreationData cd);
    }

    [Flags]
    public enum ModificationTypes
    {
        None = 0,
        Add = 1,
        Remove = 1 << 1,
        EditData = 1 << 2,
        Rename = 1 << 3,
        ColourReasigment = 1 << 4
    }

    public interface IChangesObserver : IDataVerifiable
    {
        public ModificationTypes Check_Changes();
        public void Load_Changes();
        public void Remove_Changes();
    }

    public interface IUpdatableUI
    {
        public void UpdateUIData<T>(T arg1) { }
        public void UpdateUIData<T, U>(T arg1, U arg2) { }
        public void UpdateUIData<T, U, R>(T arg1, U arg2, R arg3) { }
    }

    public interface IDataVerifiable
    {
        public bool VerifyData(out List<string> errors);
    }

    public enum CreationsState
    {
        None,
        Creating,
        Editing,
    }
}
