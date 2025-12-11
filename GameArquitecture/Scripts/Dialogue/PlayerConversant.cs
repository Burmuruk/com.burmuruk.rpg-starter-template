using Burmuruk.RPGStarterTemplate.Control;
using System;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class PlayerConversant : MonoBehaviour
    {
        [SerializeField] string playerName;
        Dialogue currentDialogue;
        DialogueNode currentNode = null;
        AIConversant currentConversant = null;
        PlayerController playerController;

        public bool IsChoosing { get; private set; }
        public bool IsActive { get => currentDialogue != null; }

        public event Action<DialogueNode> OnConversationUpdated;
        public event Action OnConversationEnded;
        //private void Awake()
        //{
        //    currentNode = currentDialogue.GetRootNode();
        //}

        private void OnEnable()
        {
            playerController = FindObjectOfType<PlayerController>();
            playerController.OnInteract += Next;
        }

        private void OnDisable()
        {
            if (playerController == null) return;

            playerController.OnInteract -= Next;
        }

        public void StartDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            currentConversant = newConversant;
            currentDialogue = newDialogue;
            currentNode = newDialogue.dialogueNode;
            TriggerEnterAction();
            OnConversationUpdated?.Invoke(currentNode);
        }

        public void Quit()
        {
            currentDialogue = null;
            TriggerExitAction();
            currentNode = null;
            IsChoosing = false;
            currentConversant = null;
            FindObjectOfType<GameManager>().StartCinematic(false);
            OnConversationEnded?.Invoke();
        }

        public string GetText()
        {
            if (currentNode == null)
            {
                return "";
            }

            return currentNode.Message;
        }

        //public IEnumerable<DialogueNodeOld> GetChoices()
        //{
        //    return currentDialogue.GetPlayerChildren(currentNode);
        //}

        public string GetCurrentConversantName()
        {
            if (IsChoosing)
            {
                return playerName;
            }
            else
            {
                return currentConversant.GetName();
            }
        }

        public void SelectChoice(int idx)
        {
            currentNode = currentNode.Children[idx];
            TriggerEnterAction();
            IsChoosing = false;
            Next();
        }

        public void Next()
        {
            if (!IsActive) return;

            int numPlayerResponses = currentNode.Children.Count();
            if (numPlayerResponses > 1)
            {
                IsChoosing = true;
                TriggerExitAction();
                OnConversationUpdated?.Invoke(currentNode);
                return;
            }
            else if (numPlayerResponses == 0)
            {
                Quit();
                return;
            }

            var children = currentNode.Children;
            int randomIndex = UnityEngine.Random.Range(0, children.Count());
            TriggerExitAction();

            currentNode = children[randomIndex];
            TriggerEnterAction();
            OnConversationUpdated?.Invoke(currentNode);
        }

        public bool HasNext()
        {
            return currentNode.Children.Count() > 0;
        }

        private void TriggerEnterAction()
        {
            if (currentNode != null)
            {
                TriggerAction(currentNode.GetOnEnterAction());
            }
        }

        private void TriggerExitAction()
        {
            if (currentNode != null)
            {
                TriggerAction(currentNode.GetOnExitAction());
            }
        }

        private void TriggerAction(string action)
        {
            if (action == "") return;

            foreach (var trigger in FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None))
            {
                if (trigger.Action == action)
                {
                    trigger.Trigger(action); 
                }
            }
        }
    }
}
