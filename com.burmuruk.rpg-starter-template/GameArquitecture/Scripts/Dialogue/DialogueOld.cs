using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    [CreateAssetMenu(fileName = "New Dialogue old", menuName = "ScriptableObjects/Dialogue", order = 0)]
    public class DialogueOld : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<DialogueNodeOld> nodes = new();
        [SerializeField] Vector2 newNodeOffset = new Vector2(250, 0);

        Dictionary<string, DialogueNodeOld> nodeLookup = new();

        private void OnValidate()
        {
            nodeLookup.Clear();

            foreach (DialogueNodeOld node in GetAllNodes())
            {
                nodeLookup[node.name] = node;
            }
        }

        public IEnumerable<DialogueNodeOld> GetAllNodes()
        {
            return nodes;
        }

        public DialogueNodeOld GetRootNode()
        {
            return nodes[0];
        }

        public IEnumerable<DialogueNodeOld> GetAllChildren(DialogueNodeOld parentNode)
        {
            foreach (var childId in parentNode.GetChildren())
            {
                if (nodeLookup.ContainsKey(childId))
                    yield return nodeLookup[childId];
            }
        }

#if UNITY_EDITOR
        public void CreateNode(DialogueNodeOld parent)
        {
            DialogueNodeOld newNode = MakeNode(parent);

            Undo.RegisterCreatedObjectUndo(newNode, "Created dialogue node");
            Undo.RecordObject(this, "Added dialogue node.");
            AddNode(newNode);
        }

        public void DeleteNode(DialogueNodeOld nodeToDelete)
        {
            Undo.RecordObject(this, "Deleted dialogue node");
            nodes.Remove(nodeToDelete);
            OnValidate();
            CleanChildren(nodeToDelete);

            Undo.DestroyObjectImmediate(nodeToDelete);
        }

        private void AddNode(DialogueNodeOld newNode)
        {
            nodes.Add(newNode);
            OnValidate();
        }

        private DialogueNodeOld MakeNode(DialogueNodeOld parent)
        {
            var newNode = CreateInstance<DialogueNodeOld>();
            newNode.name = Guid.NewGuid().ToString();

            if (parent != null)
            {
                parent.AddChild(newNode.name);
                newNode.IsPlayerSpeaking = !newNode.IsPlayerSpeaking;
                newNode.SetPosition(parent.GetRect().position + newNodeOffset);
            }

            return newNode;
        }

        public void CleanChildren(DialogueNodeOld nodeToDelete)
        {
            foreach (var node in GetAllNodes())
            {
                node.RemoveChild(nodeToDelete.name);
            }
        }

        internal IEnumerable<DialogueNodeOld> GetPlayerChildren(DialogueNodeOld currentNode)
        {
            foreach (var node in GetAllChildren(currentNode))
            {
                if (node.IsPlayerSpeaking)
                {
                    yield return node;
                }
            }
        }

        internal IEnumerable<DialogueNodeOld> GetAIChildren(DialogueNodeOld currentNode)
        {
            foreach (var node in GetAllChildren(currentNode))
            {
                if (!node.IsPlayerSpeaking)
                {
                    yield return node;
                }
            }
        }

#endif

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (nodes.Count == 0)
            {
                var newNode = MakeNode(null);
                AddNode(newNode);
            }

            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (var node in GetAllNodes())
                {
                    if (AssetDatabase.GetAssetPath(node) == "")
                    {
                        AssetDatabase.AddObjectToAsset(node, this);
                    }
                }
            }
#endif
        }

        public void OnAfterDeserialize()
        {

        }
    }
}
