using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class EnumModifierUI<T> : IClearable where T : Enum
    {
        public const string ContainerName = "EnumModifier";
        EnumRegistry registry;
        string _path = null;
        State state = State.None;

        enum State
        {
            None,
            Adding,
            Editing,
            Removing
        }

        public VisualElement Container { get; private set; }
        public Label Name { get; private set; }
        public Button BtnAddValue { get; private set; }
        public Button BtnRemoveValue { get; private set; }
        public Button BtnEditValue { get; private set; }
        public TextField TxtNewValue { get; private set; }
        public VisualElement EnumContainer { get; private set; }
        public VisualElement NewValueContainer { get; private set; }
        public int Id { get => DEnumField.SelectedId; set => DEnumField.SetValue(value); }
        public string Text
        {
            get => DEnumField.Value;
            private set => DEnumField.SetValue(registry.GetId<T>(value));
        }
        private State CurrentState
        {
            get => state;
            set
            {
                HighlightButton(false);
                state = value;
                HighlightButton(true);
            }
        }
        public DynamicEnumField DEnumField { get; set; }
        public DropdownField EnumField => DEnumField.DDField;

        public EnumModifierUI(VisualElement container)
        {
            this.Container = container;
            registry = SavingSystem.LoadEnumRegistry();
            BtnEditValue = container.Q<Button>("btnEditValue");
            BtnRemoveValue = container.Q<Button>("btnRemoveValue");
            BtnAddValue = container.Q<Button>("btnAddValue");
            DEnumField = new();
            TxtNewValue = container.Q<TextField>("txtNewEnumValue");
            EnumContainer = container.Q<VisualElement>("EnumLine");
            Name = EnumContainer.Q<Label>("lblName");
            NewValueContainer = container.Q<VisualElement>("NewElementLine");

            BtnEditValue.clicked += OnClick_EditValue;
            BtnRemoveValue.clicked += OnClick_RemoveValue;
            BtnAddValue.clicked += () => OnClick_AddButton();
            DEnumField.Init(Container, typeof(T), (int)(object)default(T));
            TxtNewValue.RegisterCallback<KeyDownEvent>(OnKeyDown_TxtCharacterType, TrickleDown.TrickleDown);
            TxtNewValue.tooltip = "Press Enter to confirm changes";

            FindEnumPath();
            EnableContainer(NewValueContainer, false);
        }

        private void FindEnumPath()
        {
            string[] guids = AssetDatabase.FindAssets(typeof(T).Name + " t:Script");
            if (guids.Length > 0)
            {
                _path = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
        }

        private EnumEntry GetSelectedEntry()
        {
            return registry
                .GetEntries(typeof(T))
                .FirstOrDefault(x => x.Name == DEnumField.Value);
        }

        private void OnClick_EditValue()
        {
            EnumEntry entry = GetSelectedEntry();

            if (entry == null || entry.Id == EnumRegistry.NoneId)
                return;

            bool shouldShow = CurrentState != State.Editing;

            if (shouldShow)
            {
                BtnEditValue.text = "^";
                TxtNewValue.SetValueWithoutNotify(entry.Name);
                ShowElements(true);
                CurrentState = State.Editing;
            }
            else
            {
                BtnEditValue.text = "Edit";
                ShowElements(false);
                CurrentState = State.None;
            }
        }

        private void OnClick_RemoveValue()
        {
            EnumEntry entry = GetSelectedEntry();

            if (entry == null || entry.Id == EnumRegistry.NoneId)
                return;

            if (!EditorUtility.DisplayDialog(
                    "Enum modification",
                    $"Remove '{entry.Name}'?",
                    "Continue",
                    "Cancel"))
            {
                return;
            }

            try
            {
                registry.Remove(typeof(T), entry.Id);

                Notify("Changes made", BorderColour.Success);
                ShowElements(false);
                DEnumField.Clear();
                CurrentState = State.None;
            }
            catch (Exception e)
            {
                Notify(e.Message, BorderColour.Error);
            }
        }

        private void OnClick_AddButton()
        {
            bool shouldShow = CurrentState != State.Adding;

            if (shouldShow)
            {
                BtnAddValue.text = "^";
                TxtNewValue.SetValueWithoutNotify(string.Empty);
                ShowElements(true);
                BtnAddValue.SetEnabled(true);
                CurrentState = State.Adding;
            }
            else
            {
                BtnAddValue.text = "+";
                ShowElements(false);
                CurrentState = State.None;
            }
        }

        private void OnKeyDown_TxtCharacterType(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            evt.StopPropagation();

            string newName = TxtNewValue.value;

            if (!VerifyVariableName(newName))
            {
                Notify("Not valid name", BorderColour.Error);
                return;
            }

            if (IsNameInUse(newName.ToLowerInvariant()))
            {
                Notify("Name in use", BorderColour.Error);
                return;
            }

            try
            {
                switch (CurrentState)
                {
                    case State.Adding:
                        {
                            EnumEntry entry = registry.Add(typeof(T), newName);
                            Clear();
                            DEnumField.SetValueWithoutNotify(entry.Id);
                            break;
                        }

                    case State.Editing:
                        {
                            EnumEntry entry = GetSelectedEntry();

                            if (entry == null)
                                return;

                            registry.Rename(typeof(T), entry.Id, newName);
                            Clear();
                            DEnumField.SetValueWithoutNotify(entry.Id);
                            break;
                        }

                    default:
                        return;
                }
            }
            catch (InvalidDataExeption e)
            {
                Notify(e.Message, BorderColour.Error);
                return;
            }

            Notify("Changes made", BorderColour.Success);
        }

        private bool IsNameInUse(string newName)
        {
            foreach (string name in Enum.GetNames(typeof(T)))
            {
                if (name.ToLower() == newName)
                {
                    Notify("The name already exists", BorderColour.Error);
                    return true;
                }
            }

            return false;
        }

        private void ShowElements(bool shouldShow = true)
        {
            DEnumField.SetEnabled(!shouldShow);
            BtnAddValue.SetEnabled(!shouldShow);
            BtnRemoveValue.SetEnabled(!shouldShow);
            EnableContainer(NewValueContainer, shouldShow);
        }

        private void HighlightButton(bool shouldHighlight)
        {
            Button button = GetStateButton();

            if (button == null)
            {
                var curState = this.state;
                var states = Enum.GetValues(typeof(State)).Cast<State>();

                foreach (var state in states.Where(s => GetStateButton(s) != null))
                {
                    this.state = state;
                    HighlightButton(false);
                }
                this.state = curState;
            }
            else
                Highlight(button, shouldHighlight, BorderColour.SpecialChange);
        }

        private Button GetStateButton()
        {
            return GetStateButton(this.state);
        }

        private Button GetStateButton(State state)
        {
            return state switch
            {
                State.Adding => BtnAddValue,
                State.Editing => BtnEditValue,
                _ => null
            };
        }

        public virtual void Clear()
        {
            state = State.None;
            DEnumField.SetValue(EnumRegistry.NoneId);
            ShowElements(false);
            HighlightButton(false);
            BtnAddValue.text = "+";
            BtnEditValue.text = "Edit";
        }
    }
}
