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

        public event Action onSceneLoaded;
        public event Action<int> OnLoadingStateFinished;

        public IEnumerator LoadLastScene(JObject state, int slot, Action<JObject> callback)
        {
            JObject slotState = new JObject();
            int curScene = SceneManager.GetActiveScene().buildIndex;
            int nextScene = 2;
            JObject slotData = null;

            if (state.ContainsKey(slot.ToString()) &&
                state[slot.ToString()] is JObject obj &&
                obj != null &&
                obj.ContainsKey("SlotData"))
            {
                slotState = (JObject)state[slot.ToString()];
                slotData = (JObject)slotState["SlotData"];
                nextScene = (int)slotState["SlotData"]["BuildIdx"];
            }
            else
            {
                slotData = new JObject();
                slotData["Slot"] = slot;
                slotData["BuildIdx"] = nextScene;
                slotData["TimePlayed"] = 0f;
                slotData["MembersCount"] = 1;

                slotState["SlotData"] = slotData;
            }

            yield return SceneManager.LoadSceneAsync(nextScene);

            onSceneLoaded?.Invoke();

            RestoreFromToken(slotState);

            callback?.Invoke(slotData);
            //yield return SceneManager.UnloadSceneAsync(curScene);
        }

        public void Save(string saveFile, int slot, JObject slotData = null)
        {
            JObject state = LoadJsonFromFile(saveFile);

            // Por seguridad, garantizamos que SlotData tenga información básica.
            if (slotData == null)
            {
                slotData = new JObject
                {
                    ["Slot"] = slot,
                    ["BuildIdx"] = SceneManager.GetActiveScene().buildIndex,
                    ["TimePlayed"] = 0f
                };
            }

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

        /// <summary>
        /// Construye un estado nuevo solo con el slot actual (SlotData + entidades).
        /// Se usa para crear copias de slot (auto-save, override, etc).
        /// </summary>
        public JObject LoadCurrentSlot(string saveFile, JObject slotData)
        {
            JObject state = new();
            int slot = 1;

            if (slotData != null && slotData.ContainsKey("Slot"))
            {
                slot = slotData["Slot"].ToObject<int>();
            }

            CaptureAsToken(ref state, slotData, slot);
            return state;
        }

        public void DeleteSlot(string fileName, int slot)
        {
            var savingData = LoadJsonFromFile(fileName);

            IDictionary<string, JToken> data = savingData;

            if (!data.ContainsKey(slot.ToString())) return;

            int curSlot = slot;

            // Reacomoda solo los slots superiores (1→2→3, etc.)
            while (data.ContainsKey((curSlot + 1).ToString()))
            {
                data[curSlot.ToString()] = data[(curSlot + 1).ToString()];
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

        private JObject LoadJsonFromFile(string saveFile)
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

        private void CaptureAsToken(ref JObject state, JObject slotData, int slot)
        {
            IDictionary<string, JToken> stateDict = state;

            JObject slotState = new();

            if (state.ContainsKey(slot.ToString()))
            {
                slotState = (JObject)stateDict[slot.ToString()];
            }

            slotState["SlotData"] = slotData ?? new JObject();

            foreach (var saveable in FindObjectsOfType<JsonSaveableEntity>())
            {
                var idComponents = saveable.CaptureAsJtoken(out JObject UniqueItems);

                if (idComponents != null)
                    slotState[saveable.GetUniqueIdentifier()] = idComponents;

                if (UniqueItems == null) continue;

                foreach (var item in UniqueItems)
                {
                    if (slotState.ContainsKey(item.Key))
                    {
                        foreach (var component in (JObject)item.Value)
                        {
                            slotState[item.Key][component.Key] = component.Value;
                        }
                    }
                    else
                    {
                        JObject newComponents = new JObject();
                        foreach (var component in (JObject)item.Value)
                        {
                            newComponents[component.Key] = component.Value;
                        }

                        slotState[item.Key] = newComponents;
                    }
                }
            }

            stateDict[slot.ToString()] = slotState;
        }

        private void RestoreFromToken(JObject state)
        {
            if (state.Count <= 0) return;

            IDictionary<string, JToken> stateDict = state;

            var saveables = FindObjectsOfType<JsonSaveableEntity>().ToList();

            for (int i = 0; i < (int)SavingExecution.General; i++)
            {
                if (!stateDict.ContainsKey(((SavingExecution)i).ToString()))
                {
                    OnLoadingStateFinished?.Invoke(i);
                    continue;
                }

                for (int x = 0; x < saveables.Count; x++)
                {
                    saveables[x].RestoreFromJToken(state, (SavingExecution)i);
                }

                OnLoadingStateFinished?.Invoke(i);
            }

            saveables = FindObjectsOfType<JsonSaveableEntity>().ToList();

            for (int i = 0; i < saveables.Count; i++)
            {
                string id = saveables[i].GetUniqueIdentifier();

                if (stateDict.ContainsKey(id))
                {
                    saveables[i].RestoreFromJToken(stateDict[id], SavingExecution.General);
                }
            }

            OnLoadingStateFinished?.Invoke((int)SavingExecution.General);
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
                if (!int.TryParse(slot.Key, out int id)) continue;

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
