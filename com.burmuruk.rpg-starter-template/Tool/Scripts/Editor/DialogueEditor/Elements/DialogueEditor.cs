using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
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
        private DialogueGraphController _controller;
        private VisualElement configTab;
        private VisualElement _notificationElement;
        private EditorCoroutine _notificationRoutine;
        private IVisualElementScheduledItem _firstNodeSchedule;

        [MenuItem("RPGTemplate/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            window = GetWindow<DialogueEditor>(false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var controller = EditorUtility.InstanceIDToObject(instanceID) as DialogueGraphController;

            if (controller != null)
            {
                if (window == null) SetWindow();

                window.LoadDialogue(controller);
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            CreateGraphView();
            CreateNotificationOverlay();
            CreateFirstNode();
        }

        private void CreateFirstNode()
        {
            _firstNodeSchedule?.Pause();
            _firstNodeSchedule = null;
            _firstNodeSchedule = graphView.schedule.Execute(() =>
            {
                //var node = ScriptableObject.CreateInstance<DialogueNode>();
                graphView.CreateNode(new Vector2(100, 100), NodeType.Dialogue);
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
            //if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(controller.dialogueGUID)))
            //    return;

            _firstNodeSchedule?.Pause();
            _firstNodeSchedule = null;
            ResetGraphView();
            LoadController(controller);
        }

        private void LoadController(DialogueGraphController controller)
        {
            _controller = controller;
            _controller.Initialize();
            configTab = _controller.SettingsContainer;
            rootVisualElement.Add(configTab);
            SetUpControllerEvents();
            var nodes = _controller.GetNodes();
            CreateNodes(nodes);
            CreateConnections(nodes);
        }

        private void CreateConnections(Dictionary<string, BaseNode> nodes)
        {
            foreach (var node in nodes.Values)
            {
                var children = new List<string>(node.Children);
                node.Children.Clear();

                foreach (var id in children)
                {
                    graphView.Connect(node.GraphViewNode.output, _controller.nodes[id].GraphViewNode.input);
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
            rootVisualElement.Remove(configTab);
            window.graphView.ResetGraph();
            SetUpGraphEvents();
        }
        #endregion

        #region Start
        private void CreateGraphView()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/BasicSS.uss");
            rootVisualElement.styleSheets.Add(styleSheet);
            graphView = new DialogueGraphView
            {
                name = "Dialogue Graph"
            };
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            _controller = CreateInstance<DialogueGraphController>();
            _controller.Initialize();
            configTab = _controller.SettingsContainer;

            rootVisualElement.Add(configTab);

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
            _controller.OnChange += () =>
            {
                window.name = window.name.Replace("*", "") + "*";
            };
            _controller.OnSave += () => window.name = window.name.Replace("*", "");
            _controller.Notify += (m) => ShowNotificationMessage(m);
            graphView.OnSave += _controller.Save;
            graphView.OnExportResults += _controller.SaveResults;
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

            var label = new Label();
            _notificationElement.Add(label);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            label.style.width = new Length(50, LengthUnit.Percent);
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

    public class DialogueGraphView : GraphView
    {
        public bool saved = true;
        private EdgeConnector<Edge> _edgeConnector;
        private CreateNodeEdgeConnectorListener _conectorListener;
        private NodeSearchProvider _searchProvider;

        public event Action<BaseNode> OnNodeCreated;
        public event Action<GraphViewNode> OnNodeDeleted;
        public event Action<Port, Port> OnPortConnected;
        public event Action<Port, Port> OnPortDisconnected;
        public Func<string, BaseNode> Get_BaseNode;
        public event Action OnSave;
        public event Action OnExportResults;

        public CreateNodeEdgeConnectorListener ConnectorListener
        {
            get
            {
                if (_conectorListener == null)
                    _conectorListener = new CreateNodeEdgeConnectorListener(this);
                return _conectorListener;
            }
        }
        public EdgeConnector<Edge> SharedEdgeConnector
        {
            get
            {
                if (_edgeConnector == null)
                    _edgeConnector = new EdgeConnector<Edge>(ConnectorListener);
                return _edgeConnector;
            }
        }

        public DialogueGraphView()
        {
            GridBackground grid = new();
            Insert(0, grid);
            grid.StretchToParentSize();

            this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            graphViewChanged = OnGraphChanged;

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    evt.StopImmediatePropagation();

                    ShowContextMenu(evt.mousePosition);
                }
            }, TrickleDown.TrickleDown);

            this.contentContainer.style.width = 5000;
            this.contentContainer.style.height = 5000;

            _searchProvider = ScriptableObject.CreateInstance<NodeSearchProvider>();
            _searchProvider.Init(this);

            //schedule.Execute(ResetPositionAndScale).ExecuteLater(1000);
            // Centrar vista en el medio
            //ScheduleExecute(() => ClearAndCenterView());
        }

        public void ResetGraph()
        {
            DeleteElements(graphElements.ToList());
            OnSave = null;
            OnExportResults = null;
            OnNodeCreated = null;
            OnNodeDeleted = null;
            Get_BaseNode = null;
            OnPortConnected = null;
            OnPortDisconnected = null;
            OnPortDisconnected += OnEdgeDisconnected;
            saved = true;
        }

        private void OnEdgeDisconnected(Port from, Port to)
        {
            (from.node as GraphViewNode).Parent.RemoveChild((to.node as GraphViewNode).Parent.Id);
        }

        void ShowContextMenu(Vector2 position)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Crear nodo"), false, () => OpenCreateNodeSearch(position, null));
            menu.AddItem(new GUIContent(saved ? "Save" : "Save*"), false, () => OnSave?.Invoke());
            menu.AddItem(new GUIContent("Export results"), false, () => OnExportResults?.Invoke());
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return (from nap in ports.ToList()
                    where nap.direction != startPort.direction && nap.node != startPort.node &&
                        !nap.connections.Any(p => GetInput(startPort.direction, p).node == startPort.node)
                    select nap).ToList();
        }

        private Port GetInput(Direction direction, Edge edge) =>
            direction switch
            {
                Direction.Input => edge.input,
                _ => edge.output
            };

        void ResetPositionAndScale()
        {
            contentViewContainer.transform.position = -new Vector3(2500, 2500, 0);
            contentViewContainer.transform.scale = Vector3.one;
        }

        private void ScheduleExecute(System.Action action)
        {
            schedule.Execute(() =>
            {
                action.Invoke();
            }).ExecuteLater(100);
        }

        private void ClearAndCenterView()
        {
            Vector2 center = new Vector2(contentContainer.layout.width / 2, contentContainer.layout.height / 2);
            contentViewContainer.transform.position = -center;
            contentViewContainer.transform.scale = Vector3.one;
        }

        public GraphViewNode LoadNode(Vector2 position, NodeType type, BaseNode node)
        {
            node.Initilize(this, position, null);
            AddElement(node.GraphViewNode);

            OnNodeCreated?.Invoke(node);
            return node.GraphViewNode;
        }

        public GraphViewNode CreateNode(Vector2 position, NodeType type, GraphViewNode prevNode = null)
        {
            var node = InstanciateNode(type);
            node.Initilize(this, position, prevNode?.Parent);
            
            AddElement(node.GraphViewNode);

            OnNodeCreated?.Invoke(node);
            return node.GraphViewNode;
        }

        private BaseNode InstanciateNode(NodeType type) =>
            type switch
            {
                NodeType.Dialogue => ScriptableObject.CreateInstance<DialogueNode>(),
                NodeType.Mission => ScriptableObject.CreateInstance<MissionNode>(),
                _ => ScriptableObject.CreateInstance<BaseNode>()
            };

        public void CreateConnectedNode(GraphViewNode fromNode)
        {
            var fromPort = fromNode.output;
            var toNode = CreateNode(fromNode.GetPosition().position + new Vector2(250, 0), NodeType.Dialogue);
            var toPort = toNode.input;

            var edge = fromPort.ConnectTo(toPort);
            AddElement(edge);
        }

        public void Connect(Port from, Port to)
        {
            var edge = from.ConnectTo(to);
            AddElement(edge);
            AddConnection(edge);
        }

        // Abre el buscador para crear nodo y conectar desde 'fromPort'
        public void OpenCreateNodeSearch(Vector2 dropPosition, Port fromPort)
        {
            // dropPosition ya viene en coords del graph (contentViewContainer) en versiones recientes.
            // Si ves desalineación, convierte: dropPosition = contentViewContainer.WorldToLocal(dropPosition);
            dropPosition = contentViewContainer.WorldToLocal(dropPosition);
            _searchProvider.SetupInvocation(fromPort, dropPosition);

            // Convierte a pantalla para SearchWindow
            var screenPos = GUIUtility.GUIToScreenPoint(Event.current != null ? Event.current.mousePosition : Vector2.zero);
            SearchWindow.Open(new SearchWindowContext(screenPos, 50, 50), _searchProvider);
        }

        private GraphViewChange OnGraphChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var from = edge.output;
                    var to = edge.input;

                    OnPortConnected?.Invoke(from, to);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is not Edge edge) continue;

                    var from = edge.output;
                    var to = edge.input;

                    OnPortDisconnected?.Invoke(from, to);
                }
            }

            return change;
        }

        public override EventPropagation DeleteSelection()
        {
            foreach (var node in selection)
            {
                if (node is GraphViewNode graphNode)
                {
                    OnNodeDeleted?.Invoke(graphNode);
                }
            }

            return base.DeleteSelection();
        }

        public void AddConnection(Edge edge)
        {
            if (Get_BaseNode == null) return;

            var inputNode = (edge.input.node as GraphViewNode).Parent;
            var outputNode = (edge.output.node as GraphViewNode).Parent;

            if (inputNode != null && outputNode != null)
                outputNode.AddChild(inputNode.Id);

            NotifyConnection(edge);
        }

        public void NotifyConnection(Edge edge)
        {
            var from = edge.output;
            var to = edge.input;

            OnPortConnected?.Invoke(from, to);
        }

        public void RemoveConnection(Edge edge)
        {
            if (Get_BaseNode == null) return;

            RemoveElement(edge);
            var node = (edge.input.node as GraphViewNode).Parent;
            if (node != null)
            {
                node.RemoveChild((edge.output.node as GraphViewNode).Parent.Id);
            }
        }
    }
}
