using Burmuruk.RPGStarterTemplate.Utilities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;

namespace Burmuruk.RPGStarterTemplate.Editor.Utilities
{
    public static class UtilitiesUI
    {
        private const string BORDER_COLOURS_STYLESHEET_NAME = "BorderColours";
        static Regex regVariableName = new Regex(@"(?m)^[a-zA-Z](?!.*\W)+\w*", RegexOptions.Compiled);
        static Regex regName = new Regex(@"(?m)^[a-zA-Z](\s*?\w)*$", RegexOptions.Compiled);
        private static StyleSheet _hightlight_Colours;
        static Dictionary<VisualElement, IVisualElementScheduledItem> highlightTimeouts = null;
        public static Dictionary<NotificationType, NotificationData> notifications = null;
        public static readonly string[] Keywords = new[]
        {
            // base
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
            "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return",
            "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while",

            // C# 7+
            "add", "alias", "async", "await", "dynamic", "from", "get", "global",
            "group", "into", "join", "let", "orderby", "partial", "remove", "select",
            "set", "value", "var", "where", "yield",

            // C# 9
            "record", "init", "with", "not"
        };

        public class NotificationData
        {
            public VisualElement container;
            public Label label;
            public IVisualElementScheduledItem timeout = null;

            public NotificationData(VisualElement element, Label label)
            {
                container = element;
                this.label = label;
            }
        }

        public static void Notify(string message, BorderColour colour, NotificationType type = NotificationType.Creation)
        {
            if (notifications.ContainsKey(type))
            {
                notifications[type].timeout?.Pause();
                notifications[type].timeout = null;
            }
            else
                return;

            string colourText = colour.ToString();
            notifications[type].container.RemoveFromClassList("Disable");
            RomoveTags();

            if (!notifications[type].container.ClassListContains(colourText))
                notifications[type].container.AddToClassList(colourText);

            notifications[type].label.text = message;
            notifications[type].timeout = notifications[type].container.schedule.Execute(() =>
            {
                DisableNotification(type);
            });

            notifications[type].timeout.ExecuteLater(5000);

            void RomoveTags()
            {
                int count = Enum.GetValues(typeof(BorderColour)).Length;

                for (int i = 0; i < count; i++)
                {
                    var cur = (BorderColour)i;

                    if (cur == colour)
                    {
                        continue;
                    }
                    else if (notifications[type].container.ClassListContains(cur.ToString()))
                    {
                        notifications[type].container.RemoveFromClassList(cur.ToString());
                    }
                }
            }
        }

        public static void Set_ErrorTooltip(VisualElement element, string message, ref List<string> errors, bool isValid = true, BorderColour colour = BorderColour.Error)
        {
            Highlight(element, !isValid, colour);

            if (!isValid)
            {
                element.tooltip = message;
                (errors ??= new()).Add(message);
            }
            else
            {
                element.tooltip = null;
            }
        }

        public static bool Verify_EmptyField(TextField element, List<string> errors, Dictionary<VisualElement, string> highlights)
        {
            var isValid = !string.IsNullOrEmpty(element.value);
            highlights[element] = element.tooltip;
            Set_ErrorTooltip(element, "Field can't be empty.", ref errors, isValid);
            return isValid;
        }

        public static bool Verify_NegativaValue(this FloatField element, List<string> errors, Dictionary<VisualElement, string> highlights) =>
            Verify_NegativaValue(element, element.value, errors, highlights);

        public static bool Verify_NegativaValue(this IntegerField element, List<string> errors, Dictionary<VisualElement, string> highlights) =>
            Verify_NegativaValue(element, element.value, errors, highlights);

        public static bool Verify_NegativaValue(VisualElement element, float value, List<string> errors, Dictionary<VisualElement, string> highlights)
        {
            errors ??= new List<string>();
            var isValid = value >= 0;
            highlights[element] = element.tooltip;
            Set_ErrorTooltip(element, "Value can't be negative.", ref errors, isValid);
            return isValid;
        }

        public static void Set_Tooltip(VisualElement element, Dictionary<VisualElement, string> highlights, string message = "", bool highlight = true, BorderColour colour = BorderColour.HighlightBorder)
        {
            Highlight(element, highlight, colour);
            
            if (highlight)
            {
                element.tooltip = message;
                highlights.TryAdd(element, message);
            }
            else
            {
                if (highlights.ContainsKey(element))
                    element.tooltip = highlights[element];
                else
                    element.tooltip = message;
            }
        }

        public static void Set_Tooltip(VisualElement element, string message, bool highlight = true, BorderColour colour = BorderColour.HighlightBorder)
        {
            Highlight(element, highlight, colour);
            element.tooltip = message;
        }

        public static void DisableNotification(NotificationType type)
        {
            if (!notifications.ContainsKey(type)) return;

            if (!notifications[type].container.ClassListContains("Disable"))
                notifications[type].container.AddToClassList("Disable");
        }

        public static void Highlight(VisualElement element, long time = 3000, BorderColour colour = BorderColour.HighlightBorder)
        {
            Highlight(element, true, colour);

            if (highlightTimeouts != null && highlightTimeouts.ContainsKey(element))
                highlightTimeouts[element].Pause();
            else
                highlightTimeouts ??= new();

            highlightTimeouts[element] = element.schedule.Execute(() =>
            {
                RemoveHighlightTimeOut(element);
                Highlight(element, false, colour);
            });

            highlightTimeouts[element].ExecuteLater(time);
        }

        static void RemoveHighlightTimeOut(VisualElement element)
        {
            if (highlightTimeouts.ContainsKey(element))
            {
                highlightTimeouts.Remove(element);
            }
        }

        public static void Highlight(VisualElement element, bool shouldHighlight, BorderColour colour = BorderColour.HighlightBorder)
        {
            string colourText = colour.ToString();
            RemoveStyleClass(element, colourText);

            if (shouldHighlight)
            {
                if (!element.ClassListContains(colourText))
                {
                    element.AddToClassList(colourText);
                }
            }
            else
            {
                if (element.ClassListContains(colourText))
                {
                    element.RemoveFromClassList(colourText);
                }
            }

            void RemoveStyleClass(VisualElement textField, string colourText)
            {
                int count = Enum.GetValues(typeof(BorderColour)).Length;

                for (int i = 0; i < count; i++)
                {
                    string colour = ((BorderColour)i).ToString();

                    if (colour == colourText)
                    {
                        continue;
                    }
                    else if (textField.ClassListContains(colour))
                    {
                        textField.RemoveFromClassList(colour);
                    }
                }
            }
        }

        public static bool IsHighlighted(VisualElement element, out BorderColour colour)
        {
            colour = BorderColour.None;

            foreach (BorderColour value in Enum.GetValues(typeof(BorderColour)))
            {
                if (element.ClassListContains(value.ToString()))
                {
                    colour = value;
                    return true;
                }
            }

            return false;
        }

        public static bool IsHighlighted(VisualElement element)
        {
            return element.ClassListContains(BorderColour.HighlightBorder.ToString());
        }

        public static void EnableContainer(VisualElement container, bool shouldEnable)
        {
            if (shouldEnable)
            {
                if (container.ClassListContains("Disable"))
                {
                    container.RemoveFromClassList("Disable");
                }
            }
            else if (!container.ClassListContains("Disable"))
            {
                container.AddToClassList("Disable");
            }
        }

        public static bool VerifyVariableName(string name)
        {
            string lower = name.ToLowerInvariant();

            for (int i = 0; i < Keywords.Length; i++)
            {
                if (lower == Keywords[i])
                    return false;
            }

            return regVariableName.IsMatch(name);
        }

        public static bool VerifyName(this string name, NotificationType type)
        {
            if (string.IsNullOrEmpty(name))
            {
                Notify("Name can't be empty.", BorderColour.Error, type);
                return false;
            }

            if (!regName.IsMatch(name))
            {
                Notify("Invalid name", BorderColour.Error, type);
                return false;
            }

            return true;
        }

        public static bool VerifyName(this string name, out string error)
        {
            if (string.IsNullOrEmpty(name))
            {
                error = "Name can't be empty.";
                return false;
            }

            if (!regName.IsMatch(name))
            {
                error = "Invalid name";
                return false;
            }

            error = null;
            return true;
        }

        public static bool IsDisabled(VisualElement element)
        {
            return element.ClassListContains("Disable");
        }

        public static VisualElement CreateDefaultTab(string fileName)
        {
            VisualElement newContainer = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Tabs/{fileName}.uxml").Instantiate();
            return newContainer;
        }
    }
}
