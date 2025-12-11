using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class PlayerManagerSample : PlayerManager
    {
        public event Action OnFormationChanged;

        protected override void Awake()
        {
            var mainInventory = GetComponent<Inventory.Inventory>();
            MainInventory = mainInventory;

            base.Awake();
            (playerController as PlayerControllerSample).OnFormationChanged += ChangeFormation;
        }

        private void ChangeFormation(Vector2 value, object args)
        {
            Formation formation = value switch
            {
                { y: 1 } => Formation.Follow,
                { y: -1 } => Formation.LockTarget,
                { x: -1 } => Formation.Protect,
                { x: 1 } => Formation.Free,
                _ => Formation.None,
            };

            players.ForEach((player) =>
            {
                if (player.enabled)
                {
                    player.SetFormation(formation, args);
                }
            });

            curFormation = (formation, args);
            OnFormationChanged?.Invoke();
        }

        protected override void SetUpPlayer(AIGuildMember player)
        {
            base.SetUpPlayer(player);
            SetColor(player);
            (player.Inventory as InventoryEquipDecorator).SetInventory((Inventory.Inventory)MainInventory);
        }

        private void SetColor(AIGuildMember member)
        {
            if (customization.DefaultColors.Length <= 0) return;

            Color? newColor = default;

            foreach (var color in customization.DefaultColors)
            {
                bool hasColor = false;

                foreach (var player in players)
                {
                    if (player.stats.color == color)
                    {
                        hasColor = true;
                        break;
                    }
                }

                if (!hasColor)
                {
                    newColor = color;
                    break;
                }
            }

            if (!newColor.HasValue) return;

            member.stats.color = newColor.Value;

            //List<Color> usedColors = new();

            //checkedPlayers.ForEach(p => usedColors.AddVariable(p.statsList.Color));

            //var availableColors = (from color in playerColors where !usedColors.Contains(color) select color).ToList();

            //var selectedColorIdx = UnityEngine.Random.Range(0, availableColors.MaxCount);

            //member.statsList.Color = playerColors[selectedColorIdx];
        }

        public override void SetPlayerControl(int idx)
        {
            base.SetPlayerControl(idx);
            MainInventory = GetComponent<Inventory.Inventory>();
        }
    }
}
