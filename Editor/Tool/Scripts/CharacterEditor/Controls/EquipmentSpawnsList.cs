using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class EquipmentSpawnsList : TreeViewList<SpawnElementData>
    {
        private List<(Transform transform, EquipmentType place)> _snapshot;
        private readonly EnumRegistry _registry;
        private bool _updatingChoices;
        private bool _loading;

        public Action<List<string>> OnChoicesChanged;

        public EquipmentSpawnsList(VisualElement container) : base(container)
        {
            _registry = SavingSystem.LoadEnumRegistry();
            TxtCount.RegisterCallback<DragPerformEvent>(OnBoneDropped);
            OnElementCreated += element =>
            {
                element.place.IsOptionAllowed = id => !_enabledElements.Any(other =>
                    !ReferenceEquals(other, element) && other.place.SelectedId == id);
                element.place.SelectionChanged += _ => UpdatePlaceChoices();
                element.place.RegistryChanged += UpdatePlaceChoices;
            };
            OnElementAdded += _ => UpdatePlaceChoices();
            OnElementRemoved += _ => UpdatePlaceChoices();
        }

        private void UpdatePlaceChoices()
        {
            if (_loading || _updatingChoices)
                return;

            _updatingChoices = true;

            try
            {
                foreach (var element in _enabledElements)
                {
                    if (_registry.GetEntry(typeof(EquipmentType), element.place.SelectedId) == null)
                        element.place.SetValueWithoutNotify(EnumRegistry.NoneId);
                }
                foreach (var element in _enabledElements)
                    element.place.RefreshChoices();

                OnChoicesChanged?.Invoke(GetChoices());
            }
            finally { _updatingChoices = false; }
        }

        private List<string> GetChoices()
        {
            var selected = new HashSet<int>(_enabledElements.Select(e => e.place.SelectedId)
                .Where(id => id != EnumRegistry.NoneId));

            return _registry.GetEntries<EquipmentType>().Where(e => !selected.Contains(e.Id))
                .Select(e => e.Name).ToList();
        }

        private List<(Transform transform, EquipmentType place)> CaptureRows() =>
            _enabledElements.Select(e => (e.SelectedTransform, (EquipmentType)e.place.SelectedId)).ToList();

        public new List<(Transform transform, EquipmentType type)> GetInfo() =>
            CaptureRows().Where(e => e.transform != null && (int)e.place != EnumRegistry.NoneId).ToList();

        private void OnBoneDropped(DragPerformEvent evt)
        {
            var values = DragAndDrop.GetGenericData("DraggedNode") as UnityEngine.Object[];

            if (values == null || values.Length == 0)
                return;

            Add();
            _enabledElements.Last.Value.transform.value = values[0];
        }

        protected override void SetupFoldOut()
        {
            base.SetupFoldOut();
            Foldout.text = "Spawn points";
        }

        private void ApplyRows(List<(Transform transform, EquipmentType place)> data)
        {
            _loading = true;
            try
            {
                DisableAllElements();

                if (data != null)
                    foreach (var item in data)
                    {
                        Add();
                        var row = _enabledElements.Last.Value;
                        row.place.SetValueWithoutNotify((int)item.place);
                        row.transform.SetValueWithoutNotify(item.transform != null ? item.transform.gameObject : null);
                    }
            }
            finally { _loading = false; UpdatePlaceChoices(); }
        }

        public void LoadInfo(List<(Transform transform, EquipmentType place)> newData)
        {
            _snapshot = newData == null ? new() : new(newData);
            ApplyRows(newData);
        }

        public new void UpdateUIData<T>(T newData) where T : List<(Transform transform, EquipmentType place)>
        {
            ApplyRows(newData);
        }

        public override void Clear()
        {
            _loading = true;

            try
            { 
                base.Clear(); _snapshot = null; 
            }
            finally 
            { 
                _loading = false; 
                UpdatePlaceChoices(); 
            }
        }

        public override ModificationTypes Check_Changes()
        {
            var current = CaptureRows();
            if (_snapshot == null)
                return current.Count == 0 ? ModificationTypes.None : ModificationTypes.Add;

            if (current.Count != _snapshot.Count)
                return ModificationTypes.EditData;

            var unmatched = new List<(Transform transform, EquipmentType place)>(_snapshot);

            foreach (var row in current)
            {
                int idx = unmatched.FindIndex(old => old.transform == row.transform && old.place == row.place);

                if (idx < 0)
                    return ModificationTypes.EditData;

                unmatched.RemoveAt(idx);
            }
            return ModificationTypes.None;
        }

        public override void Load_Changes() => ApplyRows(_snapshot);
        public override void Remove_Changes() { _snapshot = null; base.Remove_Changes(); }
    }
}
