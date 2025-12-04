using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Dialogue
{
    public class DialogueNodeOld : ScriptableObject
    {
        [SerializeField] bool isPlayerSpeaking = false;
        [SerializeField] string text;
        [SerializeField] List<string> children = new();
        [SerializeField] Rect rect = new Rect(0, 0, 200, 100);
        [SerializeField] string onEnterAction;
        [SerializeField] string onExitAction;

        public string Text
        {
            get => text;
            set
            {
#if UNITY_EDITOR
                if (text != value)
                {
                    Undo.RecordObject(this, "Update Dialogue Text");
                    text = value;
                    EditorUtility.SetDirty(this);
                }
#endif
            }
        }

        public bool IsPlayerSpeaking
        {
            get => isPlayerSpeaking;
            set
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, "Change dialogue speaker");
                isPlayerSpeaking = value;
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public string GetOnEnterAction() => onEnterAction;

        public string GetOnExitAction() => onExitAction;

        public Rect GetRect()
        {
            return rect;
        }

        public List<string> GetChildren()
        {
            return children;
        }

#if UNITY_EDITOR
        public void SetPosition(Vector2 newPosition)
        {
            Undo.RecordObject(this, "Move Dialogue Node");
            rect.position = newPosition;
            EditorUtility.SetDirty(this);
        }

        public void AddChild(string childId)
        {
            Undo.RecordObject(this, "Add dialogue link");
            children.Add(childId);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChild(string childId)
        {
            Undo.RecordObject(this, "Remove dialogue link");
            children.Remove(childId);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
