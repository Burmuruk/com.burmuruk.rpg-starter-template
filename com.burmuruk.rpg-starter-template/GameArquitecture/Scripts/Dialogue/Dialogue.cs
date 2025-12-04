using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class Dialogue : ScriptableObject
    {
        [SerializeField] public DialogueNode dialogueNode = new();
        [SerializeField] public string id;
        [SerializeField] public List<string> characters;

        public void UpdateDialogue(DialogueNode startNode)
        {
            dialogueNode = startNode;
        }
    }
}
