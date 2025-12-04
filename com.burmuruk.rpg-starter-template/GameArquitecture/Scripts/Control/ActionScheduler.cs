using System;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Utilities
{
    public enum ActionPriority
    {
        None,
        Low,
        Medium,
        High,
        Emergency,
        System
    }

    public class ActionScheduler
    {
        LinkedList<ActionStatus> actions = new();
        int curTask;

        public bool Initilized { get; private set; }

        enum ActionState
        {
            None,
            Paused,
            Running,
            Canceling,
        }

        class ActionStatus
        {
            public IScheduledAction action;
            public ActionPriority priority;
            public ActionState state;
            public Action[] tasks;

            public ActionStatus(IScheduledAction action, ActionPriority priority, ActionState state, Action[] tasks)
            {
                this.action = action;
                this.priority = priority;
                this.state = state;
                this.tasks = tasks;
            }
        }

        public void AddAction(IScheduledAction action, ActionPriority priority, params Action[] tasks)
        {
            Initilized = true;

            switch (priority)
            {
                case ActionPriority.Low:

                    actions.AddLast(new ActionStatus(action, priority, ActionState.None, tasks));
                    Start(actions.First);
                    return;

                case ActionPriority.Medium:
                case ActionPriority.High:
                    break;

                case ActionPriority.Emergency:
                case ActionPriority.System:

                    if (actions.Count > 0 && (int)actions.First.Value.priority < (int)priority)
                    {
                        if (actions.First.Value.state == ActionState.Running)
                            Pause(actions.First.Value.action);
                    }

                    break;

                default:
                    return;
            }

            AddActionByPriority(new ActionStatus(action, priority, ActionState.None, tasks));

            if (actions.First.Value.action == action)
                Start(actions.First);
        }

        public void Pause(IScheduledAction action)
        {
            ActionStatus actionStatus;

            if (GetAction(action, out actionStatus))
            {
                actionStatus.state = ActionState.Paused;
                action.PauseAction();
            }
        }

        public void Cancel(IScheduledAction action)
        {
            ActionStatus actionStatus;

            if (GetAction(action, out actionStatus))
            {
                if (actionStatus.state == ActionState.Running)
                    actionStatus.action.CancelAction();

                actions.Remove(actionStatus);
            }
        }

        public void CancelAll()
        {
            while (actions.Count > 0)
            {
                var first = actions.First;

                if (first.Value.state == ActionState.Paused || first.Value.state == ActionState.Running)
                {
                    Cancel(first.Value.action);
                }

                //actions.RemoveFirst();
            }
        }

        public void Finished(IScheduledAction action)
        {
            ActionStatus actionStatus;

            if (GetAction(action, out actionStatus))
            {
                if (actionStatus.tasks != null && curTask < actionStatus.tasks.Length)
                {
                    actionStatus.tasks[curTask++]?.Invoke();
                }
                else
                {
                    RunNextQuest();
                }

                actionStatus.state = ActionState.Running;
            }
        }

        void AddActionByPriority(ActionStatus newAction)
        {
            if (actions.Count == 0)
            {
                actions.AddFirst(newAction);
                return;
            }

            var curNode = actions.First;
            int curPriority = (int)newAction.priority;

            while(curNode.Previous.Value.action != actions.First.Value.action)
            {
                if ((int)curNode.Value.priority < curPriority)
                {
                    actions.AddAfter(curNode, newAction);
                }
            }
        }

        bool GetAction(IScheduledAction action, out ActionStatus actionIdx)
        {
            actionIdx = null;

            foreach (ActionStatus item in actions)
            {
                if (item.action == action)
                {
                    actionIdx = item;
                    return true;
                }
            }

            return false;
        }

        void RunNextQuest()
        {
            if (actions.Count <= 0) return;

            actions.RemoveFirst();

            if (actions.Count > 0)
                Start(actions.First);
        }

        void Start(LinkedListNode<ActionStatus> node)
        {
            try
            {
                var actionStatus = node.Value;

                if (actionStatus.tasks == null || actionStatus.tasks.Length == 0)
                    return;

                if (actionStatus.state == ActionState.Running)
                    return;

                actionStatus.state = ActionState.Running;
                actionStatus.tasks[0]?.Invoke();
                curTask = 1;
            }
            catch (NullReferenceException n)
            {

                throw;
            }
        }
    }

    public interface IScheduledAction
    {
        void StartAction();
        void PauseAction();
        void ContinueAction();
        void CancelAction();
        void FinishAction();
    }
}
