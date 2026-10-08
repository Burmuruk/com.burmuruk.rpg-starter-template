using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public static class ItemEquiper
    {
        public static void EquipModification(ref Equipment equipment, EquipableItem item)
        {
            if (equipment == null || item == null)
                return;

            if (((int)item.GetEquipLocation()) is var location && location == 0)
                return;

            Transform spawnPoint = equipment.GetSpawnPoint(location);

            if (spawnPoint == null)
            {
                Debug.LogWarning($"There is no mounting point for {item.name} " + $"(part {location}).");
                return;
            }

            if (item.Prefab == null)
            {
                Debug.LogWarning($"The object {item.name} does not have a prefab assigned.");
                return;
            }

            var currentModel = equipment.GetItem(location);
            var currentItems = equipment.GetItems(location);

            if (currentModel != null && currentItems != null && currentItems.Contains(item))
            {
                currentModel.transform.SetParent(spawnPoint, false);
                currentModel.SetActive(true);
                return;
            }

            if (currentModel != null)
            {
                currentModel.SetActive(false);
                Object.Destroy(currentModel);
            }

            var instance = Object.Instantiate(item.Prefab, spawnPoint);
            instance.transform.localRotation = Quaternion.identity;

            equipment.Equip(location, instance, item);
        }

        public static void UnequipModification(ref Equipment equipment, EquipableItem item)
        {
            if (equipment == null || item == null)
                return;

            int location = (int)item.GetEquipLocation();
            if (location == 0)
                return;

            var currentItems = equipment.GetItems(location);

            if (currentItems == null || !currentItems.Contains(item))
                return;

            var currentModel = equipment.GetItem(location);

            equipment.ClearPart(location);

            if (currentModel != null)
            {
                currentModel.SetActive(false);
                Object.Destroy(currentModel);
            }
        }
    }
}