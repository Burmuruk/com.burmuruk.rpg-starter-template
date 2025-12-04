using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class SavingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject slotsContainer;
        [SerializeField] GameObject mainMenu;
        [SerializeField] SlotUI[] slots;
        [SerializeField] SlotUI[] autoSaves;
        [SerializeField] GameObject btnAddMore;
        [SerializeField] GameObject btnLoad;
        [SerializeField] Image testImage;

        JsonSavingWrapper savingWrapper;

        int curSlots = 0;
        int? selectedSlot;
        bool deleting = false;

        [Serializable]
        private struct SlotUI
        {
            [SerializeField] GameObject item;
            [SerializeField] TextMeshProUGUI title;
            [SerializeField] TextMeshProUGUI timePlayed;
            [SerializeField] TextMeshProUGUI membersCount;
            [SerializeField] Image picture;

            public GameObject GameObject { get { return item; } }
            public string Title { get => title.text; set => title.text = value; }
            public string PlayedTime { get => timePlayed.text; set => timePlayed.text = value; }
            public int MembersCount { get => Int32.Parse(membersCount.text); set => membersCount.text = value.ToString(); }
            public Sprite Sprite { get => picture.sprite; set => picture.sprite = value; }
        }

        public event Action<int> OnSlotAdded;

        private void Awake()
        {
            savingWrapper = FindObjectOfType<JsonSavingWrapper>();

            EnableCurrentSlots(savingWrapper.FindAvailableSlots(out var sprites), sprites);
            int i = 1;

            foreach (var button in slots)
            {
                button.GameObject.GetComponent<MyItemButton>().SetId(i++);
                button.GameObject.GetComponent<MyItemButton>().OnPointerEnterEvent += SelectSlot;
            }

            i = -1;

            foreach (var button in autoSaves)
            {
                button.GameObject.GetComponent<MyItemButton>().SetId(i--);
                button.GameObject.GetComponent<MyItemButton>().OnPointerEnterEvent += SelectSlot;
            }

            SetSlotColour(Color.white);
        }

        public void ToggleSlots()
        {
            ShowSlots(!slotsContainer.activeSelf);
        }

        public void ShowSlots(bool shouldShow)
        {
            if (shouldShow)
                EnableCurrentSlots(savingWrapper.FindAvailableSlots(out var sprites), sprites);

            slotsContainer.SetActive(shouldShow);
        }

        public void SaveSlot(int slot)
        {
            if (!deleting)
            {
                savingWrapper.Save(slot);
            }
            else
            {
                savingWrapper.DeleteSlot(slot);
                EnterDeletingMode();
                ToggleSlots();
            }
        }

        public void LoadSlot(int slot)
        {
            if (!deleting)
            {
                ShowMenu(false);

                savingWrapper.Load(slot);
            }
            else
            {
                savingWrapper.DeleteSlot(slot);
                EnterDeletingMode();
                ToggleSlots();
            }
        }

        public void LoadSelectedSlot()
        {
            if (selectedSlot.HasValue)
                LoadSlot(selectedSlot.Value);
        }

        public void EnableCurrentSlots(List<(int id, JObject slotData)> slots, List<(int id, Sprite sprite)> images)
        {
            DisableSlots();
            EnableSlots(slots, images, out int slotsCount);

            curSlots = slotsCount;

            if (slotsCount >= 3)
            {
                btnAddMore.SetActive(false);
            }
            else if (slotsCount > 0)
            {
                btnAddMore.SetActive(true);
                btnLoad.SetActive(true);
            }
            else
            {
                btnLoad.SetActive(false);
            }
        }

        public void AddSlot()
        {
            ShowMenu(false);

            savingWrapper.Load(curSlots + 1);
        }

        public void DeleteSlot(int idx)
        {

        }

        public void EnterDeletingMode()
        {
            //if (selectedSlot.HasValue)
            //    DeleteSlot(selectedSlot.Value);
            deleting = !deleting;

            if (deleting)
            {
                SetSlotColour(Color.red);
            }
            else
            {
                SetSlotColour(Color.white);
            }
        }

        private void SetSlotColour(Color newColour)
        {
            foreach (var item in slots)
            {
                var buttons = item.GameObject.GetComponent<MyItemButton>();
                var colours = buttons.colors;
                colours.highlightedColor = newColour;
                colours.selectedColor = newColour;

                item.GameObject.GetComponent<MyItemButton>().colors = colours;
            }
        }

        private void SelectSlot(int idx)
        {

        }

        private void ShowMenu(bool shouldShow)
        {
            mainMenu.SetActive(shouldShow);
            slotsContainer.SetActive(false);
        }

        private void DisableSlots()
        {
            foreach (var slot in slots)
            {
                slot.GameObject.SetActive(false);
            }

            foreach (var autosave in autoSaves)
            {
                autosave.GameObject.SetActive(false);
            }
        }

        private void EnableSlots(List<(int id, JObject slotData)> slots, List<(int id, Sprite sprite)> images, out int slotsCount)
        {
            slotsCount = 0;

            foreach (var slot in slots)
            {
                SlotUI curSlot = default;
                if (slot.id < 0)
                {
                    curSlot = autoSaves[(slot.id * -1) - 1];
                }
                else
                {
                    curSlot = this.slots[slot.id - 1];
                    ++slotsCount;
                }

                curSlot.Title = slot.id > 0 ? "Guardado " + slot.id : "Autoguardado";
                curSlot.PlayedTime = slot.slotData["TimePlayed"].ToString();
                curSlot.MembersCount = slot.slotData["MembersCount"].ToObject<int>();
                curSlot.GameObject.SetActive(true);


                curSlot.Sprite = GetImage(slot.id);

                Sprite GetImage(int id)
                {
                    if (images is null) return null;

                    foreach (var image in images)
                    {
                        if (image.id == id)
                            return image.sprite;
                    }

                    return null;
                }
            }
        }
    } 
}
