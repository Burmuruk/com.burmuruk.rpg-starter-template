using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using System;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class ListElementTypedUI<TParent, TEnum> : IListComponent<TParent>
        where TParent : Enum
        where TEnum : Enum
    {
        public ListElementUI<TParent> Parent { get; private set; }
        public DynamicEnumField DynamicEnumField { get; private set; }
        public DropdownField EnumField { get => DynamicEnumField.DDField; }
        private readonly bool _syncWithParent;

        public ListElementTypedUI(bool syncWithParent = true)
        {
            _syncWithParent = syncWithParent;
        }

        public void Initialize(VisualElement container, int idx, ListElementUI<TParent> parent)
        {
            Parent = parent;
            DynamicEnumField ??= new DynamicEnumField();
            DynamicEnumField.Init(container, typeof(TEnum), EnumRegistry.NoneId);
        }

        public void Clear()
        {
            DynamicEnumField.Clear();
        }

        public void SetType(int id)
        {
            if (_syncWithParent)
                DynamicEnumField.SetValueWithoutNotify(id);
        }
    }

    public class ElementCreationPinnable<T> : IListComponent<T> where T : Enum
    {
        public bool pinned;
        public Button Pin { get; private set; }
        public ListElementUI<T> Parent { get; private set; }
        public string Id { get => Parent.Id; }

        public void Initialize(VisualElement container, int idx, ListElementUI<T> parent)
        {
            this.Parent = parent;
            Pin = container.Q<Button>("btnPin");
            UtilitiesUI.EnableContainer(Pin, true);
        }

        public void Swap_BasicInfoWith(ElementCreationPinnable<T> element)
        {
            var (pinned, type, id, name, toggle, amount) =
                (element.pinned,
                Parent.registry.GetName<T>(element.Parent.Type),
                element.Parent.Id,
                element.Parent.NameButton.text,
                element.Parent.Toggle.value,
                element.Parent.IFAmount.value);

            (element.pinned, element.Parent.Id, element.Parent.NameButton.text, element.Parent.Toggle.value, element.Parent.IFAmount.value) =
                (this.pinned, this.Parent.Id, this.Parent.NameButton.text, this.Parent.Toggle.value, this.Parent.IFAmount.value);

            element.Parent.SetType(Parent.registry.GetName<T>(Parent.Type));

            (this.pinned, this.Parent.Id, this.Parent.NameButton.text, this.Parent.Toggle.value, this.Parent.IFAmount.value) =
                (pinned, id, name, toggle, amount);

            Parent.SetType(type);
        }

        public void SetInfo(bool pinned, ElementType type, string id, string name)
        {
            this.pinned = pinned;
            Parent.SetType(Parent.registry.GetName<T>((int)type));
            Parent.Id = id;
            Parent.NameButton.text = name;
        }

        public void SetType(int id)
        {
        }
    }
}
