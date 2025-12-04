using System;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class ElementComponent : ElementCreationUI
    {
        private ComponentType _type = ComponentType.None;

        public override Enum Type { get => _type; set => _type = (ComponentType)value; }

        public ElementComponent()
        {

        }

        public ElementComponent(VisualElement container, int idx) : base(container, idx)
        {
            EnumField.Init(ComponentType.None);
        }

        public override void Initialize(VisualElement container, int idx)
        {
            base.Initialize(container, idx);

            EnumField.Init(ComponentType.None);
        }

        public override void SetType(string value)
        {
            Type = Enum.Parse<ComponentType>(value);
        }

        public override void Clear()
        {
            base.Clear();
            Type = default(ComponentType);
        }
    }
}
