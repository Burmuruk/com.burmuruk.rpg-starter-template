using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class GraphViewNode : Node
    {
        public const string PIN_BUTTON_NAME = "pinButton";
        public Port input;
        public Port output;

        public event Action OnSelect;
        public event Action OnDeselected;
        public event Action OnConnect;
        public event Action OnDisconnect;

        private readonly Color unreachableColor = new Color(0.8235294f, 0.8235294f, 0.8235294f, 1);
        private readonly float unreachableRadious = 5f;
        private Button collapseButton;
        private bool collapsed = false;
        private DialogueGraphView graph;

        public Button StartBtn { get; private set; }
        public Button MissionBtn { get; private set; }
        public Button DialogueBtn { get; private set; }
        public Button BranchBtn { get; private set; }
        public BaseNode Parent { get; private set; }
        public TextField TxtOnEnterAction { get; private set; }
        public TextField TxtOnExitAction { get; private set; }
        public VisualElement AlwaysVisibleContainer { get; set; }

        public GraphViewNode(DialogueGraphView graphView, BaseNode parent)
        {
            graph = graphView;
            Parent = parent;
            title = "No character";

            input = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,
                typeof(float)
            );
            input.portName = "Input";

            output = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                typeof(float)
            );
            output.portName = "Output";
            
            inputContainer.Add(input);
            outputContainer.Add(output);

            input.AddManipulator(new EdgeConnector<Edge>(graph.ConnectorListener));
            output.AddManipulator(new EdgeConnector<Edge>(graph.ConnectorListener));

            var collapsible = topContainer.parent.Q<VisualElement>("collapsible-area");
            //topContainer.parent.Remove(collapsible);
            AlwaysVisibleContainer = new VisualElement();
            topContainer.parent.Add(AlwaysVisibleContainer);
            //topContainer.parent.Add(collapsible);
            TxtOnEnterAction = new TextField("On Enter Action:");
            TxtOnExitAction = new TextField("On Exit Action:");
            extensionContainer.Add(TxtOnEnterAction);
            extensionContainer.Add(TxtOnExitAction);

            extensionContainer.Add(new VisualElement());
            extensionContainer.style.marginTop = 6;
            extensionContainer.style.marginBottom = 6;

            var colorRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            StartBtn = MakeColorButton(new Color(0.6078432f, 0.1490196f, 0.1490196f), "Enable start point");
            DialogueBtn = MakeColorButton(new Color(0.01568628f, 0.572549f, 0.6235294f), "Connected to dialogue node");
            MissionBtn = MakeColorButton(new Color(0.254902f, 0.3490196f, 0.7333333f), "Connected to mission node");
            BranchBtn = MakeColorButton(new Color(0.9921569f, 0.6941177f, 0.3176471f), "Connected to branch node");
            colorRow.Add(StartBtn);
            colorRow.Add(MissionBtn);
            colorRow.Add(DialogueBtn);
            colorRow.Add(BranchBtn);
            colorRow.Add(MakePinButton());
            colorRow.style.alignItems = Align.Center;
            titleButtonContainer.Add(colorRow);

            RefreshExpandedState();
            RefreshPorts();

            if (collapsed)
                ToggleCollapse();
        }

        public void ShowButton(Button button, bool shouldShow)
        {
            button.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ColorButton(Button button, bool shouldColor)
        {
            if (shouldColor)
                button.style.backgroundColor = button.style.borderTopColor;
            else
                button.style.backgroundColor = new Color(0.345098f, 0.345098f, 0.345098f);
        }

        protected override void ToggleCollapse()
        {
            collapsed = !collapsed;
            extensionContainer.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            //collapseButton.text = collapsed ? "?" : "?";
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnSelect?.Invoke();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            OnDeselected?.Invoke();
        }

        private Button MakeColorButton(Color color, string tooltip)
        {
            var button = new Button();
            button.tooltip = tooltip;
            button.style.borderTopColor = color;
            button.style.borderBottomColor = color;
            button.style.borderLeftColor = color;
            button.style.borderRightColor = color;
            button.style.borderTopWidth = 2; 
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.marginLeft = 1;
            button.style.marginRight = 1;
            button.style.width = 20;
            button.style.height = 20;
            return button;
        }

        private Button MakePinButton()
        {
            var button = new Button();
            button.tooltip = "Pin node";
            button.name = PIN_BUTTON_NAME;
            Texture2D pinIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/com.burmuruk.rpg-starter-template/Tool/Art/Editor/Pin.png", typeof(Texture2D));
            button.style.backgroundImage = new StyleBackground(pinIcon);
            button.style.unityBackgroundImageTintColor = new Color(0.5169811f, 0.5169811f, 0.5169811f);
            button.style.marginLeft = 1;
            button.style.marginRight = 4;
            button.style.width = 20;
            button.style.height = 20;
            return button;
        }

        public void MakeStartButton(bool value, bool executable)
        {
            if (value)
            {
                StartBtn.style.display = DisplayStyle.Flex;
                ColorButton(StartBtn, value && executable);
            }
            else
            {
                StartBtn.style.display = DisplayStyle.None;
                ColorButton(StartBtn, false);
            }

        }

        public bool IsBtnDisplayed(Button button) =>
            button.style.display == DisplayStyle.Flex;

        private void EnableStatusButton(Button button, bool enabled)
        {
            if (enabled)
            {
                button.style.opacity = 1f;
                button.SetEnabled(true);
            }
            else
            {
                button.style.opacity = 0.5f;
                button.SetEnabled(false);
            }
        }

        public void EnableExecution(bool value)
        {
            if (value)
            {
                style.backgroundColor = Color.clear;
            }
            else
            {
                style.backgroundColor = unreachableColor;
                style.borderBottomLeftRadius = 5f;
                style.borderBottomRightRadius = 5f;
                style.borderTopLeftRadius = 5f;
                style.borderTopRightRadius = 5f;
            }
        }
    }

    public class CreateNodeEdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly DialogueGraphView graph;

        public CreateNodeEdgeConnectorListener(DialogueGraphView graphView)
        {
            graph = graphView;
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            if (edge.input != null && edge.output != null)
            {
                var graph = graphView as DialogueGraphView;
                graph?.AddConnection(edge); // <-- SOLO datos, no edges
            }
        }




        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Port fromPort = edge.output ?? edge.input;
            edge.RemoveFromHierarchy();
            graph.OpenCreateNodeSearch(position, fromPort);
        }
    }

}
