using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class SearchBar : IUIListContainer<BaseCreationInfo>
    {
        VisualElement _container;
        (ElementType type, string text) lastLeftSearch = default;
        ElementType currentFilter = ElementType.None;
        List<TagData> tagButtons = new();
        bool _showCustomColour, _showElementColour;

        public event Action<ElementType> OnElementDeleted;

        public int Count { get => Creations.Components.Count; }
        public bool ShowElementColour
        {
            get => _showElementColour;
            set
            {
                _showElementColour = value;
                SearchAllElements();
            }
        }
        public bool ShowCustomColour
        {
            get => _showCustomColour;
            set
            {
                _showCustomColour = value;
                SearchAllElements();
            }
        }
        public TextField txtSearch_Left { get; private set; }
        public Button BtnClearSearch { get; private set; }
        public VisualElement InfoPanel { get; private set; }
        public ComponentsList<ElementCreationPinnable> Creations { get; private set; }
        public ElementCreationPinnable this[int idx]
        {
            get => Creations[idx];
            set => Creations[idx] = value;
        }
        public ElementType CurrentFilter
        {
            get => currentFilter;
            set
            {
                if (!tagButtons.Where(t => t.type == value).Any())
                    return;

                currentFilter = value;
                SearchAllElements(currentFilter);
            }
        }

        public SearchBar(VisualElement container, int initialAmount, bool addFilters)
        {
            _container = container;
            Creations = new(container);

            BtnClearSearch = container.Q<Button>("btnClearSearch");
            InfoPanel = container.Q<VisualElement>("elementsContainer");
            txtSearch_Left = container.Q<TextField>("txtSearch");
            Create_Elements(initialAmount);

            if (addFilters) Create_TagButtons();

            BtnClearSearch.clicked += SearchAllElements;
            txtSearch_Left.RegisterCallback<KeyDownEvent>(KeyDown_SearchBar);
            RegisterToChanges();
            SearchAllElements();
        }

        private void Create_Elements(int amount)
        {
            Creations.OnElementCreated += (ElementCreationPinnable element) =>
            {
                element.RemoveButton.clicked += () =>
                {
                    RemoveCreation(element.idx);
                };
                element.Pin.clicked += () => PinElement(element.idx);
            };
            Creations.CreationValidator += delegate { return -1; };

            for (int i = 0; i < amount; ++i)
            {
                Creations.AddElement(i.ToString());
            }
        }

        private void RemoveCreation(int elementIdx)
        {
            var type = (ElementType)Creations[elementIdx].Type;
            string id = Creations[elementIdx].Id;

            if (!SavingSystem.Remove(type, id)) return;

            OnElementDeleted?.Invoke(type);
            Notify("Element deleted.", BorderColour.Success);
        }

        private void Create_TagButtons()
        {
            var tags = _container.Q<VisualElement>("TagsContainer").Query<Button>(className: "FilterTag").ToList();
            int idx = 0;

            foreach (var btnTag in tags)
            {
                var newButton = new TagData(idx, btnTag, ElementType.None);
                tagButtons.Add(newButton);

                if (idx < SavingSystem.Data.defaultElements.Count)
                {
                    ElementType element = SavingSystem.Data.defaultElements[idx];
                    btnTag.text = element.ToString();
                    int i = idx;
                    newButton.element.clicked += () => OnClicked_FilterTag(i, element);
                    EnableContainer(btnTag, true);
                }
                else
                    EnableContainer(btnTag, false);

                ++idx;
            }
        }

        private void DisableElements()
        {
            foreach (var element in Creations.Components)
            {
                if (element.pinned) continue;

                if (element.element.ClassListContains("Disable"))
                    return;

                EnableContainer(element.element, false);
            }
        }

        private BorderColour GetElementColour(ElementType type) =>
            type switch
            {
                ElementType.Character => BorderColour.CharacterBorder,
                //ElementType.State => BorderColour.StateBorder,
                ElementType.Buff => BorderColour.BuffBorder,
                ElementType.Armour => BorderColour.ArmorBorder,
                ElementType.Weapon => BorderColour.WeaponBorder,
                ElementType.Consumable => BorderColour.ConsumableBorder,
                ElementType.Item => BorderColour.ItemBorder,
                _ => BorderColour.None
            };

        #region tracking changes
        private void RegisterToChanges()
        {
            ElementType[] elements = new ElementType[]
            {
                ElementType.Item,
                ElementType.Consumable,
                ElementType.Weapon,
                ElementType.Armour,
                ElementType.Buff,
                ElementType.Ability,
                ElementType.Character,
            };

            foreach (var element in elements)
                CreationScheduler.Add(ModificationTypes.Rename, element, this);

            foreach (var element in elements)
                CreationScheduler.Add(ModificationTypes.Remove, element, this);

            foreach (var element in elements)
                CreationScheduler.Add(ModificationTypes.Add, element, this);
        }

        public virtual void AddData(in BaseCreationInfo newValue)
        {
            SearchAllElements();

            foreach (var item in Creations.Components)
            {
                if (item.Id == newValue.Id)
                {
                    Highlight(item.element, false);
                    item.element.Children().First().AddToClassList("creation--left");

                    item.element.Children().First().schedule.Execute(() =>
                    {
                        item.element.Children().First().AddToClassList("creationNotification");
                        item.element.Children().First().RemoveFromClassList("creation--left");

                        item.element.Children().First().schedule.Execute(() =>
                        {
                            item.element.Children().First().RemoveFromClassList("creationNotification");
                        }).ExecuteLater(2000);
                    }).ExecuteLater(500);
                }
            }
        }

        public virtual void RenameCreation(in BaseCreationInfo newValue)
        {
            foreach (var component in Creations.Components)
            {
                if (component.Id == newValue.Id)
                {
                    component.NameButton.text = newValue.Name;
                    return;
                }
            }
        }

        public virtual void RemoveData(in BaseCreationInfo newValue)
        {
            for (int i = 0; i < Creations.Components.Count; i++)
            {
                if (IsDisabled(Creations[i].element)) break;

                if (Creations.Components[i].Id == newValue.Id)
                {
                    Creations.RemoveComponent(i);
                    return;
                }
            }
        } 
        #endregion

        #region events
        private void KeyDown_SearchBar(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return) return;

            string text = txtSearch_Left.value.Trim();

            SearchElementTag(text, currentFilter);
            txtSearch_Left.Focus();
        }

        private void OnClicked_FilterTag(int idx, ElementType type)
        {
            if (IsHighlighted(tagButtons[idx].element))
            {
                currentFilter = ElementType.None;
                Highlight(tagButtons[idx].element, false);

                if (lastLeftSearch.type == ElementType.None)
                    SearchAllElements();
                else
                    SearchElementTag(txtSearch_Left.value, currentFilter);
            }
            else
            {
                currentFilter = type;

                tagButtons.ForEach(b => Highlight(b.element, false));
                Highlight(tagButtons[idx].element, true);

                if (lastLeftSearch.type == ElementType.None)
                {
                    SearchAllElements(type);
                }
                else
                {
                    SearchElementTag(txtSearch_Left.value, lastLeftSearch.type);
                }
            }
        }
        #endregion

        #region Search
        private void SearchElementTag(string text, ElementType searchType)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (currentFilter != ElementType.None) searchType = currentFilter;

            List<(ElementType, List<string>)> idsFound = null;

            if (searchType == ElementType.None)
            {
                int count = Enum.GetValues(typeof(ElementType)).Length;

                for (int i = 0; i < count; i++)
                {
                    if (!SavingSystem.Data.creations.ContainsKey((ElementType)i))
                        continue;

                    if (FindValues(text, (ElementType)i, out List<string> found))
                    {
                        Debug.Log("Found without filter");
                        idsFound ??= new();

                        idsFound.Add(((ElementType)i, found));
                    }
                }
            }
            else if (FindValues(text, searchType, out List<string> found))
            {
                Debug.Log("found int filter");
                idsFound ??= new();
                idsFound.Add((searchType, found));
            }

            if (idsFound != null && idsFound.Count > 0)
            {
                Debug.Log("somenting found");
                TryEnableCharacterElements(idsFound);
                lastLeftSearch = (searchType, text);
            }
            else
            {
                Debug.Log("no items found");
                DisableElements();
                lastLeftSearch = (ElementType.None, "");
            }

            bool FindValues(string text, ElementType type, out List<string> valuesIds)
            {
                valuesIds = (from c in SavingSystem.Data.creations[type]
                             where c.Value.Id.ToLower().Contains(text.ToLower())
                             select c.Key).ToList();

                return valuesIds.Count > 0;
            }
        }

        public void SearchAllElements()
        {
            SearchAllElements(ElementType.None);
        }

        public void SearchAllElements(ElementType type)
        {
            if (currentFilter != ElementType.None)
                type = currentFilter;

            lastLeftSearch = (ElementType.None, "");
            txtSearch_Left.value = "";
            List<(ElementType type, List<string> ids)> values = new();

            if (type == ElementType.None)
            {
                foreach (var curType in SavingSystem.Data.creations.Keys)
                {
                    var ids = SavingSystem.Data.creations[curType].Select(creation => creation.Key).ToList();

                    if (ids != null && ids.Count > 0)
                        values.Add((curType, ids));
                }
            }
            else if (SavingSystem.Data.creations.ContainsKey(type))
            {
                var ids = SavingSystem.Data.creations[type].Select(creation => creation.Key).ToList();

                if (ids != null && ids.Count > 0)
                    values.Add((type, ids));
            }
            else
            {
                DisableElements();
                return;
            }

            if (values.Count > 0)
            {
                TryEnableCharacterElements(values);
            }
            else
                DisableElements();
        }

        private bool TryEnableCharacterElements(List<(ElementType type, List<string> ids)> elements)
        {
            bool enabled = false;

            if (elements == null || elements.Count == 0) return false;

            var pinned = new List<ElementCreationPinnable>();
            int idx = 0;
            while (idx < Creations.Components.Count && Creations[idx].pinned)
                pinned.Add(Creations[idx++]);

            int elementIdx = pinned.Count;

            for (int i = 0; i < elements.Count && elementIdx < Creations.Components.Count; i++)
            {
                for (int j = 0; j < elements[i].ids.Count; j++)
                {
                    for (int pinIdx = 0; pinIdx < pinned.Count; pinIdx++)
                    {
                        if (elements[i].ids[j] == pinned[pinIdx].Id)
                        {
                            elements[i].ids.RemoveAt(j--);
                            pinned.RemoveAt(pinIdx);
                            goto nextTurn;
                        }
                    }

                    Creations[elementIdx].SetInfo(false, elements[i].type, elements[i].ids[j], GetName(i, j));
                    EnableContainer(Creations[elementIdx].element, true);

                    if (ShowElementColour)
                    {
                        if (ShowCustomColour)
                        {
                            //Show custom colour on element
                        }
                        else
                            Highlight(Creations[elementIdx].element, true, GetElementColour(elements[i].type));
                    }

                    elementIdx++;
                    enabled = true;

                nextTurn:
                    ;
                }
            }

            for (int i = elementIdx; i < Creations.Components.Count; i++)
            {
                Creations[i].NameButton.text = "";
                EnableContainer(Creations[i].element, false); 
            }

            return enabled;

            string GetName(int i, int j) =>
                SavingSystem.Data.creations[elements[i].type][elements[i].ids[j]]?.Id;
        }
        #endregion

        #region Pins
        private void PinElement(int elementIdx)
        {
            var element = Creations[elementIdx];

            if (element.pinned)
            {
                UnpinAndMove(elementIdx);
            }
            else
            {
                PinAndMove(elementIdx);
            }

            UpdateIndices();
        }

        private void PinAndMove(int idx)
        {
            // Encuentra la última posición con pin
            int insertAt = 0;
            for (int i = 0; i < Creations.Components.Count; i++)
            {
                if (IsDisabled(Creations[i].element)) break;
                if (Creations[i].pinned) 
                    insertAt = i + 1;
                else if (i == 0)
                {
                    insertAt = 0;
                    break;
                }
                else
                {
                    insertAt = i;
                    break;
                }
            }

            var element = Creations[idx];
            AnimateMove(element.element, idx, insertAt);
            element.pinned = true;
            Highlight(element.Pin, true);

            if (idx != insertAt)
            {
                var temp = Creations[idx];
                Creations.Components.RemoveAt(idx);
                Creations.Components.Insert(insertAt, temp);
                Creations.Container.Remove(temp.element);
                Creations.Container.Insert(insertAt, temp.element);
            }
        }

        private void UnpinAndMove(int idx)
        {
            // Encuentra la última posición con pin
            int insertAt = 0;
            for (int i = 0; i < Creations.Components.Count; i++)
            {
                if (IsDisabled(Creations[i].element)) break;
                if (Creations[i].pinned) insertAt = i;
            }

            var element = Creations[idx];
            element.pinned = false;
            Highlight(element.Pin, true, BorderColour.LightBorder);

            if (idx != insertAt)
            {
                var temp = Creations[idx];
                Creations.Components.RemoveAt(idx);
                Creations.Components.Insert(insertAt, temp);
                Creations.Container.Remove(temp.element);
                Creations.Container.Insert(insertAt, temp.element);
            }
        }

        private void UpdateIndices()
        {
            for (int i = 0; i < Creations.Components.Count; i++)
            {
                Creations[i].idx = i;
            }
        }

        private void AnimateMove(VisualElement element, int fromIndex, int toIndex)
        {
            var height = element.resolvedStyle.height + element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom;
            float offset = (toIndex - fromIndex) * height;

            if (offset == 0) return;

            element.transform.position = new Vector3(0, 0, 0);
            element.transform.position = new Vector3(0, -offset, 0);
            element.experimental.animation
                .Position(Vector3.zero, 250)
                .Start();
        }
        #endregion
    }
}
