using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.RPGStarterTemplate.Utilities;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class EnumModifierUI<T> : IClearable, IUIListContainer<EnumModificationData> where T : Enum
    {
        public const string ContainerName = "EnumModifier";
        EnumEditor enumEditor = new();
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
        public EnumField EnumField { get; private set; }
        public TextField TxtNewValue { get; private set; }
        public VisualElement EnumContainer { get; private set; }
        public VisualElement NewValueContainer { get; private set; }
        public T Value { get => (T)EnumField.value; set => EnumField.value = value; }
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

        public EnumModifierUI(VisualElement container)
        {
            this.Container = container;
            BtnEditValue = container.Q<Button>("btnEditValue");
            BtnRemoveValue = container.Q<Button>("btnRemoveValue");
            BtnAddValue = container.Q<Button>("btnAddValue");
            EnumField = container.Q<EnumField>();
            TxtNewValue = container.Q<TextField>();
            EnumContainer = container.Q<VisualElement>("EnumLine");
            Name = EnumContainer.Q<Label>("lblName");
            NewValueContainer = container.Q<VisualElement>("NewElementLine");

            BtnEditValue.clicked += OnClick_EditValue;
            BtnRemoveValue.clicked += OnClick_RemoveValue;
            BtnAddValue.clicked += () => OnClick_AddButton();
            EnumField.Init(default(T));
            TxtNewValue.RegisterCallback<KeyUpEvent>(OnKeyUp_TxtCharacterType);

            string[] guids = AssetDatabase.FindAssets(typeof(T).Name + " t:Script");
            if (guids.Length > 0)
            {
                _path = AssetDatabase.GUIDToAssetPath(guids[0]);
            }

            EnableContainer(NewValueContainer, false);
            EnumScheduler.Add(ModificationTypes.EditData, typeof(T), this);
        }

        private void OnClick_EditValue()
        {
            if (EnumField.text == "None") return;

            bool shouldShow = CurrentState != State.Editing;

            if (shouldShow)
            {
                BtnEditValue.text = "^";
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
            if (EditorUtility.DisplayDialog("Enum modification",
                        "This function is not compleate yet. Continue may produce error with previous references if it's not" +
                        "the first time using this.",
                        "continue", "cancel"))
            { }
            else
                return;

            if (EnumField.text == "None") return;

            ShowElements(false);

            try
            {
                if (!enumEditor.RemoveOption(_path, EnumField.text)) return;
            }
            catch (InvalidDataExeption e)
            {
                Notify(e.Message, BorderColour.Error);
                CurrentState = State.None;
                return;
            }

            EnumScheduler.ChangeData(ModificationTypes.EditData, typeof(T));
            enumEditor.RecompileScripts();
        }

        private void OnClick_AddButton()
        {
            bool shouldShow = CurrentState != State.Adding;

            if (shouldShow)
            {
                BtnAddValue.text = "^";
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

        private void OnKeyUp_TxtCharacterType(KeyUpEvent evt)
        {
            if (EditorUtility.DisplayDialog("Enum modification",
                        "This function is not compleate yet. Continue may produce error with previous references if it's not" +
                        "the first time using this.",
                        "continue", "cancel"))
            {}
            else
                return;

            if (evt.keyCode == KeyCode.Return)
            {
                if (!VerifyVariableName(TxtNewValue.value))
                {
                    Notify("Not valid name", BorderColour.Error);
                    return;
                }

                if (IsNameInUse(TxtNewValue.value.ToLower()))
                {
                    Notify("name in use", BorderColour.Error);
                    return;
                }

                try
                {
                    switch (CurrentState)
                    {
                        case State.Adding:
                            if (!enumEditor.AddValue(typeof(T).Name, _path, TxtNewValue.value))
                                return;

                            break;

                        case State.Editing:
                            if (!enumEditor.Rename(_path, typeof(T).Name, EnumField.value.ToString(), TxtNewValue.text))
                                return;

                            break;

                        default:
                            return;
                    }
                }
                catch (InvalidDataExeption e)
                {
                    Notify(e.Message, BorderColour.Error);
                    return;
                }

                EnumScheduler.ChangeData(ModificationTypes.EditData, typeof(T));
                Notify("Chages made", BorderColour.Success);
                ShowElements(false);
                EnumField.SetValueWithoutNotify(CharacterType.None);
                CurrentState = State.None;

                enumEditor.RecompileScripts();
            }
        }

        private bool IsNameInUse(string newName)
        {
            foreach (var name in Enum.GetNames(typeof(T)))
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
            EnumField.SetEnabled(!shouldShow);
            BtnAddValue.SetEnabled(!shouldShow);
            BtnRemoveValue.SetEnabled(!shouldShow);
            EnableContainer(NewValueContainer, shouldShow);
        }

        private void HighlightButton(bool shouldHighlight)
        {
            Button button = state switch
            {
                State.Adding => BtnAddValue,
                State.Editing => BtnEditValue,
                _ => null
            };

            if (button == null) return;

            Highlight(button, shouldHighlight, BorderColour.SpecialChange);
        }

        public virtual void Clear()
        {
            state = State.None;
            Value = default(T);
            ShowElements(false);
            HighlightButton(false);
        }

        public virtual void EditData(in EnumModificationData newValue)
        {
            EnumField.Init(default(T));
            Debug.Log("Initialazing enum");
        }
    }
}
