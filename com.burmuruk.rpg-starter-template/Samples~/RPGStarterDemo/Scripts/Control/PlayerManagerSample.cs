using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class PlayerManagerSample : PlayerManager
    {
        public event Action OnFormationChanged;

        protected override void Awake()
        {
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
    }
}
