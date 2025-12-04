using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEBasicMelee : AIEnemyController
    {
        protected override bool FindEnemies()
        {
            if (!IsTargetClose && !IsTargetFar) return false;

            playerAction = PlayerAction.Combat;
            attackState = AttackState.BasicAttack;
            return true;
        }

        protected override void ActionManager()
        {
            base.ActionManager();

            switch (playerAction)
            {
                case PlayerAction.Combat:
                    Attack();
                    break;

                default: 
                    break;
            }
        }

        protected override bool ChooseAttack()
        {
            if (isTargetFar || isTargetClose)
            {
                playerAction = PlayerAction.Combat;
                return true;
            }

            return false;
        }

        private void Attack()
        {
            if (IsTargetFar)
            {
                Target = GetNearestTarget(eyesPerceibed);
            }
            else if (IsTargetClose)
            {
                Target = GetNearestTarget(earsPerceibed);
            }
            else if (Target == null) return;

            if (Vector3.Distance(Target.position, transform.position)
                <= stats.minDistance)
            {
                fighter.SetTarget(Target);
                fighter.BasicAttack();
            }
        }

        protected override void MovementManager()
        {
            var dis = stats.minDistance * .8f;

            switch (playerAction)
            {
                case PlayerAction.Following:

                    if (Vector3.Distance(leader.transform.position, transform.position) > dis)
                    {
                        Vector3 destiny = (transform.position - leader.transform.position).normalized * dis;
                        destiny += leader.transform.position;

                        mover.MoveTo(destiny);
                    }
                    break;

                case PlayerAction.Combat:

                    if (Vector3.Distance(Target.position, transform.position) > dis)
                    {
                        Vector3 destiny = (transform.position - Target.position).normalized * dis;
                        destiny += Target.position;

                        mover.MoveTo(destiny);
                    }
                    break;
            }
        }
    }
}