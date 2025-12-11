using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class DialogueGraphData : ScriptableObject
    {
        public RPGStarterTemplate.Dialogue.Dialogue dialogue;
        public bool saved = false;
        public List<BaseNode> nodes = new();
        public List<string> dialogueIds = new();
        public List<string> missionsIds = new();

        public void AddNode(BaseNode node)
        {
            Undo.RecordObject(this, "Added dialogue node");
            nodes.Add(node);
        }

        public void RemoveNode(BaseNode node)
        {
            Undo.RecordObject(this, "Removed dialogue node");
            nodes.Remove(node);
            //Undo.DestroyObjectImmediate(node);
        }
    }
}
