using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Combat
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Weapon", order = 1)]
    public class Weapon : EquipeableItem, IBuffUser
    {
        [Header("Equipment")]
        [SerializeField] EquipmentType m_bodyPart;
        [Space(), Header("Settings")]
        [SerializeField] WeaponType weaponType;
        [SerializeField] int m_damage;
        [SerializeField] float m_rateDamage;
        [SerializeField] float m_minDistance;
        [SerializeField] float m_maxDistance;
        [SerializeField] float reloadTime;
        [SerializeField] int maxAmmo;
        [Space(), Header("Buffs")]
        [SerializeField] BuffData[] _buffs;
        [Space(), Header("Modifications")]
        [SerializeField] Equipment equipment;

        public int Damage { get => m_damage; }
        public float DamageRate { get => m_rateDamage; }
        public float MinDistance { get => m_minDistance; }
        public float MaxDistance { get => m_maxDistance; }
        public EquipmentType BodyPart { get => m_bodyPart; }
        public int MaxAmmo { get => maxAmmo; }
        public int Ammo { get; private set; }
        public float ReloadTime { get => reloadTime; }
        public BuffData[] Buffs { get => _buffs; }

        public bool TryGetBuff(out BuffData? buffData)
        {
            buffData = null;

            if (_buffs == null || _buffs.Length == 0) return false;

            int idx = Random.Range(0, _buffs.Length);

            if (_buffs[idx].probability == 1)
            {
                buffData = _buffs[idx];
                return true;
            }

            float probability = Random.Range(0, 1.0f);

            if (probability <= _buffs[idx].probability)
            {
                buffData = _buffs[idx];
                return true;
            }
            else return false;
        }

        public void UpdateBuffData(BuffData[] buffData) =>
            _buffs = buffData;

        public override object GetEquipLocation()
        {
            return m_bodyPart;
        }

        public override void Equip(Character character)
        {
            base.Equip(character);
            ModsList.AddModification(character, ModifiableStat.BaseDamage, m_damage);
            ModsList.AddModification(character, ModifiableStat.GunFireRate, m_rateDamage);
            ModsList.AddModification(character, ModifiableStat.MinDistance, m_minDistance);
        }

        public override void Unequip(Character character)
        {
            base.Unequip(character);

            ModsList.RemoveModification(character, ModifiableStat.BaseDamage, m_damage);
            ModsList.RemoveModification(character, ModifiableStat.GunFireRate, m_rateDamage);
            ModsList.RemoveModification(character, ModifiableStat.MinDistance, m_minDistance);
        }

        public void UpdateInfo(EquipmentType bodyPart, WeaponType subType, int damage, float rateDamage, float minDistance, float maxDistance,
                float reloadTime, int maxAmmo, BuffData[] data)
        {
            (m_bodyPart, weaponType, m_damage, m_rateDamage, m_minDistance, m_maxDistance, this.reloadTime, this.maxAmmo, _buffs) =
                (bodyPart, subType, damage, rateDamage, minDistance, maxDistance, reloadTime, maxAmmo, data);
        }

        public override object GetSubType()
        {
            return weaponType;
        }

        private void EquipMod()
        {

        }

        private void UnequipMod()
        {

        }
    }
}
