using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public static class ItemEquiper
    {
        public static void EquipModification(ref Equipment equipment, EquipeableItem item) 
        {
            if (item == null || ((int)item.GetEquipLocation()) is var location && location == 0)
                return;

            Transform spawnPoint = equipment.GetSpawnPoint(location);
            var inst = UnityEngine.Object.Instantiate(item.Prefab, spawnPoint);
            //inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            equipment.Equip(location, inst, item);
        }

        public static void UnequipModification(ref Equipment equipment, EquipeableItem equipable)
        {
            if (equipable == null || ((int)equipable.GetEquipLocation()) is var location && location == 0)
                return;

            var parent = equipment.GetSpawnPoint(location);

            for (int i = 0; i < parent.childCount; i++)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}