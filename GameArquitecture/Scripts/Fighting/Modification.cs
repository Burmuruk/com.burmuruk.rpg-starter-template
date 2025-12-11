using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Combat
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/WeaponMod", order = 3)]
    public class Modification : EquipeableItem
    {
        [Header("Equipment")]
        [SerializeField] EquipmentType equipmentPlace;
        [Space(), Header("Attributes")]
        [SerializeField] ModData[] mods;

        [Serializable]
        private struct ModData
        {
            public float amount;
            public ModifiableStat modifiableStat;
        }

        public override object GetEquipLocation()
        {
            throw new System.NotImplementedException();
        }

        public override void Equip(Character character)
        {
            base.Equip(character);
        }

        public override void Unequip(Character character)
        {
            base.Unequip(character);
        }
    }
}
