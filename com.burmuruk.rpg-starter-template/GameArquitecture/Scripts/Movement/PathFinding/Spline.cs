using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    [ExecuteInEditMode]
    public class Spline : MonoBehaviour
    {
        #region variables
        [Header("Nodes")]
        [SerializeField] GameObject prefab;
        [SerializeField] NodeData nodeData;
        [SerializeField] bool findOnStart = true;
        [Header("Spline")]
        public bool shouldDraw = true;
        [SerializeField] public CyclicType cyclicType = CyclicType.None;
        [SerializeField, ReadOnly] int nodesCount = 0;
        [SerializeField] Color lineColor = Color.yellow;

        [SerializeField] bool initialized = false;
        bool isAlive = true;

        public PatrolPath<PatrolNode> path { get; private set; }
        private bool isAdded = true;
        #endregion

        #region Unity methods
        private void Awake()
        {
            nodeData = new NodeData(nodeData);
        }

        private void Start()
        {
            if (!findOnStart) return;

            initialized = false;
            Initialize();
        }

        private void OnEnable()
        {
            if (initialized) return;

            Initialize();
        }

        private void OnDisable()
        {
            if (path == null || path.Count <= 0) return;

            var point = path.FirstNode;
            for (int i = 0; i < path.Count; i++)
            {
                point.Value.OnNodeAdded -= (a, b) => AddNode(a, b);
                point.Value.OnNodeRemoved -= (rPoint) => path.Remove(rPoint);

                point = point.Next;
            }
        }

        private void OnDestroy()
        {
            isAlive = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (nodesCount != 0) return;

            initialized = false;
            Initialize();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        private void OnDrawGizmos()
        {
            if (!isAlive) return;

            if (path != null)
                nodesCount = path.Count;
            else
                nodesCount = 0;

            if (!initialized) Initialize();

            if (path == null || path.Count <= 1)
                return;
            //List<NodeData> connections = new List<NodeData>();
            //List<int> hello;
            if (!shouldDraw) return;

            LinkedListNode<PatrolNode> node = path.FirstNode;

            for (int i = 0; i < path.Count; i++)
            {
                var cur = node.Value.Position + Vector3.up * nodeData.VerticalOffset;

                if (cyclicType == CyclicType.Circle && node.Value == path.Last)
                    Debug.DrawLine(cur, path.First.Position + Vector3.up * nodeData.VerticalOffset, lineColor);
                else if (node.Value == path.Last)
                    break;
                else
                    Debug.DrawLine(cur, node.Next.Value.Position + Vector3.up * nodeData.VerticalOffset, lineColor);

                node = node.Next;
            }
        }
        #endregion

        #region public methods
        public void Initialize()
        {
            if (initialized) return;
            
            var points = transform.GetComponentsInChildren<PatrolNode>();
            Set_NodeSettings(points);

            path = new PatrolPath<PatrolNode>(cyclicType, points);
            nodesCount = path.Count;
            initialized = true;
        }
        #endregion

        #region private methods

        private void AddNode(PatrolNode current, PatrolNode newPoint)
        {
            if (current == newPoint) return;

            bool containsNode = false;
            var enumerator = path.GetEnumerator();

            for (int i = 0; i < path.Count; i++)
            {
                if (enumerator.Current == newPoint)
                {
                    containsNode = true;
                    break;
                }

                enumerator.MoveNext();
            }

            if (!containsNode)
            {
                path.AddAfter(current, newPoint);
                newPoint.OnNodeMoved += AddNode;
            }
            else
            {
                SetNodePosition(current, newPoint);
            }

            Set_NodeSettings(newPoint);
        }

        public void SetNodePosition(PatrolNode current, PatrolNode newPoint)
        {
            RemoveNode(newPoint);

            var prev = path.Prev(current);
            var next = path.Next(current);

            if (prev == null)
                path.AddAfter(current, newPoint);

            var prevDirection = prev.Position - current.Position;
            var nextDirection = next.Position - current.Position;
            var newDirection = newPoint.Position - current.Position;

            var prevAngle = Vector3.Angle(prevDirection, newDirection);
            var nextAngle = Vector3.Angle(nextDirection, newDirection);
            int idx = GetNodeIdx(current).Value;

            if (prevAngle > nextAngle)
            {
                path.AddAfter(current, newPoint);
                newPoint.gameObject.transform.SetSiblingIndex(idx + 1);
            }
            else /*if (nextAngle > prevAngle)*/
            {
                path.AddBefore(current, newPoint);
                newPoint.gameObject.transform.SetSiblingIndex(idx);
            }
        }

        private void RemoveNode(PatrolNode node)
        {
            var enumerator = path.GetEnumerator();

            for (int i = 0; i < path.Count; i++)
            {
                enumerator.MoveNext();

                if (enumerator.Current == node)
                {
                    path.Remove(node);
                    break;
                }
            }
        }

        private int? GetNodeIdx(PatrolNode node)
        {
            var enumerator = path.GetEnumerator();

            for (int i = 0; i < path.Count; i++)
            {
                enumerator.MoveNext();

                if (enumerator.Current == node)
                {
                    return i;
                }
            }

            return null;
        }

        private void Set_NodeSettings(params PatrolNode[] points)
        {
            foreach (var point in points)
            {
                point.SetNodeData(nodeData);
                point.OnNodeAdded -= AddNode;
                point.OnNodeRemoved -= (rPoint) => path.Remove(rPoint);

                point.OnNodeAdded += AddNode;
                point.OnNodeRemoved += (rPoint) => path.Remove(rPoint);
            }
        }
        #endregion

        ~Spline()
        {
            if (path) path.Dispose();
            //Debug.Log("Bye Bye Spline chan");
        }
    }

    [Serializable]
    public class NodeData
    {
        [SerializeField] private Color nodeColor = Color.blue;
        [SerializeField] private float radious = .5f;
        [SerializeField] private bool shouldDraw = true;
        [SerializeField] private float verticalOffset = 1;

        public Color NodeColor { get => nodeColor; }
        public float Radius { get => radious; }
        public bool ShouldDraw { get => shouldDraw; }
        public float VerticalOffset { get => verticalOffset; }

        public NodeData(NodeData data)
        {
            (nodeColor, radious, shouldDraw, verticalOffset) = data;
        }

        public void Deconstruct(out Color color, out float radious, out bool shouldDraw, out float vOffset)
        {
            color = nodeColor;
            radious = this.radious;
            shouldDraw = this.shouldDraw;
            vOffset = this.verticalOffset;
        }
    }
}
