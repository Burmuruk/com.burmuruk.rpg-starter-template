using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Inventory
{
    [Serializable]
    public struct Equipment
    {
        [SerializeField] GameObject body;
        [SerializeField] SpawnPointData[] spawnPoints;
        Dictionary<int, (Transform spawnPoint, GameObject item, List<EquipeableItem> equipables)> _parts;

        public event Action<int> OnEquipmentChanged;

        public GameObject Body { set => body = value; }

        public EquipeableItem this[int part]
        {
            get
            {
                if (_parts == null)
                    Initilize();

                return _parts.ContainsKey(part) ? _parts[part].equipables[0] : default;
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
            _parts = new Dictionary<int, (Transform spawnPoint, GameObject item, List<EquipeableItem> equipeables)>()
            {
                { 0, (null, body, null) }
            };
        }

        public void Equip(int part, GameObject item, params EquipeableItem[] equipables)
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

        public List<EquipeableItem> GetItems(int part)
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
    }
}