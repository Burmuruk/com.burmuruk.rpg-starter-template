using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    [CustomEditor(typeof(AIConversant))]
    public class AIConversantEditor : UnityEditor.Editor
    {
        private ObjectField dialogue;
        private DropdownField characterDD;
        private DropdownField dialogueDD;
        private SerializedProperty dialogueField;
        private SerializedProperty nameField;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            dialogueField = serializedObject.FindProperty("dialogue");
            nameField = serializedObject.FindProperty("conversantName");

            dialogue = CreateDialogueField(dialogueField);
            characterDD = CreateCharacterDD(nameField);
            dialogueDD = CreateDialogueDD();

            root.Add(dialogue);
            root.Add(dialogueDD);
            root.Add(characterDD);

            if (dialogue.value != null)
            {
                if (dialogue.value is DialogueBlock)
                    characterDD.style.display = DisplayStyle.Flex;
                else if (dialogue.value is DialogueBlock)
                    characterDD.style.display = DisplayStyle.Flex;
            }
            else
                characterDD.style.display = DisplayStyle.None;

            return root;
        }

        private ObjectField CreateDialogueField(SerializedProperty prop)
        {
            var field = new ObjectField("Dialogue")
            {
                objectType = typeof(Dialogue)
            };

            field.RegisterValueChangedCallback(OnValueChanged_Dialogue);
            field.BindProperty(prop); // ? lo importante
            return field;
        }


        private void OnValueChanged_Dialogue(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue == null)
            {
                dialogueDD.style.display = DisplayStyle.None;
                characterDD.style.display = DisplayStyle.None;
                characterDD.value = "";
                dialogueDD.value = "";
                dialogueField.objectReferenceValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (evt.newValue is Dialogue dialogue)
            {
                if (evt.newValue is DialogueBlock db)
                {
                    dialogueField.objectReferenceValue = null;
                    Setup_Dropdown(dialogueDD, db.dialogues.Select(d => d.dialogue.name).ToList());
                    characterDD.style.display = DisplayStyle.None;
                }
                else
                {
                    Setup_Dropdown(characterDD, dialogue.characters);
                    dialogueDD.style.display = DisplayStyle.None;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void Setup_Dropdown(DropdownField dd, List<string> choices)
        {
            dd.choices = choices??=new();
            dd.style.display = DisplayStyle.Flex;
            dd.value = dd.choices.FirstOrDefault();
        }

        private DropdownField CreateCharacterDD(SerializedProperty serialized)
        {
            var options = new List<string> ();
            var dropdown = new DropdownField("character name", options, 0);
            dropdown.value = serialized.stringValue;

            dropdown.RegisterValueChangedCallback(evt =>
            {
                serialized.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            return dropdown;
        }

        private DropdownField CreateDialogueDD()
        {
            var options = new List<string> ();
            var dropdown = new DropdownField("Dialogue name", options, 0);
            dropdown.style.display = DisplayStyle.None;
            dropdown.SetValueWithoutNotify(default);

            dropdown.RegisterValueChangedCallback(evt =>
            {
                var db = this.dialogue.value as DialogueBlock;
                Dialogue dialogue = null;

                if (db?.dialogues != null)
                {
                    foreach (var data in db.dialogues)
                    {
                        if (data.dialogue.name == evt.newValue)
                        {
                            dialogue = data.dialogue;
                            dialogueField.objectReferenceValue = data.dialogue;
                            break;
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
                Setup_Dropdown(characterDD, dialogue?.characters);
            });

            return dropdown;
        }
    }
}
