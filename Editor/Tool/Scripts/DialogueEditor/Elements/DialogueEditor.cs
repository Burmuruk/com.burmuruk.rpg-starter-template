using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public enum NodeType
    {
        None,
        Dialogue,
        Mission,
        SubMission
    }

    public class DialogueEditor : EditorWindow
    {
        public static DialogueEditor window;
        private DialogueGraphView graphView;
        [SerializeField] private DialogueGraphController _controller;
        private bool _isLoading;
        private VisualElement configTab;
        private VisualElement _notificationElement;
        private EditorCoroutine _notificationRoutine;
        private IVisualElementScheduledItem _firstNodeSchedule;

        [MenuItem("RPGTemplate/Dialogue Editor", priority = 1)]
        public static void ShowEditorWindow()
        {
            window = GetWindow<DialogueEditor>(false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            DialogueGraphController controller = EditorUtility.InstanceIDToObject(instanceID) as DialogueGraphController;

            if (controller != null)
            {
                if (window == null)
                    SetWindow();

                window.LoadDialogue(controller);
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            window = this;
            saveChangesMessage = "This dialogue graph has unsaved changes. Would you like to save before closing?";
            bool restoreSession = _controller != null;
            rootVisualElement.Clear();
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            CreateNotificationOverlay();
            CreateGraphView();

            if (!restoreSession)
                CreateFirstNode();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            _firstNodeSchedule?.Pause();
            _firstNodeSchedule = null;

            if (_notificationRoutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_notificationRoutine);
                _notificationRoutine = null;
            }

            RemoveControllerEvents();

            if (_controller != null)
            {
                _controller.CaptureSession();
                _controller.ClearPinViews();
            }

            if (window == this)
                window = null;
        }

        private void CreateFirstNode()
        {
            _firstNodeSchedule?.Pause();
            _firstNodeSchedule = null;
            _firstNodeSchedule = graphView.schedule.Execute(() =>
            {
                //var node = ScriptableObject.CreateInstance<DialogueNode>();
                graphView.CreateNode(new Vector2(100, 180), NodeType.Dialogue);
            });
            _firstNodeSchedule.ExecuteLater(500);
        }

        private static void SetWindow()
        {
            if (window == null)
            {
                window = (DialogueEditor)GetWindow(typeof(DialogueEditor));

                if (window == null)
                    ShowEditorWindow(); //Creates a new window
            }
        }

        #region Loading
        private void LoadDialogue(DialogueGraphController controller)
        {
            if (hasUnsavedChanges)
            {
                if (!EditorUtility.DisplayDialog("Unsaved dialogue graph",
                    "Save changes before opening another dialogue graph?", "Save", "Cancel")) return;
                SaveChanges();

                if (hasUnsavedChanges) return;
            }
            //if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(controller.dialogueGUID)))
            //    return;

            _firstNodeSchedule?.Pause();
            _firstNodeSchedule = null;
            ResetGraphView();
            LoadController(DialogueGraphController.CreateWorkingCopy(controller));
            OnControllerSaved();
        }

        private void LoadController(DialogueGraphController controller)
        {
            _isLoading = true;
            try
            {
                _controller = controller;
                _controller.Initialize();

                configTab = _controller.SettingsContainer;

                rootVisualElement.Add(_controller.Container);

                SetUpControllerEvents();
                var nodes = _controller.GetNodes();
                CreateNodes(nodes);
                CreateConnections(nodes);
                _controller.RestorePins();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void CreateConnections(Dictionary<string, BaseNode> nodes)
        {
            foreach (var node in nodes.Values)
            {
                List<string> children = new(node.Children);
                node.Children.Clear();

                foreach (var id in children)
                {
                    if (_controller.nodes.TryGetValue(id, out var child))
                        graphView.Connect(node.GraphViewNode.output, child.GraphViewNode.input);
                    else
                        Debug.LogWarning($"Dialogue node {node.Id} references missing node {id}. Connection skipped.");
                }
            }
        }

        private void CreateNodes(Dictionary<string, BaseNode> nodes)
        {
            foreach (var node in nodes.Values)
            {
                node.ClearEvents();
                graphView.LoadNode(node.Position, GetNodeType(node), node);
                node.LoadData();
            }

            _controller.Load_CharacterData();
        }

        private NodeType GetNodeType(BaseNode node)
        {
            switch (node)
            {
                case DialogueNode:
                    return NodeType.Dialogue;
                case MissionNode:
                    return NodeType.Mission;
                default:
                    return NodeType.None;
            }
        }

        private void ResetGraphView()
        {
            RemoveControllerEvents();
            _controller?.ClearPinViews();
            if (_controller != null && _controller.Container != null)
            {
                rootVisualElement.Remove(_controller.Container);
            }
            graphView.ResetGraph();
            SetUpGraphEvents();
        }
        #endregion

        #region Start
        private void CreateGraphView()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/BasicSS.uss");
            rootVisualElement.styleSheets.Add(styleSheet);
            graphView = new DialogueGraphView
            {
                name = "Dialogue Graph"
            };
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            if (_controller != null)
            {
                SetUpGraphEvents();
                LoadController(_controller);
                graphView.saved = !hasUnsavedChanges;
                return;
            }

            _controller = CreateInstance<DialogueGraphController>();
            _controller.Initialize();
            configTab = _controller.SettingsContainer;

            rootVisualElement.Add(_controller.Container);

            configTab.style.visibility = Visibility.Hidden;
            SetUpGraphEvents();
            SetUpControllerEvents();
        }

        private void SetUpGraphEvents()
        {
            graphView.OnNodeCreated += (node) =>
            {
                node.OnSelected += (n) =>
                {
                    DisplayNodeOptions(n, true);
                };
                node.OnDeselected += (n) =>
                {
                    DisplayNodeOptions(n, false);
                };
            };
        }

        private void SetUpControllerEvents()
        {
            _controller.OnChange += OnControllerChanged;
            _controller.OnSave += OnControllerSaved;
            _controller.Notify += OnControllerNotification;
            graphView.OnSave += _controller.Save;
            graphView.OnExportResults += _controller.SaveResults;
            graphView.OnChanged += OnControllerChanged;
            graphView.OnNodeCreated += _controller.AddNode;
            graphView.OnNodeDeleted += _controller.RemoveNode;

            graphView.OnNodeCreated += (node) =>
            {
                node.OnPinned += (element, pinned) =>
                {
                    if (pinned)
                    {
                        _controller.AddPin(element);
                    }
                    else
                    {
                        _controller.RemovePin(element);
                    }
                };
            };

            graphView.Get_BaseNode += _controller.GetNode;
            graphView.OnPortConnected += _controller.OnPortConnected;
            graphView.OnPortDisconnected += _controller.OnPortDisconnected;
        }

        private void OnControllerChanged()
        {
            if (_isLoading) return;

            hasUnsavedChanges = true;
            graphView.saved = false;
            Repaint();
        }

        private void OnControllerSaved()
        {
            hasUnsavedChanges = false;
            graphView.saved = true;
            Repaint();
        }

        public override void SaveChanges()
        {
            // OnSave clears the dirty flag only after a successful save.
            _controller?.Save();
        }

        private void OnControllerNotification(string message)
        {
            if (_isLoading) return;

            ShowNotificationMessage(message);
        }

        private void RemoveControllerEvents()
        {
            if (_controller == null)
                return;

            _controller.OnChange -= OnControllerChanged;
            _controller.OnSave -= OnControllerSaved;
            _controller.Notify -= OnControllerNotification;
        }
        #endregion

        private void DisplayNodeOptions(BaseNode node, bool shouldDisplay)
        {
            if (graphView.selection.Count != 1 || graphView.selection[0] is not GraphViewNode)
            {
                configTab.style.visibility = Visibility.Hidden;
                return;
            }

            switch (node)
            {
                case DialogueNode:
                    configTab.style.visibility = shouldDisplay ? Visibility.Visible : Visibility.Hidden;
                    break;
                default:
                    break;
            }

            _controller.TxtDialogueName.SetValueWithoutNotify(node.dialogueName);
            Utilities.UtilitiesUI.EnableContainer(_controller.TxtDialogueName, node.IsStartNode);
        }

        private void OnSelectionChanged()
        {
            //var newDialogue = Selection.activeObject as RPGStarterTemplate.Dialogue.Dialogue;

            //if (newDialogue != null)
            //{
            //    selectedDialogue = newDialogue;
            //    Repaint();
            //}
        }

        #region Notification
        public bool ShowCharacterDialogues(string id)
        {
            return true;
        }

        private void CreateNotificationOverlay()
        {
            _notificationElement = new VisualElement();
            _notificationElement.style.position = Position.Absolute;
            _notificationElement.style.top = 0;
            _notificationElement.style.left = 0;
            _notificationElement.style.right = 0;
            _notificationElement.style.bottom = 0;

            _notificationElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            _notificationElement.style.fontSize = 18;
            _notificationElement.style.color = Color.white;

            _notificationElement.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            _notificationElement.style.paddingTop = 10;
            _notificationElement.style.paddingBottom = 10;
            _notificationElement.style.paddingLeft = 20;
            _notificationElement.style.paddingRight = 20;
            _notificationElement.style.alignContent = Align.Center;
            _notificationElement.style.alignItems = Align.Center;
            _notificationElement.style.justifyContent = Justify.Center;

            Label label = new();
            _notificationElement.Add(label);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            label.style.width = new Length(80, LengthUnit.Percent);
            label.style.fontSize = new Length(30, LengthUnit.Pixel);
            _notificationElement.style.borderBottomLeftRadius = 15;
            _notificationElement.style.borderBottomRightRadius = 15;
            _notificationElement.style.borderTopLeftRadius = 15;
            _notificationElement.style.borderTopRightRadius = 15;

        }

        public void ShowNotificationMessage(string message, float duration = 1f)
        {
            rootVisualElement.Add(_notificationElement);
            _notificationElement.Q<Label>().text = message;
            _notificationElement.Q<Label>().style.opacity = 1;

            if (_notificationRoutine != null)
                EditorCoroutineUtility.StopCoroutine(_notificationRoutine);
            _notificationRoutine = EditorCoroutineUtility.StartCoroutine(FadeOutNotification(duration), this);
        }

        private IEnumerator FadeOutNotification(float duration)
        {
            yield return new EditorWaitForSeconds(duration);

            float t = 0f;
            while (t < 1f)
            {
                t += 0.05f;
                _notificationElement.Q<Label>().style.opacity = 1f - t;
                yield return new EditorWaitForSeconds(0.02f);
            }

            _notificationElement.Q<Label>().style.opacity = 0;
            rootVisualElement.Remove(_notificationElement);
        }
        #endregion
    }
}
