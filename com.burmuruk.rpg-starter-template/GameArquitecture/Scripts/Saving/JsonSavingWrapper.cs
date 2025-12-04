using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Stats;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public class JsonSavingWrapper : MonoBehaviour
    {
        const string DEFAULT_SAVEFILE = "miGuardado-";
        const string DEFAULT_IMAGE_NAME = "Slot";
        const string DEFAULT_IMAGE_EXTENTION = ".png";

        private int _lastBuildIdx = 0;

        public event Action<float> OnSaving;
        public event Action<float> OnLoading;
        public event Action<JObject> OnLoaded;
        public UnityEvent OnSavingUI;
        public UnityEvent OnLoadingUI;
        public UnityEvent OnLoadedUI;

        public event Action<int> OnLoadingStateFinished
        {
            add => GetComponent<JsonSavingSystem>().OnLoadingStateFinished += value;
            remove => GetComponent<JsonSavingSystem>().OnLoadingStateFinished -= value;
        }

        private void Awake()
        {
            var saver = GetComponent<JsonSavingSystem>();

            OnLoading += (_) =>
            {
                FindObjectOfType<GameManager>()?.SetState(GameManager.State.Loading);
            };

            saver.onSceneLoaded += () =>
            {
                Movement.PathFindig.NavSaver.Restart();
                Movement.PathFindig.NavSaver.LoadNavMesh();
                TemporalSaver.RemoveAllData();

                if (_lastBuildIdx == 0)
                {
                    GetComponent<PersistentObjSpawner>().TrySpawnObjects();
                }
            };

            OnLoaded += (args) =>
            {
                RestoreSlotData(args);
                FindObjectOfType<GameManager>()?.SetState(GameManager.State.Playing);
            };
            OnLoadingStateFinished += LoadStage;

            DontDestroyOnLoad(gameObject);
            PersistentObjects.Register(gameObject);
        }

        /// <summary>
        /// Saves the current state into the indicated slot.
        /// </summary>
        /// <param name="slot">Positive for manual and negative for auto-save</param>
        /// <param name="slotData">Scene data</param>
        public void Save(int slot, JObject slotData = null)
        {
            OnSaving?.Invoke(0);

            StartCoroutine(CaptureScreenshot(slot, slotData));
        }

        /// <summary>
        /// Captura un screenshot de la pantalla actual y devuelve:
        /// - base64: string PNG en Base64 para guardar en JSON
        /// - pngBytes: bytes del PNG para guardar en archivo físico
        /// </summary>
        private IEnumerator CaptureScreenshot(int slot, JObject slotData)
        {
            yield return new WaitForEndOfFrame();

            slotData ??= FindObjectOfType<LevelManager>().CaptureLevelData();
            int w = Screen.width;
            int h = Screen.height;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string base64 = Convert.ToBase64String(png);

            UnityEngine.Object.Destroy(tex);

            slotData["Image"] = base64;

            GetComponent<JsonSavingSystem>().Save(DEFAULT_SAVEFILE, slot, slotData);
            TakeSlotPicture(slot, png);

            OnSaving?.Invoke(1);
            yield break;
        }

        private IEnumerator CaptureScreenshotAuto(JObject slotData, bool overrideManualSave)
        {
            yield return new WaitForEndOfFrame();

            var saver = GetComponent<JsonSavingSystem>();
            var data = saver.LoadSave(DEFAULT_SAVEFILE);
            JObject newSave = new JObject();

            if (slotData == null || !slotData.ContainsKey("Slot"))
            {
                slotData = CreateDefaultSlotData(1);
            }

            int manualSlotIndex = slotData["Slot"].ToObject<int>();

            int w = Screen.width;
            int h = Screen.height;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string base64 = Convert.ToBase64String(png);

            UnityEngine.Object.Destroy(tex);
            slotData["Image"] = base64;

            foreach (var kvp in data)
            {
                if (!int.TryParse(kvp.Key, out int id)) continue;
                if (id > 0)
                {
                    newSave[kvp.Key] = kvp.Value;
                }
            }

            for (int i = -1; i >= -3; i--)
            {
                string key = i.ToString();

                if (!data.ContainsKey(key)) continue;

                int newIndex = i - 1;
                if (newIndex < -3)
                {
                    DeleteSlotPicture(i);
                    continue;
                }

                newSave[newIndex.ToString()] = data[key];
                RenameSlotPicture(i, newIndex);
            }

            var curDataState = saver.LoadCurrentSlot(DEFAULT_SAVEFILE, slotData);
            string slotIdxKey = manualSlotIndex.ToString();

            if (curDataState.ContainsKey(slotIdxKey))
            {
                newSave["-1"] = curDataState[slotIdxKey];
            }
            else
            {
                JObject fallbackState = new JObject
                {
                    ["SlotData"] = slotData
                };
                newSave["-1"] = fallbackState;
            }

            TakeSlotPicture(-1, png);

            if (overrideManualSave)
            {
                if (curDataState.ContainsKey(slotIdxKey))
                {
                    newSave[slotIdxKey] = curDataState[slotIdxKey];
                }

                TakeSlotPicture(-1, png);
            }

            saver.OverwriteSave(DEFAULT_SAVEFILE, newSave);

            OnSaving?.Invoke(1);

            yield break;
        }

        private JObject CreateDefaultSlotData(int slot)
        {
            JObject slotData = new JObject
            {
                ["Slot"] = slot,
                ["BuildIdx"] = SceneManager.GetActiveScene().buildIndex,
                ["TimePlayed"] = 0f
            };

            return slotData;
        }

        private void TakeSlotPicture(int slot, byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length == 0) return;

            string path = Path.Combine(
                Application.persistentDataPath,
                DEFAULT_IMAGE_NAME + slot.ToString() + DEFAULT_IMAGE_EXTENTION);

            File.WriteAllBytes(path, pngBytes);
        }

        private void DeleteSlotPicture(int slot)
        {
            string path = Path.Combine(
                Application.persistentDataPath,
                DEFAULT_IMAGE_NAME + slot.ToString() + DEFAULT_IMAGE_EXTENTION);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void RenameSlotPicture(int lastSlot, int newSlot)
        {
            string oldPath = Path.Combine(
                Application.persistentDataPath,
                DEFAULT_IMAGE_NAME + lastSlot.ToString() + DEFAULT_IMAGE_EXTENTION);

            string newPath = Path.Combine(
                Application.persistentDataPath,
                DEFAULT_IMAGE_NAME + newSlot.ToString() + DEFAULT_IMAGE_EXTENTION);

            if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
            }
        }

        /// <summary>
        /// Loads the indicated slot.
        /// Positive number: manual. Nagative: auto-saves.
        /// </summary>
        public void Load(int slot)
        {
            _lastBuildIdx = SceneManager.GetActiveScene().buildIndex;
            OnLoading?.Invoke(0);
            OnLoadingUI?.Invoke();
            FindObjectOfType<BuffsManager>()?.RemoveAllBuffs();

            Task.Delay(50).GetAwaiter().OnCompleted(() => LoadWithoutFade(slot));
        }

        public void DeleteSlot(int idx)
        {
            GetComponent<JsonSavingSystem>().DeleteSlot(DEFAULT_SAVEFILE, idx);
            DeleteSlotPicture(idx);
        }

        private void LoadWithoutFade(int slot)
        {
            GetComponent<JsonSavingSystem>().Load(
                DEFAULT_SAVEFILE,
                slot,
                (args) =>
                {
                    OnLoaded?.Invoke(args);
                    OnLoadedUI?.Invoke();
                });

            OnLoading?.Invoke(1);
        }

        /// <summary>
        /// Creates a new auto-save in -1 and sort the rest:
        /// -1 -> -2, -2 -> -3, -3 se elimina.
        /// </summary>
        /// <param name="slotData">Data from the manual slot (must have "Slot").</param>
        /// <param name="overrideManualSave">
        /// true = override the manual slot with current data.
        /// </param>
        public void AddNewAutoSaveSlot(JObject slotData, bool overrideManualSave)
        {
            OnSaving?.Invoke(0);

            StartCoroutine(CaptureScreenshotAuto(slotData, overrideManualSave));
        }

        public List<(int id, JObject slotData)> FindAvailableSlots(out List<(int id, Sprite sprite)> images)
        {
            images = null;
            var saver = GetComponent<JsonSavingSystem>();

            var slots = saver.LookForSlots(DEFAULT_SAVEFILE);
            if (slots is null) return null;

            foreach (var slot in slots)
            {
                if (TryLoadSlotImage(slot.id, out Sprite newSprite))
                {
                    (images ??= new()).Add((slot.id, newSprite));
                }
            }

            return slots;
        }

        private bool TryLoadSlotImage(int slot, out Sprite sprite)
        {
            sprite = null;
            string path = Path.Combine(
                Application.persistentDataPath,
                DEFAULT_IMAGE_NAME + slot.ToString() + DEFAULT_IMAGE_EXTENTION);

            if (!File.Exists(path))
                return false;

            byte[] data = File.ReadAllBytes(path);

            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, data);

            sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(.5f, .5f));

            return true;
        }

        private void RestoreSlotData(JObject slotData)
        {
            var data = new SlotData(
                slotData["Slot"].ToObject<int>(),
                slotData["BuildIdx"].ToObject<int>(),
                slotData["TimePlayed"].ToObject<float>());

            FindObjectOfType<LevelManager>().SaveSlotData(data);
        }

        protected virtual void LoadStage(int stage)
        {
            switch ((SavingExecution)stage)
            {
                case SavingExecution.Admin:
                    break;

                case SavingExecution.System:
                    break;

                case SavingExecution.Organization:
                    FindObjectOfType<LevelManager>().SetPaths();
                    FindObjectOfType<PlayerManager>().UpdateLeaderPosition();
                    break;

                case SavingExecution.General:
                    break;
            }
        }
    }
}
