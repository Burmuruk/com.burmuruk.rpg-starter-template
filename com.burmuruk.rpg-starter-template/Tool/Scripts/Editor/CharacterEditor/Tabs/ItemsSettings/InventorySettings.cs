using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class InventorySettings : SubWindow, IUIListContainer<BaseCreationInfo>
    {
        const string INFO_INVENTORY_SETTINGS_NAME = "InventorySettings";
        Inventory _changes = default;
        Dictionary<(string name, string type), string> _DropDownIds = new();
        string _selectedType;
        private Label _warning;

        ElementType[] inventoryChoices = new ElementType[]
        {
            ElementType.None,
            ElementType.Item,
            ElementType.Consumable,
            ElementType.Weapon,
            ElementType.Armour,
            ElementType.Ability,
        };

        public event Action<ComponentType> OnElementClicked;

        public Button btnBackInventorySettings { get; private set; }
        public Toggle TglAddInventory { get; private set; }
        public ComponentsListUI<ElementCreation> MClInventoryElements { get; private set; }

        public override void Initialize(VisualElement container)
        {
            _instance = UtilitiesUI.CreateDefaultTab(INFO_INVENTORY_SETTINGS_NAME);
            container.Add(_instance);
            base.Initialize(_instance);

            _warning = _instance.Q<Label>("lblWarning");
            TglAddInventory = _instance.Q<Toggle>("tglAddInventory");
            btnBackInventorySettings = _instance.Q<Button>();
            btnBackInventorySettings.clicked += () => GoBack?.Invoke();
            _instance.Q<VisualElement>(ComponentsList.CONTAINER_NAME);
            MClInventoryElements = new ComponentsListUI<ElementCreation>(_instance);
            Setup_ComponentsList();

            Populate_DDFType();
            Populate_DDFElement();
            RegisterToChanges();
            TglAddInventory.RegisterValueChangedCallback((evt) =>
            {
                EnableContainer(_warning, !evt.newValue);
            });
            //MultiColumnListView lstInventory = new MultiColumnListView();
        }

        private void Setup_ComponentsList()
        {
            MClInventoryElements.OnElementCreated += Setup_Element;
            MClInventoryElements.AddElementExtraData += Add_ElementId;
            MClInventoryElements.DeletionValidator += TryRemove_Element;
            MClInventoryElements.OnComponentClicked += (idx) =>
            {
                var type = (ComponentType)MClInventoryElements.Components[idx].Type;
                OnElementClicked(type);
            };

            MClInventoryElements.DDFType.RegisterValueChangedCallback((evt) => OnValueChanged_EFInventoryType(evt));
            MClInventoryElements.DDFElement.RegisterValueChangedCallback((evt) => OnValueChanged_DDFInventoryElement(evt.newValue));
        }

        private void Setup_Element(ElementCreation creation)
        {
            EnableContainer(creation.IFAmount, true);
            creation.IFAmount.RegisterValueChangedCallback(evt => Update_ElementAmount(evt, creation));
            creation.IFAmount.RegisterCallback<FocusOutEvent>(evt => Check_InvalidAmount(creation, evt));
            creation.RemoveButton.clicked += () => MClInventoryElements.RemoveComponent(creation.idx);
            creation.NameButton.SetEnabled(false);
        }

        private void Check_InvalidAmount(ElementCreation creation, FocusOutEvent evt)
        {
            if (string.IsNullOrEmpty(creation.IFAmount.text))
                creation.IFAmount.SetValueWithoutNotify(MClInventoryElements.Amounts[creation.idx]);
        }

        private void Update_ElementAmount(ChangeEvent<int> evt, ElementCreation creation)
        {
            if (!string.IsNullOrEmpty(creation.IFAmount.text))
                Update_ElementAmount(evt.newValue, creation);
        }

        private void Update_ElementAmount(int value, ElementCreation creation)
        {
            if (value <= 0)
            {
                MClInventoryElements.RemoveComponent(creation.idx);
            }
            else
            {
                var data = SavingSystem.Load(creation.Id);
                if (string.IsNullOrEmpty(data.Id)) return;

                switch (data)
                {
                    case ItemCreationData item:

                        if (value > item.Data.Capacity)
                        {
                            creation.IFAmount.SetValueWithoutNotify(item.Data.Capacity);
                            Notify("Max capacity exceeded", BorderColour.HighlightBorder);
                            return;
                        }

                        break;

                    case BuffUserCreationData buff:
                        if (value > buff.Data.Capacity)
                        {
                            creation.IFAmount.SetValueWithoutNotify(buff.Data.Capacity);
                            Notify("Max capacity exceeded", BorderColour.HighlightBorder);
                            return;
                        }

                        break;

                    default:
                        break;
                }

                MClInventoryElements.ChangeAmount(creation.idx, value);
            }
        }

        private void Add_ElementId(ElementCreation element)
        {
            element.Id = _DropDownIds[(element.NameButton.text, element.Type.ToString())];
            element.IFAmount.value = 1;
        }

        #region Traking changes
        private void RegisterToChanges()
        {
            ElementType[] elements = new ElementType[]
            {
                ElementType.Item,
                ElementType.Consumable,
                ElementType.Weapon,
                ElementType.Armour,
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
            Populate_DDFType();
            Populate_DDFElement();
        }

        public virtual void RenameCreation(in BaseCreationInfo newValue)
        {
            Populate_DDFElement();

            foreach (var component in MClInventoryElements.Components)
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
            Populate_DDFType();
            Populate_DDFElement();

            foreach (var component in MClInventoryElements.Components)
            {
                if (component.Id == newValue.Id)
                {
                    MClInventoryElements.RemoveComponent(component.idx);
                    return;
                }
            }
        }
        #endregion

        private void OnValueChanged_EFInventoryType(ChangeEvent<string> evt)
        {
            Populate_DDFElement();
            _selectedType = evt.newValue;
        }

        private void OnValueChanged_DDFInventoryElement(string name)
        {
            if (name == "None") return;

            int? elementIdx = Check_HasInventoryComponent(name);

            if (elementIdx.HasValue)
            {
                var component = MClInventoryElements[elementIdx.Value];
                Update_ElementAmount(component.IFAmount.value + 1, component);
            }
            else
                MClInventoryElements.AddElement(name, MClInventoryElements.DDFType.value);

            MClInventoryElements.DDFElement.SetValueWithoutNotify("None");
        }

        private void Populate_DDFType()
        {
            MClInventoryElements.DDFType.choices.Clear();
            MClInventoryElements.DDFElement.choices.Clear();
            _DropDownIds.Clear();

            foreach (var type in inventoryChoices)
            {
                if (!SavingSystem.Data.creations.ContainsKey(type))
                    continue;

                string typeName = type.ToString();
                MClInventoryElements.DDFType.choices.Add(type.ToString());

                foreach (var creation in SavingSystem.Data.creations[type])
                {
                    if (creation.Value == null) continue;

                    _DropDownIds.Add((creation.Value.Id, typeName), creation.Key);
                }
            }

            MClInventoryElements.DDFType.SetValueWithoutNotify(Get_SelectedType());
        }

        private void Populate_DDFElement()
        {
            var value = MClInventoryElements.DDFType.value;

            if (!Verify_DDFType(value, out var type))
            {
                MClInventoryElements.DDFElement.SetValueWithoutNotify("None");
                return;
            }

            MClInventoryElements.DDFElement.choices.Clear();

            foreach (var creation in SavingSystem.Data.creations[type])
            {
                MClInventoryElements.DDFElement.choices.Add(creation.Value.Id);
            }

            MClInventoryElements.DDFElement.SetValueWithoutNotify("None");
        }

        /// <summary>
        /// Verifies that the current value exists between the saved creations.
        /// </summary>
        /// <param name="value">Current Value.</param>
        /// <param name="type">Selected type.</param>
        /// <returns></returns>
        private bool Verify_DDFType(string value, out ElementType type)
        {
            type = ElementType.None;

            if (string.IsNullOrEmpty(value) || value == "None")
                return false;

            if (!Enum.TryParse(value, out type)) return false;

            if (!SavingSystem.Data.creations.ContainsKey(type)) return false;

            return true;
        }

        private string Get_SelectedType()
        {
            if (string.IsNullOrEmpty(_selectedType) || _selectedType == "None")
                return "None";

            foreach (var item in _DropDownIds)
            {
                if (item.Key.type.ToString() == _selectedType)
                {
                    return item.Key.type.ToString();
                }
            }

            _selectedType = string.Empty;
            return "None";
        }

        private int? Check_HasInventoryComponent(string name)
        {
            for (int i = 0; i < MClInventoryElements.Components.Count; i++)
            {
                if (!MClInventoryElements.Components[i].element.ClassListContains("Disable") &&
                    MClInventoryElements.Components[i].NameButton.text == name)
                    return i;
            }

            return null;
        }

        public Inventory GetInventory()
        {
            var inventory = new Inventory();
            inventory.items = new();

            for (int i = 0; i < MClInventoryElements.Components.Count; i++)
            {
                if (MClInventoryElements[i].element.ClassListContains("Disable"))
                    continue;

                var curElement = MClInventoryElements[i];
                string type = curElement.Type.ToString();
                string id = _DropDownIds[(curElement.NameButton.text, type)];
                inventory.items[id] = MClInventoryElements.Amounts[i];
            }

            inventory.addInventory = TglAddInventory.value;

            return inventory;
        }

        public void LoadInventoryItems(in Inventory inventory)
        {
            var elements = MClInventoryElements;
            elements.RestartValues();
            var newInventory = inventory;
            TglAddInventory.value = inventory.addInventory;

            if (inventory.items != null)
                foreach (var item in inventory.items)
                {
                    int amount = item.Value;

                    if (!SavingSystem.Data.TryGetCreation(item.Key, out var data, out var type))
                        continue;

                    Action<ElementCreation> ChangeValue = (e) => elements.ChangeAmount(e.idx, item.Value);
                    elements.OnElementAdded += ChangeValue;

                    if (!elements.AddElement(data.Id, type.ToString()))
                        newInventory.items.Remove(item.Key);

                    elements.OnElementAdded -= ChangeValue;
                }

            _changes = newInventory;
        }

        public void UpdateUIData<T>(in T inventory) where T : Inventory
        {
            var elements = MClInventoryElements;
            elements.RestartValues();
            TglAddInventory.value = inventory.addInventory;

            if (inventory.items != null)
                foreach (var item in inventory.items)
                {
                    int amount = item.Value;

                    if (!SavingSystem.Data.TryGetCreation(item.Key, out var data, out var type))
                        continue;

                    Action<ElementCreation> ChangeValue = (e) => elements.ChangeAmount(e.idx, item.Value);
                    elements.OnElementAdded += ChangeValue;
                    elements.OnElementCreated += ChangeValue;

                    elements.AddElement(data.Id, type.ToString());

                    elements.OnElementAdded -= ChangeValue;
                    elements.OnElementCreated -= ChangeValue;
                }
        }

        public override void Clear()
        {
            if (_changes != null)
                _changes.items = null;
            MClInventoryElements.Clear();
            _selectedType = null;
            TglAddInventory.value = false;
            EnableContainer(_warning, false);
        }

        public override void Remove_Changes()
        {
            if (_changes == null) return;

            _changes.items = null;
            _changes.addInventory = false;
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            return true;
        }

        public override ModificationTypes Check_Changes()
        {
            var inventory = GetInventory();

            if (_changes == null) return ModificationTypes.Add;

            if (_changes?.items == null ^ inventory?.items == null)
                return ModificationTypes.EditData;
            else if (_changes?.items?.Count != inventory?.items?.Count)
                return ModificationTypes.EditData;
            else
                foreach (var item in inventory.items)
                {
                    if (!_changes.items.ContainsKey(item.Key) || _changes.items[item.Key] != item.Value)
                        return ModificationTypes.EditData;
                }

            if (_changes.addInventory != TglAddInventory.value)
                return ModificationTypes.EditData;

            return ModificationTypes.None;
        }

        public override void Load_Changes()
        {
            var newData = _changes;
            LoadInventoryItems(newData);
        }

        #region Components List

        private bool TryRemove_Element(int idx)
        {
            if (((ComponentType)MClInventoryElements[idx].Type) == ComponentType.Inventory)
            {
                foreach (var component in MClInventoryElements.Components)
                {
                    if (!component.element.ClassListContains("Disable"))
                    {
                        if ((ComponentType)component.Type == ComponentType.Equipment)
                        {
                            Notify("Equipment requires an Inventory component to store the items", BorderColour.Error);
                            return false;
                        }
                    }
                    else
                        break;
                }
            }

            return true;
        }
        #endregion
    }
}
