using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class DialogueBlock : Dialogue
    {
        public List<DialogueData> dialogues = new();

        [Serializable]
        public struct DialogueData
        {
            public string id;
            public Dialogue dialogue;
            public string name;
            public List<string> characters;
        }

        public Dialogue this[string id]
        {
            get => GetDialogueById(id);
            set
            {
                var dialogueData = dialogues.FirstOrDefault(d => d.id == id);
                if (dialogueData.id != null)
                {
                    dialogueData.dialogue.UpdateDialogue(value.dialogueNode);
                }
                else
                {
                    dialogues.Add(new DialogueData { id = id, dialogue = value });
                    AttachDialogues(dialogues[^1].dialogue);
                }
            }
        }

        public void RemoveDialogue(string id)
        {
            dialogues.RemoveAll(d => d.id == id);
        }

        public bool ContainsDialogue(string id)
        {
            return dialogues.Any(d => d.id == id);
        }

        public Dialogue GetDialogueById(string id) => dialogues.FirstOrDefault(d => d.id == id).dialogue;

        public void AttachDialogues(Dialogue dialogue)
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                if (AssetDatabase.GetAssetPath(dialogue) != "")
                    return;

                AssetDatabase.AddObjectToAsset(dialogue, this);
            }
#endif
        }
    }
}
