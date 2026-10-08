using System.Collections.Generic;
using UnityEngine;
using Burmuruk.AI;
using Burmuruk.Collections;
using System;
using System.Collections;
using System.Linq;

namespace Burmuruk.WorldG.Patrol
{
    [Serializable]
    public class NodeListSuplier : INodeListSupplier
    {
        IPathNode[][][] connections;

        public float NodeDistance { get; private set; }

        public float PlayerRadious { get; private set; }

        public float MaxAngle { get; private set; }
        public bool Initilized { get; private set; }

        public IEnumerable<IPathNode> Nodes => throw new NotImplementedException();

        public NodeListSuplier(IPathNode[][][] connections)
        {
            this.connections = connections;
            Initilized = true;
        }

        public IPathNode FindNearestNode(Vector3 start)
        {
            if (connections == null || connections.Length <= 0) return null;

            (int x, int y, int z)? index = null;
            int length = connections.Length - 1;

            for (int i = 0; i < connections.Length; i++)
            {
                if (i == length ||
                    (connections[i][0][0].Position.x > start.x))
                {
                    if (i > 0)
                        RoundIdx(ref i, start.x, connections[i][0][0].Position.x, connections[i - 1][0][0].Position.x);

                    length = connections[i].Length - 1;

                    for (int j = 0; j < connections[i].Length; j++)
                    {
                        if (j == length ||
                            (start.z > connections[i][j][0].Position.z))
                        {
                            if (j > 0)
                                RoundIdx(ref j, start.z, connections[i][j][0].Position.z, connections[i][j - 1][0].Position.z);

                            length = connections[i][j].Length - 1;

                            for (int k = 0; k < connections[i][j].Length; k++)
                            {
                                if (k == length ||
                                    (start.y < connections[i][j][k].Position.y))
                                {
                                    if (k > 0)
                                        RoundIdx(ref k, start.y, connections[i][j][k].Position.y, connections[i][j][k - 1].Position.y);

                                    index = (i, j, k);
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    break;
                }
            }

            return index.HasValue ? connections[index.Value.x][index.Value.y][index.Value.z] : null;

            void RoundIdx(ref int idx, float position, float current, float previous)
            {
                if (Mathf.Abs(position - previous) < Mathf.Abs(position - current))
                {
                    --idx;
                }
            }
        }

        public void SetConnections(IPathNode[][][] connections)
        {
            this.connections = connections;
            Initilized = true;
        }

        public void SetNodes(ICollection<IPathNode> nodes)
        {
            throw new NotImplementedException();
        }

        public void SetTarget(float pRadious = 0.2F, float maxDistance = 2, float maxAngle = 45, float height = 1)
        {
            PlayerRadious = pRadious;
            NodeDistance = maxDistance;
            MaxAngle = maxAngle;
        }

        public bool ValidatePosition(Vector3 position, IPathNode nearestPoint)
        {
            var curDirections = GetDirections(nearestPoint.Position, position);

            if (curDirections.Count <= 0) return false;

            foreach (var nextDir in nearestPoint.NodeConnections)
            {
                var directions = GetDirections(nearestPoint.Position, nextDir.node.Position);

                foreach (var curdir in directions)
                {
                    bool founded = false;

                    for (int i = 0; i < curDirections.Count; i++)
                    {
                        if (curdir == curDirections[i])
                        {
                            curDirections.Remove(curDirections[i]);
                            founded = true;
                            break;
                        }
                    }

                    if (founded) break;
                }

                if (curDirections.Count <= 0)
                    return true;
            }

            return false;
        }

        public IPathNode FindNearestNodeAround(IPathNode start, Vector3 destiny, float maxDistance = 0)
        {
            if (start == null) return null;

            if (maxDistance <= 0)
                maxDistance = Vector3.Distance(start.Position, destiny);

            IPathNode curNode = start;
            var visited = new HashSet<IPathNode>();

            while (visited.Add(curNode) && curNode.NodeConnections != null)
            {
                IPathNode closest = null;
                float bestDistance = Vector3.SqrMagnitude(curNode.Position - destiny);

                foreach (var connection in curNode.NodeConnections)
                {
                    var next = connection.node;

                    if (next == null || !next.IsEnabled || visited.Contains(next) ||
                        Vector3.Distance(start.Position, next.Position) > maxDistance) 
                        continue;

                    float distance = Vector3.SqrMagnitude(next.Position - destiny);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        closest = next;
                    }
                }

                if (closest == null) break;

                curNode = closest;
            }

            return curNode.IsEnabled ? curNode : null;
        }

        List<Direction> GetDirections(Vector3 curPos, Vector3 nextPos)
        {
            List<Direction> directions = new();

            if (nextPos.x > curPos.x)
            {
                directions.Add(Direction.Right);
            }
            else if (nextPos.x < curPos.x)
            {
                directions.Add(Direction.Left);
            }

            if (nextPos.z > curPos.z)
            {
                directions.Add(Direction.Next);
            }
            else if (nextPos.z < curPos.z)
            {
                directions.Add(Direction.Previous);
            }

            return directions;
        }

        public void SetConnections(IPathNode[] connections)
        {
            throw new NotImplementedException();
        }
    }

    //public class NodesEnumerator : IEnumerator<IPathNode>
    //{
    //    IPathNode[][][] connection;
    //    int x = -1;
    //    int y = 0;
    //    int z = -1;

    //    public IPathNode Current => connection[x][y][z];

    //    object IEnumerator.Current => connection[x][y][z];

    //    public void Dispose()
    //    {
    //        connection = null;
    //    }

    //    public bool MoveNext()
    //    {
    //        if (connection[x][y][z];
    //    }

    //    public void Reset()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
