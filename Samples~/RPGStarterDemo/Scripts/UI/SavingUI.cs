using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class SavingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject slotsContainer;
        [SerializeField] GameObject mainMenu;
        [SerializeField] SaveSlotView slotTemplate;
        [SerializeField] Transform manualSlotsParent;
        [SerializeField] Transform autoSavesParent;
        [SerializeField] GameObject btnAddMore;
        [SerializeField] GameObject btnCancelDelete;
        [Header("Mode")]
        [SerializeField, Tooltip("True: save. False: load.")] bool saveMode;
        [Header("Selection")]
        [SerializeField] Color selectedColor = new Color(.75f, .85f, 1f);
        [SerializeField] Color deleteSelectedColor = new Color(1f, .55f, .55f);

        JsonSavingWrapper savingWrapper;
        readonly Dictionary<int, SaveSlotView> views = new Dictionary<int, SaveSlotView>();
        readonly List<Sprite> ownedSprites = new List<Sprite>();
        int? selectedSlot;
        int? pendingNewSlot;
        bool deleting;
        bool saving;
        public event Action<int> OnSlotAdded;

        private void Awake()
        {
            if (slotTemplate != null && slotTemplate.gameObject.scene.IsValid())
                slotTemplate.gameObject.SetActive(false);

            SceneManager.sceneLoaded += OnSceneLoaded;
            ResolveWrapper();
            ApplyState();
        }

        private bool ResolveWrapper()
        {
            if (savingWrapper != null)
                return true;

            savingWrapper = FindObjectOfType<JsonSavingWrapper>();

            if (savingWrapper == null)
            {
                Debug.LogWarning("SavingUI: JsonSavingWrapper not found.", this);
                return false;
            }

            savingWrapper.OnSaving += OnSavingProgress;
            return true;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (savingWrapper != null)
                savingWrapper.OnSaving -= OnSavingProgress;

            ReleaseSprites();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 0)
                ShowMenu(true);
        }

        public void ToggleSavingSlots() => ShowSlots(!(slotsContainer.activeSelf && saveMode), true);

        public void ToggleLoadingSlots() => ShowSlots(!(slotsContainer.activeSelf && !saveMode), false);

        public void ToggleSlots() => ShowSlots(!slotsContainer.activeSelf);

        public void ShowSlots(bool shouldShow) => ShowSlots(shouldShow, saveMode);

        public void ShowSlots(bool shouldShow, bool shouldSave)
        {
            saveMode = shouldSave;
            ResetSelection();

            if (shouldShow)
                RefreshSlots();

            slotsContainer.SetActive(shouldShow);
            ApplyState();
        }

        public void RefreshSlots()
        {
            if (!ResolveWrapper())
                return;

            var data = savingWrapper.FindAvailableSlots(out var images);
            ClearViews();
            ReleaseSprites();

            if (images != null)
            {
                foreach (var image in images)
                {
                    if (image.sprite != null)
                        ownedSprites.Add(image.sprite);
                }
            }

            EnableCurrentSlots(data, images);
        }

        public void EnableCurrentSlots(List<(int id, JObject slotData)> slots, List<(int id, Sprite sprite)> images)
        {
            ClearViews();

            if (slotTemplate == null || manualSlotsParent == null || autoSavesParent == null)
            {
                Debug.LogError("SavingUI: assign template and containers.", this);
                return;
            }

            if (slots != null)
            {
                var ordered = new List<(int id, JObject slotData)>(slots);
                ordered.Sort((a, b) =>
                {
                    if ((a.id > 0) != (b.id > 0))
                        return a.id > 0 ? -1 : 1;

                    return Math.Abs(a.id).CompareTo(Math.Abs(b.id));
                });

                foreach (var slot in ordered)
                {
                    if (slot.id == 0 || slot.id < -3 || slot.id > 3 || views.ContainsKey(slot.id))
                        continue;

                    Sprite sprite = null;

                    if (images != null)
                    {
                        foreach (var image in images)
                        {
                            if (image.id != slot.id)
                                continue;

                            sprite = image.sprite;
                            break;
                        }
                    }

                    var view = Instantiate(slotTemplate, slot.id > 0 ? manualSlotsParent : autoSavesParent);
                    view.Bind(slot.id, slot.slotData, sprite, OnSlotClicked);
                    views.Add(slot.id, view);
                    view.gameObject.SetActive(true);
                }
            }

            if (selectedSlot.HasValue && !views.ContainsKey(selectedSlot.Value))
                selectedSlot = null;

            ApplyState();
        }

        public void OnSlotClicked(int id)
        {
            if (saving || !views.ContainsKey(id))
                return;

            if (!deleting && saveMode && id < 0)
                return;

            selectedSlot = id;
            ApplyState();

            if (deleting) return;

            if (saveMode)
                SaveSlot(id);
            else
                LoadSlot(id);
        }

        public void EnterDeletingMode()
        {
            if (saving) return;

            if (!deleting)
            {
                selectedSlot = null;
                deleting = true;
                ApplyState();
                return;
            }

            if (selectedSlot.HasValue)
                DeleteSlot(selectedSlot.Value);
        }

        public void CancelDeletingMode()
        {
            ResetSelection();
            ApplyState();
        }

        public void DeleteSlot(int id)
        {
            if (saving || !deleting || selectedSlot != id || !views.ContainsKey(id) || !ResolveWrapper())
                return;

            savingWrapper.DeleteSlot(id);
            ResetSelection();
            RefreshSlots();
        }

        public void SaveSlot(int id)
        {
            if (saving || deleting || id < 1 || id > 3 || !ResolveWrapper())
                return;

            saving = true;
            ApplyState();
            savingWrapper.Save(id);
        }

        public void LoadSlot(int id)
        {
            if (saving || deleting || !views.ContainsKey(id) || !ResolveWrapper())
                return;

            ShowMenu(false);
            savingWrapper.Load(id);
        }
        public void LoadSelectedSlot()
        {
            if (selectedSlot.HasValue)
                LoadSlot(selectedSlot.Value);
        }

        public void AddSlot()
        {
            if (saving || deleting || !ResolveWrapper())
                return;

            RefreshSlots();
            int id = FirstFreeManualId();

            if (id == 0) return;

            if (saveMode)
            {
                pendingNewSlot = id;
                SaveSlot(id);
            }
            else
            {
                ShowMenu(false);
                savingWrapper.Load(id);
                OnSlotAdded?.Invoke(id);
            }
        }

        private int FirstFreeManualId()
        {
            for (int id = 1; id <= 3; id++)
                if (!views.ContainsKey(id))
                    return id;

            return 0;
        }

        private void OnSavingProgress(float progress)
        {
            if (!saving || progress < 1f)
                return;

            saving = false;
            int? added = pendingNewSlot;
            pendingNewSlot = null;
            RefreshSlots();

            if (added.HasValue)
                OnSlotAdded?.Invoke(added.Value);
        }

        public void ShowMenu(bool shouldShow)
        {
            ResetSelection();
            ApplyState();

            if (mainMenu != null)
                mainMenu.SetActive(shouldShow);

            if (slotsContainer != null)
                slotsContainer.SetActive(false);
        }

        public void LoadScene(string sceneName)
        {
            if (saving || !ResolveWrapper())
                return;

            ShowMenu(false);
            savingWrapper.LoadScene(sceneName);
        }

        private void ResetSelection()
        {
            selectedSlot = null;
            deleting = false;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
        private void ApplyState()
        {
            foreach (var pair in views)
            {
                pair.Value.SetState(!saving && (deleting || !saveMode || pair.Key > 0),
                    selectedSlot == pair.Key, deleting ? deleteSelectedColor : selectedColor);
            }

            if (btnAddMore != null)
                btnAddMore.SetActive(saveMode && !saving && !deleting && FirstFreeManualId() != 0);

            if (btnCancelDelete != null)
                btnCancelDelete.SetActive(deleting);
        }
        private void ClearViews()
        {
            foreach (var view in views.Values)
            {
                if (view == null) continue;

                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            views.Clear();
        }
        private void ReleaseSprites()
        {
            var textures = new HashSet<Texture>();

            foreach (var sprite in ownedSprites)
            {
                if (sprite == null) continue;

                if (sprite.texture != null)
                    textures.Add(sprite.texture);

                Destroy(sprite);
            }

            foreach (var texture in textures)
                Destroy(texture);

            ownedSprites.Clear();
        }
    }
}
