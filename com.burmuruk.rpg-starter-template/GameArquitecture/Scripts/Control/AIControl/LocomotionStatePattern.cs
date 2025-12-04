namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public interface LocomotionContext
    {
        public void SetTarget(LocomotionState newState);
    }

    public abstract class LocomotionState
    {
        public abstract void CheckOrders(LocomotionContext context);
        public abstract void Attack(LocomotionContext context);
        public abstract void CheckItems(LocomotionContext context);
        public abstract void CheckSelfStatus(LocomotionContext context);
        public abstract void Patrol(LocomotionContext context);
        public abstract void Cover(LocomotionContext context);
        public abstract void Danger(LocomotionContext context);
        public abstract void Ability(LocomotionContext context);
    }

    public class LocomotionStatePattern : LocomotionContext
    {
        LocomotionState currentState = new BaseState();

        public void Ability() => currentState.Ability(this);

        public void Attack() => currentState.Attack(this);

        public void CheckItems() => currentState.CheckItems(this);

        public void CheckOrders() => currentState.CheckOrders(this);

        public void CheckSelfStatus() => currentState.CheckSelfStatus(this);

        public void Cover() => currentState.Cover(this);

        public void Danger() => currentState.Danger(this);

        public void Patrol() => currentState.Patrol(this);

        public void SetTarget(LocomotionState newState) => currentState = newState;
    }

    public class BaseState : LocomotionState
    {
        public override void Ability(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void Attack(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void CheckItems(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void CheckOrders(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void CheckSelfStatus(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void Cover(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void Danger(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }

        public override void Patrol(LocomotionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
