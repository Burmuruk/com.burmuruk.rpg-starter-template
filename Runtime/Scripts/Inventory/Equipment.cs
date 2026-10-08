using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    [Serializable]
    public class Equipment
    {
        [SerializeField] GameObject body;
        [SerializeField] SpawnPointData[] spawnPoints;
        Dictionary<int, (Transform spawnPoint, GameObject item, List<EquipableItem> equipables)> _parts;

        public event Action<int> OnEquipmentChanged;

        public GameObject Body { set => body = value; }

        public EquipableItem this[int part]
        {
            get
            {
                if (_parts == null)
                    Initilize();

                if (_parts.ContainsKey(part) && _parts[part].equipables.Count > 0)
                    return _parts[part].equipables[0];

                return default;
            }
        }

        [Serializable]
        public struct SpawnPointData
        {
            public Transform spawnPoint;
            public int spawnType;
        }

        public void Initilize()
        {
            _parts = new Dictionary<int, (Transform spawnPoint, GameObject item, List<EquipableItem> equipeables)>();

            if (spawnPoints != null)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    _parts[spawnPoint.spawnType] = (spawnPoint.spawnPoint, null, new());
                }
            }

            _parts[0] = (null, body, null);
        }

        public void Equip(int part, GameObject item, params EquipableItem[] equipables)
        {
            if (_parts == null)
                Initilize();

            if (part == 0) return;

            if (!_parts.ContainsKey(part))
                _parts.Add(part, (GetSpawnPoint(part), item, equipables.ToList()));
            else
                _parts[part] = (GetSpawnPoint(part), item, equipables.ToList());
            //OnEquipmentChanged?.Invoke(equipables.ID);
        }

        public Transform GetSpawnPoint(int part)
        {
            if (spawnPoints == null)
                return null;

            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint.spawnType == part)
                    return spawnPoint.spawnPoint;
            };

            return null;
        }

        public GameObject GetItem(int part)
        {
            if (_parts == null)
                Initilize();

            return _parts.ContainsKey(part) ? _parts[part].item : null;
        }

        public List<EquipableItem> GetItems(int part)
        {
            if (_parts == null)
                Initilize();

            return _parts.ContainsKey(part) ? _parts[part].equipables : null;
        }

        private Transform GetSpawnPoints(int part)
        {
            foreach (var point in spawnPoints)
            {
                if (point.spawnType == part)
                    return point.spawnPoint;
            }

            return null;
        }

        public void ClearPart(int part)
        {
            if (_parts == null)
                Initilize();

            if (part == 0)
                return;

            _parts[part] = (GetSpawnPoint(part), null, new List<EquipableItem>()
            );
        }
    }
}