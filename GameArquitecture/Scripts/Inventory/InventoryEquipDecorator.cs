using Burmuruk.RPGStarterTemplate.Control;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    [RequireComponent(typeof(MeshRenderer))]
    public class InventoryEquipDecorator : MonoBehaviour, IInventory
    {
        [SerializeField] List<InitalEquipedItemData> _initialItems;
        [SerializeField] Inventory _inventory;
        [SerializeField] Equipment _equipment;

        [Serializable]
        public struct InitalEquipedItemData
        {
            [SerializeField] InventoryItem _item;
            [SerializeField] int _amount;
            [SerializeField] bool _isEquip;
            [SerializeField, Tooltip("Leave empty to use a Chraracter component attached to this GameObject")] 
            Character _character;

            public int Amount { get => _amount; set => _amount = value; }
            public InventoryItem Item { get => _item; private set => _item = value; }
            public bool IsEquiped {  get => _isEquip; private set => _isEquip = value; }
            public Character Character { get => _character; private set => _character = value; }

            public void Initilize(InventoryItem item, int amount, bool equip, Character character = null)
            {
                Item = item;
                Amount = amount;
                IsEquiped = equip;
                Character = character;
            }
        }

        EquipeableItem _alarmedRemovedItem = default;
        (Character player, EquipeableItem item) _alarmedEquipItem = default;
        public ref Equipment Equipped { get => ref _equipment; }

        public event Action OnTryDeleteEquiped;
        public event Action OnTryAlreadyEquiped;

        private void Start()
        {
            InitInventory();
        }

        public void SetInventory(Inventory inventory) => this._inventory = inventory;

        public bool TryEquip(Character player, InventoryItem item, out List<EquipeableItem> unequippedItems)
        {
            unequippedItems = null;
            if (item == null) return false;

            var equiped = (EquipeableItem)item;
            if (equiped.Characters.Contains(player)) return false;

            _alarmedEquipItem = (player, (EquipeableItem)item);

            if (equiped.IsEquip && !HasAvailableItem(item.ID))
            {
                OnTryAlreadyEquiped?.Invoke();
                return false;
            }

            unequippedItems = UnequipWeaponSlot(player, _alarmedEquipItem.item);
            Equip();
            return true;

            bool HasAvailableItem(in int itemId)
            {
                return _inventory.GetItemCount(itemId) > equiped.Characters.Count;
            }
        }

        private List<EquipeableItem> UnequipWeaponSlot(Character player, EquipeableItem item)
        {
            var equippedItems = player.Equipment.GetItems((int)item.GetEquipLocation());

            if (equippedItems == null || equippedItems.Count <= 0) return null;

            foreach (var equippedItem in equippedItems)
            {
                Unequip(player, equippedItem);
            }

            return equippedItems;
        }

        private void Equip()
        {
            var (player, equipeableItem) = _alarmedEquipItem;
            equipeableItem.Equip(player);

            VerifyBonus(equipeableItem, player);
            UpdateModel(player, equipeableItem);

            _alarmedEquipItem = default;
        }

        private void VerifyBonus(EquipeableItem equipeableItem, Character player)
        {
            var place = equipeableItem.GetEquipLocation();
            //equipment.par
        }

        private void InitInventory()
        {
            if (_initialItems != null)
            {
                foreach (var itemData in _initialItems)
                {
                    if (itemData.Amount <= 0) continue;

                    for (int i = 0; i < itemData.Amount; i++)
                    {
                        Add(itemData.Item.ID); 
                    }

                    if (itemData.IsEquiped)
                    {
                        var character = itemData.Character;
                        if (itemData.Character == null)
                            character = gameObject.GetComponent<Character>();

                        if (character == null) return;

                        TryEquip(character, itemData.Item, out _);
                    }
                }
            }
        }

        private void UpdateModel(Character player, InventoryItem prefab)
        {
            if (prefab is EquipeableItem equipable && equipable != null)
            {
                ItemEquiper.EquipModification(ref player.Equipment, equipable);
            }
        }

        public bool Unequip(Character player, EquipeableItem item)
        {
            if (item is var unequipped && unequipped == null)
                return false;

            if (!item.Characters.Contains(player)) return false;
            
            item.Unequip(player);
            
            ItemEquiper.UnequipModification(ref player.Equipment, unequipped);
            return true;
        }

        public bool Add(int id)
        {
            return _inventory.Add(id);
        }

        public bool Remove(int id)
        {
            var item = _inventory.GetItem(id);

            if (item == null) return false;

            _alarmedRemovedItem = (EquipeableItem)item;

            if (((EquipeableItem)item).IsEquip)
            {
                OnTryDeleteEquiped?.Invoke();
                return false;
            }

            RemoveAlarmedItem();

            return true;
        }

        private void RemoveAlarmedItem()
        {
            if (_alarmedRemovedItem == null) return;

            _inventory.Remove(_alarmedRemovedItem.ID);

            _alarmedRemovedItem = default;
        }

        public InventoryItem GetItem(int id)
        {
            return _inventory.GetItem(id);
        }

        public List<InventoryItem> GetList(ItemType type)
        {
            return _inventory.GetList(type);
        }

        public List<InventoryItem> GetEquipedItems(ItemType itemType, Character character)
        {
            var items = _inventory.GetList(itemType);

            List<InventoryItem> equipedItems = new(); 

            foreach (var item in items)
            {
                var equiped = item as EquipeableItem;
                if (equiped.IsEquip && equiped.Characters.Contains(character))
                {
                    equipedItems.Add(equiped);
                }
            }

            return equipedItems;
        }

        public int GetItemCount(int id)
        {
            return _inventory.GetItemCount(id);
        }
    }
}
