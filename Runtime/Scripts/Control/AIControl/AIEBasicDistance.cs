using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEBasicDistance : AIEnemyController
    {
        [Header("Shaders")]
        [SerializeField, Min(0)] float DissolveTime = 2;
        private readonly List<(Renderer renderer, int index)> dissolveRenderers = new();
        private MaterialPropertyBlock dissolveProperties;
        private Coroutine transition;
        private bool withdrawing;
        private bool appearing;
        private bool managedByHorde;
        private Vector3 searchCenter;
        private float nextSearchMove;
        public bool IsReady => !appearing && !withdrawing && gameObject.activeInHierarchy && Health != null && Health.IsAlive;
        public bool IsWithdrawing => withdrawing;

        public void Deploy(AIEHordeDistance chief)
        {
            managedByHorde = true;
            SetLeader(chief);
            SetOrder(LeaderOrder.Attack);

            if (transition != null) 
                StopCoroutine(transition);

            withdrawing = false;
            appearing = true;
            fighter.Pause(true);
            mover.ResetRoute();
            mover.PauseAction();
            GetRenderers();
            SetDissolve(0);
            transition = StartCoroutine(Appear());
        }

        private IEnumerator Appear()
        {
            yield return Fade(0, 1);

            // Dissolve is an extra material pass; hide it once the spawn effect ends
            // so the original textured materials are visible again.
            SetDissolve(0);

            appearing = false;
            transition = null;

            mover.ContinueAction();
            fighter.Pause(false);
        }

        public void Withdraw()
        {
            if (withdrawing) return;

            if (!gameObject.activeInHierarchy)
            {
                Restart();
                return;
            }

            if (transition != null) StopCoroutine(transition);

            appearing = false;
            withdrawing = true;

            StopEncounterActions();
            fighter.Pause(true);
            mover.PauseAction();
            GetRenderers();
            transition = StartCoroutine(Disappear());
        }

        private IEnumerator Disappear()
        {
            yield return Fade(1, 0);

            Restart();
            withdrawing = false;
            transition = null;
            gameObject.SetActive(false);
        }

        protected override void DecisionManager()
        {
            if (appearing || withdrawing) return;

            if (!managedByHorde)
            {
                base.DecisionManager();
                return;
            }

            if (!(leader is AIEHordeDistance chief) || !chief.isActiveAndEnabled || chief.Health == null || !chief.Health.IsAlive)
            {
                Withdraw();
                return;
            }

            Transform observed = chief.ObserveEncounter();

            if (IsValidTarget(observed))
            {
                UpdateCombatState(true);

                Target = observed;
                playerAction = PlayerAction.Combat;

                fighter.Pause(false);
                fighter.BasicAttack();
                PursueCombatTarget();
            }
            else
            {
                UpdateCombatState(false);

                Target = null;
                fighter.SetTarget(null);
                mover.FacingTarget = null;
                playerAction = PlayerAction.Following;
                Search(chief.LastSeenPosition, chief.SearchRadius);
            }
        }

        private void Search(Vector3 position, float radius)
        {
            if ((searchCenter - position).sqrMagnitude > .25f)
            {
                searchCenter = position;
                mover.ResetRoute();
                nextSearchMove = 0;
            }

            if (mover.IsWorking || Time.time < nextSearchMove) 
                return;

            Vector3 destination = position;
            Vector3 offset = transform.position - position;
            offset.y = 0;

            if (offset.sqrMagnitude <= 2.25f)
            {
                Vector2 random = Random.insideUnitCircle * radius;
                destination += new Vector3(random.x, 0, random.y);
            }

            nextSearchMove = Time.time + Random.Range(.75f, 1.5f);
            mover.MoveTo(destination);
        }

        protected override bool FindEnemies()
        {
            if (!base.FindEnemies()) return false;

            attackState = AttackState.BasicAttack;
            return true;
        }

        protected override void ActionManager()
        {
            if (playerAction == PlayerAction.Combat && IsValidTarget(Target)) 
                fighter.BasicAttack();
        }

        protected override void MovementManager()
        {
            if (playerAction == PlayerAction.Combat) 
                PursueCombatTarget();
            else if (playerAction == PlayerAction.Following && leader != null && !mover.IsWorking)
                mover.MoveTo(leader.transform.position);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetDissolve(0);
            transition = null;
            appearing = withdrawing = false;
        }

        private void GetRenderers()
        {
            dissolveProperties ??= new MaterialPropertyBlock();
            dissolveRenderers.Clear();

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].shader.name.Contains("Dissolve") && materials[i].HasProperty("_Intensity"))
                        dissolveRenderers.Add((renderer, i));
                }
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0;
            float duration = dissolveRenderers.Count > 0 ? Mathf.Max(0, DissolveTime) : 0;

            while (elapsed < duration)
            {
                SetDissolve(Mathf.Lerp(from, to, elapsed / duration));

                yield return null;
                elapsed += Time.deltaTime;
            }
            SetDissolve(to);
        }

        private void SetDissolve(float intensity)
        {
            foreach (var item in dissolveRenderers)
            {
                if (item.renderer == null) continue;

                item.renderer.GetPropertyBlock(dissolveProperties, item.index);
                dissolveProperties.SetFloat("_Intensity", intensity);
                item.renderer.SetPropertyBlock(dissolveProperties, item.index);
            }
        }
    }
}
