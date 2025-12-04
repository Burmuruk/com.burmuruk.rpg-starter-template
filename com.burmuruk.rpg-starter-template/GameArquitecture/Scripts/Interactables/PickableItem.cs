using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Saving;
using System;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Interaction
{
    public class PickableItem : MonoBehaviour
    {
        [SerializeField] InventoryItem item;

        public event Action<GameObject> OnPickedUp;

        public InventoryItem PickUp()
        {
            OnPickedUp?.Invoke(gameObject);
            return item;
        }
    }

    public interface IInteractable
    {
        void Interact();
    }
}
