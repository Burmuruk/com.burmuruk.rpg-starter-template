using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Interaction;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class AIConversant : MonoBehaviour, IInteractable
    {
        [SerializeField] Dialogue dialogue;
        [SerializeField] string conversantName;


        public bool HandleRaycast(Character callingController)
        {
            if (dialogue == null) return false;

            if (Input.GetMouseButtonDown(0))
            {
                callingController.GetComponent<PlayerConversant>().StartDialogue(this, dialogue);
            }

            return true;
        }

        public string GetName()
        {
            return conversantName;
        }

        public void Interact()
        {
            if (FindObjectOfType<PlayerConversant>() is var pc && pc != null)
            {
                FindObjectOfType<GameManager>().StartCinematic(true);
                pc.StartDialogue(this, dialogue);
            }
        }
    }
}
