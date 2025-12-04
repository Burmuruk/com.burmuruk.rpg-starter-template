using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    public interface IPathNode
    {
        public uint ID { get; }
        public Vector3 Position { get; }
        public bool IsEnabled { get; }

        public List<NodeConnection> NodeConnections { get; }

        public float GetDistanceBetweenNodes(in NodeConnection connection);
        
        public void ClearConnections();
        public void Enable(bool shouldEnable = true);
    }
}
