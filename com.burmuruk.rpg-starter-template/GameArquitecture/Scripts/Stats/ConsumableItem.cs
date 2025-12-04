using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using UnityEngine;
using Character = Burmuruk.RPGStarterTemplate.Control.Character;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Consumable", order = 3)]
    public class ConsumableItem : EquipeableItem, IUsable, IBuffUser
    {
        [Space(), Header("Attributes")]
        [SerializeField] BuffData[] _buffs;
        [SerializeField] float consumptionTime;
        [SerializeField] float areaRadious;

        //public int Value { get => value; }
        public float ConsumptionTime { get => consumptionTime; }
        public float AreaRadious { get => areaRadious; }
        public BuffData[] Buffs { get => _buffs; }

        public override object GetEquipLocation()
        {
            return EquipmentLocation.Items;
        }

        public override object GetSubType()
        {
            if (_buffs != null && _buffs.Length > 0)
                return _buffs[0].stat;

            return ModifiableStat.None;
        }

        public void UpdateInfo(BuffData[] buffs, float consumptionTime, float areaRadious)
        {
            (this._buffs, this.consumptionTime, this.areaRadious) =
            (buffs, consumptionTime, areaRadious);
        }

        public virtual void Use(Character character, object args, Action callback)
        {
            foreach (var buff in _buffs)
            {
                if (buff.stat == ModifiableStat.HP)
                {
                    if (buff.value < 0)
                    {
                        BuffsManager.Instance.AddBuff(character, buff, () => character.Health.ApplyDamage((int)buff.value));
                    }
                    else
                        BuffsManager.Instance.AddBuff(character, buff, () => character.Health.Heal((int)buff.value));
                }
                else
                {
                    BuffsManager.Instance.AddBuff(character, buff);
                }
            }

            callback?.Invoke();
        }

        public void UpdateBuffData(BuffData[] buffData) => _buffs = buffData;
    }

    public interface IPickable
    {
        void Use();
    }

    public interface IItem
    {
        void Use();
    }
}
