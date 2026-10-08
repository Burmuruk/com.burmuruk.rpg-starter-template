using Burmuruk.RPGStarterTemplate.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Stats.Samples
{
    [Serializable]
    public class BuffVFXEntry
    {
        public VisualBuff visual;
        public GameObject prefab;
    }

    public class BuffVFXSample : MonoBehaviour
    {
        [SerializeField]
        private List<BuffVFXEntry> effects;

        private Dictionary<VisualBuff, GameObject> lookup;
        private readonly Dictionary<Guid, GameObject> activeVFX = new();

        private void Awake()
        {
            lookup = effects.ToDictionary(
                k => k.visual,
                v => v.prefab);
        }

        private void OnEnable()
        {
            Invoke(nameof(BindEvents), 0.2f);
        }

        private void BindEvents()
        {
            BuffsManager.Instance.OnEffectApplied += OnBuffApplied;
            BuffsManager.Instance.OnEffectRemoved += OnBuffRemoved;
        }

        private void OnDisable()
        {
            if (BuffsManager.Instance == null)
                return;

            BuffsManager.Instance.OnEffectApplied -= OnBuffApplied;
            BuffsManager.Instance.OnEffectRemoved -= OnBuffRemoved;
        }

        private void OnBuffApplied(Guid id, Character character, BuffData buff)
        {
            TryGetVFX(buff, out GameObject prefab);

            GameObject instance = Instantiate(prefab, character.transform);

            activeVFX[id] = instance;
        }

        private void OnBuffRemoved(Guid id, Character character, BuffData buff)
        {
            if (!activeVFX.TryGetValue(id, out GameObject vfx))
                return;

            Destroy(vfx);
            activeVFX.Remove(id);
        }

        private bool TryGetVFX(BuffData buff, out GameObject prefab)
        {
            if (buff.visual == null)
            {
                prefab = null;
                return false;
            }

            return lookup.TryGetValue(buff.visual, out prefab);
        }
    }
}
