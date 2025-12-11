using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class DialogueNode : BaseNode
    {
        [SerializeField] private string _text;

        public TextField TFMessage { get; private set; }
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    Undo.RecordObject(this, "Update Dialogue Text");
                    _text = value;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public override void Initilize(DialogueGraphView graph, Vector2 startPosition, BaseNode prev)
        {
            base.Initilize(graph, startPosition, prev);
            TFMessage = AddTextField(GraphViewNode.AlwaysVisibleContainer, "Message");

        }

        public override void Save()
        {
            base.Save();

            _text = TFMessage?.value;
        }

        public override RPGStarterTemplate.Dialogue.DialogueNode GetNodeData(RPGStarterTemplate.Dialogue.DialogueNode nodeData)
        {
            nodeData ??= new();
            nodeData.Message = this.TFMessage.value;
            base.GetNodeData(nodeData);
            return nodeData;
        }

        public override void LoadData()
        {
            base.LoadData();
            TFMessage.SetValueWithoutNotify(_text);
        }
    }
}
