using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class MissionNode : BaseNode
    {
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private string _instructions;
        public TextField TFTitle { get; private set; }
        public TextField TFDescription { get; private set; }
        public TextField TFInstructions { get; private set; }

        public override void Initilize(DialogueGraphView graph, Vector2 startPosition, BaseNode prev)
        {
            base.Initilize(graph, startPosition, prev);
            
            TFTitle = AddTextField(GraphViewNode.extensionContainer, "Title");
            TFDescription = AddTextField(GraphViewNode.extensionContainer, "Description");
            TFInstructions = AddTextField(GraphViewNode.extensionContainer, "Instructions");
        }

        public override void Save()
        {
            base.Save();
            _title = TFTitle.value;
            _description = TFDescription.value;
            _instructions = TFInstructions.value;
        }

        public override void LoadData()
        {
            base.LoadData();
            TFTitle.SetValueWithoutNotify(_title);
            TFDescription.SetValueWithoutNotify(_description);
            TFInstructions.SetValueWithoutNotify(_instructions);
        }
    }
}
