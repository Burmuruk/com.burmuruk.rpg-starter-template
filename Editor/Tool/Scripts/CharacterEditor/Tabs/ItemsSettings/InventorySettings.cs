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
        Dictionary<(string name, int type), string> _DropDownIds = new();
        int _selectedType;
        private Label _warning;
        EnumRegistry _registry;

        ElementType[] inventoryChoices = new ElementType[]
        {
            ElementType.None,
            ElementType.Item,
            ElementType.Consumable,
            ElementType.Weapon,
            ElementType.Armour,
            ElementType.Ability,
        };

        public event Action<ElementType> OnElementClicked;

        public Button btnBackInventorySettings { get; private set; }
        public Toggle TglAddInventory { get; private set; }
        public ComponentsListUI<ListElementUI<ElementType>> MClInventoryElements { get; private set; }

        public override void Initialize(VisualElement container)
        {
            _instance = UtilitiesUI.CreateDefaultTab(INFO_INVENTORY_SETTINGS_NAME);
            container.Add(_instance);
            _registry = SavingSystem.LoadEnumRegistry();
            base.Initialize(_instance);

            _warning = _instance.Q<Label>("lblWarning");
            TglAddInventory = _instance.Q<Toggle>("tglAddInventory");
            btnBackInventorySettings = _instance.Q<Button>();
            btnBackInventorySettings.clicked += () => GoBack?.Invoke();
            _instance.Q<VisualElement>(ComponentsList.CONTAINER_NAME);
            MClInventoryElements = new ComponentsListUI<ListElementUI<ElementType>>(_instance);
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
            MClInventoryElements.OnComponentClicked += (idx) =>
            {
                var type = (ElementType)MClInventoryElements.Components[idx].Type;
                OnElementClicked?.Invoke(type);
            };

            MClInventoryElements.DDFType.RegisterValueChangedCallback((evt) => OnValueChanged_EFInventoryType(evt));
            MClInventoryElements.DDFElement.RegisterValueChangedCallback((evt) => OnValueChanged_DDFInventoryElement(evt.newValue));
        }

        private void Setup_Element(ListElementUI<ElementType> creation)
        {
            EnableContainer(creation.IFAmount, true);
            creation.IFAmount.RegisterValueChangedCallback(evt => Update_ElementAmount(evt, creation));
            creation.IFAmount.RegisterCallback<FocusOutEvent>(evt => Check_InvalidAmount(creation, evt));
            creation.RemoveButton.clicked += () => MClInventoryElements.RemoveComponent(creation.idx);
            creation.NameButton.SetEnabled(false);
        }

        private void Check_InvalidAmount(ListElementUI<ElementType> creation, FocusOutEvent evt)
        {
            if (string.IsNullOrEmpty(creation.IFAmount.text))
                creation.IFAmount.SetValueWithoutNotify(MClInventoryElements.Amounts[creation.idx]);
        }

        private void Update_ElementAmount(ChangeEvent<int> evt, ListElementUI<ElementType> creation)
        {
            if (!string.IsNullOrEmpty(creation.IFAmount.text))
                Update_ElementAmount(evt.newValue, creation);
        }

        private void Update_ElementAmount(int value, ListElementUI<ElementType> creation)
        {
            if (value <= 0)
            {
                MClInventoryElements.RemoveComponent(creation.idx);
            }
            else
            {
                var data = SavingSystem.Load(creation.Id);
                if (string.IsNullOrEmpty(data.Id))
                    return;

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

        private void Add_ElementId(ListElementUI<ElementType> element)
        {
            element.Id = _DropDownIds[(element.NameButton.text, element.Type)];
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
            _selectedType = _registry.GetId<ElementType>(evt.newValue);
        }

        private void OnValueChanged_DDFInventoryElement(string name)
        {
            if (name == "None")
                return;

            int? elementIdx = Check_HasInventoryComponent(name);

            if (elementIdx.HasValue)
            {
                var component = MClInventoryElements[elementIdx.Value];
                Update_ElementAmount(component.IFAmount.value + 1, component);
            }
            else
                MClInventoryElements.AddElement(name, _selectedType);

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

                MClInventoryElements.DDFType.choices.Add(_registry.GetName<ElementType>((int)type));

                foreach (var creation in SavingSystem.Data.creations[type])
                {
                    if (creation.Value == null)
                        continue;

                    _DropDownIds.Add((creation.Value.Id, (int)type), creation.Key);
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

            int id = _registry.GetId<ElementType>(value);
            if (id == EnumRegistry.NoneId)
                return false;
            type = (ElementType)id;

            if (!SavingSystem.Data.creations.ContainsKey(type))
                return false;

            return true;
        }

        private string Get_SelectedType()
        {
            if (_selectedType == EnumRegistry.NoneId)
                return "None";

            foreach (var item in _DropDownIds)
            {
                if (item.Key.type == _selectedType)
                {
                    return _registry.GetName<ElementType>(item.Key.type);
                }
            }

            _selectedType = EnumRegistry.NoneId;
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
            var inventory = new Inventory
            {
                items = new()
            };

            for (int i = 0; i < MClInventoryElements.Components.Count; i++)
            {
                if (MClInventoryElements[i].element.ClassListContains("Disable"))
                    continue;

                var curElement = MClInventoryElements[i];
                string id = _DropDownIds[(curElement.NameButton.text, curElement.Type)];
                inventory.items[id] = MClInventoryElements.Amounts[i];
            }

            inventory.addInventory = TglAddInventory.value;

            return inventory;
        }

        public void LoadInventoryItems(in Inventory inventory)
        {
            var elements = MClInventoryElements;
            elements.RestartValues();
            TglAddInventory.value = inventory.addInventory;

            if (inventory.items != null)
            {
                foreach (var item in inventory.items)
                {
                    if (!SavingSystem.Data.TryGetCreation(item.Key, out var data, out ElementType type))
                        continue;

                    void ChangeValue(ListElementUI<ElementType> element)
                    {
                        elements.ChangeAmount(element.idx, item.Value);
                    }

                    elements.OnElementAdded += ChangeValue;

                    if (!elements.AddElement(data.Id, (int)type))
                    {
                        // No modifiques inventory.items mientras lo recorres.
                    }

                    elements.OnElementAdded -= ChangeValue;
                }
            }

            _changes = new Inventory
            {
                addInventory = inventory.addInventory,
                items = inventory.items != null
                    ? new Dictionary<string, int>(inventory.items)
                    : null
            };
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

                    if (!SavingSystem.Data.TryGetCreation(item.Key, out var data, out ElementType type))
                        continue;

                    Action<ListElementUI<ElementType>> ChangeValue = (e) => elements.ChangeAmount(e.idx, item.Value);
                    elements.OnElementAdded += ChangeValue;
                    elements.OnElementCreated += ChangeValue;

                    elements.AddElement(data.Id, (int)type);

                    elements.OnElementAdded -= ChangeValue;
                    elements.OnElementCreated -= ChangeValue;
                }
        }

        public override void Clear()
        {
            MClInventoryElements.Clear();
            _selectedType = EnumRegistry.NoneId;
            TglAddInventory.value = false;
            EnableContainer(_warning, false);

            _changes = null;
        }

        public override void Remove_Changes()
        {
            _changes = null;
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            return true;
        }

        public override ModificationTypes Check_Changes()
        {
            var inventory = GetInventory();

            if (_changes == null)
                return ModificationTypes.Add;

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

    }
}
