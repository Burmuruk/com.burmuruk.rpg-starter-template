using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Interaction;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.Utilities;
using Burmuruk.WorldG.Patrol;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIEnemyController : Character
    {
        [SerializeField] List<InventoryItem> itemsToDrop;
        [SerializeField] PatrolController patrolController;
        [SerializeField] protected PlayerAction playerAction;
        [SerializeField] protected AttackState attackState;
        [SerializeField] protected PlayerDistance playerDistance;
        [SerializeField] protected PlayerState playerState;
        [SerializeField] protected RageState rageState;
        [SerializeField] protected Awareness awareness;
        [SerializeField] protected LeaderOrder curOrder;
        protected AIEnemyController leader;
        protected List<(float value, Character enemy)> rage;
        protected List<AbiltyTrigger> abilities = new();
        List<Character> _enemies = new();
        private bool inCombat;
        private Vector3 chaseDestination;
        private float nextRepathTime;
        private Animator animator;
        private static readonly HashSet<AIEnemyController> activeEnemies = new();

        public Character CurEnemy { get; private set; }
        protected virtual void OnEnable() => activeEnemies.Add(this);
        protected virtual void OnDisable() => activeEnemies.Remove(this);
        public override event Action<bool> OnCombatStarted;

        #region Enums
        public enum PlayerAction
        {
            None,
            Combat,
            Patrol,
            Dead,
            Flee,
            Following
        }

        public enum AttackState
        {
            None,
            BasicAttack,
            SpecialAttack,
            Cover
        }

        public enum PlayerDistance
        {
            None,
            Close,
            Free,
            Far,
            FarAway
        }

        public enum PlayerState
        {
            None,
            Normal,
            Danger,
        }

        public enum RageState
        {
            None,
            Low,
            Medium,
            High,
            UltraHigh,
            Revenge
        }

        public enum Awareness
        {
            None,
            Front,
            Surounded,
            Alone,
            Threatened
        }

        public enum LeaderOrder
        {
            None,
            Attack,
            Follow
        }
        #endregion

        public static bool IsThreatening(Character character)
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy.isActiveAndEnabled && enemy.Health != null && enemy.Health.IsAlive &&
                    enemy.playerAction == PlayerAction.Combat && enemy.Target == character.transform)
                    return true;
            }
            return false;
        }

        protected void PursueCombatTarget()
        {
            if (!IsValidTarget(Target)) return;

            mover.FacingTarget = Target;
            float distance = stats.minDistance * .8f;

            if (Vector3.Distance(Target.position, transform.position) <= distance)
            {
                if (mover.IsWorking) mover.CancelAction();
                return;
            }

            if (mover.IsWorking && Time.time >= nextRepathTime && 
                (Target.position - chaseDestination).sqrMagnitude > .25f)
            {
                mover.CancelAction();
            }

            if (!mover.IsWorking)
            {
                chaseDestination = Target.position;
                nextRepathTime = Time.time + .25f;
                mover.MoveTo(chaseDestination, stoppingDistance: distance);
            }
        }

        public class AbiltyTrigger
        {
            public Ability ability;
            public CoolDownAction cd;
            public int uses;

            public AbiltyTrigger(Ability ability, CoolDownAction coolDown)
            {
                this.ability = ability;
                this.cd = coolDown;
                uses = 0;
            }

            public void UseAbility()
            {
                uses++;
                //ability.Use();
            }
        }

        public void Restart()
        {
            SetDefaultStats();
            StopPatrol();
            UpdateCombatState(false);

            Target = null;
            mover.FacingTarget = null;

            mover.ResetRoute();
            mover.ContinueAction();
            fighter.ResetCombat();
            health.Heal(health.MaxHp);

            playerAction = PlayerAction.None;
            attackState = AttackState.None;
            curOrder = LeaderOrder.None;
            eyesPerceibed = earsPerceibed = Array.Empty<Collider>();
            isTargetFar = isTargetClose = false;
        }

        public Transform GetObservedTarget() => 
            isActiveAndEnabled && health != null && health.IsAlive ? GetPerceivedTarget() : null;

        protected void StopPatrol() => patrolController?.StopPatrolling();

        public void StopEncounterActions()
        {
            StopPatrol();
            UpdateCombatState(false);

            Target = null;
            mover.FacingTarget = null;

            mover.ResetRoute();
            fighter.ResetCombat();
            playerAction = PlayerAction.None;
        }

        public void SetLeader(AIEnemyController leader)
        {
            if (!leader)
            {
                this.leader = null;
                curOrder = LeaderOrder.None;
                return;
            }

            this.leader = leader;
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }

        public void SetOrder(LeaderOrder order)
        {
            curOrder = order;
        }

        protected override void Update()
        {
            base.Update();

            if (animator != null)
                animator.SetInteger("Health", health.HP);
        }

        public override void SetStats(BasicStats stats)
        {
            base.SetStats(stats);

            SetAbilities();
            //statsList.OnDied += DropItem;
            //patrolController = new PatrolController();
            Health.OnDamaged -= HandleDamage;
            Health.OnDamaged += HandleDamage;

            if (patrolController != null)
            {
                patrolController.Initialize(mover, mover.Finder);
            }

            animator = GetComponent<Animator>();
        }

        private void HandleDamage(int obj)
        {
            if (animator != null)
                animator.SetBool("Hit", true);
        }

        protected override void PerceptionManager()
        {
            base.PerceptionManager();
        }

        protected override void DecisionManager()
        {
            if (playerAction == PlayerAction.Dead) return;

            bool hasAction = CheckLeader() ||
                CheckOwnState() ||
                FindEnemies() ||
                ChooseAttack() ||
                TryTakeCover();

            if (!hasAction)
                playerAction = PlayerAction.None;

            UpdateCombatState(playerAction == PlayerAction.Combat);

            if (!hasAction)
                hasAction = CheckPatrolPath();

            if (hasAction)
            { 
                ActionManager();
                MovementManager();
            }
        }

        protected void UpdateCombatState(bool value)
        {
            if (inCombat == value) return;

            inCombat = value;
            if (value)
                patrolController?.StopPatrolling();
            else
            {
                mover.FacingTarget = null;
                Target = null;
                attackState = AttackState.None;
                fighter.StartAutoBasicAttack(false);
            }

            mover.CancelAction();
            OnCombatStarted?.Invoke(value);
        }

        protected override void GetNextTarget(Transform target)
        {
            Target = null;
        }

        protected override void ActionManager()
        {
            base.ActionManager();
        }

        protected virtual bool CheckPatrolPath()
        {
            if (!patrolController) return false;

            if (mover == null || mover.nodeList == null) return false;

            playerAction = PlayerAction.Patrol;
            patrolController.StartPatrolling();
            return true;
        }

        protected virtual bool CheckLeader()
        {
            if (!leader) return false;

            switch (curOrder)
            {
                case LeaderOrder.Attack:

                    if (!IsValidTarget(leader.Target)) return false;

                    Target = leader.Target;
                    playerAction = PlayerAction.Combat;
                    break;

                case LeaderOrder.Follow:

                    playerAction = PlayerAction.Following;
                    break;

                default:
                    return false;
            }

            return true;
        }

        protected virtual bool CheckOwnState()
        {
            if (health.HP < health.MaxHp * .4f)
            {
                return TryHeal();
            }
            //else if (Inventory.EquipedWeapon.Ammo <= 0)
            //{
            //    //Reload
            //    throw new NotImplementedException();
            //}

            return false;
        }

        protected virtual bool FindEnemies()
        {
            Transform enemy = GetPerceivedTarget();

            if (enemy != null)
            {
                Target = enemy;
                playerAction = PlayerAction.Combat;
                return true;
            }

            return false;
        }

        protected virtual bool ChooseAttack()
        {
            return false;
        }

        protected virtual bool TryTakeCover()
        {
            return false;
        }

        protected virtual bool TryHeal()
        {
            return false;
        }

        protected virtual void ChooseAbility()
        {
            foreach (var ability in abilities)
            {
                if (ability.cd.CanUse)
                {
                    ability.UseAbility();
                    StartCoroutine(ability.cd.CoolDown());
                }
            }
        }

        private void SetAbilities()
        {
            abilities.Clear();
            if (!(Inventory is InventoryEquipDecorator equipmentInventory)) return;
            var items = equipmentInventory.Equipped.GetItems((int)EquipmentType.Ability);

            if (items == null) return;
            
            foreach (var item in items)
            {
                var ability = (Ability)item;
                var cd = new CoolDownAction(ability.CoolDown);

                abilities.Add(new AbiltyTrigger(ability, cd));
            }
        }

        private void MoveCloseToPlayer(float minDis, float maxDis, Transform player)
        {
            var (x, z) = (Mathf.Cos(UnityEngine.Random.Range(-1, 1)), Mathf.Sin(UnityEngine.Random.Range(-1, .1f)));
            var dis = minDis;

            var pos = new Vector3(x * dis, player.transform.position.y, z * dis);
            pos = pos.normalized * UnityEngine.Random.Range(minDis, maxDis);
        }

        protected override void Dead()
        {
            UpdateCombatState(false);
            patrolController?.StopPatrolling();
            mover.CancelAction();
            playerAction = PlayerAction.Dead;
            base.Dead();

            DropItem();
        }

        public void DropItem()
        {
            if (itemsToDrop == null || itemsToDrop.Count <= 0) return;

            var rand = UnityEngine.Random.Range(0, itemsToDrop.Count);

            try
            {
                FindObjectOfType<PickupSpawner>()?.AddItem(itemsToDrop[rand], transform.position);
            }
            catch (NullReferenceException)
            {
                Debug.Log("No prefab detected to drop");
            }

            //var item = Instantiate(itemsToDrop.Items[rand].Prefab);
            //item.transform.position = transform.position;
        }
    }
}
