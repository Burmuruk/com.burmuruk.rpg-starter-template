using Burmuruk.WorldG.Patrol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Burmuruk.AI.PathFinding
{
    public enum FinderType
    {
        None,
        Dijkstra,
        AStar
    }

    public class PathFinder
    {
        #region Variables
        INodeListSupplier nodesList;
        IPathFinder algorithem;
        public List<LinkedList<IPathNode>> paths;
        public int curPath = -1;

        //states
        (IPathNode start, IPathNode end)[] curNodes = null;
        public bool isCalculating = false;
        public event Action OnPathCalculated;

        #endregion

        public LinkedList<IPathNode> BestRoute
        {
            get
            {
                if (isCalculating || paths == null || paths.Count <= 0)
                    return null;

                return paths[0];
            }
        }

        public PathFinder() { }

        public PathFinder(INodeListSupplier nodesList)
        {
            this.nodesList = nodesList;
            paths = new List<LinkedList<IPathNode>>();
        }

        public void SetNodeList(INodeListSupplier nodesList)
        {
            this.nodesList = nodesList;
            paths = new List<LinkedList<IPathNode>>();
        }

        public LinkedList<IPathNode> Get_Route(IPathNode start, IPathNode end, out float distance)
        {
            if (algorithem == null) throw new InvalidOperationException("Algorithm is null");
            return algorithem.Get_Route(start, end, out distance);
        }

        /// <summary>
        /// Simplified: runs a single pathfinding call asynchronously and stores the result in paths[0]
        /// </summary>
        public void Find_BestRoute<T>(params (IPathNode start, Vector3 end)[] pairs) where T : IPathFinder, new()
        {
            if (isCalculating) return;
            if (nodesList == null || !nodesList.Initilized || pairs == null) return;

            if (algorithem == null) algorithem = new T();

            isCalculating = true;
            curNodes = new (IPathNode, IPathNode)[pairs.Length];

            // resolve end nodes
            for (int i = 0; i < pairs.Length; i++)
            {
                curNodes[i].start = pairs[i].start;
                curNodes[i].end = nodesList.FindNearestNode(pairs[i].end);
            }

            Task<(LinkedList<IPathNode> path, float dist)> task = Task.Run(() =>
            {
                try
                {
                    // compute only the first pair (most callers use single pair)
                    var p = curNodes[0];
                    float d;
                    var route = algorithem.Get_Route(p.start, p.end, out d);
                    return (route, d);
                }
                catch (Exception)
                {
                    return (null, 0f);
                }
            });

            var awaiter = task.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                try
                {
                    var result = awaiter.GetResult();

                    paths = new List<LinkedList<IPathNode>>();
                    if (result.path != null)
                    {
                        paths.Add(result.path);
                        curPath = 0;
                    }
                    else
                    {
                        curPath = -1;
                    }
                }
                catch (Exception)
                {
                    paths = new List<LinkedList<IPathNode>>();
                    curPath = -1;
                }
                finally
                {
                    isCalculating = false;
                    OnPathCalculated?.Invoke();
                }
            });
        }

        /// <summary>
        /// Quick validation of consecutive node-to-node connections using Physics.Raycast.
        /// Must be called from main thread. Returns true if the path has no dynamic obstruction.
        /// </summary>
        public bool ValidatePath(LinkedList<IPathNode> path, LayerMask? mask = null)
        {
            if (path == null || path.Count < 2) return true;
            //LayerMask useMask = mask ?? 1 << 9;

            IPathNode prev = null;
            foreach (var node in path)
            {
                if (prev != null)
                {
                    Vector3 a = prev.Position + Vector3.up * 0.4f;
                    Vector3 b = node.Position + Vector3.up * 0.4f;
                    float dist = Vector3.Distance(a, b);
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
                    if (Physics.Raycast(a, (b - a).normalized, out RaycastHit hit, dist, 1 <<9))
                    {
                        if (!hit.collider.isTrigger)
                            return false;
                    }
#endif
                }
                prev = node;
            }

            return true;
        }
    }

    public struct RequiredLists
    {
        public LinkedList<IPathNode> unCheckedNodes;
        public Dictionary<IPathNode, NodeData> data;
        public LinkedList<IPathNode> shortestPath;
        public IPathNode start;
        public IPathNode end;
        public bool endReached;

        public void Initialize()
        {
            unCheckedNodes = new LinkedList<IPathNode>();
            data = new Dictionary<IPathNode, NodeData>();
            shortestPath = new LinkedList<IPathNode>();
            endReached = false;
        }

        public void Initialize(IPathNode start, IPathNode end)
        {
            unCheckedNodes = new LinkedList<IPathNode>();
            data = new Dictionary<IPathNode, NodeData>();
            shortestPath = new LinkedList<IPathNode>();
            this.start = start;
            this.end = end;
            endReached = false;
        }
    }

    public enum NodeState
    {
        Unchecked,
        Waiting,
        Checked,
    }

    public struct NodeData
    {
        public float weight;
        public NodeState state;
        public IPathNode prev;

        public NodeData(IPathNode prev)
        {
            weight = 0;
            state = NodeState.Unchecked;
            this.prev = prev;
        }

        public NodeData(NodeState state)
        {
            weight = 0;
            this.state = state;
            prev = null;
        }

        public void Update_Weight(float value, IPathNode prev) =>
            (this.weight, this.prev) = (value, prev);

        public void Set_State(NodeState value = NodeState.Checked) =>
            this.state = value;

        public void Deconstructor(out IPathNode node) =>
            node = this.prev;
    }
}
