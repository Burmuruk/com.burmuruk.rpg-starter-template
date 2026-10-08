using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Combat.Samples
{
    public class FighterSample : Fighter
    {
        [SerializeField] Animator animator;

        public override void Initilize(InventoryEquipDecorator inventory, Func<BasicStats> stats)
        {
            base.Initilize(inventory, stats);

            animator = GetComponent<Animator>();
        }

        protected override void StartBasicAttack(EquipableItem equipable)
        {
            if (animator == null)
            {
                base.StartBasicAttack(equipable);
                return;
            }

            SetAttackTrigger(equipable);
        }

        private void SetAttackTrigger(EquipableItem equipable)
        {
            if (equipable is Weapon weapon)
            {
                string typeName = weapon.MinDistance switch
                {
                    < 5 => "CloseRange",
                    _ => "LongRange"
                };

                animator.SetTrigger(typeName);

                string typeWeapon =
                    weapon.Name.Contains("Bow") ? "CrossBow" : "Gun";

                animator.SetTrigger(typeWeapon);

                string triggerName =
                    (WeaponType)weapon.GetSubType() switch
                    {
                        WeaponType.Gun => "GunAttack",
                        _ => "MeleeAttack"
                    };

                animator.SetTrigger(triggerName);
            }
            else
            {
                animator.SetTrigger("UnarmedAttack");
            }
        }

        // Animation Event
        public void AnimationEvent_Hit()
        {
            Hit();
        }
    }
}