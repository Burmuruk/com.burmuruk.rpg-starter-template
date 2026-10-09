using System;
using System.Collections.Generic;
using UnityEngine;
using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Utilities;
using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class Character : MonoBehaviour, IJsonSaveable, ISelectable
    {
        #region Variables
        [Header("References"), Space()]
        [SerializeField] protected Transform farPercept;
        [SerializeField] protected Transform closePercept;
        [SerializeField] protected Material[] shaders;

        [Space(), Header("Perception"), Space()]
        [SerializeField] protected bool hasFarPerception;
        [SerializeField] protected bool hasClosePerception;
        [SerializeField] protected string enemyTag;
        [SerializeField] private CharacterType characterType;

        [Space(), Header("Status"), Space()]
        [SerializeField] public BasicStats stats;

        [HideInInspector] public Movement.Movement mover;
        [HideInInspector] public Fighter fighter;
        [HideInInspector] public ActionScheduler actionScheduler = new();
        [HideInInspector] protected Health health;
        [HideInInspector] protected IInventory inventory;

        protected Collider[] eyesPerceibed, earsPerceibed;
        protected bool isTargetFar = false;
        protected bool isTargetClose = false;
        protected Transform m_target;
        #endregion

        [Serializable]
        public struct ShaderRef
        {
            public string name; public Renderer Renderer;
        }

        public virtual event Action<bool> OnCombatStarted;

        #region Proerties
        public Health Health { get => health; }
        public IInventory Inventory
        {
            get
            {
                return (inventory ??= gameObject.GetComponent<IInventory>());
            }
            set => inventory = value;
        }
        public Collider[] CloseEnemies { get => earsPerceibed; }
        public Collider[] FarEnemies { get => eyesPerceibed; }
        public bool IsTargetFar { get => isTargetFar; }
        public bool IsTargetClose { get => isTargetClose; }
        public CharacterType CharacterType { get => characterType; }
        public bool IsSelected => throw new NotImplementedException();
        public ref Equipment Equipment { get => ref (inventory as InventoryEquipDecorator).Equipped; }
        public string EnemyTag => enemyTag;
        public Transform Target
        {
            get => m_target;
            set
            {
                if (m_target != null)
                {
                    if (value != m_target)
                    {
                        if (m_target.TryGetComponent<Health>(out var oldHealth))
                            oldHealth.OnDied -= GetNextTarget;
                    }
                    else
                    {
                        return;
                    }
                }

                m_target = value;
                fighter.SetTarget(value);

                if (value != null && value.TryGetComponent<Health>(out var targetHealth))
                    targetHealth.OnDied += GetNextTarget;
            }
        }
        #endregion

        #region Unity methods

        protected virtual void Update()
        {
            if (health != null && health.HP <= 0) return;

            DecisionManager();
        }

        protected virtual void FixedUpdate()
        {
            if (health != null && health.HP <= 0) return;

            eyesPerceibed = hasFarPerception ? Physics.OverlapSphere(farPercept != null ? farPercept.position : transform.position, stats.farDectection, 1 << 10) : Array.Empty<Collider>();
            earsPerceibed = hasClosePerception ? Physics.OverlapSphere(closePercept != null ? closePercept.position : transform.position, stats.closeDetection, 1 << 10) : Array.Empty<Collider>();
            isTargetFar = isTargetClose = false;

            PerceptionManager();
        }

        private void OnDrawGizmosSelected()
        {
            if (/*!newStats.Initilized ||*/ !farPercept || !closePercept) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(farPercept.position, stats.farDectection);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(closePercept.position, stats.closeDetection);
        }
        #endregion

        #region Public methods
        public virtual void SetUpMods()
        {
            health.RegisterValues(this);
            ModsList.AddVariable((Character)this, (ModifiableStat)3, () => stats.speed, (value) => stats.speed = value);
            ModsList.AddVariable((Character)this, (ModifiableStat)4, () => stats.damage, (value) => { stats.damage = (int)value; });
            ModsList.AddVariable((Character)this, (ModifiableStat)6, () => stats.damageRate, (value) => { stats.damageRate = value; });
            ModsList.AddVariable((Character)this, (ModifiableStat)7, () => stats.minDistance, (value) => { stats.minDistance = value; });
}

        public virtual void SetStats(BasicStats newStats)
        {
            GetComponents();

            stats = newStats;
            var invent = Inventory as InventoryEquipDecorator;
            if (!fighter)
                GetComponents();
            fighter.Initilize(invent, () => stats);
            mover.Initialize(invent, actionScheduler, () => stats);
        }

        public virtual void SetDefaultStats()
        {
            SetStats(stats);
        }

        public void SetPosition(Vector3 position)
        {
            mover.ChangePositionTo(position);
            //mover.UpdatePosition();
        } 
        #endregion

        protected virtual void GetComponents()
        {
            health ??= GetComponent<Health>();
            health.OnDied -= Dead;
            health.OnDied += Dead;

            mover ??= GetComponent<Movement.Movement>();
            fighter ??= GetComponent<Fighter>();
        }

        public virtual void StopActions(bool shouldPause) { }

        protected virtual void PerceptionManager()
        {
            if (hasFarPerception)
            {
                isTargetFar = PerceptEnemy(ref eyesPerceibed);
            }
            if (hasClosePerception)
            {
                isTargetClose = PerceptEnemy(ref earsPerceibed);
            }
        }

        protected virtual bool PerceptEnemy(ref Collider[] perceibed)
        {
            if (perceibed == null) return false;

            List<Collider> enemies = new();
            bool founded = false;

            for (int i = 0; i < perceibed.Length; i++)
            {
                ref var cur = ref perceibed[i];

                if (cur != null && cur.CompareTag(enemyTag) && IsValidTarget(cur.transform))
                {
                    enemies.Add(cur);
                    founded = true;
                }
            }

            perceibed = enemies.ToArray();

            return founded;
        }

        protected virtual void DecisionManager() { }

        protected virtual void ActionManager() { }

        protected virtual void MovementManager() { }

        protected Transform GetNearestTarget(Collider[] eyesPerceibed)
        {
            (Transform enemy, float dis) closest = (null, float.MaxValue);

            if (eyesPerceibed == null) return null;

            foreach (var enemy in eyesPerceibed)
            {
                if (enemy == null || !enemy.CompareTag(enemyTag) || !IsValidTarget(enemy.transform)) continue;

                if (Vector3.Distance(enemy.transform.position, transform.position) is var d && d < closest.dis)
                {
                    closest = (enemy.transform, d);
                }
            }

            return closest.enemy;
        }

        protected Transform GetPerceivedTarget()
        {
            Transform enemy = hasFarPerception ? GetNearestTarget(eyesPerceibed) : null;
            return enemy != null ? enemy : hasClosePerception ? GetNearestTarget(earsPerceibed) : null;
        }

        protected bool IsValidTarget(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy &&
                target.TryGetComponent<Health>(out var targetHealth) && targetHealth.IsAlive;
        }

        protected virtual void GetNextTarget(Transform target)
        {
            var nearEnemies = (from enemy in Physics.OverlapSphere(transform.position, 12, 1 << 10)
                                 where enemy.TryGetComponent<Character>(out _) && enemy.transform.CompareTag(enemyTag) && IsValidTarget(enemy.transform)
                                 select enemy).ToArray();

            var result = GetClosestEnemy(nearEnemies);

            if (result.obj == null)
            {
                Target = null;
                OnCombatStarted?.Invoke(false);
            }
            else
            {
                Target = result.obj.transform;
            }
        }

        protected (Component obj, float dis) GetClosestEnemy(Component[] enemies)
        {
            if (enemies == null || enemies.Length == 0) return default;

            (int idx, float distance) closest = (0, float.MaxValue);

            for (int i = 0; i < enemies.Length; i++)
            {
                var dis = Vector3.Distance(enemies[i].transform.position, transform.position);
                if (dis < closest.distance)
                {
                    closest = (i, dis);
                }
            }

            return (enemies[closest.idx], closest.distance);
        }

        private void Dead(Transform transform) => Dead();

        protected virtual void Dead()
        {
            gameObject.SetActive(false);
            StopAllCoroutines();

            if (BuffsManager.Instance != null)
                BuffsManager.Instance.RemoveAllBuffs(this);
        }

        #region Saving
        public JToken CaptureAsJToken(out SavingExecution execution)
        {
            execution = SavingExecution.References;
            return CaptureCharacterData();
        }

        public void LoadAsJToken(JToken state)
        {
            RestoreCharacterData(state);
        }

        public JToken CaptureInventory()
        {
            JObject state = new JObject();
            int i = 0;

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                var items = inventory.GetList(type);

                if (items == null) continue;

                for (int j = 0; j < items.Count; j++)
                {
                    JObject itemState = new();

                    itemState["Id"] = items[j].ID;
                    itemState["Count"] = inventory.GetItemCount(items[j].ID);

                    if (items[j] is EquipableItem equipeable && equipeable.Characters.Contains(this))
                        itemState["Equipped"] = true;

                    state[i++.ToString()] = itemState;
                }
            }

            return state;
        }

        public void RestoreInventory(JToken jToken)
        {
            if (!(jToken is JObject state)) return;

            int i = 0;

            while (state.ContainsKey(i.ToString()))
            {
                int id = state[i.ToString()]["Id"].ToObject<int>();

                for (int j = 0; j < state[i.ToString()]["Count"].ToObject<int>(); j++)
                {
                    inventory.Add(id);
                }

                if (inventory.GetItem(id) is EquipableItem equipeable && 
                    state[i.ToString()]["Equipped"]?.ToObject<bool>() == true)
                {
                    (inventory as InventoryEquipDecorator)
                        .TryEquip(this, equipeable, out _);
                }

                i++;
            }
        }

        protected virtual JToken CaptureCharacterData()
        {
            JObject state = new JObject();

            state["Position"] = VectorToJToken.CaptureVector(transform.position);
            state["Rotation"] = VectorToJToken.CaptureVector(transform.rotation.eulerAngles);

            return state;
        }

        protected virtual void RestoreCharacterData(JToken jToken)
        {
            if (jToken is JObject jObject)
            {
                IDictionary<string, JToken> data = jObject;
                transform.position = data["Position"].ToObject<Vector3>();
                transform.rotation = Quaternion.Euler(data["Rotation"].ToObject<Vector3>());

                mover.CancelAction();
                mover.UpdatePosition();
            }
        }

        protected JObject CaptureEquipment()
        {
            JObject equipmentData = new();

            var equipper = this.inventory as InventoryEquipDecorator;

            if (equipper == null) return equipmentData;

            foreach (var part in Enum.GetValues(typeof(EquipmentType)))
            {
                int partId = (int)part;
                var items = equipper.Equipped.GetItems(partId);

                if (items != null && items.Count > 0)
                {
                    equipmentData[partId.ToString()] = items[0].ID;
                }
            }

            return equipmentData;
        }

        protected void RestoreEquipment(JToken jToken)
        {
            if (jToken is JObject state)
            {
                var equiper = inventory as InventoryEquipDecorator;
                var enumValues = Enum.GetValues(typeof(EquipmentType));

                foreach (var part in enumValues)
                {
                    var items = equiper.Equipped.GetItems((int)part);

                    if (items == null) continue;

                    foreach (var item in items)
                    {
                        equiper.Unequip(this, item);
                    }
                }

                foreach (var part in enumValues)
                {
                    int partId = (int)part;

                    if (!state.ContainsKey(partId.ToString())) continue;

                    var item = inventory.GetItem(state[partId.ToString()].ToObject<int>());

                    equiper.TryEquip(this, item, out _);
                }
            }
        }

        protected JObject CaptureBasicStatus()
        {
            JObject basicStatsData = new();
            Character character = (Character)this;

            basicStatsData["MaxHp"] = ModsList.TryGetRealValue(health.MaxHp, character, ModifiableStat.MaxHP);
            basicStatsData["HP"] = ModsList.TryGetRealValue(health.HP, character, ModifiableStat.HP);
            basicStatsData["Speed"] = ModsList.TryGetRealValue(stats.speed, character, ModifiableStat.Speed);
            basicStatsData["Damage"] = ModsList.TryGetRealValue(stats.damage, character, ModifiableStat.BaseDamage);
            basicStatsData["DamageRate"] = ModsList.TryGetRealValue(stats.damageRate, character, ModifiableStat.GunFireRate);
            basicStatsData["Color"] = VectorToJToken.CaptureVector(stats.color);
            basicStatsData["EyesRadious"] = stats.farDectection;
            basicStatsData["EarsRadious"] = stats.closeDetection;
            basicStatsData["MinDistance"] = ModsList.TryGetRealValue(stats.minDistance, character, ModifiableStat.MinDistance);

            return basicStatsData;
        }

        protected void RestoreBasicStatus(JToken jToken)
        {
            if (jToken is JObject state)
            {
                health.LoadValues(state["HP"].ToObject<int>(), state["MaxHp"].ToObject<int>());
                stats.speed = state["Speed"].ToObject<float>();
                stats.damage = state["Damage"].ToObject<int>();
                stats.damageRate = state["DamageRate"].ToObject<float>();
                stats.color = state["Color"].ToObject<Vector4>();
                stats.farDectection = state["EyesRadious"].ToObject<float>();
                stats.closeDetection = state["EarsRadious"].ToObject<float>();
                stats.minDistance = state["MinDistance"].ToObject<float>();

                SetStats(stats);
            }
        }
        #endregion

        public virtual void Select()
        {
            foreach (Renderer rend in GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in rend.materials)
                {
                    if (!material.shader.name.Contains("Outliner"))
                        continue;

                    material.SetFloat("_Enabled", 1);
                    break;
                }
            }
        }

        public void Deselect()
        {
            foreach (Renderer rend in GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in rend.materials)
                {
                    if (!material.shader.name.Contains("Outliner"))
                        continue;

                    material.SetFloat("_Enabled", 0);
                    break;
                }
            }
        }
    }
}
