using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class BaseNode : ScriptableObject
    {
        [SerializeField] public string characterID = null;
        [SerializeField] public string dialogueName;
        [SerializeField] bool isPlayerSpeaking = false;
        [SerializeField] bool _isStartNode = false;
        [SerializeField] private bool _isPinned = false;
        [SerializeField] private bool _expanded = true;
        [SerializeField] private Vector2 _position;
        [SerializeField] List<string> _children = new();
        [SerializeField] private bool _isExecutable = true;
        [SerializeField] private string onEnterAction;
        [SerializeField] private string onExitAction;
        //[SerializeField] string onEnterAction;
        //[SerializeField] string onExitAction;

        private string _id = null;
        private bool isInfoDisplayed;
        private bool isCreatingNode;
        public DialogueGraphView _graphView;

        public event Action<BaseNode> OnSelected;
        public event Action<BaseNode> OnDeselected;
        public event Action<BaseNode, bool> OnPinned;
        public event Action<string, bool> OnStartPointCreated;
        internal Action<BaseNode, bool> OnExecutionChanged;

        public Vector2 Position { get => _position; private set => _position = value; }
        public VisualElement Element { get; private set; }
        public GraphViewNode GraphViewNode { get; private set; }
        public Button BtnPin { get; private set; }
        public List<string> Children => _children;
        public bool IsExecutable
        {
            get => _isExecutable;
            set
            {
                _isExecutable = value;
                GraphViewNode.EnableExecution(value);
            }
        }

        public string Id { get => _id; private set => _id = value; }
        public TextField TxtCharacterId { get; private set; }
        public string Title { get => GraphViewNode.title; set => GraphViewNode.title = value; }
        public bool IsPlayerSpeaking
        {
            get => isPlayerSpeaking;
            set
            {
                Undo.RecordObject(this, "Change dialogue speaker");
                isPlayerSpeaking = value;
                EditorUtility.SetDirty(this);
            }
        }
        public virtual bool IsStartNode =>
            !GraphViewNode.input.connected;

        public virtual void Initilize(DialogueGraphView graph, Vector2 startPosition, BaseNode prevNode)
        {
            _graphView = graph;
            GraphViewNode = new GraphViewNode(graph, this);
            Id ??= Guid.NewGuid().ToString();
            characterID ??= Guid.NewGuid().ToString();
            SetPosition(startPosition);
            BtnPin = GraphViewNode.Q<Button>(GraphViewNode.PIN_BUTTON_NAME);
            GraphViewNode.StartBtn.clicked += StartBtn_clicked;

            GraphViewNode.OnSelect += () => OnSelected?.Invoke(this);
            GraphViewNode.OnDeselected += () => OnDeselected?.Invoke(this);
            BtnPin.clicked += () =>
            {
                _isPinned = !_isPinned;
                BtnPin.style.unityBackgroundImageTintColor = _isPinned ?
                    new Color(0.7960784f, 0.6313726f, 0.1019608f) :
                    new Color(0.5169811f, 0.5169811f, 0.5169811f);
                OnPinned?.Invoke(this, _isPinned);
            };
            Set_CharacterData(prevNode);
            MakeStartButton(prevNode == null);
        }

        public void ClearEvents()
        {
            OnPinned = null;
            OnSelected = null;
            OnDeselected = null;
            OnStartPointCreated = null;
            OnExecutionChanged = null;
        }

        public virtual void LoadData()
        {
            MakeStartButton(_isStartNode);
            GraphViewNode.EnableExecution(_isExecutable);
            _id = this.name;
        }

        private void StartBtn_clicked()
        {
            IsExecutable = !IsExecutable;
            GraphViewNode.ColorButton(GraphViewNode.StartBtn, _isExecutable);
            OnExecutionChanged?.Invoke(this, _isExecutable);
        }

        public virtual RPGStarterTemplate.Dialogue.DialogueNode GetNodeData(RPGStarterTemplate.Dialogue.DialogueNode nodeData)
        {
            nodeData ??= new();
            nodeData.Id = this.Id;
            nodeData.Children = (from c in GraphViewNode.output.connections
                        select (c.input.node as GraphViewNode).Parent.GetNodeData(null)).ToList();
            nodeData.onEnterAction = onEnterAction;
            nodeData.onExitAction = onExitAction;
            nodeData.characterName = Title;
            return nodeData;
        }

        public void MakeStartButton(bool value)
        {
            if ((GraphViewNode.StartBtn.style.display == DisplayStyle.Flex && value)) return;

            GraphViewNode.MakeStartButton(value, IsExecutable);
            OnStartPointCreated?.Invoke(Id, value);
        }

        public void OnPortConnected(BaseNode from, BaseNode to, Port fromp, Port toP)
        {
            Color_StatusButtonConnection(from, fromp, to);
            Color_StatusButtonConnection(to, toP, from);
        }

        public void OnPortDisconnected(BaseNode from, BaseNode to, Port fromp, Port toP)
        {
            Color_StatusButtonDesconnection(from, fromp, to);
            Color_StatusButtonDesconnection(to, toP, from);
        }

        public void Set_DefaultStatusButton()
        {
            GraphViewNode.ShowButton(GraphViewNode.BranchBtn, false);
            GraphViewNode.ShowButton(GraphViewNode.MissionBtn, false);
            GraphViewNode.ShowButton(GraphViewNode.DialogueBtn, false);
            MakeStartButton(true);
        }

        public static void UpdateExecution(BaseNode node, bool notify, params Edge[] exception)
        {
            bool initialState = node.IsExecutable;
            var connnections = node.GraphViewNode.input.connections.Except(exception);
            
            foreach (var edge in connnections)
            {
                if (!(edge.output.node as GraphViewNode).Parent.IsExecutable)
                {
                    node.IsExecutable = false;
                    Notify(node, notify, initialState);
                    return;
                }
            }

            node.IsExecutable = true;
            Notify(node, notify, initialState);

            static void Notify(BaseNode node, bool notify, bool initialState)
            {
                if (notify && initialState != node.IsExecutable)
                    node.OnExecutionChanged?.Invoke(node, node.IsExecutable);
            }
        }

        private void Color_StatusButtonConnection(BaseNode node, Port port, BaseNode other)
        {
            bool value = false;

            node.GraphViewNode.ShowButton(node.GraphViewNode.BranchBtn, false);

            if (port.direction == Direction.Output)
            {
                value = port.connections.Any(c => (c.input.node as GraphViewNode).Parent is MissionNode);
                node.GraphViewNode.ShowButton(node.GraphViewNode.MissionBtn, value); 

                value = port.connections.Any(c => (c.input.node as GraphViewNode).Parent is DialogueNode);
                node.GraphViewNode.ShowButton(node.GraphViewNode.DialogueBtn, value);
            }
            else
            {
                if (other.IsExecutable && !node.IsExecutable)
                {
                    UpdateExecution(node, true);
                }
            }

            value = port.direction != Direction.Input && node.IsStartNode;
            node.MakeStartButton(value);
        }

        private void Color_StatusButtonDesconnection(BaseNode node, Port port, BaseNode other)
        {
            bool value = false;

            node.GraphViewNode.ShowButton(node.GraphViewNode.BranchBtn, false);

            if (port.direction == Direction.Output)
            {
                value = port.connections.Count(c => (c.input.node as GraphViewNode).Parent is MissionNode) > 1;
                node.GraphViewNode.ShowButton(node.GraphViewNode.MissionBtn, value); 

                value = port.connections.Count(c => (c.input.node as GraphViewNode).Parent is DialogueNode) > 1;
                node.GraphViewNode.ShowButton(node.GraphViewNode.DialogueBtn, value); 
            }
            else
            {
                var edge = GetEdge(other.GraphViewNode.output, port);
                UpdateExecution(node, true, edge);
            }

             value = (port.direction == Direction.Output && node.IsStartNode) || 
                (port.direction == Direction.Input && port.connections.Count() == 1);
            node.MakeStartButton(value);
        }

        private Edge GetEdge(Port output, Port input)
        {
            return output.connections.First(c => c.input == input);
        }

        protected virtual void Set_CharacterData(BaseNode prevNode)
        {
            if (prevNode == null) return;

            if (!string.IsNullOrEmpty(prevNode.characterID))
            {
                characterID = prevNode.characterID;
                this.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor = 
                    prevNode.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor;
                Title = prevNode.Title;
            }

            if (!prevNode.IsExecutable)
                IsExecutable = false;
        }

        public List<string> GetChildren()
        {
            return _children;
        }

#if UNITY_EDITOR
        public void SetPosition(Vector2 newPosition)
        {
            Undo.RecordObject(this, "Move Dialogue Node");
            GraphViewNode.SetPosition(new Rect(newPosition, new Vector2(200, 150)));
            EditorUtility.SetDirty(this);
        }

        public void AddChild(string childId)
        {
            _children.Add(childId);
        }

        public void RemoveChild(string childId)
        {
            _children.Remove(childId);
        }

        public virtual void Save()
        {
            if (GraphViewNode != null)
            {
                _position = GraphViewNode.GetPosition().position;
                _expanded = GraphViewNode.expanded;
                this.name = Id;
                onEnterAction = this.onEnterAction = GraphViewNode.TxtOnEnterAction.value;
                onExitAction = this.onExitAction = GraphViewNode.TxtOnExitAction.value;
            }
            _isStartNode = IsStartNode;
        }
#endif

        protected TextField AddTextField(VisualElement container, string tag)
        {
            var content = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1 ,
                    marginTop = 6,
                    marginBottom = 6,
                }
            };
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var label = new Label(tag);
            label.style.marginLeft = 2;
            row.Add(label);
            TextField textField = new TextField(500, true, false, '*')
            { style = { flexGrow = 1, flexShrink = 1, maxWidth = 400, flexBasis = 100 } };
            row.Add(textField);

            content.Add(row);
            container.Add(row);
            return textField;
        }
    }
}
