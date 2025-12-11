using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class DropElementData : IClearable, IVElement, IChangesObserver, IDataProvider, IUpdatableUI
    {
        private VisualElement container;
        private GameObject _changes;

        public VisualElement Container => container;
        public Label Label { get; private set; }
        public ObjectField Drop { get; set; }

        public DropElementData()
        {
            this.container = new VisualElement();
            Drop = new ObjectField("")
            {
                allowSceneObjects = false
            };

            var row1 = InsertInRow(Drop, "Model");
            container.Add(row1);
            //EditorApplication.delayCall += () =>
            Drop.objectType = typeof(GameObject);
        }

        private VisualElement InsertInRow(VisualElement element, string name)
        {
            var row = Get_Row();
            Label label = new Label()
            {
                style =
                {
                    flexShrink = 2,
                    flexGrow = 0,
                    flexBasis = 103,
                    minWidth = new StyleLength(StyleKeyword.None),
                    //maxWidth = new StyleLength(StyleKeyword.None),
                    paddingRight = 5
                },
                text = name
            };
            //label.AddToClassList("ElementTag");
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

        public void Clear()
        {
            Drop.value = null;
            _changes = null;
        }

        public ModificationTypes Check_Changes()
        {
            if (_changes == null && Drop.value != null)
                return ModificationTypes.Add;

            return _changes == Drop.value ? ModificationTypes.None : ModificationTypes.EditData;
        }

        public void Load_Changes()
        {
            Drop.value = _changes;
        }

        public void Remove_Changes()
        {
            _changes = null;
        }

        public bool VerifyData(out List<string> errors)
        {
            errors = null;
            return Drop.value != null;
        }

        public CreationData GetInfo()
        {
            return Drop.value == null ? null : new DropData(SavingSystem.GetAssetReference(Drop.value));
        }

        public void UpdateInfo(CreationData cd)
        {
            _changes = SavingSystem.GetAsset<GameObject>((cd as DropData).Name);
            Load_Changes();
        }

        public void UpdateUIData<T>(T arg)
        {
            if (arg is not DropData cd) return;

            Drop.value = SavingSystem.GetAsset<GameObject>(cd.Name);
        }
    }

    public class DropData : CreationData
    {
        public string Name { get => Id; }

        public DropData(string name) : base(null)
        {
            Id = name;
        }
    }
}
