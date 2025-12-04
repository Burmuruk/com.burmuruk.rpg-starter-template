using Burmuruk.Utilities;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEBasicDistance : AIEnemyController
    {
        [Space(), Header("Shaders")]
        [SerializeField] float DissolveTime = 2;
        CoolDownAction cd_Appear;
        Material dissolveShader;
        bool visible = false;

        private void Start()
        {
            var delta = Time.deltaTime * 4;
            var cyclesCount = DissolveTime / delta;
            var rate = 1 / cyclesCount;
            cd_Appear = new CoolDownAction(DissolveTime, delta, () => EncreaseDissolve(rate), _ => { mover.ContinueAction(); visible = true; });
            GetDissolveShader();
        }

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

        protected override bool CheckLeader()
        {
            if (!leader) return false;

            switch (curOrder)
            {
                case LeaderOrder.Attack:
                    
                    if (!cd_Appear.CanUse) return false;

                    if (!visible)
                    {
                        mover.CancelAll();
                        mover.PauseAction();
                        playerAction = PlayerAction.None;
                        StartCoroutine(cd_Appear.Tick());
                        break;
                    }
                    else if (Target)
                    {
                        playerAction = PlayerAction.Combat;
                    }

                    break;

                case LeaderOrder.Follow:
                    playerAction = PlayerAction.Following;
                    break;

                default:
                    return false;
            }

            return true;
        }

        private void Attack()
        {
            if (Target == null)
            {
                Target = GetNearestTarget(eyesPerceibed).GetComponent<Character>().transform;
                if (Target == null)
                    GetNearestTarget(earsPerceibed); 
            }

            if (Target == null) return;

            if (Vector3.Distance(Target.transform.position, transform.position)
                <= stats.minDistance)
            {
                fighter.SetTarget(Target.transform);
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

                    if (Vector3.Distance(Target.transform.position, transform.position) > dis)
                    {
                        Vector3 destiny = (transform.position - Target.transform.position).normalized * dis;
                        destiny += Target.transform.position;

                        mover.MoveTo(destiny);
                    }
                    break;
            }
        }

        private void EncreaseDissolve(float value)
        {
            var cur = dissolveShader.GetFloat("_Intensity");
            cur = Mathf.Min(cur + value, 1);
            dissolveShader.SetFloat("_Intensity", cur);
        }

        private void GetDissolveShader()
        {
            var rend = GetComponent<Renderer>();

            foreach (var material in rend.materials)
            {
                if (material.shader.name.Contains("Dissolve"))
                {
                    dissolveShader = material;
                    break;
                }
            }
        }
    }
}
