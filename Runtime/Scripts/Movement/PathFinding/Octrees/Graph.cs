using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement.PathFindig
{
    public class Graph
    {
        public List<Edge> edges = new List<Edge>();
        public List<Node> nodes = new List<Node>();
        //List<Node> pathList = new List<Node>();

        public Graph() { }

        public void AddNode(OctreeNode octreeNode)
        {
            if (FindNode(octreeNode.id) == null)
            {
                Node node = new Node(octreeNode);
                nodes.Add(node);
            }
        }

        public void AddEdge(OctreeNode fromNode, OctreeNode toNode)
        {
            Node from = FindNode(fromNode.id);
            Node to = FindNode(toNode.id);

            if (from != null && to != null)
            {
                Edge e = new Edge(from, to);
                edges.Add(e);
                from.edgeList.Add(e);
                Edge f = new Edge(to, from);
                edges.Add(f);
                to.edgeList.Add(f);
            }
        }

        Node FindNode(int octreNodeId)
        {
            foreach (var node in nodes)
            {
                if (node.GetNode().id == octreNodeId)
                {
                    return node;
                }
            }

            return null;
        }

        public void Draw()
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Debug.DrawLine(edges[i].startNode.octreeNode.nodeBounds.center,
                    edges[i].endNode.octreeNode.nodeBounds.center, Color.red);
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                Gizmos.color = new Color(1, 1, 0);
                Gizmos.DrawWireSphere(nodes[i].octreeNode.nodeBounds.center, .25f);
            }
        }

        public bool AStar(OctreeNode startOctNode, OctreeNode endOctNode, List<Node> pathList)
        {
            pathList.Clear();
            Node start = FindNode(startOctNode.id);
            Node end = FindNode(endOctNode.id);

            if (start == null || end == null)
                return false;

            List<Node> open = new();
            List<Node> close = new();
            float tentative_g_score = 0;
            bool tentative_is_better;

            start.g = 0;
            start.h = Vector3.SqrMagnitude(startOctNode.nodeBounds.center - endOctNode.nodeBounds.center);
            start.f = start.h;

            open.Add(start);

            while (open.Count > 0)
            {
                int i = LowestF(open);
                Node curNode = open[i];

                if (curNode.octreeNode.id == endOctNode.id)
                {
                    ReconstructPath(start, end, pathList);
                    return true;
                }

                open.RemoveAt(i);
                close.Add(curNode);

                Node neighbour;

                foreach (var edge in curNode.edgeList)
                {
                    neighbour = edge.endNode;
                    neighbour.g = curNode.g + Vector3.SqrMagnitude(curNode.octreeNode.nodeBounds.center -
                        neighbour.octreeNode.nodeBounds.center);

                    if (close.IndexOf(neighbour) > -1)
                        continue;

                    tentative_g_score = curNode.g + Vector3.SqrMagnitude(curNode.octreeNode.nodeBounds.center -
                        neighbour.octreeNode.nodeBounds.center);

                    if (open.IndexOf(neighbour) == -1)
                    {
                        open.Add(neighbour);
                        tentative_is_better = true;
                    }
                    else if (tentative_g_score < neighbour.g)
                    {
                        tentative_is_better = true;
                    }
                    else
                        tentative_is_better = false;

                    if (tentative_is_better)
                    {
                        neighbour.cameFrom = curNode;
                        neighbour.g = tentative_g_score;
                        neighbour.h = Vector3.SqrMagnitude(curNode.octreeNode.nodeBounds.center -
                            endOctNode.nodeBounds.center);

                        neighbour.f = neighbour.g + neighbour.h;
                    }
                }
            }
            return false;
        }

        public void ReconstructPath(Node start, Node end, List<Node> pathList)
        {
            pathList.Clear();
            pathList.Add(end);
            var previous = end.cameFrom;

            while (previous != start && previous != null)
            {
                pathList.Insert(0, previous);
                previous = previous.cameFrom;
            }

            pathList.Insert(0, start);
        }

        int LowestF(List<Node> nodes)
        {
            if (nodes.Count == 0) return 0;

            float lowestF = nodes[0].f;
            int iteratorCount = 0;

            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].f <= lowestF)
                {
                    lowestF = nodes[i].f;
                    iteratorCount = i;
                }
            }

            return iteratorCount;
        }
    }
}
