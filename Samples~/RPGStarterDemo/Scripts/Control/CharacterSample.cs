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

        public override void Select()
        {
            base.Select();

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
    }
}
