using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    public class Pickup : MonoBehaviour
    {
        [SerializeField] public InventoryItem inventoryItem;
        [SerializeField] public GameObject prefab;

        public event Action<GameObject> OnPickedUp;

        public int ID { get => inventoryItem.ID; }
        public GameObject Prefab { get => prefab; set => prefab = value; }

        public int PickUp()
        {
            OnPickedUp?.Invoke(gameObject);
            Invoke("DestroyItem", .1f);
            return ID;
        }

        private void DestroyItem()
        {
            Destroy(gameObject);
        }
    }
}