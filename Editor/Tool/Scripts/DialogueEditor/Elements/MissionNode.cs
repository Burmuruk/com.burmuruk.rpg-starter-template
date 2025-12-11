using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class MissionNode : BaseNode
    {
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
    }
}
