using Burmuruk.RPGStarterTemplate.Control;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public abstract class EquipeableItem : InventoryItem
    {
        int maxCount;
        List<Character> characters;

        public event Action<Character, EquipeableItem> OnUnequiped;

        public int MaxCount { get => maxCount; }
        public bool IsEquip { get => characters.Count > 0; }
        public List<Character> Characters
        {
            get
            {
                if (characters != null && characters.Count > 0 && characters[0] == null)
                {
                    characters.Clear();
                }

                return characters;
            }
        }

        public EquipeableItem(params Character[] characters)
        {
            if (characters.Length > 0)
                this.characters = new List<Character>(characters);
            else
                this.characters = new List<Character>();
        }

        public EquipeableItem(int count, params Character[] characters) : this (characters)
        {
            this.maxCount = count;
        }

        public abstract object GetEquipLocation();

        public virtual void Equip(Character character)
        {
            characters.Add(character);
        }

        public virtual void Unequip(Character character)
        {
            characters.Remove(character);
            OnUnequiped?.Invoke(character, this);
        }
    }
}
