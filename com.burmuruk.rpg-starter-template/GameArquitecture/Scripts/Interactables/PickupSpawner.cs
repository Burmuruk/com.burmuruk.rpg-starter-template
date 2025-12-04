using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Interaction
{
    public class PickupSpawner : MonoBehaviour, IJsonSaveable
    {
        [SerializeField] ItemsList list;
        [SerializeField] Dictionary<GameObject, PickupItemData> items = new();
        int id;

        class PickupItemData
        {
            public Vector3 position;
            public Quaternion rotation;
            [HideInInspector] public bool picked;
            public Pickup pickup;
            public int id;
        }

        public int ID => id == 0 ? id = GetHashCode() : id;

        public void RegisterCurrentItems()
        {
            var pickups = FindObjectsOfType<Pickup>();
            items.Clear();
            int i = 0;

            foreach (var pickup in pickups)
            {
                PickupItemData data = new PickupItemData()
                {
                    position = pickup.transform.position,
                    rotation = pickup.transform.rotation,
                    picked = false,
                    pickup = pickup,
                    id = pickup.ID,
                };

                pickup.OnPickedUp += RemoveItem;

                items[pickup.gameObject] = data;
            }
        }

        public JToken CaptureAsJToken(out SavingExecution execution)
        {
            execution = SavingExecution.General;
            JObject state = new JObject();
            List<PickupItemData> pickups = new();
            int i = 0;

            foreach (var item in items)
            {
                JObject itemState = new JObject();

                itemState["Id"] = item.Value.id;
                itemState["Position"] = VectorToJToken.CaptureVector(item.Value.position);
                itemState["Rotation"] = VectorToJToken.CaptureVector(item.Value.rotation.eulerAngles);
                itemState["Picked"] = item.Value.picked;

                state[i++.ToString()] = itemState;
                item.Value.pickup.OnPickedUp += RemoveItem;
            }

            return state;
        }

        public void LoadAsJToken(JToken jToken)
        {
            if (!(jToken is JObject state)) return;
            items.Clear();
            int i = 0;

            DestroyLastItems();

            GameObject parent = new GameObject("Pickups");
            parent.transform.position = Vector3.zero;

            while (state.ContainsKey(i.ToString()))
            {
                if (state[i.ToString()]["Picked"].ToObject<bool>())
                {
                    i++;
                    continue;
                }

                var curItemState = state[i.ToString()];

                var itemData = new PickupItemData()
                {
                    position = curItemState["Position"].ToObject<Vector3>(),
                    rotation = Quaternion.Euler(curItemState["Rotation"].ToObject<Vector3>()),
                    picked = false,
                    id = curItemState["Id"].ToObject<int>(),
                };

                var item = list.Get(curItemState["Id"].ToObject<int>());
                Pickup inst = Instantiate(item.Pickup, itemData.position, itemData.rotation, parent.transform);

                itemData.pickup = inst;
                items[inst.gameObject] = itemData;

                inst.OnPickedUp += RemoveItem;
                i++;
            }

            //DestroyLastItems();
        }

        private bool DestroyLastItems()
        {
            var pickups = FindObjectsOfType<Pickup>();

            if (pickups == null || pickups.Length == 0) return false;

            for (int i = 0; i < pickups.Length; i++)
            {
                Destroy(pickups[i].gameObject);
            }

            //foreach (var item in items)
            //{
            //    var prefab = list.Get(item.Key).Prefab;

            //    Instantiate(prefab, item.Value.position, item.Value.rotation);
            //}

            return true;
        }
        public void AddItem(InventoryItem item, Vector3 pos)
        {
            var pickup = Instantiate(item.Pickup, pos, Quaternion.identity, transform);
            var itemData = new PickupItemData()
            {
                position = pos,
                rotation = pickup.transform.rotation,
                picked = false,
                pickup = pickup
            };

            items.Add(pickup.gameObject, itemData);
        }

        private void RemoveItem(GameObject itemToRemove)
        {
            if (!itemToRemove.TryGetComponent<Pickup>(out Pickup pickup))
                return;

            var itemData = items[itemToRemove];

            itemData.picked = true;
            items[itemToRemove] = itemData;
        }
    }
}
