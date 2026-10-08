using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class PinNode : IDisposable
    {
        private BaseNode _node;
        private Button _button;
        public string nodeId => _node.Id;
        public string note => _node.PinNote;
        public VisualElement Element { get; private set; }
        public TextField NoteField { get; private set; }
        public Label LblMessage { get; private set; }

        public void Initialize(VisualElement container, BaseNode node, Action onChanged)
        {
            _node = node;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.burmuruk.rpg-starter-template/Tool/UIToolkit/DialogueEditor/Elements/PinNode.uxml");
            Element = visualTree.Instantiate();
            Element.userData = node.Id;

            _button = Element.Q<Button>("PinNodeContainer");
            _button.clicked += FocusNode;
            Element.Q<Button>("btnStart").clicked += node.StartBtn_clicked;
            Element.Q<Button>("btnPin").clicked += node.TogglePin;
            NoteField = Element.Q<TextField>("txtNote");
            LblMessage = Element.Q<Label>("lblMessage");

            NoteField.SetValueWithoutNotify(node.PinNote);
            BindEvents(node, onChanged);
            container.Add(Element);
            Refresh();
        }

        private void BindEvents(BaseNode node, Action onChanged)
        {
            NoteField.RegisterValueChangedCallback(evt =>
            {
                node.PinNote = evt.newValue;
                onChanged?.Invoke();
            });
            
            foreach (var control in new VisualElement[] { NoteField,
                Element.Q<Button>("btnStart"), Element.Q<Button>("btnPin"),
                Element.Q<Button>("btnDialogue"), Element.Q<Button>("btnMission") })
            {
                control.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                control.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
                control.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
                control.RegisterCallback<MouseUpEvent>(evt => evt.StopPropagation());
                control.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                control.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation());
            }

            node.GraphViewNode.OnVisualStateChanged += Refresh;
        }

        private void FocusNode()
        {
            var graph = _node._graphView;
            graph.ClearSelection();
            graph.AddToSelection(_node.GraphViewNode);
            graph.FrameSelection();
            graph.Focus();
        }

        private void Refresh()
        {
            Mirror("btnStart", _node.GraphViewNode.StartBtn);
            Mirror("btnDialogue", _node.GraphViewNode.DialogueBtn);
            Mirror("btnMission", _node.GraphViewNode.MissionBtn);
            Mirror("btnPin", _node.BtnPin);
            _button.style.backgroundColor = _node.IsExecutable ? _node.GraphViewNode.BaseColour * new Vector4(1,1,1,.5f) : _node.GraphViewNode.unreachableColor;

            if (_node is DialogueNode dialogueNode)
            {
                LblMessage.text = dialogueNode.TFMessage.text;
            }
                Utilities.UtilitiesUI.EnableContainer(LblMessage, false);

            Utilities.UtilitiesUI.EnableContainer(LblMessage, true);
        }

        private void Mirror(string name, Button source)
        {
            var target = Element.Q<Button>(name);
            target.style.display = source.style.display;
            target.style.backgroundColor = source.style.backgroundColor;
            target.style.backgroundImage = source.style.backgroundImage;
            target.style.unityBackgroundImageTintColor = source.style.unityBackgroundImageTintColor;
            target.style.borderTopColor = source.style.borderTopColor;
            target.style.borderBottomColor = source.style.borderBottomColor;
            target.style.borderLeftColor = source.style.borderLeftColor;
            target.style.borderRightColor = source.style.borderRightColor;
            target.SetEnabled(source.enabledSelf);
            target.tooltip = source.tooltip;
        }

        public void Dispose()
        {
            _node.GraphViewNode.OnVisualStateChanged -= Refresh;
            Element.RemoveFromHierarchy();
        }
    }
}
