using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEBasicMachineGun : AIEnemyController
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

        private void Attack()
        {
            if (Target == null) return;

            if (Vector3.Distance(m_target.position, transform.position) <= stats.minDistance)
            {
                fighter.BasicAttack();
            }
        }

        protected override void MovementManager()
        {
            switch (playerAction)
            {
                case PlayerAction.Combat:

                    PursueCombatTarget();
                    break;
            }
        }
    }
}
