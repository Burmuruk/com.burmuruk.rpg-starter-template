using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Stats;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public partial class TabCharacterEditor : BaseLevelEditor
    {
        Button btnSettingAccept;
        Button btnSettingCancel;

        const string COMPONENTS_CONTAINER_NAME = "componentsConatiner";
        const string SETTINGS_COLOUR_CONTAINER = "ColourContainer";
        const string DDF_ADD_COMPONENT_NAME = "ddfElement";
        const string BTN_GO_BACK_SETTINGS = "btnGoBack";
        const string INFO_EXTRA_SETTINGS_NAME = "infoContainer";

        VisualElement infoExtraSetting;
        VisualElement componentsContainer;
        DropdownField ddfAddComponent;

        const string BTN_SETTINGS_ACCEPT_NAME = "btnSettingAccept";
        const string BTN_SETTINGS_CANCEL_NAME = "btnSettingCancel";
        (ElementType type, string name, int elementIdx) editingElement = default;

        public class StatsVisualizer : ScriptableObject
        {
            [SerializeField] public BasicStats stats;
        }

        private void Create_BaseSettingsTab()
        {
            btnSettingCancel = infoSetup.Q<Button>(BTN_SETTINGS_CANCEL_NAME);
            btnSettingAccept = infoSetup.Q<Button>(BTN_SETTINGS_ACCEPT_NAME);
            btnSettingCancel.clicked += OnCancel_BtnSetting;
            btnSettingAccept.clicked += OnAccept_BtnAccept;
        }

        private void Create_CharacterTab()
        {
            ScriptableObject.CreateInstance<CharacterSettings>();
            CharacterSettings characterSettings = CreateInstance<CharacterSettings>();
            var parent = infoSetup.Q<ScrollView>("infoContainer").Q("unity-content-container");
            characterSettings.Initialize(infoContainers[INFO_CHARACTER_NAME].element, nameSettings, parent);

            CreationControls.Add(ElementType.Character, characterSettings);
        }

        private void OnCancel_BtnSetting()
        {
            ElementType type = currentSettingTag.type;
            var state = (CreationControls[type] as BaseInfoTracker).CreationsState;

            switch (state)
            {
                case CreationsState.None:
                    break;

                case CreationsState.Creating:
                    ((IClearable)CreationControls[type]).Clear();
                    break;

                case CreationsState.Editing:
                    ((IChangesObserver)CreationControls[type]).Load_Changes();
                    break;

                default:
                    break;
            }
        }

        private void OnAccept_BtnAccept()
        {
            ElementType type = currentSettingTag.type;
            var state = (CreationControls[type] as BaseInfoTracker).CreationsState;

            try
            {
                switch (state)
                {
                    case CreationsState.None:
                        break;

                    case CreationsState.Creating:
                        if (!Save_Creation()) return;

                        Notify("Creation saved", BorderColour.Success);
                        break;

                    case CreationsState.Editing:
                        if (!Edit_Creation()) return;

                        Notify("Changes saved", BorderColour.Success);
                        nameSettings.SetState(CreationsState.Creating);
                        break;

                    default:
                        break;
                }

                OnCancel_BtnSetting();
                EnableContainer(infoSetup, false);
                ChangeTab(INFO_GENERAL_SETTINGS_CHARACTER_NAME);
                //EnableContainer(infoSetup, false);
                Highlight(btnsRight_Tag[currentSettingTag.idx].element, false);
                currentSettingTag = (ElementType.None, -1);
                editingElement = (ElementType.None, "", -1);
            }
            catch (InvalidDataExeption e)
            {
                Notify(e.Message, BorderColour.Error);
            }
        }

        private bool Edit_Creation()
        {
            try
            {
                ElementType type = currentSettingTag.type;
                bool result = CreationControls[type].Save();

                if (!result)
                {
                    if (IsDisabled(ntf))
                        Notify("Couldn't save changes.", BorderColour.Error);

                    return false;
                }

                Notify("Changes saved", BorderColour.Success);
                return true;
            }
            catch (InvalidExeption e)
            {
                throw e;
            }
        }
    }
}
