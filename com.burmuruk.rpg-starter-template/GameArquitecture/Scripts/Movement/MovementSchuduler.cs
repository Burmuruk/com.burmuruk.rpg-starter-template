using System;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Movement
{
    class MovementSchuduler
    {
        enum Status
        {
            None,
            Waiting,
            Running,
            Paused,
            Canceled,
            BackGround
        }

        record MyTask()
        {
            public Status Status { get; set; }
            public IMoveAction Caller { get; private set; }
            public Priority Priority { get; private set; }
            public Action[] Actions { get; private set; }

            public MyTask(Status status, IMoveAction caller, Priority priority, Action[] actions) : this()
            {
                Status = status; 
                Caller = caller; 
                Priority = priority; 
                Actions = actions;
            }
        }

        List<MyTask> actionsList = new();
        int? curActionIdx;

        public void AddAction(Priority priority, IMoveAction caller, params Action[] actions)
        {
            if (FindTaskByCaller(caller, out _)) return;

            int priorityValue = (int)priority;
            //var cur = actionsList.First;

            //for (int i = 0; i < actionsList.MaxCount; i++)
            //{
            //    if (caller == cur.Caller)
            //    {
            //        actionsList.AddAfter(cur)
            //    }

            //    cur = cur.Next;
            //}

            actionsList.Add(new(Status.Waiting, caller, priority, actions));
        }

        public void StopAction(IMoveAction caller)
        {
            MyTask curTask;

            if (FindTaskByCaller(caller, out curTask))
            {
                curTask.Caller.PauseAction();
                actionsList.RemoveAt(0);
            }
        }

        public void CancelAction(IMoveAction caller)
        {
            MyTask curTask;

            if (FindTaskByCaller(caller, out curTask))
            {
                curTask.Caller.StopAction();
                actionsList.RemoveAt(0);
            }
        }

        public void CancelAll()
        {
            actionsList[0].Caller.StopAction();
            actionsList = new();
        }

        private void RemoveAction()
        {

        }

        private bool FindTaskByCaller(IMoveAction moveAction, out MyTask task)
        {
            task = null;

            for (int i = 0; i < actionsList.Count; i++)
            {
                if (actionsList[i].Caller == moveAction)
                {
                    task = actionsList[i];
                    return true;
                }
            }

            return false;
        }

        private void CheckNextTask()
        {
            if (actionsList[0].Status == Status.Waiting)
            {
                actionsList[0].Status = Status.Waiting; 
            }
        }
    }

    public enum Priority
    {
        None,
        Low,
        Medium,
        High,
        Emergency
    }

    public interface IMoveAction
    {
        event Action OnFinished;
        void StopAction();
        void StartAction();
        void PauseAction();
    }
}
