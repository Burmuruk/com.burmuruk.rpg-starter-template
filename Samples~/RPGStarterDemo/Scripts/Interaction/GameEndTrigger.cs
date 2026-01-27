using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Control.Samples;
using Burmuruk.RPGStarterTemplate.Stats;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Interaction.Samples
{
    internal class GameEndTrigger : MonoBehaviour
    {
        int totalEnemies;

        private void Start()
        {
            var enemies = FindObjectsOfType<AIEnemyController>(true);
            totalEnemies = enemies != null ? enemies.Length : 0;

            foreach (var enemy in enemies)
            {
                enemy.GetComponent<Health>().OnDied += (_) => CheckEenmies();
            }
        }

        public void CheckEenmies()
        {
            --totalEnemies;

            if (totalEnemies <= 0)
            {
                FindObjectOfType<LevelManagerSample>()?.EndGame();
            }
        }
    }
}
