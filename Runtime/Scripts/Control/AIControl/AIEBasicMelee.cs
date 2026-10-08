using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEBasicMelee : AIEnemyController
    {
        protected override bool FindEnemies()
        {
            if (!base.FindEnemies()) return false;

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
            return false;
        }

        private void Attack()
        {
            if (!IsValidTarget(Target)) return;

            if (Vector3.Distance(Target.position, transform.position) <= stats.minDistance)
            {
                fighter.BasicAttack();
            }
        }

        protected override void MovementManager()
        {
            var dis = stats.minDistance * .8f;

            switch (playerAction)
            {
                case PlayerAction.Following:

                    if (leader == null) return;

                    if (Vector3.Distance(leader.transform.position, transform.position) > dis)
                    {
                        Vector3 destiny = (transform.position - leader.transform.position).normalized * dis;
                        destiny += leader.transform.position;

                        mover.MoveTo(destiny);
                    }
                    break;

                case PlayerAction.Combat:

                    PursueCombatTarget();
                    break;
            }
        }
    }
}
