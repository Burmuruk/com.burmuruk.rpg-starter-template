using Burmuruk.RPGStarterTemplate.Saving;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public class Inventory : MonoBehaviour, IInventory, ISaveable
    {
        [Header("Status")]
        [SerializeField] ItemsList m_ItemsList;
        [SerializeField] InventoryItem[] startingItems;

        int id;
        bool isPersistentData = false;

        Dictionary<int, (InventoryItem item, int count)> m_owned = new();

        public int ID { get => id; }
        public bool IsPersistentData { get => isPersistentData; }

        private void Awake()
        {
            id = GetHashCode();
            Initialize();
        }

        public object CaptureState()
        {
            return m_owned;
        }
        public void RestoreState(object args)
        {
            m_owned = (Dictionary<int, (InventoryItem item, int count)>)args;
        }

        public virtual bool Add(int id)
        {
            if (m_owned.ContainsKey(id))
            {
                if (m_owned[id].count >= m_owned[id].item.Capacity)
                    return false;

                m_owned[id] = (
                    m_owned[id].item,
                    m_owned[id].count + 1);
            }
            else
            {
                if (m_ItemsList.Get(id) is var d && d == null)
                    return false;

                m_owned.Add(id, (d, 1));
            }

            return true;
        }

        public virtual bool Remove(int id)
        {
            if (m_owned[id].count > 1)
                m_owned[id] = (m_owned[id].item, m_owned[id].count - 1);
            else
                m_owned.Remove(id);

            return true;
        }

        public List<InventoryItem> GetList(ItemType type)
        {
            return (from inventoryItem in m_owned.Values
                    where inventoryItem.item.Type == type
                    select inventoryItem.item).ToList();
        }

        public InventoryItem GetItem(int id)
        {
            if (m_owned.ContainsKey(id))
            {
                return m_owned[id].item;
            }

            return null;
        }

        public int GetItemCount(int id)
        {
            if (m_owned.ContainsKey(id))
            {
                return m_owned[id].count;
            }

            return 0;
        }

        private void Initialize()
        {
            if (startingItems == null || startingItems.Length == 0)
                return;

            foreach (var item in startingItems)
            {
                Add(item.ID);
            }
        }
    }
}
