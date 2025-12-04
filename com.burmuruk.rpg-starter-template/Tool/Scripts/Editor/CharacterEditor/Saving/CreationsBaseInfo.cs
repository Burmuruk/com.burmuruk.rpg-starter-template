using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class CreationsBaseInfo : UnityEditor.Editor, IChangesObserver, IClearable
    {
        const string TXT_CREATION_NAME = "txtName";
        const string CREATION_COLOUR_NAME = "cfSettingColour";
        string _lastName;
        Color _lastColour;

        public event Action<CreationsState> CreationsStateChanged;

        public Button BtnState { get; private set; }
        public TextField TxtName { get; set; }
        public ColorField Colour { get; private set; }
        public CreationsState CreationsState { get; private set; } = CreationsState.Creating;

        public void Initialize(VisualElement container)
        {
            BtnState = container.Q<Button>("btnState");
            TxtName = container.Q<TextField>(TXT_CREATION_NAME);
            Colour = container.Q<ColorField>(CREATION_COLOUR_NAME);

            //TxtName.RegisterCallback<KeyUpEvent>(OnKeyUp_txtNameCreation);
            TxtName.RegisterValueChangedCallback(OnValueChanged_TxtName);
            Colour.RegisterValueChangedCallback(OnValueChanged_CFCreationColour);
            BtnState.clicked += Change_State;
            TxtName.value = name;
            BtnState.text = "Creating";
        }

        private void Change_State()
        {
            if (CreationsState == CreationsState.Creating) return;

            SetState(CreationsState.Creating);
            CreationsStateChanged?.Invoke(CreationsState);
        }

        public void SetState(CreationsState state)
        {
            CreationsState = state;
            BtnState.text = state.ToString();
            BtnState.SetEnabled(state == CreationsState.Editing);
            CreationsStateChanged?.Invoke(state);
        }

        public void UpdateName(string newName, string original)
        {
            TxtName.SetValueWithoutNotify(newName);
            _lastName = original;
        }

        private void OnValueChanged_TxtName(ChangeEvent<string> evt)
        {
            string newName = evt.newValue.Trim().ToLower();

            if (!newName.VerifyName(NotificationType.Creation))
            {
                Highlight(TxtName, true, BorderColour.Error);
                TxtName.tooltip = "The name can't be empty.";
            }
            else if (IsTheNameUsed(newName))
            {
                Highlight(TxtName, true, BorderColour.Error);
                Notify("Name in use", BorderColour.Error);
            }
            else
            {
                Highlight(TxtName, false);
                DisableNotification(NotificationType.Creation);
            }
        }

        public bool VerifyData(out List<string> errors)
        {
            var reslut = ValidateName(out var error);
            errors = new List<string>();

            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            return reslut;
        }

        private bool ValidateName(out string error)
        {
            string newName = TxtName.value.Trim().ToLower();
            error = null;

            if (!newName.VerifyName(out var invalidError))
            {
                Highlight(TxtName, true, BorderColour.Error);
                TxtName.tooltip = invalidError;
                error = invalidError;
                return false;
            }
            if (IsTheNameUsed(newName))
            {
                Highlight(TxtName, true, BorderColour.Error);
                TxtName.tooltip = "Name in use.";
                error = TxtName.tooltip;
                return false;
            }

            Highlight(TxtName, false);
            TxtName.tooltip = null;

            return true;
        }

        private void OnValueChanged_CFCreationColour(ChangeEvent<Color> evt)
        {
            //characterData.color = evt.newValue;
            //((CharacterData)editingData[ElementType.Character].data).color = evt.newValue;
        }

        public bool IsTheNameUsed(string name)
        {
            foreach (var creationType in SavingSystem.Data.creations.Keys)
            {
                foreach (var creation in SavingSystem.Data.creations[creationType].Values)
                {
                    if (creation.Id.ToLower() == name)
                    {
                        if (CreationsState == CreationsState.Editing && creation.Id.ToLower() == _lastName.ToLower())
                            continue;

                        return true;
                    }
                }
            }

            return false;
        }

        public ModificationTypes Check_Changes()
        {
            ModificationTypes modification = ModificationTypes.None;

            try
            {
                if (_lastName != TxtName.value)
                    modification = ModificationTypes.Rename;

                //if (_lastColour != Colour.value)
                //    modification |= ModificationTypes.EditData;
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }

            return modification;
        }

        public void Load_Changes()
        {
            TxtName.value = _lastName;
        }

        public void Clear()
        {
            Highlight(TxtName, false, BorderColour.Error);
            TxtName.tooltip = "";
            TxtName.value = "";
            _lastName = "";
        }

        public void Remove_Changes() { }
    }
}
