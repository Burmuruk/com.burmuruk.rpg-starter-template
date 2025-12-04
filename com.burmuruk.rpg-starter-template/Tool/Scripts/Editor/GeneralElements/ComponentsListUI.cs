using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ComponentsListUI<T> : ComponentsList<T> where T : ElementCreationUI, IClearable, new()
    {
        public DropdownField DDFType { get; private set; }
        public DropdownField DDFElement { get; private set; }

        public ComponentsListUI(VisualElement container) : base(container)
        {
            DDFType = container.Q<DropdownField>("ddfType");
            DDFElement = container.Q<DropdownField>("ddfElement");

            VisualElement element = new VisualElement();
            element.style.height = 50;
            element.style.width = 30;
            container.hierarchy.Add(element);
        }

        public void AddComponent(ChangeEvent<string> evt)
        {
            if (Contains(evt.newValue))
            {
                DDFElement.SetValueWithoutNotify("None");
                return;
            }

            if (DDFType == null)
                AddElement(evt.newValue);
            else
                AddElement(evt.newValue, DDFType.value);

            DDFElement.SetValueWithoutNotify("None");
        }

        public override void Clear()
        {
            base.Clear();

            DDFType?.SetValueWithoutNotify("None");
            DDFElement?.SetValueWithoutNotify("None");
        }
    }
}
