using Burmuruk.RPGStarterTemplate.Editor.Controls;
using System;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public abstract class ElementCreationUI : IClearable
    {
        public int idx;
        public VisualElement element;
        public EventCallback<ClickEvent> OnNameClicked;

        public string Id { get; set; }
        public Button NameButton { get; protected set; }
        public Button RemoveButton { get; protected set; }
        public Toggle Toggle { get; private set; }
        public IntegerField IFAmount { get; private set; }
        public EnumField EnumField { get; private set; }
        public abstract Enum Type { get; set; }

        public ElementCreationUI()
        {

        }

        public ElementCreationUI(VisualElement container, int idx)
        {
            Initialize(container, idx);
        }

        public virtual void Initialize(VisualElement container, int idx)
        {
            this.idx = idx;
            element = container;
            NameButton = container.Q<Button>("btnEditComponent");
            RemoveButton = container.Q<Button>("btnRemove");
            Toggle = container.Q<Toggle>();
            IFAmount = container.Q<IntegerField>("txtAmount");
            EnumField = container.Q<EnumField>();

            Toggle.AddToClassList("Disable");
        }

        public abstract void SetType(string value);

        public virtual void Clear()
        {
            Id = null;
            NameButton.text = "";
            Toggle.value = false;
            IFAmount.value = default;
            EnumField.value = default;
        }
    }
}
