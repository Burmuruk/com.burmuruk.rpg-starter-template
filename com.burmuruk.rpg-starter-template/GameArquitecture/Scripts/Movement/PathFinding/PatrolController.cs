using Burmuruk.AI.PathFinding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Burmuruk.RPGStarterTemplate.Movement;

namespace Burmuruk.WorldG.Patrol
{
    public class PatrolController : MonoBehaviour
    {
        #region Variables
        [SerializeField] Spline splinePrefab;
        [SerializeField] CyclicType cyclicType = CyclicType.None;
        //[SerializeField] PathFinder<AStar> pathFinder;
        INodeListSupplier nodeList;
        [Space(20)]
        [SerializeField] bool shouldRepeat = false;
        [SerializeField] List<Task> tasks = new List<Task>();
        Movement mover;
        PathFinder finder;
        Spline spline;
        PatrolState state;

        public event Action OnFinished;
        public event Action OnPatrolFinished;

        enum PatrolState
        {
            None,
            Running,
            Canceling,
            Repeating
        }

        public enum TaskType
        {
            None,
            Turn,
            Move,
            Wait
        }

        [Serializable]
        public struct Task
        {
            public TaskType type;
            public float value;
        }

        Dictionary<TaskType, Action> actionsList = null;
        List<Action> tasksList = new List<Action>();

        int currentAction = -1;
        Transform currentPoint = default;
        Transform nextPoint = default;
        object taskValue = default;
        IEnumerator<ISplineNode> enumerator = null;
        PatrolController innerController = null;

        #endregion
        public Transform NextPoint
        {
            get
            {
                if (!spline && spline.path == null) return default;

                enumerator??= spline.path.GetEnumerator();

                //if (innerController != null)
                //{
                //    var next = innerController.NextPoint;
                //    if (next != null)
                //        return next;
                //    else
                //        innerController = null;
                //}

                if (enumerator.MoveNext())
                {
                    //if (enumerator.Current.PatrolController)
                    //{
                    //    innerController = enumerator.Current.PatrolController;
                    //    return innerController.NextPoint;
                    //}
                    //else

                    return currentPoint = enumerator.Current.Transform;
                }
                else
                    return default;
            }
        } 
        public Movement Mover { get => mover; set => mover = value; }
        public bool CancelRequested { get; private set; }


        #region public methods
        public void SetNodeList(INodeListSupplier nodeList, CyclicType cyclicType)
        {
            this.cyclicType = cyclicType;

            finder = new PathFinder(nodeList);
        }

        public void FindNodes<U>(CyclicType cyclicType, INodeListSupplier nodeList)
            where U : MonoBehaviour, IPathNode, ISplineNode
        {
            this.cyclicType = cyclicType;
            List<IPathNode> nodes = new();

            for (int i = 0; i < transform.childCount; i++)
            {
                var node = transform.GetChild(i).GetComponent<U>();
                if (node)
                    nodes.Add(node);
            }

            nodeList.SetNodes(nodes.ToArray());
            this.finder = new PathFinder(nodeList);
        }

        public void CreatePatrolWithSpline<T>(IPathNode start, Vector3 end, CyclicType cyclicType) where T : IPathFinder, new()
        {
            this.cyclicType = cyclicType;
            finder.OnPathCalculated += () =>
            {
                var route = finder.BestRoute;
                //print("Total connections!! =>  " + route?.MaxCount);
                enumerator?.Dispose();
                enumerator = null;
                CreateSpline();
            };
            finder.Find_BestRoute<T>((start, end));
            //finder.FindRoute(start, nodeList.FindNearestNode(end));
        }

        private void CreateSpline()
        {
            var route = finder.BestRoute.ToArray();

            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).GetComponent<Spline>())
                {
                    Destroy(transform.GetChild(i).gameObject);
                    break;
                }

            var splineGO = Instantiate(splinePrefab, transform.parent.transform);
            var spline = splineGO.GetComponent<Spline>();

            for (int i = 0; i < route.Length; i++)
            {
                var go = new GameObject("Node " + i, typeof(PatrolNode));
                go.transform.parent = splineGO.transform;
                go.transform.position = route[i].Position;
            }

            if (this.spline) Destroy(this.spline.gameObject);
            this.spline = spline;
            spline.cyclicType = cyclicType;
            Initialize();

            OnFinished?.Invoke();
            //Execute_Tasks();
        }

        public void Initialize(Movement movement, PathFinder finder)
        {
            mover = movement;
            this.finder = finder;

            Initialize();
        }

        public void Initialize()
        {
            if (!spline) spline = transform.GetComponentInChildren<Spline>();
            spline.Initialize();

            tasksList = new();
            enumerator = null;
            if (actionsList == null && !InitializeTasks())
                return;

            foreach (var task in tasks)
            {
                tasksList.Add(actionsList[task.type]);
            }
        }

        public void StartPatrolling()
        {
            if (state != PatrolState.None) return;

            mover.OnFinished += ContinueTasks;

            Execute_Tasks();
        }

        private void Execute_Tasks()
        {
            if (CancelRequested) { FinishPatrol(); return; }

            state = PatrolState.Running;
            currentAction++;

            if (tasksList != null && currentAction < tasksList.Count)
            {
                taskValue = tasks[currentAction].value;
                tasksList[currentAction].Invoke();
                return;
            }
            else if (shouldRepeat && tasksList != null)
            {
                currentAction = -1;
                state = PatrolState.Repeating;
                Execute_Tasks();
                return;
            }

            FinishPatrol();
            state = PatrolState.None;
        }

        public void AbortPatrol() => CancelRequested = true;
        #endregion

        #region private methods
        //private void Awake()
        //{
        //    //mover = GetComponent<Movement>();

        //    //InitializeTasks();
        //}

        private void FinishPatrol()
        {
            currentAction = -1;
            enumerator?.Reset();
            OnPatrolFinished?.Invoke();
            CancelRequested = false;

            mover.OnFinished -= ContinueTasks;
            state = PatrolState.None;
        }

        private bool InitializeTasks()
        {
            if (!mover) return false;

            actionsList = new Dictionary<TaskType, Action>()
                {
                    //{ TaskType.Turn, () => mover.TurnTo((float)taskValue) },
                    { TaskType.Move, () => {

                        Transform p = NextPoint;
                        if (p == null) 
                        { 
                            FinishPatrol(); 
                            return;
                        }

                        mover.MoveTo(p.position); } 
                    },
                    { 
                        TaskType.Wait, () => Invoke("ContinueTasks", (float)taskValue)
                    }
                };

            return true;
        }

        private void ContinueTasks()
        {
            state = PatrolState.None;
            Execute_Tasks();
        }
        #endregion
    }
}
