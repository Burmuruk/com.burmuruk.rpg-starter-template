using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEHordeDistance : AIEnemyController
    {
        [Header("Horde Settings")]
        [SerializeField] GameObject horde;
        [SerializeField, Min(0)] float coolDownHorde = 5;
        [SerializeField, Min(0)] float hordeInvokeTime = 1;
        [SerializeField, Min(.1f)] float searchTimeout = 12;
        [SerializeField, Min(.1f)] float searchRadius = 5;
        [SerializeField, Min(.1f)] float spawnRadius = 3;
        [SerializeField, Min(.1f)] float retreatDistance = 6;
        [SerializeField, Min(.1f)] float maxRetreatTime = 3;
        [SerializeField, Min(.1f)] float rangedAttackDistance = 10;

        private enum EncounterPhase { Patrol, Retreat, Summon, Fight, Withdraw }
        private EncounterPhase phase;
        private readonly List<AIEBasicDistance> hordeMembers = new();
        private Vector3 lastSeenPosition;
        private float lastSeenTime;
        private float phaseStarted;
        private float nextSummonTime;
        private float nextRetreatMove;
        private bool troopsDeployed;
        private bool initialized;
        public Vector3 LastSeenPosition => lastSeenPosition;
        public float SearchRadius => Mathf.Max(.1f, searchRadius);
        public event Action OnTroopsDeployed;

        public override void SetStats(BasicStats newStats)
        {
            newStats.minDistance = Mathf.Max(newStats.minDistance, rangedAttackDistance);
            base.SetStats(newStats);
            InitializeHorde();
        }

        private void InitializeHorde()
        {
            if (initialized) return;

            initialized = true;

            if (horde == null)
            {
                Debug.LogWarning("Assign a Horde group containing AIEBasicDistance enemies to " + name, this);
                return;
            }

            if (horde == gameObject || transform.IsChildOf(horde.transform))
            {
                Debug.LogWarning("The Horde group must not contain its leader.", this);
                horde = null;
                return;
            }

            hordeMembers.AddRange(horde.GetComponentsInChildren<AIEBasicDistance>(true));

            if (horde.transform.IsChildOf(transform)) 
                horde.transform.SetParent(transform.parent, true);

            horde.SetActive(false);
        }

        /// <summary>
        /// Only current perception updates this position; a hidden target is never tracked by its transform.
        /// </summary>
        /// <returns></returns>
        public Transform ObserveEncounter()
        {
            Transform observed = GetObservedTarget();

            if (observed == null)
            {
                foreach (var member in hordeMembers)
                {
                    if (member == null || !member.IsReady)
                        continue;

                    observed = member.GetObservedTarget();

                    if (observed != null)
                        break;
                }
            }

            if (observed != null)
            {
                lastSeenPosition = observed.position;
                lastSeenTime = Time.time;
            }

            return observed;
        }

        protected override void DecisionManager()
        {
            if (health == null || !health.IsAlive || mover == null || fighter == null) 
                return;

            InitializeHorde();

            if (phase == EncounterPhase.Withdraw)
            {
                FinishWithdrawalIfReady();
                return;
            }

            Transform observed = ObserveEncounter();

            if (phase == EncounterPhase.Patrol)
            {
                if (observed == null || Time.time < nextSummonTime)
                {
                    CheckPatrolPath();
                    return;
                }

                StopPatrol();
                UpdateCombatState(true);
                mover.ResetRoute();

                phase = EncounterPhase.Retreat;
                phaseStarted = Time.time;
                nextRetreatMove = 0;
                fighter.Pause(true);
            }

            playerAction = PlayerAction.Combat;
            Target = observed;

            if (Time.time - lastSeenTime >= Mathf.Max(.1f, searchTimeout))
            {
                BeginWithdrawal();
                return;
            }

            switch (phase)
            {
                case EncounterPhase.Retreat:
                    fighter.Pause(true);
                    MoveAway(lastSeenPosition);

                    if (PlanarDistance(transform.position, lastSeenPosition) >= retreatDistance ||
                        Time.time - phaseStarted >= maxRetreatTime)
                    {
                        mover.ResetRoute();
                        phase = EncounterPhase.Summon;
                        phaseStarted = Time.time;
                    }
                    break;

                case EncounterPhase.Summon:

                    fighter.Pause(true);

                    if (!troopsDeployed && Time.time - phaseStarted >= Mathf.Max(0, hordeInvokeTime))
                        DeployHorde();

                    if (troopsDeployed && AllTroopsReady())
                    {
                        phase = EncounterPhase.Fight;
                        fighter.Pause(false);
                        OnTroopsDeployed?.Invoke();
                    }

                    break;

                case EncounterPhase.Fight:

                    if (observed == null)
                    {
                        fighter.SetTarget(null);
                        mover.FacingTarget = null;

                        if (!mover.IsWorking) 
                            mover.MoveTo(lastSeenPosition);
                        return;
                    }

                    fighter.Pause(false);
                    mover.FacingTarget = observed;
                    float standOff = Mathf.Max(retreatDistance, stats.minDistance * .7f);

                    if (PlanarDistance(transform.position, observed.position) < standOff)
                        MoveAway(observed.position);
                    else
                        PursueCombatTarget();

                    fighter.BasicAttack();
                    break;
            }
        }

        private void MoveAway(Vector3 position)
        {
            if (mover.IsWorking && Time.time < nextRetreatMove) return;

            mover.ResetRoute();
            Vector3 away = Vector3.ProjectOnPlane(transform.position - position, Vector3.up);

            if (away.sqrMagnitude < .001f) 
                away = -transform.forward;

            float distance = Mathf.Max(retreatDistance, stats.minDistance * .8f);
            mover.MoveTo(position + away.normalized * distance);
            nextRetreatMove = Time.time + .5f;
        }

        private void DeployHorde()
        {
            troopsDeployed = true;

            if (horde == null) return;

            horde.SetActive(true);

            foreach (var member in hordeMembers)
            {
                if (member == null) continue;

                member.Restart();

                if (mover.nodeList != null) 
                    member.mover.SetConnections(mover.nodeList);

                if (!PlaceNearLastSighting(member))
                {
                    member.gameObject.SetActive(false);
                    continue;
                }

                member.gameObject.SetActive(true);
                member.Deploy(this);
            }
        }

        private bool PlaceNearLastSighting(AIEBasicDistance member)
        {
            if (member.mover.nodeList == null) return false;

            var node = member.mover.nodeList.FindNearestNode(lastSeenPosition);

            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(spawnRadius * .5f, spawnRadius);
                Vector3 position = lastSeenPosition + new Vector3(offset.x, 0, offset.y);

                if (member.mover.ChangePositionCloseToNode(node, position)) 
                    return true;
            }

            return false;
        }

        private bool AllTroopsReady()
        {
            foreach (var member in hordeMembers)
            {
                if (member != null && member.gameObject.activeInHierarchy && !member.IsReady && member.Health.IsAlive)
                    return false;
            }

            return true;
        }

        private void BeginWithdrawal()
        {
            phase = EncounterPhase.Withdraw;
            StopEncounterActions();
            fighter.Pause(true);

            foreach (var member in hordeMembers)
            {
                if (member != null)
                    member.Withdraw();
            }

            FinishWithdrawalIfReady();
        }

        private void FinishWithdrawalIfReady()
        {
            foreach (var member in hordeMembers)
            {
                if (member != null && member.IsWithdrawing)
                    return;
            }

            if (horde != null) horde.SetActive(false);

            troopsDeployed = false;
            phase = EncounterPhase.Patrol;
            nextSummonTime = Time.time + Mathf.Max(0, coolDownHorde);

            fighter.Pause(false);
            playerAction = PlayerAction.None;
            CheckPatrolPath();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            foreach (var member in hordeMembers)
            {
                if (member == null) continue;

                member.Restart();
                member.GetComponent<Health>()?.ApplyDamage(member.Health.MaxHp);
            }

            //if (horde != null) 
            //    horde.SetActive(false);

            troopsDeployed = false;
            phase = EncounterPhase.Patrol;
        }

        private static float PlanarDistance(Vector3 first, Vector3 second) => 
            Vector3.ProjectOnPlane(first - second, Vector3.up).magnitude;
    }
}
