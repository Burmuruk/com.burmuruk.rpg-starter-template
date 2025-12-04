using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.AI
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] CharacterProgress progress;
        [SerializeField] Inventory.Inventory inventory;
        [SerializeField] List<EnemyGroup> m_enemies = new();

        [Serializable]
        public struct EnemyGroup
        {
            [SerializeField] Transform transform;
            [SerializeField] List<AIEnemyController> enemies;
            [SerializeField] bool canBeRespawned;
            public State GroupState { get; private set; }
            public List<AIEnemyController> Enemies { get => enemies; }
            public int Id { get; private set; }

            public enum State
            {
                None,
                Defeated,
                Inactive,
            }

            public event Action OnGroupDefeated;

            public void RespawnEnemies()
            {
                if (!canBeRespawned) return;

                foreach (var enemy in enemies)
                {

                    enemy.gameObject.SetActive(true);
                }
            }
        }

        private void Start()
        {
            inventory = FindObjectOfType<LevelManager>().gameObject.GetComponent<Inventory.Inventory>();

            if (inventory == null || m_enemies == null) return;

            foreach (var group in m_enemies)
            {
                foreach (var enemy in group.Enemies)
                {
                    var stats = progress.GetDataByLevel(enemy.CharacterType, 0);
                    if (stats.HasValue)
                        enemy.SetStats(stats.Value);
                    else
                        enemy.SetDefaultStats();

                    (enemy.Inventory as InventoryEquipDecorator).SetInventory(inventory);
                    enemy.SetUpMods();
                }
            }
        }

        public void EnableGroup(int id)
        {

        }
    }
}
