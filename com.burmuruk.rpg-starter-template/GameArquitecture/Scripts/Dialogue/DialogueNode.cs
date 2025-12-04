using System;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    [Serializable]
    public class DialogueNode
    {
        public string Id;
        public string characterName;
        public string Message;
        public List<DialogueNode> Children = new ();
        public string onEnterAction;
        public string onExitAction;

        internal string GetOnEnterAction() => onEnterAction;

        internal string GetOnExitAction() => onExitAction;
    }
}
