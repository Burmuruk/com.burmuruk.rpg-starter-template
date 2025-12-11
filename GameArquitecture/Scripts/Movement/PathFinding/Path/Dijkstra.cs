using Burmuruk.WorldG.Patrol;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.AI.PathFinding
{
    public class Dijkstra : IPathFinder
    {
        #region Variables
        IPathNode start;
        IPathNode end;
        bool pathCalculated = false;
        bool endReached = false;

        public LinkedList<IPathNode> shortestPath;

        
        #endregion

        #region Properties
        public bool Calculated { get => pathCalculated; }
        public LinkedList<IPathNode> ShortestPath
        {
            get
            {
                if (pathCalculated)
                    return shortestPath;
                else
                    return null;
            }
        }
        #endregion

        public Dijkstra()
        {

        }

        public Dijkstra(IPathNode start, IPathNode end)
        {
            this.start = start;
            this.end = end;
        }

        public LinkedList<IPathNode> Get_Route(IPathNode start, IPathNode end, out float distance)
        {
            distance = 0.0f;
            RequiredLists lists = new RequiredLists();
            lists.Initialize(start, end);
            
            Start_Algorithm(ref lists, out distance);

            return lists.shortestPath;
        }

        public LinkedList<IPathNode> Find_Route(IPathNode start, IPathNode end, out float distance)
        {
            distance = 0;
            RequiredLists lists = new RequiredLists();
            lists.Initialize();
            this.start = start;
            this.end = end;

            Start_Algorithm(ref lists);
            Get_ShortestPath(ref lists, out distance);
            Clear();

            return lists.shortestPath;
        }

        public void Start_Algorithm(ref RequiredLists lists, out float distance)
        {
            distance = 0;
            Clear();
            
            Start_Algorithm(ref lists);

            Get_ShortestPath(ref lists, out distance);
            Debug.Log($"Shortest {lists.shortestPath.First.Value.ID} to {lists.shortestPath.Last.Value.ID}");
        }

        public void Start_Algorithm(out float distance)
        {
            distance = 0;
            Clear();
            RequiredLists lists = new RequiredLists();

            lists.Initialize();

            Start_Algorithm(ref lists);
            Get_ShortestPath(ref lists, out distance);

            shortestPath = lists.shortestPath;
            pathCalculated = true;
        }

        public void Start_Algorithm(ref RequiredLists lists)
        {
            ref var unCheckedNodes = ref lists.unCheckedNodes;
            ref var data = ref lists.data;

            data.Add(lists.start, new NodeData(null));
            IPathNode cur = lists.start;
            float curWeight = 0;
            bool finished = false;
            lists.endReached = false;

            do
            {
                (IPathNode node, float weight) minWeight = (null, float.MaxValue);

                var curData = data[cur];
                ChangeState(NodeState.Checked, cur, ref data);

                if (cur.ID == lists.end.ID)
                {
                    lists.endReached = true;
                    Update_CurrentNode(ref cur, unCheckedNodes.Last.Value, ref curWeight, ref lists);
                }

                foreach (var conection in cur.NodeConnections)
                {
                    //if (conection.connectionType != ConnectionType.BIDIMENSIONAL) continue;
                    float weight = conection.Magnitude + curWeight;
                    var next = conection.node;

                    if (!data.ContainsKey(next) ||
                                    data[conection.node].state == NodeState.Unchecked)
                    {
                        StartNode(next, ref lists);

                        UpdateWeight(weight, conection.node, cur, ref data);
                    }
                    else if (weight < data[conection.node].weight)
                    {
                        UpdateWeight(weight, conection.node, cur, ref data);

                        unCheckedNodes.AddLast(next);
                        ChangeState(NodeState.Waiting, conection.node, ref data);
                    }

                    if (data[conection.node].state == NodeState.Checked) continue;

                    if (weight < minWeight.weight)
                        minWeight = (next, weight);
                }

                if (minWeight.weight != float.MaxValue)
                {
                    Update_CurrentNode(ref cur, minWeight.node, ref curWeight, ref lists);
                }
                else if (unCheckedNodes.Count > 0)
                {
                    Update_CurrentNode(ref cur, unCheckedNodes.Last.Value, ref curWeight, ref lists);
                }
                else
                    finished = true;

            } while (!finished);
        }

        public void Clear()
        {
            pathCalculated = false;
            endReached = false;
            shortestPath = null;
        }

        void Update_CurrentNode(ref IPathNode cur, in IPathNode next, ref float curWeight, ref RequiredLists lists)
        {
            cur = next;
            curWeight = lists.data[cur].weight;

            lists.unCheckedNodes.Remove(cur);
        }

        private void UpdateWeight(float value, IPathNode cur, IPathNode prev, ref Dictionary<IPathNode, NodeData> data)
        {
            var node = data[cur];
            node.Update_Weight(value, prev);
            data[cur] = node;
        }

        private void ChangeState(NodeState state, IPathNode cur, ref Dictionary<IPathNode, NodeData> data)
        {
            var node = data[cur];
            node.Set_State(state);

            data[cur] = node;
        }

        private void Get_ShortestPath(ref RequiredLists lists, out float distance)
        {
            distance = 0f;
            if (!lists.endReached) return;

            ref var data = ref lists.data;
            ref var shortestPath = ref lists.shortestPath;
            ref var end = ref lists.end;
            ref var start = ref lists.start;

            distance = data[end].weight;
            shortestPath = new LinkedList<IPathNode>();
            IPathNode cur = end;

            while (data[cur].prev.ID != start.ID)
            {
                shortestPath.AddFirst(cur);

                IPathNode next = data[cur].prev;
                if (data[next].prev == null)
                {
                    Debug.LogError($"isNull from {cur.ID} to {next.ID}");
                    shortestPath = null;
                    return;
                }

                cur = data[cur].prev;
            }

            shortestPath.AddFirst(data[cur].prev);
        }

        private void StartNode(IPathNode node, ref RequiredLists lists)
        {
            lists.data.Add(node, new NodeData(NodeState.Waiting));

            lists.unCheckedNodes.AddLast(node);
        }
    }

    //public unsafe class MyLinkedList<T> where T : unmanaged
    //{
    //    public MyLinkedNode<T>* first;
    //    public MyLinkedNode<T>* last;

    //    public MyLinkedList(T* first)
    //    {
    //        MyLinkedNode<T> Hi = new MyLinkedNode<T>(first);
    //        this.first = &Hi;
    //        this.last = &Hi;
    //    }

    //    public void AddFirst(T* node2)
    //    {
    //        first->prev = node2;
    //    }

    //    public void AddLast(T* node2)
    //    {
    //        last->next = node2;
    //    }
    //}

    //public unsafe struct MyLinkedNode<T> where T : unmanaged
    //{
    //    public T* prev;
    //    public T* next;
    //    public T* node2;

    //    public MyLinkedNode(T* node2)
    //    {
    //        prev = null;
    //        next = null;
    //        this.node2 = node2;
    //    }
    //}
}
