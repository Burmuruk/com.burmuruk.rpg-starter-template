using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Combat
{
    public class Fighter : MonoBehaviour
    {
        [SerializeField] float detectionRadious = 8;

        Func<BasicStats> m_Stats;
        Health m_targetHealth;
        InventoryEquipDecorator m_inventory;
        Movement.Movement m_movement;
        AbilitiesManager habManager;

        Transform m_target;
        CoolDownAction cdBasicAttack;

        bool canAttack = true;
        bool autoAttack = false;

        BasicStats Stats => m_Stats.Invoke();

        public int Damage => Stats.damage;

        private void Update()
        {
            if (autoAttack)
                BasicAttack();
        }

        public virtual void Initilize(InventoryEquipDecorator inventory, Func<BasicStats> stats) 
        {
            m_inventory = inventory;
            m_Stats = stats;

            float rate = Stats.damageRate;
            cdBasicAttack = new CoolDownAction(in rate);

            autoAttack = false;
        }

        public void Pause(bool shouldPause)
        {
            canAttack = !shouldPause;
        }

        public void ResetCombat()
        {
            StopAllCoroutines();

            cdBasicAttack?.Cancel();
            autoAttack = false;
            SetTarget(null);
            canAttack = true;
        }

        public void SetTarget(Transform target)
        {
            if (m_targetHealth != null)
                m_targetHealth.OnDied -= RemoveTarget;

            m_target = target;
            m_targetHealth = target != null ? target.GetComponent<Health>() : null;

            if (m_targetHealth != null)
                m_targetHealth.OnDied += RemoveTarget;
        }

        public void RemoveTarget(Transform target)
        {
            if (m_target != target)
                return;

            SetTarget(null);
        }

        /// <summary>
        /// Attempts to start a basic attack.
        /// </summary>
        public void BasicAttack()
        {
            if (!CanBasicAttack())
                return;

            EquipableItem equipable =
                m_inventory?.Equipped[(int)EquipmentType.WeaponR];

            StartBasicAttack(equipable);
            StartCoroutine(cdBasicAttack.CoolDown());
        }

        /// <summary>
        /// Determines whether a new basic attack can be started.
        /// </summary>
        protected virtual bool CanBasicAttack()
        {
            if (!canAttack ||
                !isActiveAndEnabled ||
                m_target == null ||
                !m_target.gameObject.activeInHierarchy ||
                m_targetHealth == null ||
                !m_targetHealth.IsAlive ||
                cdBasicAttack == null ||
                !cdBasicAttack.CanUse)
            {
                return false;
            }

            return Vector3.Distance(m_target.position, transform.position) <= Stats.minDistance;
        }

        /// <summary>
        /// Starts the basic attack.
        ///
        /// The default implementation hits immediately.
        /// Derived classes can override this method to play
        /// an animation and call Hit() later.
        /// </summary>
        protected virtual void StartBasicAttack(EquipableItem equipable)
        {
            Hit();
        }

        /// <summary>
        /// Applies the effects of the current basic attack.
        /// </summary>
        protected virtual void Hit()
        {
            if (m_targetHealth == null || !m_targetHealth.IsAlive)
            {
                return;
            }

            m_targetHealth.ApplyDamage(Stats.damage);

            EquipableItem equipable = m_inventory?.Equipped[(int)EquipmentType.WeaponR];

            if (equipable is Weapon weapon && BuffsManager.Instance != null)
            {
                if (weapon.TryGetBuffs(out var buffsData))
                {
                    foreach (var buff in buffsData)
                    {
                        BuffsManager.Instance.ApplyEffectIfNotActive(m_target.GetComponent<Character>(), buff, rollProbability: true);
                    }
                }
            }
        }

        /// <summary>
        /// Enables or disables automatic basic attacks.
        /// </summary>
        public void StartAutoBasicAttack(bool start)
        {
            autoAttack = start;
        }

        public void SpecialAttack(AbilityType type)
        {
            var habilities = m_inventory.GetList(ItemType.Ability);

            foreach (var hability in habilities)
            {
                if ((AbilityType)hability.GetSubType() == type)
                {
                    var args = GetSpecialAttackArgs(type);

                    // AbilitiesManager.habilitiesList[
                    //     modifiableStat]?.Invoke(args);

                    return;
                }
            }
        }

        private object GetSpecialAttackArgs(AbilityType type) =>
            type switch
            {
                AbilityType.Dash => m_movement.CurDirection,
                AbilityType.StealHealth => m_target,
                _ => null
            };
    }
}