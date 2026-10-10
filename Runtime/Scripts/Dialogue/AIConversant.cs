using Burmuruk.RPGStarterTemplate.Interaction;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class AIConversant : MonoBehaviour, IInteractable
    {
        [SerializeField] private string conversantName;
        [SerializeField] private Dialogue dialogue;
        [SerializeField] private PlayerConversant dialogueController;
        [SerializeField] private DialogueTrigger dialogueTriggers;

        public Dialogue Dialogue => dialogue;

        private void Awake()
        {
            if (dialogueController == null)
                dialogueController = FindObjectOfType<PlayerConversant>();

            if (dialogueTriggers == null)
                dialogueTriggers = GetComponent<DialogueTrigger>();
        }

        public string GetName()
        {
            return conversantName;
        }

        public void Interact()
        {
            if (dialogueController == null || dialogue == null)
                return;

            if (!dialogueController.IsActive)
                dialogueController.StartDialogue(dialogue, dialogueTriggers);
            else if (dialogueController.IsChoosing)
                dialogueController.SelectChoice(0);
            else
                dialogueController.Next();
        }

        public void StartDialogue(Dialogue dialogue)
        {
            if (dialogueController == null || dialogue == null)
                return;

            this.dialogue = dialogue;
            dialogueController.StartDialogue(dialogue, dialogueTriggers);
        }

        public void ChangeDialogue(Dialogue newDialogue)
        {
            if (newDialogue == null)
                return;
            dialogue = newDialogue;
        }
    }
}