using System;
using System.Collections.Generic;
using Burmuruk.RPGStarterTemplate.Control;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class PlayerConversant : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameManager gameManager;

        private Dialogue currentDialogue;
        private DialogueNode currentNode;
        private DialogueTrigger currentTriggers;

        public bool IsActive => currentDialogue != null;
        public bool IsChoosing { get; private set; }

        public event Action<DialogueNode> OnConversationUpdated;
        public event Action OnConversationEnded;

        private void OnEnable()
        {
            if (playerController == null)
                playerController = FindObjectOfType<PlayerController>();

            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();

            if (playerController != null)
                playerController.OnInteract += Next;
        }

        private void OnDisable()
        {
            if (playerController != null)
                playerController.OnInteract -= Next;

            Quit();
        }

        public void StartDialogue(Dialogue dialogue, DialogueTrigger triggers = null)
        {
            if (dialogue == null || dialogue.dialogueNode == null)
                return;

            if (IsActive)
                return;

            currentDialogue = dialogue;
            currentTriggers = triggers;
            IsChoosing = false;

            if (gameManager != null)
                gameManager.StartCinematic(true);

            EnterNode(dialogue.dialogueNode);
        }

        public void Next()
        {
            if (!IsActive || IsChoosing || currentNode == null)
                return;

            var children = currentNode.Children;

            if (children == null || children.Count == 0)
            {
                Quit();
                return;
            }

            if (children.Count > 1)
            {
                IsChoosing = true;
                OnConversationUpdated?.Invoke(currentNode);
                return;
            }

            MoveToNode(children[0]);
        }

        public void SelectChoice(int index)
        {
            if (!IsActive || !IsChoosing || currentNode == null)
                return;

            var children = currentNode.Children;

            if (children == null || index < 0 || index >= children.Count)
                return;

            var selectedNode = children[index];

            if (selectedNode == null)
                return;

            IsChoosing = false;
            MoveToNode(selectedNode);
        }

        public IReadOnlyList<DialogueNode> GetChoices()
        {
            if (IsChoosing && currentNode?.Children != null)
                return currentNode.Children;

            return Array.Empty<DialogueNode>();
        }

        public string GetText()
        {
            return currentNode?.Message ?? string.Empty;
        }

        public string GetCurrentConversantName()
        {
            return currentNode?.characterName ?? string.Empty;
        }

        public bool HasNext()
        {
            return currentNode?.Children != null
                && currentNode.Children.Count > 0;
        }

        public void Quit()
        {
            if (!IsActive)
                return;

            var exitAction = currentNode?.onExitAction;
            var triggers = currentTriggers;

            currentDialogue = null;
            currentNode = null;
            currentTriggers = null;
            IsChoosing = false;

            triggers?.Execute(exitAction);

            if (gameManager != null)
                gameManager.StartCinematic(false);

            OnConversationEnded?.Invoke();
        }

        private void MoveToNode(DialogueNode nextNode)
        {
            if (nextNode == null)
            {
                Quit();
                return;
            }

            var previousNode = currentNode;
            currentTriggers?.Execute(previousNode?.onExitAction);

            if (!IsActive || currentNode != previousNode)
                return;

            EnterNode(nextNode);
        }

        private void EnterNode(DialogueNode node)
        {
            currentNode = node;
            currentTriggers?.Execute(node.onEnterAction);

            if (IsActive && currentNode == node)
                OnConversationUpdated?.Invoke(node);
        }
    }
}