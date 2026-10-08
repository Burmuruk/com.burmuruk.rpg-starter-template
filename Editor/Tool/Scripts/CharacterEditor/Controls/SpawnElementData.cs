using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class SpawnElementData : IClearable, IVElement, IChangesObserver, IDataProvider, IUpdatableUI
    {
        public VisualElement container;
        public ObjectField transform;
        public DynamicEnumField place;
        public string path;

        EnumRegistry _registry;
        private SpawnRowData _snapshot;
        public Transform SelectedTransform => transform.value is GameObject go
            ? go.transform : transform.value as Transform;

        public VisualElement Container => container;

        public SpawnElementData()
        {
            this.container = new VisualElement();

            _registry = SavingSystem.LoadEnumRegistry();
            transform = new ObjectField("");
            place = new DynamicEnumField();
            container.Add(new DropdownField());
            place.Init(container, typeof(EquipmentType), EnumRegistry.NoneId);
            place.SelectionChanged += a => VerifyData(out _);

            var row1 = InsertInRow(transform, "Spawn point");
            var row2 = InsertInRow(place.DDField, "Place");
            row2.style.marginBottom = 6;
            container.Add(row1);
            container.Add(row2);

            Setup_Transform();
        }

        private VisualElement InsertInRow(VisualElement element, string name)
        {
            var row = Get_Row();
            Label label = new Label();
            label.AddToClassList("ElementTag");
            label.style.flexShrink = 2;
            label.style.flexGrow = 0;
            label.style.maxWidth = 20;
            label.style.minWidth = new StyleLength(StyleKeyword.None);
            label.style.maxWidth = new StyleLength(StyleKeyword.None);
            label.style.paddingRight = 5;
            label.text = name;
            element.AddToClassList("LineElements");
            element.style.flexShrink = 1;
            element.style.flexGrow = 1;
            element.style.flexBasis = new Length(50, LengthUnit.Percent);

            row.Add(label);
            row.Add(element);
            return row;
        }

        private VisualElement Get_Row()
        {
            VisualElement row = new VisualElement();

            row.AddToClassList("LineContainer");
            return row;
        }

        private void Setup_Transform()
        {
            transform.objectType = typeof(GameObject);

            transform.RegisterCallback<DragEnterEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            transform.RegisterCallback<DragPerformEvent>(OnBoneDropped);
            transform.RegisterValueChangedCallback(a => { VerifyData(out _); });
        }

        private void OnTransformChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            VerifyData(out _);
        }

        private void OnBoneDropped(DragPerformEvent evt)
        {
            var values = DragAndDrop.GetGenericData("DraggedNode") as UnityEngine.Object[];

            if (values != null && values.Length > 0)
            {
                transform.value = values[0];
            }
        }

        public void Clear()
        {
            _snapshot = null;
            path = null;
            transform.SetValueWithoutNotify(null);
            place.Clear();
            Utilities.UtilitiesUI.Set_Tooltip(transform, null, false);
            Utilities.UtilitiesUI.Set_Tooltip(place.DDField, null, false);
        }

        public bool VerifyData(out List<string> errors)
        {
            errors = null;
            bool result = true;
            bool isValid = false;
            result &= isValid = transform.value != null;
            Utilities.UtilitiesUI.Set_ErrorTooltip(transform, "Value can't be empty", ref errors, isValid);

            result &= isValid = place.SelectedId != EnumRegistry.NoneId;
            Utilities.UtilitiesUI.Set_ErrorTooltip(place.DDField, "Invalid place", ref errors, isValid);

            return result;
        }

        public ModificationTypes Check_Changes()
        {
            if (_snapshot == null)
                return SelectedTransform != null || place.SelectedId != EnumRegistry.NoneId
                    ? ModificationTypes.Add : ModificationTypes.None;
            return SelectedTransform != _snapshot.transform || place.SelectedId != _snapshot.placeId || path != _snapshot.path
                ? ModificationTypes.EditData : ModificationTypes.None;
        }

        public void Load_Changes() => Apply(_snapshot);
        public void Remove_Changes() => _snapshot = null;
        public CreationData GetInfo() => new SpawnRowData
        {
            transform = SelectedTransform,
            placeId = place.SelectedId,
            path = this.path
        };

        public void UpdateInfo(CreationData cd)
        {
            if (cd is not SpawnRowData data)
                return;
            _snapshot = new SpawnRowData { transform = data.transform, placeId = data.placeId, path = data.path };
            Apply(data);
        }

        //public void UpdateUIData<T>(T cd) where T : CreationData
        //{
        //    if (cd is SpawnRowData data)
        //        Apply(data);
        //}

        private void Apply(SpawnRowData data)
        {
            transform.value = data?.transform != null ? data.transform.gameObject : null;
            place.SetValue(data?.placeId ?? EnumRegistry.NoneId);
            path = data?.path;
        }
    }

    // UI snapshot only; EquipmentSpawnsList.GetInfo supplies the existing persistence format.
    public class SpawnRowData : CreationData
    {
        public Transform transform;
        public int placeId;
        public string path;
        public SpawnRowData() : base(null) { }
    }
}
