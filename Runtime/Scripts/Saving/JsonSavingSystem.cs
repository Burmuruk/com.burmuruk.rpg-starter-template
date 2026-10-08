using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public class JsonSavingSystem : MonoBehaviour
    {
        private const string extension = ".json";
        private const int CurrentFormatVersion = 2;
        private const string FormatKey = "SaveFormatVersion";
        private const string StagesKey = "Stages";

        [SerializeField, Tooltip("Reload the saved scene before restoring state.")]
        private bool reloadScene = true;
        private bool loading;

        public event Action onSlotLoaded;
        public event Action<int> OnLoadingStateFinished;

        public IEnumerator LoadLastScene(JObject state, int slot, Action<JObject> callback)
        {
            if (loading)
                throw new InvalidOperationException("A save is already being restored.");
            if (!(state?[slot.ToString()] is JObject slotState) ||
                !(slotState["SlotData"] is JObject slotData))
                yield break;

            ValidateFormat(slotState);
            int nextScene = slotData[SlotData.BuildIndexKey]?.Value<int>()
                ?? throw new InvalidDataException("The slot has no scene build index.");
            loading = true;
            try
            {
                if (reloadScene || SceneManager.GetActiveScene().buildIndex != nextScene)
                    yield return SceneManager.LoadSceneAsync(nextScene);

                onSlotLoaded?.Invoke();
                // Advance the enumerator here so finally also runs if restoration fails.
                var restore = RestoreFromToken(slotState);
                try
                {
                    while (restore.MoveNext())
                        yield return restore.Current;
                }
                finally { (restore as IDisposable)?.Dispose(); }
                callback?.Invoke(slotData);
            }
            finally { loading = false; }
        }

        public void Save(string saveFile, int slot, JObject slotData = null)
        {
            JObject state = LoadJsonFromFile(saveFile);

            CaptureAsToken(ref state, slotData, slot);
            SaveFileAsJson(saveFile, state);
        }

        public void OverwriteSave(string saveFile, JObject data)
        {
            SaveFileAsJson(saveFile, data);
        }

        public JObject LoadSave(string saveFile)
        {
            return LoadJsonFromFile(saveFile);
        }

        public JObject GetCurrentSlotData(string saveFile, JObject slotData)
        {
            JObject state = new();
            int slot = 1;

            if (slotData != null && slotData.ContainsKey(SlotData.SlotKey))
            {
                slot = slotData[SlotData.SlotKey].ToObject<int>();
            }

            CaptureAsToken(ref state, slotData, slot);
            return state;
        }

        public void DeleteSlot(string fileName, int slot)
        {
            var savingData = LoadJsonFromFile(fileName);

            IDictionary<string, JToken> data = savingData;

            if (!data.ContainsKey(slot.ToString()))
                return;

            int curSlot = slot;

            while (data.ContainsKey((curSlot + 1).ToString()))
            {
                data[curSlot.ToString()] = data[(curSlot + 1).ToString()].DeepClone();
                if (data[curSlot.ToString()]?["SlotData"] is JObject metadata)
                    metadata[SlotData.SlotKey] = curSlot;
                ++curSlot;
            }

            data.Remove(curSlot.ToString());

            SaveFileAsJson(fileName, (JObject)data);
        }

        public void Load(string saveFile, int slot, Action<JObject> callback)
        {
            JObject state = LoadJsonFromFile(saveFile);
            StartCoroutine(LoadLastScene(state, slot, callback));
        }

        public JObject LoadJsonFromFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);

            if (!File.Exists(path))
            {
                return new JObject();
            }

            string total = File.ReadAllText(path);
            string json = Encrypter.DecryptString(total);
            JObject decrypted = JObject.Parse(json);

            return decrypted;
        }

        private void SaveFileAsJson(string saveFile, JObject state)
        {
            string path = GetPathFromSaveFile(saveFile);
            File.WriteAllText(path, Encrypter.EncryptString(state));
        }

        private JObject BuildSlotData(JObject data, int slot)
        {
            var metadata = data == null ? new JObject() : (JObject)data.DeepClone();
            metadata[SlotData.SlotKey] = slot;
            if (metadata[SlotData.BuildIndexKey] == null)
                metadata[SlotData.BuildIndexKey] = SceneManager.GetActiveScene().buildIndex;
            if (metadata[SlotData.TimePlayedKey] == null)
                metadata[SlotData.TimePlayedKey] = 0f;
            return metadata;
        }

        private void CaptureAsToken(ref JObject state, JObject slotData, int slot)
        {
            if (loading)
                throw new InvalidOperationException("Cannot capture a partially restored scene.");
            state ??= new JObject();
            var stages = new JObject();
            var slotState = new JObject
            {
                [FormatKey] = CurrentFormatVersion,
                ["SlotData"] = BuildSlotData(slotData, slot),
                [StagesKey] = stages
            };
            // A fresh snapshot removes data for objects/components no longer present.
            foreach (var entity in FindEntities().Values)
            {
                foreach (var entry in entity.CaptureStages())
                {
                    var stage = stages[entry.Key] as JObject;
                    if (stage == null)
                        stages[entry.Key] = stage = new JObject();
                    stage[entity.GetUniqueIdentifier()] = entry.Value.DeepClone();
                }
            }
            state[slot.ToString()] = slotState;
        }

        private static Dictionary<string, JsonSaveableEntity> FindEntities()
        {
            var result = new Dictionary<string, JsonSaveableEntity>();
            // Preserve the original active-object scope. Include disabled behaviours
            // on active GameObjects, but not inactive GameObjects or prefab assets.
            foreach (var entity in FindObjectsOfType<JsonSaveableEntity>())
            {
                if (entity == null || entity.IsRetiring)
                    continue;
                entity.SetUniqueIdentifier();
                string id = entity.GetUniqueIdentifier();
                if (result.ContainsKey(id))
                    throw new InvalidDataException($"Duplicate save entity ID: {id}");
                result.Add(id, entity);
            }
            return result;
        }

        private static void ValidateFormat(JObject state)
        {
            if (state[FormatKey] == null)
                return; // Original project format.
            if (state[FormatKey].Type != JTokenType.Integer ||
                state[FormatKey].Value<int>() != CurrentFormatVersion ||
                !(state[StagesKey] is JObject))
                throw new InvalidDataException("Unsupported or invalid save format.");
        }

        private IEnumerator RestoreFromToken(JObject state)
        {
            ValidateFormat(state);
            state = SavingExecutionAliases.NormalizeSlot(state);
            bool isCurrentFormat = state[FormatKey] != null;
            var stages = isCurrentFormat ? (JObject)state[StagesKey] : null;
            var executions = Enum.GetValues(typeof(SavingExecution)).Cast<SavingExecution>()
                .Distinct().OrderBy(e => (int)e).ToArray();
            if (isCurrentFormat)
            {
                var known = new HashSet<string>(executions.Select(e => e.ToString()));
                foreach (var entry in stages)
                    if (!known.Contains(entry.Key))
                        Debug.LogWarning($"Saved stage '{entry.Key}' no longer exists; its data will not be restored.");
            }

            foreach (var execution in executions)
            {
                string stageName = execution.ToString();
                // Refresh after previous stages and their event callbacks spawned objects.
                var entities = FindEntities();
                if (isCurrentFormat)
                {
                    if (stages[stageName] is JObject stage)
                    {
                        foreach (var entry in stage)
                        {
                            if (!(entry.Value is JObject components))
                                throw new InvalidDataException($"Invalid entity data in stage '{stageName}'.");
                            if (entities.TryGetValue(entry.Key, out var entity) && entity != null && !entity.IsRetiring)
                                entity.RestoreComponents(components);
                            else
                                Debug.LogWarning($"No entity '{entry.Key}' found for saved stage '{stageName}'.");
                        }
                    }
                }
                else
                {
                    // Original format: early stages were global by component type;
                    // later stages were nested under the entity ID. Read all stages.
                    foreach (var entity in entities.Values)
                    {
                        if (entity == null || entity.IsRetiring)
                            continue;
                        if (state[stageName] is JObject globalComponents)
                            entity.RestoreComponents(globalComponents);
                        if (entity == null || entity.IsRetiring)
                            continue;
                        if (state[entity.GetUniqueIdentifier()] is JObject perEntity &&
                            perEntity[stageName] is JObject components)
                            entity.RestoreComponents(components);
                    }
                }
                OnLoadingStateFinished?.Invoke((int)execution);
                // Let deferred Destroy and Start finish before discovering the next stage.
                yield return null;
            }
        }

        private string GetPathFromSaveFile(string saveFile)
        {
            return Path.Combine(Application.persistentDataPath, saveFile + extension);
        }

        public List<(int id, JObject slotData)> LookForSlots(string saveFile)
        {
            var data = LoadJsonFromFile(saveFile);

            IDictionary<string, JToken> stateDict = data;
            List<(int id, JObject slotData)> slots = new();

            foreach (var slot in stateDict)
            {
                if (!int.TryParse(slot.Key, out int id))
                    continue;

                try
                {
                    slots.Add((id, (JObject)slot.Value["SlotData"]));
                }
                catch (InvalidOperationException)
                {
                }
            }

            return slots;
        }
    }
}
