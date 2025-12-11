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

        public event Action onSlotLoaded;
        public event Action<int> OnLoadingStateFinished;

        public IEnumerator LoadLastScene(JObject state, int slot, Action<JObject> callback)
        {
            JObject slotState = new JObject();
            int curScene = SceneManager.GetActiveScene().buildIndex;
            int nextScene = 2;

            if (state.ContainsKey(slot.ToString()) &&
                state[slot.ToString()] is JObject obj &&
                obj != null &&
                obj.ContainsKey("SlotData"))
            {
                slotState = (JObject)state[slot.ToString()];
                nextScene = (int)slotState["SlotData"][SlotData.BuildIndexKey];
            }
            else
            {
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(nextScene);

            onSlotLoaded?.Invoke();

            RestoreFromToken(slotState);

            callback?.Invoke((JObject)slotState["SlotData"]);
        }

        public void Save(string saveFile, int slot, JObject slotData = null)
        {
            JObject state = LoadJsonFromFile(saveFile);

            if (slotData == null)
            {
                slotData = new JObject
                {
                    [SlotData.SlotKey] = slot,
                    [SlotData.BuildIndexKey] = SceneManager.GetActiveScene().buildIndex,
                    [SlotData.TimePlayedKey] = 0f
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

            if (!data.ContainsKey(slot.ToString())) return;

            int curSlot = slot;

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

        private void CaptureAsToken(ref JObject state, JObject slotData, int slot)
        {
            IDictionary<string, JToken> stateDict = state;

            JObject slotState = new();
            slotState["SlotData"] = slotData;

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
            state = (JObject)stateDict;
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
