using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public enum BorderColour
    {
        None,
        Success,
        Error,
        HighlightBorder,
        LightBorder,
        StateBorder,
        BuffBorder,
        CharacterBorder,
        ArmorBorder,
        WeaponBorder,
        ConsumableBorder,
        ItemBorder,
        SpecialChange
    }

    public class BaseLevelEditor : EditorWindow
    {
        protected VisualElement container;

        protected Dictionary<string, Button> tabButtons = new();
        protected Dictionary<string, (VisualElement element, ElementType type)> infoContainers = new();
        protected string curTab = "";
        protected string lastTab = "";
        protected VisualElement ntf; //notification
        protected Label ntfLbl; //notification label

        Button selectedButton;
        protected const string acceptButtonName = "AceptButton";
        protected const string cancelButtonName = "CancelButton";

        protected bool changesInTab = false;
        protected BorderColour borderColor = BorderColour.None;

        protected virtual void GetTabButtons() { }
        protected virtual void GetInfoContainers() { }

        public List<StyleSheet> BaseStyleSheets =>
            new List<StyleSheet>()
            {
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/BasicSS.uss"),
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/TagSystem.uss"),
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/BorderColours.uss"),
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/LineTags.uss"),
            };

        protected void GetNotificationSection()
        {
            ntf = container.Q<VisualElement>("notifications");
            ntfLbl = container.Q<Label>("lblNotifications");
            ntf.AddToClassList("Disable");
        }

        protected virtual void ChangeTab(string tab)
        {
            lastTab = curTab;

            if (string.IsNullOrEmpty(tab))
            {
                CloseCurrentTab();
                curTab = "";
                return;
            }

            foreach (var curTab in infoContainers.Values)
            {
                EnableContainer(curTab.element, false);
            }

            EnableContainer(infoContainers[tab].element, true);
            curTab = tab;
        }

        protected void ChangeTab(VisualElement visualElement)
        {
            if (visualElement == null)
            {
                CloseCurrentTab();
                return;
            }

            foreach (var container in infoContainers)
            {
                if (!container.Value.element.ClassListContains("Disable"))
                {
                    container.Value.element.AddToClassList("Disable");
                }
            }

            if (visualElement.ClassListContains("Disable"))
                visualElement.RemoveFromClassList("Disable");
        }

        protected void CloseCurrentTab()
        {
            if (string.IsNullOrEmpty(curTab)) return;

            EnableContainer(infoContainers[curTab].element, false);
            return;
        }

        protected void SelectTabBtn(string tabButtonName)
        {
            foreach (var button in tabButtons)
            {
                if (button.Key == tabButtonName)
                {
                    if (!button.Value.ClassListContains("Selected"))
                    {
                        button.Value.AddToClassList("Selected");
                    }
                }
                else
                {
                    if (button.Value.ClassListContains("Selected"))
                    {
                        button.Value.RemoveFromClassList("Selected");
                    }
                }
            }
        }

        private void SelectButton(string button, bool shouldSelect = true)
        {
            if (selectedButton != null)
                selectedButton.RemoveFromClassList("Selected");

            if (shouldSelect)
            {
                tabButtons[button].AddToClassList("Selected");
            }
            else if (tabButtons[button].ClassListContains("Selected"))
            {
                tabButtons[button].RemoveFromClassList("Selected");
            }

            selectedButton = tabButtons[button];
        }

        protected Button GetAceptButton(VisualElement container) =>
            container.Q<Button>(acceptButtonName);

        protected Button GetCancelButton(VisualElement container) =>
            container.Q<Button>(cancelButtonName);

        protected bool HasSpecialCharacter(string value)
        {
            return value.Any(chr => !char.IsLetterOrDigit(chr));
        }

        protected bool HasInvalidNumberInName(string name)
        {
            if (Int32.TryParse(name, out _))
                return true;

            bool startsWithNumber = false;

            foreach (var item in name)
            {
                if (char.IsDigit(item) && !startsWithNumber)
                    return true;
                else
                    return false;
            }

            return false;
        }
    }
}
