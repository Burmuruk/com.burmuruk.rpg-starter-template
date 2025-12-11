using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    public enum ConnectionType
    {
        None,
        BIDIMENSIONAL,
        A_TO_B,
        B_TO_A
    }

    [System.Serializable]
    public struct NodeConnection
    {
        public IPathNode node;
        public ConnectionType connectionType;
        public float Magnitude { get; private set; }

        public NodeConnection(ref IPathNode current, ref IPathNode node)
        {
            this.node = node;
            this.connectionType = ConnectionType.None;
            this.Magnitude = 0;
            
            Magnitude = DistanceBewtweenNodes(current, node);
        }

        public NodeConnection(ref IPathNode current, ref IPathNode node, float magnitude, ConnectionType type = ConnectionType.None) : this(ref current, ref node)
        {
            this.connectionType = type;
            Magnitude = magnitude;
        }

        public NodeConnection(IPathNode node, ConnectionType type, float magnitude)
        {
            this.node = node;
            this.connectionType = type;
            Magnitude = magnitude;
        }

        public void Deconstruct(out IPathNode node,  out ConnectionType connection, out float magnitud)
        {
            node = this.node;
            connection = this.connectionType;
            magnitud = Magnitude;
        }

        private float DistanceBewtweenNodes(IPathNode a, IPathNode b)
        {
            return Vector3.Distance(a.Position, b.Position);
        }
    }

    [ExecuteInEditMode]
    public class PatrolNode : MonoBehaviour, IPathNode, ISplineNode
    {
        [SerializeField]
        public List<NodeConnection> nodeConnections = new List<NodeConnection>();
        [SerializeField] bool updateData = false;

        public NodeData nodeData = null;
        [HideInInspector] public uint idx = 0;
        public static CopyData copyData;

        private bool isSelected = false;
        private PatrolController patrol;
        private float selectionTime = 2f;
        private Task selectionTask = null;
        private CancellationTokenSource cancellationToken;
        private PatrolNode lastNode = null;

        public event Action<PatrolNode, PatrolNode> OnNodeAdded;
        public event Action<PatrolNode> OnNodeRemoved;
        public event Action<PatrolNode, PatrolNode> OnNodeMoved;

        public uint ID => idx;
        public Transform Transform { get => transform; }
        public List<NodeConnection> NodeConnections { get => nodeConnections; }
        public NodeData NodeData => nodeData;
        public bool IsSelected => isSelected;
        public Vector3 Position { get => transform.position; }
        public Action OnStart { get; set; }
        public PatrolController PatrolController { get => patrol; set => patrol = value; }

        public bool IsEnabled { get; private set; } = true;

        #region Unity methods

        private void OnEnable()
        {
            if (!copyData.point) return;

            copyData.point.OnNodeAdded?.Invoke(copyData.point, this);
            lastNode = copyData.point;
        }

        private void OnDisable()
        {
            OnNodeRemoved?.Invoke(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (lastNode != null && selectionTask != null)
            {
                OnNodeMoved?.Invoke(lastNode, this);
            }

            Select();

            foreach (var item in nodeConnections)
            {
                if (item.connectionType == ConnectionType.BIDIMENSIONAL)
                    Debug.DrawRay(transform.position, item.node.Position - transform.position, Color.blue);
            }

            if (NodeData != null && nodeData.ShouldDraw)
            {
                Gizmos.color = nodeData.NodeColor;
                Gizmos.DrawSphere(transform.localPosition, (float)nodeData.Radius); 
            }
        }
        #endregion

        public void ClearConnections()
        {
            nodeConnections.Clear();
        }

        public float GetDistanceBetweenNodes(in NodeConnection connection)
        {
            Vector3 value = connection.node.Position - transform.position;
            return value.magnitude;
        }

        public void SetIndex(uint idx) => this.idx = idx;

        public void SetNodeData(NodeData nodeData)
        {
            this.nodeData = new NodeData(nodeData);
        }

        private void Select()
        {
            if (!gameObject.activeSelf) return;

            if (copyData.point != null)
                copyData.point.Deselect();

            copyData = new CopyData(true, this);
            isSelected = true;

            if (lastNode == null) return;

            if (selectionTask != null)
            {
                return;
                //cancellationToken.Cancel();
            }

            StartTimer();
        }

        public void Deselect()
        {
            if (copyData.point != this) return;

            copyData.point = null;
            copyData.wasSelected = false;
            isSelected = false;
        }

        private void StartTimer()
        {
            SynchronizationContext context = SynchronizationContext.Current;
            cancellationToken ??= new CancellationTokenSource();
            var token = cancellationToken.Token;

            selectionTask = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(selectionTime));

                if (token.IsCancellationRequested)
                {
                    cancellationToken = null;
                    return;
                }

                context.Post(_ => 
                {
                    OnNodeMoved = null;
                    lastNode = null;
                    selectionTask = null;
                }, null);
            }, token);
        }

        public void Enable(bool shouldEnable = true)
        {
            IsEnabled = shouldEnable;
        }
    }
}
