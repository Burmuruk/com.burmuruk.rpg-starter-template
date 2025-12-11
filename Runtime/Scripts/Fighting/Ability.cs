using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Combat
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Ability", order = 2)]
    public class Ability : EquipeableItem, IUsable
    {
        [Header("Attributes")]
        [SerializeField] AbilityType type;
        [SerializeField] float speed;
        [SerializeField] int mode;
        [SerializeField] bool isHuman = true;
        [SerializeField] float coolDown;

        public int Mode { get => mode; }
        public bool IsHuman { get => isHuman; }
        public EquipmentType BodyPart => EquipmentType.None;
        public float CoolDown { get => coolDown; }

        public void Remove(Character stats)
        {
            throw new System.NotImplementedException();
        }

        public void Use(Character character, object args, Action callback)
        {
            AbilitiesManager.habilitiesList[type].Invoke(character, args, callback);
        }

        public override object GetSubType()
        {
            return Convert.ChangeType(type, typeof(AbilityType));
        }

        public override object GetEquipLocation()
        {
            return EquipmentLocation.None;
        }
    }
}
