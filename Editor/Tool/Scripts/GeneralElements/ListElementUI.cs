using Burmuruk.RPGStarterTemplate.Editor.Controls;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class ListElement : IClearable
    {
        public int idx;

        public string Id { get; set; }

        public virtual void Clear()
        {
            Id = null;
        }
    }

    public class ListElement<T> : ListElement where T : Enum
    {
        private int _type;
        public readonly EnumRegistry registry;

        public virtual int Type
        {
            get => _type;
            set => _type = registry.Contains<T>(value) ? value : EnumRegistry.NoneId;
        }

        public ListElement()
        {
            registry = SavingSystem.LoadEnumRegistry();
        }

        public virtual void Initialize(VisualElement container, int idx) { }

        public virtual void SetType(string name)
        {
            Type = registry.GetId<T>(name);
        }

        public virtual void SetType(int id)
        {
            Type = id;
        }

        public override void Clear()
        {
            base.Clear();
            Type = EnumRegistry.NoneId;
        }
    }

    public abstract class ListElementUI : ListElement
    {
        public VisualElement element;
        public EventCallback<ClickEvent> OnNameClicked;

        public Button NameButton { get; protected set; }
        public Button RemoveButton { get; protected set; }
        public Toggle Toggle { get; protected set; }
        public IntegerField IFAmount { get; protected set; }
        public DropdownField DropDown { get; protected set; }

        public abstract int Type { get; set; }
        public abstract void SetType(string name);
        public abstract void SetType(int id);

        public abstract void Initialize(VisualElement container, int idx);

        public override void Clear()
        {
            base.Clear();
            if (NameButton != null)
                NameButton.text = "";
            if (Toggle != null)
                Toggle.value = false;
            if (IFAmount != null)
                IFAmount.value = default;
            if (DropDown != null)
                DropDown.value = default;
        }
    }

    public class ListElementUI<T> : ListElementUI where T : Enum
    {
        private readonly ListElement<T> _typedElement = new();
        public List<IListComponent<T>> Components { get; private set; } = new();
        public override int Type
        {
            get => _typedElement.Type;
            set
            {
                _typedElement.Type = value;
                foreach (var component in Components)
                {
                    component.SetType(value);
                }
            }
        }

        public ListElementUI()
        {
        }

        public ListElementUI(params IListComponent<T>[] components)
        {
            Components.AddRange(components);
        }

        public override void Initialize(VisualElement container, int idx)
        {
            this.idx = idx;
            element = container;
            NameButton = container.Q<Button>("btnEditComponent");
            RemoveButton = container.Q<Button>("btnRemove");
            Toggle = container.Q<Toggle>();
            IFAmount = container.Q<IntegerField>("txtAmount");
            DropDown = container.Q<DropdownField>();

            Toggle.AddToClassList("Disable");

            foreach (var component in Components)
            {
                component.Initialize(container, idx, this);
            }
        }

        public override void Clear()
        {
            base.Clear();
            _typedElement.Clear();

            foreach (var component in Components)
            {
                if (component is IClearable clearable)
                    clearable.Clear();
            }
        }

        public U GetComponent<U>() where U : class, IListComponent<T>
        {
            foreach (var component in Components)
            {
                if (component is U uComponent)
                {
                    return uComponent;
                }
            }
            return null;
        }

        public override void SetType(string name)
        {
            _typedElement.SetType(name);
            SetType(_typedElement.Type);
        }

        public override void SetType(int id)
        {
            _typedElement.SetType(id);
            foreach (var component in Components)
            {
                component.SetType(Type);
            }
        }

        public EnumRegistry registry => _typedElement.registry;
    }

    public interface IListComponent<T> where T : Enum
    {
        public ListElementUI<T> Parent { get; }
        public void Initialize(VisualElement container, int idx, ListElementUI<T> parent);
        public void SetType(int id);
    }
}
