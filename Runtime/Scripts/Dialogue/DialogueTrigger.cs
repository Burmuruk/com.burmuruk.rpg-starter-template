using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    [System.Serializable]
    public class DialogueAction
    {
        public string name;
        public UnityEngine.Events.UnityEvent action;
    }

    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private List<DialogueAction> actions = new();

        public void Execute(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return;

            foreach (var entry in actions)
            {
                if (entry.name == actionName)
                    entry.action?.Invoke();
            }
        }
    }
}