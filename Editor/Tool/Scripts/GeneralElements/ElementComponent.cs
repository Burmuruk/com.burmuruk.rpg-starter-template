using Burmuruk.RPGStarterTemplate.Editor.Controls;
using System;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class ElementComponent2 : ListElement<ComponentType>
    {
        public DynamicEnumField EnumField { get; private set; }


        public override void Initialize(VisualElement container, int idx)
        {
            EnumField ??= new DynamicEnumField();
            EnumField.Init(container, typeof(ComponentType), EnumRegistry.NoneId);

            base.Initialize(container, idx);
        }

        public override void SetType(string name)
        {
            Type = registry.GetId<ComponentType>(name);
        }

        public override void Clear()
        {
            base.Clear();
            Type = EnumRegistry.NoneId;
        }
    }
}
