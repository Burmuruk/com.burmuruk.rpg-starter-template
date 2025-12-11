using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Dialogue.Editor
{
    public class DialogueEditorIMGUI : EditorWindow
    {
        DialogueOld selectedDialogue;
        [NonSerialized] GUIStyle nodeStyle;
        [NonSerialized] GUIStyle playerNodeStyle;
        [NonSerialized] DialogueNodeOld draggingNode = null;
        [NonSerialized] Vector2 draggingOffset;
        [NonSerialized] DialogueNodeOld creatingNode = null;
        [NonSerialized] DialogueNodeOld deleatingNode;
        [NonSerialized] DialogueNodeOld linkingParentNode = null;
        Vector2 scrollPosition;
        [NonSerialized] bool draggingCanvas = false;
        Vector2 draggingCanvasOffset;

        const float canvasSize = 4000;
        const float backgroundSize = 50;

        //[MenuItem("RPGTemplate/Dialogue Editor (IMGUI)")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditorIMGUI), false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var dialogue = EditorUtility.InstanceIDToObject(instanceID) as DialogueOld;

            if (dialogue != null)
            {
                ShowEditorWindow();
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;

            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.padding = new RectOffset(20, 20, 20, 20);
            nodeStyle.border = new RectOffset(12, 12, 12, 12);

            playerNodeStyle = new GUIStyle();
            playerNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
            playerNodeStyle.padding = new RectOffset(20, 20, 20, 20);
            playerNodeStyle.border = new RectOffset(12, 12, 12, 12);
        }

        private void OnSelectionChanged()
        {
            DialogueOld newDialogue = Selection.activeObject as DialogueOld;

            if (newDialogue != null)
            {
                selectedDialogue = newDialogue;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (selectedDialogue == null)
            {
                EditorGUILayout.LabelField("No Dialogue Selected.");
            }
            else
            {
                ProcessEvents();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                var canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);
                Texture2D backgroundText = Resources.Load("background") as Texture2D;
                Rect textCoords = new Rect(0, 0, canvasSize / backgroundSize, canvasSize / backgroundSize);
                GUI.DrawTextureWithTexCoords(canvas, backgroundText, textCoords);

                foreach (var node in selectedDialogue.GetAllNodes())
                {
                    DrawNode(node);
                }

                foreach (var node in selectedDialogue.GetAllNodes())
                {
                    DrawConnections(node);
                }

                EditorGUILayout.EndScrollView();

                if (creatingNode != null)
                {
                    selectedDialogue.CreateNode(creatingNode);
                    creatingNode = null;
                }
                if (deleatingNode != null)
                {
                    selectedDialogue.DeleteNode(deleatingNode);
                    deleatingNode = null;
                }
            }
        }

        private void ProcessEvents()
        {
            if (Event.current.type == EventType.MouseDown && draggingNode == null)
            {
                draggingNode = GetNodeAtPoint(Event.current.mousePosition + scrollPosition);

                if (draggingNode != null)
                {
                    draggingOffset = draggingNode.GetRect().position - Event.current.mousePosition + scrollPosition;
                    Selection.activeObject = draggingNode;
                }
                else
                {
                    draggingCanvas = true;
                    draggingCanvasOffset = Event.current.mousePosition + scrollPosition;
                    Selection.activeObject = selectedDialogue;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && draggingNode != null)
            {
                draggingNode.SetPosition(Event.current.mousePosition + draggingOffset - scrollPosition);

                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseDrag && draggingCanvas)
            {
                scrollPosition = draggingCanvasOffset - Event.current.mousePosition;
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseUp && draggingNode != null)
            {
                draggingNode = null;
            }
            else if (Event.current.type == EventType.MouseUp && draggingCanvas)
            {
                draggingCanvas = false;
            }
        }

        private void DrawNode(DialogueNodeOld node)
        {
            GUIStyle style = node.IsPlayerSpeaking ? playerNodeStyle : nodeStyle;

            GUILayout.BeginArea(node.GetRect(), style);
            EditorGUI.BeginChangeCheck();

            node.Text = EditorGUILayout.TextField(node.Text);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(" - "))
            {
                deleatingNode = node;
            }
            DrawLinkMethods(node);
            if (GUILayout.Button(" + "))
            {
                creatingNode = node;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawLinkMethods(DialogueNodeOld node)
        {
            if (linkingParentNode == null)
            {
                if (GUILayout.Button("Link"))
                {
                    linkingParentNode = node;
                }
            }
            else if (linkingParentNode == node)
            {
                if (GUILayout.Button("Cancel"))
                {
                    linkingParentNode = null;
                }
            }
            else if (linkingParentNode.GetChildren().Contains(node.name))
            {
                if (GUILayout.Button("Unlik"))
                {
                    linkingParentNode.RemoveChild(node.name);
                    linkingParentNode = null;
                }
            }
            else
            {
                if (GUILayout.Button("Child"))
                {
                    Undo.RecordObject(selectedDialogue, "Add dialogue link");
                    linkingParentNode.AddChild(node.name);
                    linkingParentNode = null;
                }
            }
        }

        private void DrawConnections(DialogueNodeOld node)
        {
            Vector3 startPosition = new Vector2(node.GetRect().max.x, node.GetRect().center.y);

            foreach (var childNode in selectedDialogue.GetAllChildren(node))
            {
                Vector3 endPosition = new Vector2(childNode.GetRect().min.x, childNode.GetRect().center.y);
                Vector3 controlPointOffset = endPosition - startPosition;
                controlPointOffset.y = 0;
                controlPointOffset.x *= .8f;

                Handles.DrawBezier(
                    startPosition,
                    endPosition,
                    startPosition + controlPointOffset,
                    endPosition - controlPointOffset,
                    Color.white, null, 4f);
            }
        }

        private DialogueNodeOld GetNodeAtPoint(Vector2 point)
        {
            DialogueNodeOld foundNode = null;

            foreach (var node in selectedDialogue.GetAllNodes())
            {
                if (node.GetRect().Contains(point))
                    return foundNode = node;
            }

            return foundNode;
        }
    }
}
