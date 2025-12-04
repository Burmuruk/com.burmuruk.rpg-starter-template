using Burmuruk.WorldG.Patrol;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.AI
{
    public class LayerNode : IPathNode
    {
        public int Layer { get; private set; }

        public uint ID { get; private set; }

        public Vector3 Position => throw new NotImplementedException();

        public bool IsEnabled { get; private set; } = true;

        public List<NodeConnection> NodeConnections => throw new NotImplementedException();

        public void ClearConnections()
        {
            NodeConnections.Clear();
        }

        public void Enable(bool shouldEnable = true)
        {
            IsEnabled = shouldEnable;
        }

        public float GetDistanceBetweenNodes(in NodeConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
