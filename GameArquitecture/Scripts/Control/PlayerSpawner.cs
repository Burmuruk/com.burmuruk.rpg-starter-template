using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.Utilities;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class PlayerSpawner : ActivationObject
    {
        private void Awake()
        {
            if (TemporalSaver.TryLoad(_id, out object data))
                Enabled = (bool)data;
        }
    }
}
