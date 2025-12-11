using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    [Serializable]
    public struct CharacterData
    {
        public string className;
        public string characterName;
        public Color color;
        public CharacterType characterType;
        public string enemyTag;
        public bool shouldSave;
        public List<string> drops;
        public Dictionary<ComponentType, CharacterComponent> components;
        public CharacterProgress progress;
        public BasicStats basicStats;
        public string model;
    }

    public class CharacterComponent { }

    [Serializable]
    public class Inventory : CharacterComponent
    {
        public bool addInventory;
        public Dictionary<string, int> items;

        public void FromJson(JObject json)
        {
            addInventory = json["add"].ToObject<bool>();
            var itemsData = json["items"];
            items = new ();

            foreach (var item in itemsData)
            {
                if (item is not JObject o) continue;

                items.Add(o.Properties().First().Name, o.Properties().First().Value.ToObject<int>());
            }
        }
    }

    [Serializable]
    public class Equipment : CharacterComponent
    {
        public string modelPath;
        public List<(string path, EquipmentType type)> spawnPoints;
        public Dictionary<string, EquipData> equipment;

        public Equipment()
        {
            equipment = new();
            spawnPoints = null;
        }
    }

    [Serializable]
    public struct EquipData
    {
        public ElementType type;
        public EquipmentType place;
        public bool equipped;
    }

    [Serializable]
    public class Health : CharacterComponent
    {
        public int HP;
        public int MaxHP;
    }
}
