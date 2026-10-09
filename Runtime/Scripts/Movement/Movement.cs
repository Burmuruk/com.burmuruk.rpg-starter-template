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
        [SerializeField] float m_maxVel = 7;
        [SerializeField] float m_maxSteerForce = 5;
        [SerializeField] float m_threshold = 1;
        [SerializeField] float m_slowingRadious = 1.5f;
        [SerializeField] bool smoothPathCorners = true;

        protected Rigidbody _rb;
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
        Vector3 requestedDestination;
        float? arrivalDistance;
        public Transform FacingTarget { get; set; }
        Vector3 target = Vector3.zero;
        IPathNode m_pathNodeTarget;
        IPathNode curNodePosition = null;
        IPathNode navigationStart;
        Vector3 recoveryPosition;
        int curNodeIdx;

        public INodeListSupplier nodeList;
        LinkedList<IPathNode> m_curPath;
        Vector3[] pathTargets;
        IEnumerator<IPathNode> enumerator;

        public event Action OnFinished = delegate { };

        public Vector3 CurDirection { get; private set; }
        public Vector3 Veloctiy { get => _rb.velocity; }
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
            _rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            recoveryPosition = transform.position;
        }

        protected virtual void FixedUpdate()
        {
            Move();
            if (m_canMove && FacingTarget != null)
            {
                Vector3 direction = Vector3.ProjectOnPlane(FacingTarget.position - transform.position, Vector3.up);
                if (direction.sqrMagnitude > 0.0001f)
                    _rb.MoveRotation(Quaternion.LookRotation(direction));
            }
        }
        #endregion

        public virtual void Initialize(InventoryEquipDecorator inventory, ActionScheduler scheduler, Func<BasicStats> stats)
        {
            this.stats = stats;
            this.m_inventory = inventory;
            m_scheduler = scheduler;
        }

        public void SetConnections(INodeListSupplier nodeList)
        {
            ResetRoute();

            if (m_pathFinder != null) 
                m_pathFinder.OnPathCalculated -= SetPath;

            m_pathFinder = new PathFinder(nodeList);
            this.nodeList = nodeList;

            m_pathFinder.OnPathCalculated += SetPath;
            curNodePosition = nodeList.FindNearestNode(transform.position);
            navigationStart = null;
            recoveryPosition = transform.position;
            RememberNavigationStart();
        }

        public void MoveToDirection(Vector3 direction, bool abortWhenLarger = true)
        {
            if (IsWorking || !CanMove) return;

            RememberNavigationStart();

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

        public void MoveTo(Vector3 point, bool abortWhenLarger = false, float? stoppingDistance = null)
        {
            if (IsWorking || !CanMove) return;

            RememberNavigationStart();

            m_state = MovementState.Calculating;
            requestedDestination = point;
            arrivalDistance = stoppingDistance.HasValue ? Mathf.Max(0.01f, stoppingDistance.Value) : (float?)null;

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

            RememberNavigationStart();

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
            if (nodeList == null || node == null) return false;

            var nextNode = nodeList.FindNearestNodeAround(node, point);

            if (nextNode == null)
            {
                return false;
            }

            ResetRoute();
            curNodePosition = nextNode;
            navigationStart = nextNode;
            recoveryPosition = nextNode.Position + Vector3.up * col.bounds.extents.y;
            _rb.position = nextNode.Position + Vector3.up * col.bounds.extents.y;
            _rb.velocity = Vector3.zero;

            m_state = MovementState.None;
            return true;
        }

        public void ChangePositionTo(Vector3 position)
        {
            if (nodeList == null) return;

            var nextNode = nodeList.FindNearestNode(position);

            if (nextNode == null)
            {
                return;
            }

            ResetRoute();
            curNodePosition = nextNode;
            navigationStart = nextNode;
            recoveryPosition = nextNode.Position + Vector3.up * col.bounds.extents.y;
            _rb.position = nextNode.Position + Vector3.up * col.bounds.extents.y;
            _rb.velocity = Vector3.zero;

            m_state = MovementState.None;
            return;
        }

        private void RememberNavigationStart()
        {
            if (nodeList == null || col == null) return;
            if (_rb != null && Mathf.Abs(_rb.velocity.y) > 0.5f) return;

            Vector3 feet = new Vector3(col.bounds.center.x, col.bounds.min.y, col.bounds.center.z);
            var node = nodeList.FindNearestNode(feet);
            // Keep the previous safe floor when a movement request arrives during a fall.
            if (node != null && node.IsEnabled && Mathf.Abs(feet.y - node.Position.y) <= 0.5f &&
                Vector3.ProjectOnPlane(feet - node.Position, Vector3.up).sqrMagnitude <=
                Mathf.Pow(Mathf.Max(nodeList.NodeDistance, 0.5f), 2))
            {
                navigationStart = node;
                recoveryPosition = node.Position + Vector3.up * (transform.position.y - col.bounds.min.y + 0.05f);
            }
        }

        public void ReturnToNavigationStart()
        {
            ResetRoute();
            curNodePosition = navigationStart;
            _rb.position = recoveryPosition;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        public float GetSpeed()
        {
            return Stats.speed;
        }

        public void ResetRoute()
        {
            m_pathFinder?.CancelPendingRequest();
            m_scheduler?.Discard(this);
            m_curPath = null;
            pathTargets = null;
            enumerator?.Dispose();
            enumerator = null;
            m_pathNodeTarget = null;
            curNodePosition = null;
            curNodeIdx = 0;
            arrivalDistance = null;
            abortOnLargerPath = false;
            m_state = MovementState.None;
            target = destiny = requestedDestination = transform.position;
            CurDirection = Vector3.zero;

            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (col == null) col = GetComponent<Collider>();
            if (_rb != null) ApplyPlanarVelocity(Vector3.zero);
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
            ApplyPlanarVelocity(Vector3.zero);
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
                    ResetRoute();
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
            ApplyPlanarVelocity(Vector3.zero);
            m_curPath = null;
            pathTargets = null;
            enumerator = null;
            m_pathNodeTarget = null;
            abortOnLargerPath = false;
            detachRotation = false;
            arrivalDistance = null;

            m_state = MovementState.None;
            m_scheduler.Finished(this);
            OnFinished?.Invoke();
        }

        public void Flee(Vector3 target)
        {
            if (!m_canMove && !IsMoving) return;

            RememberNavigationStart();

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
            if (m_state == MovementState.Moving && arrivalDistance.HasValue)
                adaptiveThreshold = arrivalDistance.Value;

            ApplyPlanarVelocity(SteeringBehaviours.Seek2D(this, target));
            var pos1 = transform.position + colYExtents;
            var pos2 = target;
            float d = Vector3.Distance(pos1, pos2);

            if (d <= adaptiveThreshold)
            {
                if (m_state == MovementState.FollowingPath)
                {
                    if (!GetNextNode())
                    {
                        if (arrivalDistance.HasValue)
                        {
                            target = requestedDestination + colYExtents;
                            destiny = target;
                            m_state = MovementState.Moving;
                        }
                        else
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
                ApplyPlanarVelocity(SteeringBehaviours.Arrival(this, destiny, SlowingRadious, adaptiveThreshold));
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.velocity, Vector3.up);
            CurDirection = planarVelocity.sqrMagnitude > 0.0001f ? planarVelocity.normalized : CurDirection;

            if (!detachRotation && FacingTarget == null)
            {
                if (planarVelocity.sqrMagnitude > 0.01f)
                {
                    Quaternion rotation = Quaternion.LookRotation(planarVelocity.normalized);
                    _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, rotation, Time.fixedDeltaTime * m_maxSteerForce));
                }
            }
        }

        private void ApplyPlanarVelocity(Vector3 velocity)
        {
            velocity.y = _rb.velocity.y;
            _rb.velocity = velocity;
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
                    target = pathTargets != null ? pathTargets[curNodeIdx] : enumerator.Current.Position;

                    m_pathNodeTarget = enumerator.Current;
                    curNodeIdx++;

                    return true;
                }
            }

            return false;
        }

        private void SetPath()
        {
            if (m_state != MovementState.Calculating) return;
            if (m_pathFinder.BestRoute == null || m_pathFinder.BestRoute.Count == 0)
            { Cancel(); return; }

            m_curPath = m_pathFinder.BestRoute;

            if (m_pathFinder != null && !m_pathFinder.ValidatePath(m_curPath))
            {
                FinishAction();
                return;
            }

            pathTargets = CreatePathTargets();
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

        private Vector3[] CreatePathTargets()
        {
            if (!smoothPathCorners || m_curPath.Count < 3) return null;

            var points = new Vector3[m_curPath.Count];
            var node = m_curPath.First;

            for (int i = 0; node != null; i++, node = node.Next)
            {
                points[i] = node.Value.Position;
                
                if (node.Previous != null && node.Next != null &&
                    Mathf.Abs(node.Previous.Value.Position.y - node.Value.Position.y) < 0.01f &&
                    Mathf.Abs(node.Next.Value.Position.y - node.Value.Position.y) < 0.01f)
                {
                    points[i] = Vector3.Lerp(node.Previous.Value.Position, node.Value.Position, 0.5f);
                }
            }

            var bounds = col.bounds;
            float radius = Mathf.Max(0.01f, Mathf.Min(bounds.extents.x, bounds.extents.z));
            float top = Mathf.Max(radius, bounds.size.y - radius);

            for (int i = 1; i < points.Length; i++)
            {
                Vector3 delta = points[i] - points[i - 1];
                Vector3 bottomPoint = points[i - 1] + Vector3.up * (radius + 0.05f);
                Vector3 topPoint = points[i - 1] + Vector3.up * (top + 0.05f);

                if (Physics.CheckCapsule(bottomPoint, topPoint, radius, 1 << 9, QueryTriggerInteraction.Ignore) ||
                    (delta.sqrMagnitude > 0.0001f && Physics.CapsuleCast(bottomPoint, topPoint, radius,
                        delta.normalized, delta.magnitude, 1 << 9, QueryTriggerInteraction.Ignore)))
                    return null;
            }

            return points;
        }
    }
}
