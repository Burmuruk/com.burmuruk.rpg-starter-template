using Burmuruk.AI.PathFinding;
using System;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    public interface ISplineNode
    {
        public uint ID { get; }
        public NodeData NodeData { get; }
        public bool IsSelected { get; }
        public Vector3 Position { get; }
        public Transform Transform { get; }
        public PatrolController PatrolController { get; set; }
        public void SetNodeData(NodeData nodeData);
        public static bool operator true(ISplineNode p) => p != null;

        public static bool operator false(ISplineNode p) => p == null;
        public event Action<PatrolNode, PatrolNode> OnNodeAdded;
        public event Action<PatrolNode> OnNodeRemoved;
    }
}
