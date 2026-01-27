using Burmuruk.RPGStarterTemplate.Control.AI;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Interaction.Samples
{
    internal class EnemiesTrainCameraTrigger : MonoBehaviour
    {
        [SerializeField] private AIEHordeDistance chiefEnemy;

        private void Start()
        {
            chiefEnemy.OnTroopsDeployed += HandleTroopsDeployed;
        }

        private void HandleTroopsDeployed()
        {
            GetComponent<Interactable>()?.Interact();
        }
    }
}
