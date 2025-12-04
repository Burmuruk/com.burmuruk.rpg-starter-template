using Burmuruk.AI.PathFinding;
using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.RPGStarterTemplate.Utilities;
using Burmuruk.WorldG.Patrol;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement
{
    public enum MovementState
    {
        None,
        Moving,
        FollowingPath,
        Calculating
    }

    [RequireComponent(typeof(Rigidbody))]
    public class Movement : MonoBehaviour, IScheduledAction
    {
        [SerializeField] float m_maxVel;
        [SerializeField] float m_maxSteerForce;
        [SerializeField] float m_threshold;
        [SerializeField] float m_slowingRadious;

        Rigidbody m_rb;
        Func<BasicStats> stats;
        InventoryEquipDecorator m_inventory;
        ActionScheduler m_scheduler;
        PathFinder m_pathFinder;
        Collider col;

        bool detachRotation = false;
        public float wanderDisplacement;
        public float wanderRadious;
        public bool usePathFinding = false;
        bool m_canMove = true;
        int nodeIdxSlowingRadious;
        bool abortOnLargerPath = false;
        float maxDistance = 0;

        MovementState m_state = MovementState.None;
        Vector3 colYExtents = Vector3.zero;
        Vector3 destiny = Vector3.zero;
        Vector3 target = Vector3.zero;
        IPathNode m_pathNodeTarget;
        IPathNode curNodePosition = null;
        int curNodeIdx;

        public INodeListSupplier nodeList;
        LinkedList<IPathNode> m_curPath;
        IEnumerator<IPathNode> enumerator;

        public event Action OnFinished = delegate { };

        public Vector3 CurDirection { get; private set; }
        public Vector3 Veloctiy { get => m_rb.velocity; }
        public bool IsWorking
        {
            get
            {
                if (m_state == MovementState.Calculating || IsMoving)
                    return true;

                return false;
            }
        }
        public bool IsMoving
        {
            get
            {
                if (m_state == MovementState.Moving || m_state == MovementState.FollowingPath)
                    return true;

                return false;
            }
        }
        public bool DetachRotation
        {
            get => detachRotation;
            set
            {
                if (m_state == MovementState.None)
                    detachRotation = value;
            }
        }
        float Threshold
        {
            get
            {
                return m_threshold;
            }
        }
        bool CanMove
        {
            get
            {
                if (nodeList == null || !m_canMove) return false;

                return true;
            }
        }
        BasicStats Stats { get => stats.Invoke(); }

        public PathFinder Finder { get => m_pathFinder; }
        float SlowingRadious => Threshold + m_slowingRadious;

        #region Unity mehthods
        private void Awake()
        {
            m_rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            Move();

        }
        #endregion

        public void Initialize(InventoryEquipDecorator inventory, ActionScheduler scheduler, Func<BasicStats> stats)
        {
            this.stats = stats;
            this.m_inventory = inventory;
            m_scheduler = scheduler;
        }

        public void SetConnections(INodeListSupplier nodeList)
        {
            m_pathFinder = new PathFinder(nodeList);
            this.nodeList = nodeList;

            m_pathFinder.OnPathCalculated += SetPath;
            curNodePosition = nodeList.FindNearestNode(transform.position);
        }

        public void MoveToDirection(Vector3 direction, bool abortWhenLarger = true)
        {
            if (IsWorking || !CanMove) return;

            m_state = MovementState.Calculating;

            if (abortWhenLarger)
            {
                this.abortOnLargerPath = abortWhenLarger;
                maxDistance = direction.magnitude;
            }

            try
            {
                curNodePosition ??= nodeList.FindNearestNode(transform.position);

                m_scheduler.AddAction(this, ActionPriority.Low, () =>
                {
                    Vector3 point = transform.position + direction;
                    var nearestPoint = nodeList.FindNearestNode(point);
                    bool result = nodeList.ValidatePosition(point, nearestPoint);

                    if (result)
                    {
                        m_state = MovementState.Moving;
                        target = point;
                        m_pathNodeTarget = nearestPoint;
                    }
                });
            }
            catch (NullReferenceException)
            {
                FinishAction();
            }
        }

        public void MoveTo(Vector3 point, bool abortWhenLarger = false)
        {
            if (IsWorking || !CanMove) return;

            m_state = MovementState.Calculating;

            if (abortWhenLarger)
            {
                this.abortOnLargerPath = abortWhenLarger;
                maxDistance = Vector3.Distance(point, transform.position);
            }

            try
            {
                curNodePosition = nodeList.FindNearestNode(transform.position);

                m_scheduler.AddAction(this, ActionPriority.Low, () =>
                    m_pathFinder.Find_BestRoute<AStar>((curNodePosition, point)));
            }
            catch (NullReferenceException)
            {
                FinishAction();
            }
        }

        public void FollowWithDistance(Movement target, float gap, params Character[] fellows)
        {
            if (IsWorking || !CanMove) return;

            m_state = MovementState.Calculating;
            Vector3 point = SteeringBehaviours.GetFollowPosition(target, this, gap, fellows);

            try
            {
                curNodePosition ??= nodeList.FindNearestNode(transform.position);

                m_pathFinder.Find_BestRoute<AStar>((curNodePosition, point));
            }
            catch (NullReferenceException)
            {
                m_state = MovementState.None;
                FinishAction();
            }
        }

        public bool ChangePositionCloseToNode(IPathNode node, Vector3 point)
        {
            if (IsWorking) m_scheduler.CancelAll();

            m_state = MovementState.Calculating;

            var nextNode = nodeList.FindNearestNodeAround(node, point);

            if (nextNode == null)
            {
                m_state = MovementState.None;
                return false;
            }

            curNodePosition = nextNode;
            transform.position = nextNode.Position + Vector3.up * col.bounds.extents.y;

            m_state = MovementState.None;
            return true;
        }

        public void ChangePositionTo(Vector3 position)
        {
            if (IsWorking) m_scheduler.CancelAll();
            m_state = MovementState.Calculating;

            var nextNode = nodeList.FindNearestNode(position);

            if (nextNode == null)
            {
                m_state = MovementState.None;
                return;
            }

            curNodePosition = nextNode;
            transform.position = nextNode.Position + Vector3.up * col.bounds.extents.y;

            m_state = MovementState.None;
            return;
        }

        public float GetSpeed()
        {
            return Stats.speed;
        }

        public float getMaxVel()
        {
            return m_maxVel = Stats.speed;
        }

        public float getMaxSteerForce()
        {
            return m_maxSteerForce;
        }

        public void StartAction()
        {
            if (!m_scheduler.Initilized) return;
        }

        public void PauseAction()
        {
            m_canMove = false;
            m_rb.velocity = Vector3.zero;
        }

        public void ContinueAction()
        {
            m_canMove = true;
        }

        public void CancelAll() => m_scheduler.CancelAll();

        public void CancelAction()
        {
            switch (m_state)
            {
                case MovementState.FollowingPath:
                case MovementState.Moving:
                    FinishAction();
                    break;
                case MovementState.Calculating:
                    m_state = MovementState.None;
                    break;
                case MovementState.None:
                default:
                    break;
            }
        }

        public void StopAction()
        {
            if (m_state == MovementState.FollowingPath)
                FinishAction();
        }

        public void FinishAction()
        {
            m_rb.velocity = new Vector3(0, m_rb.velocity.y, 0);
            m_curPath = null;
            enumerator = null;
            m_pathNodeTarget = null;
            abortOnLargerPath = false;
            detachRotation = false;

            OnFinished?.Invoke();

            m_state = MovementState.None;
            m_scheduler.Finished(this);
        }

        public void Flee(Vector3 target)
        {
            if (!m_canMove && !IsMoving) return;

            m_state = MovementState.Calculating;

            Vector3 newPosition = SteeringBehaviours.Flee(this, target);

            try
            {
                curNodePosition ??= nodeList.FindNearestNode(transform.position);

                m_scheduler.AddAction(this, ActionPriority.Low, () =>
                    m_pathFinder.Find_BestRoute<AStar>((curNodePosition, newPosition)));
            }
            catch (NullReferenceException)
            {
                m_state = MovementState.None;
                FinishAction();
            }
        }

        public void Pursue()
        {

        }

        public void UpdatePosition()
        {
            curNodePosition = nodeList.FindNearestNode(transform.position);
        }

        private void Move()
        {
            if (!m_canMove || !IsMoving) return;

            float adaptiveThreshold = Mathf.Max(Threshold, (nodeList?.NodeDistance ?? 1f) * 0.3f);

            m_rb.velocity = SteeringBehaviours.Seek2D(this, target);
            var pos1 = transform.position + colYExtents;
            var pos2 = target;
            float d = Vector3.Distance(pos1, pos2);

            if (d <= adaptiveThreshold)
            {
                if (m_state == MovementState.FollowingPath)
                {
                    if (!GetNextNode())
                    {
                        FinishAction();
                        return;
                    }
                    else
                        curNodePosition = m_pathNodeTarget;
                }
                else if (m_state == MovementState.Moving)
                {
                    curNodePosition = m_pathNodeTarget;
                    FinishAction();
                    return;
                }
            }
            else if (d <= SlowingRadious &&
                (m_state == MovementState.Moving ||
                (m_state == MovementState.FollowingPath && curNodeIdx >= nodeIdxSlowingRadious)))
            {
                if (detachRotation)
                {
                    m_rb.velocity = Vector3.ProjectOnPlane(SteeringBehaviours.Arrival(this, destiny, SlowingRadious, adaptiveThreshold), new Vector3(0, 1, 0));
                }
                else
                {
                    m_rb.velocity = SteeringBehaviours.Arrival(this, destiny, SlowingRadious, adaptiveThreshold);
                }

                CurDirection = m_rb.velocity.sqrMagnitude > 0.0001f ? m_rb.velocity.normalized : CurDirection;
            }
            else
            {
                CurDirection = m_rb.velocity.sqrMagnitude > 0.0001f ? m_rb.velocity.normalized : CurDirection;
            }

            if (!detachRotation)
            {
                SteeringBehaviours.LookAt(transform, m_rb.velocity, m_maxSteerForce);
            }
        }

        private bool GetNextNode()
        {
            if (m_curPath != null)
            {
                if (enumerator == null)
                {
                    enumerator = m_curPath.GetEnumerator();
                    curNodeIdx = 0;
                }

                if (enumerator.MoveNext())
                {
                    // --- IMPORTANT: use node position directly (no intermediate moving target) ---
                    target = enumerator.Current.Position;

                    m_pathNodeTarget = enumerator.Current;
                    curNodeIdx++;

                    return true;
                }
            }

            return false;
        }

        private void SetPath()
        {
            if (m_pathFinder.BestRoute == null || m_pathFinder.BestRoute.Count == 0)
            { Cancel(); return; }

            m_curPath = m_pathFinder.BestRoute;

            if (m_pathFinder != null && !m_pathFinder.ValidatePath(m_curPath))
            {
                FinishAction();
                return;
            }

            if (!GetNextNode()) { Cancel(); return; }
            if (abortOnLargerPath && m_curPath.Count * .5f > maxDistance + .5f * 5)
            {
                Cancel();
                return;
            }

            var minNodes = Mathf.Max((int)Mathf.Round(SlowingRadious / nodeList.NodeDistance), 0);
            nodeIdxSlowingRadious = m_curPath.Count - minNodes;

            colYExtents = Vector3.down * col.bounds.extents.y;
            destiny = m_curPath.Last.Value.Position;

            if (m_state == MovementState.None) { Cancel(); return; }

            m_state = MovementState.FollowingPath;

            DrawCurrentPath();

            void DrawCurrentPath()
            {
                IPathNode lastNode = null;
                foreach (var node in m_pathFinder.BestRoute)
                {
                    if (lastNode != null)
                        UnityEngine.Debug.DrawLine(lastNode.Position, node.Position, Color.black, 5);

                    lastNode = node;
                }
            }

            void Cancel()
            {
                FinishAction();
                return;
            }
        }
    }
}
