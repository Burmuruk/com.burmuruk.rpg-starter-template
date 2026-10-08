using Burmuruk.RPGStarterTemplate.Control;
using System;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public abstract class EquipableItem : InventoryItem
    {
        int maxCount;
        List<Character> characters;

        public event Action<Character, EquipableItem> OnUnequiped;

        public int MaxCount => maxCount;
        public bool IsEquip => characters.Count > 0;
        public List<Character> Characters
        {
            get
            {
                characters ??= new List<Character>();
                characters.RemoveAll(character => character == null);

                return characters;
            }
        }

        public EquipableItem(params Character[] characters)
        {
            if (characters.Length > 0)
                this.characters = new List<Character>(characters);
            else
                this.characters = new List<Character>();
        }

        public EquipableItem(int count, params Character[] characters) : this(characters)
        {
            this.maxCount = count;
        }

        public abstract object GetEquipLocation();

        public virtual void Equip(Character character)
        {
            if (character == null)
                return;

            if (!characters.Contains(character))
                characters.Add(character);
        }

        public virtual void Unequip(Character character)
        {
            if (characters.Remove(character))
                OnUnequiped?.Invoke(character, this);
        }
    }
}
