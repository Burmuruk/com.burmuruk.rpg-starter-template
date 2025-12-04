using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Armor", order = 1)]
    public class ArmourElement : EquipeableItem
    {
        [Header("Equipment")]
        [SerializeField] EquipmentType m_bodyPart;
        [Space(), Header("Attributes")]
        [SerializeField] List<ModData> modifications;

        [Serializable]
        struct ModData
        {
            public ModifiableStat ModsStat;
            public float value;
        }

        public override void Equip(Character character)
        {
            base.Equip(character);

            foreach (var mod in modifications)
            {
                ModsList.AddModification(character, mod.ModsStat, mod.value); 
            }
        }

        public override void Unequip(Character character)
        {
            foreach (var mod in modifications)
            {
                ModsList.RemoveModification(character, mod.ModsStat, mod.value);
            }
        }

        public override object GetEquipLocation()
        {
            return m_bodyPart;
        }

        public void UpdateInfo(EquipmentType type)
        {
            m_bodyPart = type;
        }

        public void Copy(ArmourElement original)
        {
            m_bodyPart = original.m_bodyPart;
        }
    }
}
