using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using System;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class ElementCreation : ElementCreationUI
    {
        private ElementType _type = ElementType.None;

        public override Enum Type { get => _type; set => _type = (ElementType)value; }

        public ElementCreation()
        {

        }

        public ElementCreation(VisualElement container, int idx) : base(container, idx)
        {
            EnumField.Init(ElementType.None);
        }

        public override void Initialize(VisualElement container, int idx)
        {
            base.Initialize(container, idx);

            EnumField.Init(ElementType.None);
        }

        public override void SetType(string value)
        {
            Type = Enum.Parse<ElementType>(value);
        }

        public override void Clear()
        {
            base.Clear();
            Type = default(ElementType);
        }
    }

    public class ElementCreationPinnable : ElementCreation
    {
        public bool pinned;
        public Button Pin { get; private set; }

        public ElementCreationPinnable()
        {

        }

        public ElementCreationPinnable(VisualElement container, int idx) : base(container, idx)
        {
            Pin = container.Q<Button>("btnPin");
            UtilitiesUI.EnableContainer(Pin, true);
            pinned = false;
        }

        public override void Initialize(VisualElement container, int idx)
        {
            base.Initialize(container, idx);

            Pin = container.Q<Button>("btnPin");
            UtilitiesUI.EnableContainer(Pin, true);
        }

        public void Swap_BasicInfoWith(ElementCreationPinnable element)
        {
            var (pinned, type, id, name, toggle, amount) =
                (element.pinned,
                element.Type.ToString(),
                element.Id,
                element.NameButton.text,
                element.Toggle.value,
                element.IFAmount.value);

            (element.pinned, element.Id, element.NameButton.text, element.Toggle.value, element.IFAmount.value) =
                (this.pinned, Id, NameButton.text, Toggle.value, IFAmount.value);

            element.SetType(Type.ToString());

            (this.pinned, Id, NameButton.text, Toggle.value, IFAmount.value) =
                (pinned, id, name, toggle, amount);

            SetType(type);
        }

        public void SetInfo(bool pinned, ElementType type, string id, string name)
        {
            this.pinned = pinned;
            SetType(type.ToString());
            Id = id;
            NameButton.text = name;
        }
    }
}
