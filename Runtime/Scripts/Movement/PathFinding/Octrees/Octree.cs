using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement.PathFindig
{
    public class Octree
    {
        public OctreeNode rootNode;
        public List<OctreeNode> emptyLeaves = new();
        public Graph navigationGraph;

        public Octree(GameObject[] worldObjects, float minNodeSize, Graph navGraph, int maxDepth)
        {
            Bounds bounds = new Bounds();
            navigationGraph = navGraph;
            OctreeNode.max_depth = maxDepth;

            foreach (var go in worldObjects)
            {
                bounds.Encapsulate(go.GetComponent<Collider>().bounds);
            }

            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Vector3 sizeVector = Vector3.one * maxSize * 1.1f;
            bounds.SetMinMax(bounds.center - sizeVector, bounds.center + sizeVector);

            rootNode = new OctreeNode(bounds, minNodeSize, null, 0);
            AddObjects(worldObjects);
            GetEmptyLeaves(rootNode);
            ConnectLeafNodeNeighbours();
        }

        public void AddObjects(GameObject[] worldObjects)
        {
            foreach (var go in worldObjects)
            {
                rootNode.AddObject(go);
            }
        }

        public void GetEmptyLeaves(OctreeNode node)
        {
            if (node.children == null && node.containedObjects.Count == 0)
            {
                emptyLeaves.Add(node);
                navigationGraph.AddNode(node);
            }
            else if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child != null)
                        GetEmptyLeaves(child);
                }
            }
        }

        public bool IsPointAvailable(Vector3 point)
        {
            foreach (var leaf in emptyLeaves)
            {
                if (leaf.nodeBounds.Contains(point))
                    return true;
            }

            return false;
        }

        void ConnectLeafNodeNeighbours()
        {
            var nodeMap = new Dictionary<Vector3Int, OctreeNode>();

            foreach (var node in emptyLeaves)
            {
                Vector3Int gridPos = Vector3Int.RoundToInt(node.nodeBounds.center / node.nodeBounds.size.y);
                nodeMap[gridPos] = node;
            }

            Vector3Int[] directions =
            {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

            foreach (var node in emptyLeaves)
            {
                Vector3Int current = Vector3Int.RoundToInt(node.nodeBounds.center / node.nodeBounds.size.y);

                foreach (var dir in directions)
                {
                    if (nodeMap.TryGetValue(current + dir, out var neighbor))
                    {
                        navigationGraph.AddEdge(node, neighbor);
                    }
                }
            }
        }
    } 
}