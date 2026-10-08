using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.RPGStarterTemplate.Utilities;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Movement.Samples
{
    public class MovementSample : Movement
    {
        protected Animator animator;

        public override void Initialize(InventoryEquipDecorator inventory, ActionScheduler scheduler, Func<BasicStats> stats)
        {
            base.Initialize(inventory, scheduler, stats);

            animator = GetComponent<Animator>();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (animator != null)
                animator.SetFloat("Speed", _rb.velocity.magnitude);
        }
    }
}
