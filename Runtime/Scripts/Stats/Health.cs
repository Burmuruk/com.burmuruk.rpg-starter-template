using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    public class Health : MonoBehaviour, IJsonSaveable
    {
        [SerializeField] int _hp;
        [SerializeField] int _maxHp;

        public event Action<Transform> OnDied;
        public event Action<int> OnDamaged;

        public int HP { get => _hp; }
        public int MaxHp { get => _maxHp; }
        public bool IsAlive { get => _hp > 0; }

        private void Awake()
        {
            ModsList.AddVariable(GetComponent<Control.Character>(), ModifiableStat.HP, () => _hp, (value) => { _hp = (int)value; });
        }

        public void ApplyDamage(int damage)
        {
            if (!IsAlive) return;

            _hp = Math.Max(_hp - damage, 0);

            if (_hp <= 0)
            {
                Die();
                return;
            }

            OnDamaged?.Invoke(_hp);
        }

        public void Heal(int value)
        {
            _hp = Math.Min(_hp + value, _maxHp);
        }

        private void Die()
        {
            OnDied?.Invoke(transform);
        }

        public JToken CaptureAsJToken(out SavingExecution execution)
        {
            execution = SavingExecution.References;
            return JToken.FromObject(_hp);
        }

        public void LoadAsJToken(JToken state)
        {
            _hp = state.ToObject<int>();
        }

        public void RegisterValues(Control.Character character)
        {
            ModsList.AddVariable(character, ModifiableStat.MaxHP, () => _maxHp, (value) => { _maxHp = (int)value; });
            ModsList.AddVariable(character, ModifiableStat.HP, () => _hp, (value) => { _hp = (int)value; });
        }

        public void LoadValues(int hp, int maxHp)
        {
            _hp = hp;
            _maxHp = maxHp;
        }
    }
}