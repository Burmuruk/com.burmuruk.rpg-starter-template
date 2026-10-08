using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
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
        public event Action OnChanged;

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
            OnPortDisconnected += OnEdgeDisconnected;

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
            // Clearing a view must not delete connections from the graph data.
            ClearSelection();
            foreach (var element in graphElements.ToList())
                RemoveElement(element);
            OnSave = null;
            OnExportResults = null;
            OnChanged = null;
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
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Create node"), false, () => OpenCreateNodeSearch(position, null));
            menu.AddItem(new GUIContent(saved ? "Save" : "Save*"), false, () => OnSave?.Invoke());
            menu.AddItem(new GUIContent("Export results"), false, () => OnExportResults?.Invoke());
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return (from port in ports.ToList()
                    where port.direction != startPort.direction && port.node != startPort.node &&
                        !port.connections.Any(p => GetInput(startPort.direction, p).node == startPort.node)
                    select port).ToList();
        }

        public static bool AreConnected(Port a, Port b)
        {
            return AreConnected(a, b) || AreConnected(b, a);
        }

        public bool IsChild(Port from, Port to)
        {
            if (from == null || to == null)
                return false;

            if (from == to)
                return true;

            HashSet<Port> visited = new();
            Stack<Port> stack = new();

            stack.Push(from);

            while (stack.Count > 0)
            {
                Port current = stack.Pop();

                if (current == null)
                    continue;

                if (!visited.Add(current))
                    continue;

                if (current == to)
                    return true;

                foreach (var edge in current.connections)
                {
                    stack.Push(GetInput(Direction.Output, edge));
                }
            }

            return false;
        }

        private Port GetInput(Direction direction, Edge edge)
        {
            return direction switch
            {
                Direction.Input => edge.input,
                _ => edge.output
            };
        }

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
            Vector2 center = new(contentContainer.layout.width / 2, contentContainer.layout.height / 2);
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

        private BaseNode InstanciateNode(NodeType type)
        {
            return type switch
            {
                NodeType.Dialogue => ScriptableObject.CreateInstance<DialogueNode>(),
                NodeType.Mission => ScriptableObject.CreateInstance<MissionNode>(),
                _ => ScriptableObject.CreateInstance<BaseNode>()
            };
        }

        public void CreateConnectedNode(GraphViewNode fromNode)
        {
            var fromPort = fromNode.output;
            var toNode = CreateNode(fromNode.GetPosition().position + new Vector2(250, 0), NodeType.Dialogue);
            var toPort = toNode.input;

            Connect(fromPort, toPort);
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
            if (change.movedElements != null && change.movedElements.Count > 0)
                OnChanged?.Invoke();
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var from = edge.output;
                    var to = edge.input;

                    AddConnection(edge);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is not Edge edge)
                        continue;

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
            if (Get_BaseNode == null)
                return;

            if (!edge.input.connections.Contains(edge)) edge.input.Connect(edge);
            if (!edge.output.connections.Contains(edge)) edge.output.Connect(edge);
            if (edge.parent == null) AddElement(edge);

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
            if (Get_BaseNode == null)
                return;

            OnPortDisconnected?.Invoke(edge.output, edge.input);
            edge.input.Disconnect(edge);
            edge.output.Disconnect(edge);
            RemoveElement(edge);
        }
    }
}
