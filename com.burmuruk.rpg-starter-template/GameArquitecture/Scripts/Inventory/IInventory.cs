using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public interface IInventory
    {
        public bool Add(int id);
        public bool Remove(int id);
        public List<InventoryItem> GetList(ItemType type);
        public InventoryItem GetItem(int id);
        public int GetItemCount(int id);
    }
}
