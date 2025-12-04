using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class AIGuildMember : Character, IPlayable
    {
        [SerializeField] Formation formation;
        [SerializeField] PlayerState playerState;
        [SerializeField] Character mainPlayer;
        [SerializeField] PlayerDistance playerDistance;
        [SerializeField] AttackState attackState;
        [SerializeField] float fellowGap;

        object formationArgs;
        CoolDownAction cdTeleport;
        public int id = -1;

        #region Enums

        public enum PlayerDistance
        {
            None,
            Close,
            Free,
            Far,
            FarAway
        }

        public enum AttackState
        {
            None,
            BasicAttack,
            SpecialAttack,
        }
        #endregion

        const float closeDistance = 3;
        const float freeDistance = 6;
        const float farDistance = 9;
        bool detachRotation = false;

        public event Action OnEnemyDetected;
        public  event Action OnFormationChanged;
        public override event Action<bool> OnCombatStarted;

        public bool IsControlled { get; set; }
        public Character[] Fellows { get; set; }
        public float FellowGap { get => fellowGap; }
        public Formation Formation { get => formation; }
        public PlayerState PlayerState
        {
            get => playerState;
            set
            {
                if (playerState == PlayerState.Combat && value != PlayerState.Combat)
                {
                    playerState = value;
                    OnCombatStarted?.Invoke(false);
                    return;
                }

                playerState = value;
            }
        }
        public Character Leader { get => mainPlayer; }

        protected virtual void Start()
        {
            cdTeleport = new CoolDownAction(1.5f);
            //if (TryGetComponent<Health>(out var health))
            //    health.OnDied +
        }

        protected override void FixedUpdate()
        {
            if (mainPlayer == null) return;

            playerDistance = Vector3.Distance(mainPlayer.transform.position, transform.position) switch
            {
                <= closeDistance => PlayerDistance.Close,
                <= freeDistance => PlayerDistance.Free,
                <= farDistance => PlayerDistance.Far,
                > farDistance => PlayerDistance.FarAway
            };

            base.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (playerState == PlayerState.Combat && !isTargetFar && !isTargetClose)
            {
                Collider closest = null;
                List<(Component enemy, float distance)> closestEnemies = new();

                foreach (Character character in Fellows)
                {
                    if (character.IsTargetClose || character.IsTargetFar)
                    {
                        closestEnemies.Add(GetClosestEnemy(character.CloseEnemies));
                    }
                }

                if (closestEnemies.Count > 0)
                {

                }
                else
                {
                    OnCombatStarted(false);
                }
            }

            if (Target)
            {
                Debug.DrawRay(Target.transform.position, Vector3.up * 5, Color.red);
            }
        }

        protected override void Dead()
        {
            base.Dead();

            if (IsControlled)
                FindObjectOfType<LevelManager>().Die();
        }

        public void DisableControll()
        {
            IsControlled = false;
        }

        public void EnableControll()
        {
            IsControlled = true;
        }

        public override void StopActions(bool shouldPause)
        {
            if (shouldPause)
            {
                playerState = PlayerState.None;
                mover.PauseAction();
            }
            else
            {
                playerState = PlayerState.None;
                mover.ContinueAction();
            }

            fighter.Pause(shouldPause);
        }

        public void SetFormation(Formation formation, object args)
        {
            //if (playerState != PlayerAction.Combat) return;

            this.formation = formation;

            formationArgs = args;
            //print ("Current formation: \t" + this.formation.ToString());
        }

        public void SetMainPlayer(Character character)
        {
            mainPlayer = character;
        }

        public void SetTarget(Character enemy)
        {
            Target = enemy.transform;
        }

        public void AttackEnemy(Character enemy)
        {
            Target = enemy.transform;
            PlayerState = PlayerState.Combat;
            OnCombatStarted?.Invoke(true);

            fighter.BasicAttack();
        }

        public void AutoAttackEnemy(Character enemy)
        {
            Target = enemy.transform;
            PlayerState = PlayerState.Combat;
            OnCombatStarted?.Invoke(true);

            fighter.StartAutoBasicAttack(true);
        }

        public void Retreat()
        {
            Target = null;
            PlayerState = PlayerState.None;
            OnCombatStarted?.Invoke(false);
        }

        public void AnalizeDamage()
        {
            if (PlayerState == PlayerState.Dead) return;
            
            PlayerState = PlayerState.Combat;
        }

        protected override void GetNextTarget(Transform target)
        {
            base.GetNextTarget(target);

            if (IsControlled)
            {
                target.GetComponent<Character>().Deselect();
                if (Target)
                    Target.GetComponent<Character>().Select();
                else
                    PlayerState = PlayerState.None;
            }
        }

        protected override void DecisionManager()
        {
            if (PlayerState == PlayerState.Paused) return;

            if (IsControlled)
            {
                ControlledDecisionManager();
                ActionManager();
                //MovementManager();
                return;
            }

            if (playerDistance == PlayerDistance.FarAway)
            {
                PlayerState = PlayerState.Teleporting;

                ActionManager();
                MovementManager();
                return;
            }

            switch (formation)
            {
                case Formation.Follow:

                    PlayerState = PlayerState.FollowPlayer;
                    attackState = AttackState.None;
                    break;

                case Formation.None:
                case Formation.Free:

                    if (playerDistance == PlayerDistance.Free || playerDistance == PlayerDistance.Close)
                    {
                        if ((isTargetFar || isTargetClose) && 
                            Vector3.Distance(transform.position, 
                                GetNearestTarget(eyesPerceibed).position) < freeDistance)
                        {
                            PlayerState = PlayerState.Combat;
                            attackState = AttackState.BasicAttack;
                        }
                        else
                        {
                            PlayerState = PlayerState.None;
                            attackState = AttackState.None;
                        }
                    }
                    else if (playerDistance == PlayerDistance.Far)
                    {
                        PlayerState = PlayerState.FollowPlayer;
                        attackState = AttackState.None;
                    }

                    break;

                case Formation.Protect:

                    if (playerDistance == PlayerDistance.Close)
                    {
                        if ((isTargetFar || isTargetClose) &&
                            Vector3.Distance(transform.position, 
                            GetNearestTarget(eyesPerceibed).position) < stats.minDistance)
                        {
                            PlayerState = PlayerState.Combat;
                            attackState = AttackState.BasicAttack;
                        }
                        else
                        {
                            PlayerState = PlayerState.None;
                            attackState = AttackState.None;
                        }
                    }
                    else
                    {
                        PlayerState = PlayerState.FollowPlayer;
                        attackState = AttackState.None;
                    }
                    
                    break;

                case Formation.LockTarget:
                    if (playerDistance == PlayerDistance.Free || playerDistance == PlayerDistance.Close)
                    {
                        PlayerState = PlayerState.Combat;
                        attackState = AttackState.BasicAttack;
                    }
                    break;

                default:
                    break;
            }

            ActionManager();
            MovementManager();
        }

        protected override void ActionManager()
        {
            base.ActionManager();

            switch (playerState, attackState)
            {
                case (PlayerState.FollowPlayer, _):
                    break;

                case (PlayerState.Combat, AttackState.BasicAttack):
                    if (isTargetFar || isTargetClose || Target)
                    {
                        if (formation == Formation.LockTarget)
                        {
                            Target = ((Character)formationArgs).transform;
                        }
                        else if (Target == null)
                        {
                            Target = GetNearestTarget(eyesPerceibed);
                        }

                        OnCombatStarted?.Invoke(true);
                        fighter.SetTarget(Target);
                        fighter.BasicAttack();
                    }
                    else if (Target == null)
                    {
                        Target = GetNearestTarget(eyesPerceibed);
                    }
                    break;

                case (PlayerState.Teleporting, _):
                    Invoke("MoveCloseToPlayer", 1);
                    break;
            }
        }

        protected override void MovementManager()
        {
            base.MovementManager();

            switch (playerState, attackState)
            {
                case (PlayerState.FollowPlayer, _):
                    FollowPlayer();
                    break;

                case (PlayerState.Combat, AttackState.BasicAttack):
                    if (isTargetFar || isTargetClose)
                    {
                        if (formation == Formation.Protect) break;

                        var dis = stats.minDistance * .8f;
                        if (Vector3.Distance(Target.position, transform.position) > dis)
                        {
                            var destiniy = (transform.position - Target.position).normalized * dis;
                            destiniy += Target.position;
                            mover.MoveTo(destiniy);
                            Debug.DrawRay(destiniy, Vector3.up * 5);
                        }
                    }
                    break;

                case (PlayerState.Teleporting, _):
                    Invoke("MoveCloseToPlayer", 1);
                    break;
            }
        }

        protected virtual void IdentifyHazards()
        {
            throw new NotImplementedException();
        }

        private void ControlledDecisionManager()
        {
            if (Target || isTargetClose || isTargetFar)
            {
                OnCombatStarted?.Invoke(true);

                playerState = PlayerState.Combat;
                attackState = AttackState.BasicAttack;
            }
            else
                OnCombatStarted?.Invoke(false);
        }

        public void MoveCloseToPlayer()
        {
            if (!cdTeleport.CanUse) return;

            StartCoroutine(cdTeleport.CoolDown());

            Vector3 pos = default;
            var startNode = mover.nodeList.FindNearestNode(mainPlayer.transform.position);

            do
            {
                var (x, z) = (Mathf.Cos(UnityEngine.Random.Range(-1, 1)), Mathf.Sin(UnityEngine.Random.Range(-1, .1f)));
                var dis = freeDistance / 2;

                pos = new Vector3(x * dis, mainPlayer.transform.position.y, z * dis);
                pos = pos.normalized * UnityEngine.Random.Range(closeDistance, freeDistance);
            }
            while (!mover.ChangePositionCloseToNode(startNode, mainPlayer.transform.position + pos));
        }

        private void FollowPlayer()
        {
            mover.FollowWithDistance(mainPlayer.mover, fellowGap, Fellows);
        }

        private void GetRemainEnemies()
        {
            
        }

        #region Saving
        protected override JToken CaptureCharacterData()
        {
            var state = base.CaptureCharacterData();

            state["Equipment"] = CaptureEquipment();
            state["BasicStats"] = CaptureBasicStatus();

            return state;
        }

        protected override void RestoreCharacterData(JToken jToken)
        {
            base.RestoreCharacterData(jToken);

            RestoreBasicStatus(jToken["BasicStats"]);
            RestoreEquipment(jToken["Equipment"]);
        }

        public JToken CaptureBuffs()
        {
            JObject characterState = new JObject();
            int i = 0;
            var buffsDic = FindObjectOfType<BuffsManager>().GetCharacterTimers(this);

            foreach (var buffNode in buffsDic)
            {
                JObject buffState = new JObject();

                buffState["CurTime"] = buffNode.Key.CurrentTime;
                buffState["Duration"] = buffNode.Value.buff.duration;
                buffState["Value"] = buffNode.Value.buff.value;
                buffState["Rate"] = buffNode.Value.buff.rate;
                buffState["Percentage"] = buffNode.Value.buff.percentage;
                //buffState["AffectAll"] = buffNode.Value.buff.affectAll;
                buffState["Stat"] = (int)buffNode.Value.buff.stat;

                characterState[i++] = buffState;
            }

            return characterState;
        }

        public void RestoreBuffs(JToken jToken)
        {
            if (!(jToken is JObject jObject)) return;

            var buffManager = FindObjectOfType<BuffsManager>();
            int i = 0;

            while (jObject.ContainsKey(i.ToString()))
            {
                var curToken = jObject[i];

                BuffData buffData = new BuffData()
                {
                    duration = curToken["Duration"].ToObject<float>(),
                    value = curToken["Value"].ToObject<float>(),
                    rate = curToken["Rate"].ToObject<float>(),
                    percentage = curToken["Percentage"].ToObject<bool>(),
                    //affectAll = curToken["AffectAll"].ToObject<bool>(),
                    stat = curToken["Stat"].ToObject<ModifiableStat>(),
                };
                int damage = (int)buffData.value;

                buffManager.AddBuff((Character)this, buffData, () => health.ApplyDamage(damage));
            }
        } 
        #endregion
    }
}
