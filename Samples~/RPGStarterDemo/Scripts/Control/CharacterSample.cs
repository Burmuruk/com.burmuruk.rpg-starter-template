using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class CharacterSample : Character
    {
        Animator animator;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (animator != null)
                animator.SetInteger("Health", health.HP);
        }

        protected override void GetComponents()
        {
            base.GetComponents();

            animator ??= GetComponent<Animator>();
        }
    }
}
