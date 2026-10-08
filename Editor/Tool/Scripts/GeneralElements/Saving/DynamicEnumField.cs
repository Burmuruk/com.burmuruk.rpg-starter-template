using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class DynamicEnumField : IUIListContainer<EnumModificationData>, IClearable
    {
        private Type enumType;
        private List<EnumEntry> entries = new();
        private EnumRegistry _registry;

        public VisualElement Container { get; private set; }
        public DropdownField DDField { get; private set; }
        public int SelectedId { get; private set; }
        public string Value => DDField.value;
        public Func<int, bool> IsOptionAllowed { get; set; }
        public event Action<int> SelectionChanged;
        // Raised once per scheduler notification, including rename/reorder.
        // RefreshChoices itself does not raise this event, avoiding refresh loops.
        public event Action RegistryChanged;

        public void Init(VisualElement container, Type enumType, int selectedId)
        {
            if (DDField != null)
                throw new InvalidOperationException("Initialize each DynamicEnumField only once.");
            Container = container ?? throw new ArgumentNullException(nameof(container));
            DDField = container.Q<DropdownField>() ??
                throw new InvalidOperationException("Add a DropdownField before calling Init.");
            this.enumType = enumType;
            _registry = SavingSystem.LoadEnumRegistry();
            SelectedId = selectedId;
            RefreshChoices();
            DDField.RegisterValueChangedCallback(OnValueChanged);
            EnumScheduler.Add(ModificationTypes.Add, enumType, this);
            EnumScheduler.Add(ModificationTypes.EditData, enumType, this);
            EnumScheduler.Add(ModificationTypes.Rename, enumType, this);
            EnumScheduler.Add(ModificationTypes.Remove, enumType, this);
        }

        private bool Allowed(int id) => 
            id == EnumRegistry.NoneId || id == SelectedId || IsOptionAllowed == null || IsOptionAllowed(id);

        private int Resolve(int id) => 
            entries.Any(e => e.Id == id) && Allowed(id) ? id : EnumRegistry.NoneId;

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            int oldId = SelectedId;
            int id = _registry.GetEntry(enumType, evt.newValue)?.Id ?? EnumRegistry.NoneId;
            SetValueWithoutNotify(id);
            if (oldId != SelectedId)
                SelectionChanged?.Invoke(SelectedId);
        }

        public void SetEnabled(bool enabled) => DDField.SetEnabled(enabled);

        public void RefreshChoices()
        {
            entries = _registry.GetEntries(enumType).OrderBy(e => e.Order).ToList();
            if (!entries.Any(e => e.Id == SelectedId))
                SelectedId = EnumRegistry.NoneId;
            DDField.choices = entries.Where(e => Allowed(e.Id)).Select(e => e.Name).ToList();
            DDField.SetValueWithoutNotify(entries.FirstOrDefault(e => e.Id == SelectedId)?.Name ?? "None");
        }

        public void SetValueWithoutNotify(int id)
        {
            SelectedId = Resolve(id);
            RefreshChoices();
        }

        public void SetValue(int id)
        {
            int oldId = SelectedId;
            string oldText = Value;
            SelectedId = Resolve(id);
            string newText = entries.FirstOrDefault(e => e.Id == SelectedId)?.Name ?? "None";
            // Preserve normal DropdownField callbacks for existing callers.
            DDField.choices = entries.Where(e => Allowed(e.Id)).Select(e => e.Name).ToList();
            DDField.SetValueWithoutNotify(oldText);
            DDField.value = newText;
            if (oldId != SelectedId)
                SelectionChanged?.Invoke(SelectedId);
        }

        public void Clear() => SetValueWithoutNotify(EnumRegistry.NoneId);

        private void OnRegistryChanged()
        {
            int oldId = SelectedId;
            RefreshChoices();
            if (oldId != SelectedId)
                SelectionChanged?.Invoke(SelectedId);
            RegistryChanged?.Invoke();
        }

        public virtual void AddData(in EnumModificationData value) => OnRegistryChanged();
        public virtual void EditData(in EnumModificationData value) => OnRegistryChanged();
        public virtual void RenameCreation(in EnumModificationData value) => OnRegistryChanged();
        public virtual void RemoveData(in EnumModificationData value) => OnRegistryChanged();
    }
}
