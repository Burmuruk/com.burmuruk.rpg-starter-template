using System;
using UnityEngine.UIElements;
using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using Burmuruk.RPGStarterTemplate.Stats;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ElementStatVariable : ElementCreation<ModifiableStat>
    {
        private VariableType _variableType;

        public Label LblHeader { get; set; }
        public Label LblVariableType { get; set; }
        public Label LblOldName { get; set; }
        public Label Modification { get; private set; }
        public string OldName { get; set; }
        public string NewName { get => NameButton.text; }
        public VariableType VariableType
        {
            get => _variableType;
            set
            {
                _variableType = value;
                LblVariableType.text = _variableType.ToString();
            }
        }

        public override void Initialize(VisualElement container, int idx)
        {
            this.idx = idx;
            element = container;

            LblHeader = container.Q<Label>("lbHeader");
            LblVariableType = container.Q<Label>("lbType");
            LblOldName = container.Q<Label>("lbValue");
            Modification = container.Q<Label>("lbModification");
            RemoveButton = container.Q<Button>("btnRemove");

            NameButton = container.Q<Button>("btnName");
        }
    }
}
