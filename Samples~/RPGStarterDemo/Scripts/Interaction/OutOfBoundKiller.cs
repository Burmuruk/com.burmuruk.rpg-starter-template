using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Stats;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Interaction.Samples
{
    internal class OutOfBoundKiller : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Health>(out var health))
            {
                if (other.TryGetComponent<AIGuildMember>(out var player) && !player.IsControlled)
                    return;

                health.ApplyDamage(health.MaxHp);
            }
        }
    }
}
