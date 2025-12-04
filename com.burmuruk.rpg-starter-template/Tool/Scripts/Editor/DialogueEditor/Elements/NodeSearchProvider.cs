using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    class NodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView _graph;
        private Port _fromPort;
        private Vector2 _spawnPos;

        public void Init(DialogueGraphView graph) => _graph = graph;

        public void SetupInvocation(Port fromPort, Vector2 spawnPos)
        {
            _fromPort = fromPort;
            _spawnPos = spawnPos;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var names = new List<string>();
            var values = new List<SearchTreeEntry>()
                { new SearchTreeGroupEntry(new GUIContent("Create Node"), 0) };
            int i = 1;
            var allNames = Enum.GetNames(typeof(NodeType)).Except(new[] { NodeType.None.ToString() });

            foreach (var name in allNames)
            {
                values.Add(new SearchTreeEntry(new GUIContent(name)) { level = 1, userData = (NodeType)i++ });
            }

            return values;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var type = (NodeType)entry.userData;

            GraphViewNode fromNode = _fromPort?.node != null ? _fromPort.node as GraphViewNode : null;
            var newNode = _graph.CreateNode(_spawnPos, type, fromNode);
            
            if (fromNode != null)
                Set_Connection(newNode);

            return true;
        }

        private void Set_Connection(GraphViewNode newNode)
        {
            if (_fromPort.direction == Direction.Output)
                _graph.Connect(_fromPort, newNode.input);
            else
                _graph.Connect(newNode.output, _fromPort);
        }
    }

}
