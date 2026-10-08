using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class CreationRepository
    {
        private readonly CreationDatabase database;
        private readonly JsonStore jsonStore;
        private readonly CreationSerializer serializer;

        public CreationDatabase Database => database;

        public event Action<ModificationTypes, ElementType, string, CreationData> CreationModified;

        public CreationRepository(CreationDatabase database, JsonStore jsonStore, CreationSerializer serializer)
        {
            this.database = database;
            this.jsonStore = jsonStore;
            this.serializer = serializer;
        }

        public bool LoadAll()
        {
            JObject root = jsonStore.Read();

            database.creations.Clear();

            if (!jsonStore.TryGet(root, JsonStore.CREATIONS_KEY, out JObject creations))
            {
                return false;
            }

            bool loadedAnything = false;

            foreach (var property in creations.Properties())
            {
                if (property.Value is not JObject creationJson)
                    continue;

                if (!serializer.TryDeserialize(creationJson, out ElementType type, out CreationData data))
                {
                    continue;
                }

                if (!database.creations.TryGetValue(type, out Dictionary<string, CreationData> typeDatabase))
                {
                    typeDatabase = new Dictionary<string, CreationData>();

                    database.creations.Add(type, typeDatabase);
                }

                typeDatabase[property.Name] = data;

                loadedAnything = true;
            }

            return loadedAnything;
        }

        public CreationData Load(ElementType type, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (!database.creations.TryGetValue(type, out Dictionary<string, CreationData> creations))
            {
                return null;
            }

            creations.TryGetValue(id, out CreationData data);

            return data;
        }

        public CreationData Load(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return database.TryGetCreation(id, out CreationData data, out _) ? data : null;
        }

        public bool Save(ElementType type, string id, CreationData creationData, ModificationTypes modificationType)
        {
            if (creationData == null)
                return false;

            if (!creationData.Id.VerifyName(NotificationType.Creation))
            {
                return false;
            }

            string creationId = id;

            if (string.IsNullOrEmpty(creationId))
                creationId = Guid.NewGuid().ToString();

            JObject serialized = serializer.Serialize( type, creationData);

            if (serialized == null)
                return false;

            JObject root = jsonStore.Read();

            JObject creations = jsonStore.GetOrCreateObject(root, JsonStore.CREATIONS_KEY);

            creations[creationId] = serialized;

            jsonStore.Write(root);

            if (!database.creations.TryGetValue(type, out Dictionary<string, CreationData> typeDatabase))
            {
                typeDatabase = new Dictionary<string, CreationData>();

                database.creations.Add(type, typeDatabase);
            }

            typeDatabase[creationId] = creationData;

            CreationModified?.Invoke(modificationType, type, creationId, creationData);

            return true;
        }

        public bool Remove(ElementType type, string id)
        {
            if (!database.creations.TryGetValue(type, out Dictionary<string, CreationData> creations))
            {
                return false;
            }

            if (!creations.TryGetValue(id, out CreationData previousData))
            {
                return false;
            }

            JObject root = jsonStore.Read();

            if (!jsonStore.TryGet(root, JsonStore.CREATIONS_KEY, out JObject creationsJson))
            {
                return false;
            }

            if (!creationsJson.Remove(id))
                return false;

            jsonStore.Write(root);

            creations.Remove(id);

            CreationModified?.Invoke(ModificationTypes.Remove, type, id, previousData);

            return true;
        }
    }
}