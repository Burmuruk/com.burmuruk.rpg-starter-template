using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using System.Collections.Generic;
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
        public DropdownField place;
        public string path;

        public VisualElement Container => container;

        public SpawnElementData()
        {
            this.container = new VisualElement();

            transform = new ObjectField("");
            place = new DropdownField();
            place.choices = new List<string>(Enum.GetNames(typeof(EquipmentType)));

            var row1 = InsertInRow(transform, "Spawn point");
            var row2 = InsertInRow(place, "Place");
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
        }

        private void OnBoneDropped(DragPerformEvent evt)
        {
            var values = DragAndDrop.GetGenericData("DraggedNode") as UnityEngine.Object[];

            if (values != null && values.Length > 0)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Debug.Log($"Spawn point: {values[i].name}");
                }
                transform.SetValueWithoutNotify(values[0]);
            }
        }

        public void Clear()
        {
            transform.value = null;
            place.value = default(EquipmentType).ToString();
            Utilities.UtilitiesUI.Set_Tooltip(transform, null, false);
            Utilities.UtilitiesUI.Set_Tooltip(place, null, false);
        }

        public bool VerifyData(out List<string> errors)
        {
            errors = null;
            bool result = true;
            bool isValid = false;
            result &= isValid = transform.value != null;
            Utilities.UtilitiesUI.Set_ErrorTooltip(transform, "Value can't be empty", ref errors, isValid);

            var place = Enum.Parse<EquipmentType>(this.place.value);
            result &= isValid = place != EquipmentType.None && place != EquipmentType.Body;
            Utilities.UtilitiesUI.Set_ErrorTooltip(this.place, "Invalid place", ref errors, isValid);

            return result;
        }

        public ModificationTypes Check_Changes()
        {
            throw new System.NotImplementedException();
        }

        public void Load_Changes()
        {
            throw new System.NotImplementedException();
        }

        public void Remove_Changes()
        {
            throw new System.NotImplementedException();
        }

        public CreationData GetInfo()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateInfo(CreationData cd)
        {
            throw new System.NotImplementedException();
        }
    }
}
