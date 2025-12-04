using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    public interface INodeListSupplier
    {
        public float NodeDistance { get; }
        public float PlayerRadious { get; }
        public float MaxAngle { get; }
        public bool Initilized { get; }
        //public IEnumerable<IPathNode> Nodes { get; }

        public void SetTarget(float pRadious = .2f, float maxDistance = 2, float maxAngle = 45, float height = 1);

        public IPathNode FindNearestNode(Vector3 start);

        public bool ValidatePosition(Vector3 position, IPathNode nearestPoint);

        public IPathNode FindNearestNodeAround(IPathNode start, Vector3 destiny, float maxDistance = 0);

        public void SetNodes(ICollection<IPathNode> nodes);

        public void SetConnections(IPathNode[] connections);
    }
}
