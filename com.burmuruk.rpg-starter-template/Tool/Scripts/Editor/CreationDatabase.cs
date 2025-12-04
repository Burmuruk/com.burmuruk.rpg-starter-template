using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Editor.Saving.Json;
using Burmuruk.RPGStarterTemplate.Inventory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class CreationDatabase
    {
        public List<CharacterProfile> characters = new();
        public Dictionary<ElementType, Dictionary<string, CreationData>> creations = new();
        public List<ElementType> defaultElements = new()
        {
            ElementType.Character,
            ElementType.Item,
            ElementType.Consumable,
            ElementType.Weapon,
            ElementType.Armour,
            //ElementType.State,
            ElementType.Buff,
        };

        [Serializable]
        public struct CharacterProfile
        {
            public string name;
        }

        public bool TryGetCreation(string name, ElementType type, out string id)
        {
            id = null;
            if (!creations.ContainsKey(type)) return false;

            foreach (var creation in creations[type])
            {
                if (creation.Value.Id == name)
                {
                    id = creation.Key;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCreation(string id, out CreationData data, out ElementType type)
        {
            data = default;
            type = default;

            foreach (var key in creations.Keys)
            {
                foreach (var creation in creations[key])
                {
                    if (creation.Key == id)
                    {
                        data = creation.Value;
                        type = key;
                        return true;
                    }
                }
            }

            return true;
        }
    }

    [Serializable]
    public class ElementEntry
    {
        public ElementType type;
        public List<NamedData> items = new();
    }

    [Serializable]
    public class NamedData
    {
        public string name;

        [SerializeReference]
        public CreationData data;
    }

    [Serializable]
    public class ItemCreationData : CreationData
    {
        public RPGStarterTemplate.Inventory.InventoryItem Data;
        public ItemDataArgs args;

        public ItemCreationData(string id, RPGStarterTemplate.Inventory.InventoryItem data, ItemDataArgs args) : base(id)
        {
            Data = data;
            this.args = args;
        }

        public override JObject GetJson()
        {
            var status = base.GetJson();
            
            status["itemData"] = JsonSerializerHelper.ConvertDynamicDataToJson(Data.GetType(), Data);
            status["args"] = JsonSerializerHelper.ConvertDynamicDataToJson(args.GetType(), args);
            return status;
        }

        public override void RestoreFromJson(JObject json)
        {
            base.RestoreFromJson(json);
            Data = (InventoryItem)JsonSerializerHelper.FromJson((JObject)json["itemData"]);
            args = (ItemDataArgs)JsonSerializerHelper.FromJson((JObject)json["args"]);
        }
    }

    [Serializable]
    public class BuffUserCreationData : CreationData
    {
        public RPGStarterTemplate.Inventory.InventoryItem Data;
        public BuffsNamesDataArgs Names;

        public BuffUserCreationData(string name, RPGStarterTemplate.Inventory.InventoryItem item, BuffsNamesDataArgs args) : base(name)
        {
            Data = item;
            Names = args;
        }

        public override JObject GetJson()
        {
            var status = base.GetJson();

            status["itemData"] = JsonSerializerHelper.ConvertDynamicDataToJson(Data.GetType(), Data);
            status["buffData"] = JsonSerializerHelper.ConvertDynamicDataToJson(Names.GetType(), Names);

            return status;
        }

        public override void RestoreFromJson(JObject json)
        {
            base.RestoreFromJson(json);

            Data = (RPGStarterTemplate.Inventory.InventoryItem)JsonSerializerHelper.FromJson((JObject)json["itemData"]);
            Names = (BuffsNamesDataArgs)JsonSerializerHelper.FromJson((JObject)json["buffData"]);
        }
    }

    [Serializable]
    public class CharacterCreationData : CreationData
    {
        public CharacterData Data;

        public CharacterCreationData(string name, CharacterData data) : base(name)
        {
            Data = data;
        }

        public override JObject GetJson()
        {
            var status = base.GetJson();
            status["CharacterData"] = JsonSerializerHelper.ConvertDynamicDataToJson(Data.GetType(), Data);

            return status;
        }

        public override void RestoreFromJson(JObject json)
        {
            base.RestoreFromJson(json);

            Data = (CharacterData)JsonSerializerHelper.FromJson((JObject)json["CharacterData"]);
        }
    }

    public enum ElementType
    {
        None,
        Component,
        Item,
        Character,
        Buff,
        //Mod,
        //State,
        Ability,
        Creation,
        Weapon,
        Armour,
        Consumable,
    }

    public enum ComponentType
    {
        None,
        Health,
        Fighter,
        Mover,
        Flying,
        Inventory,
        Equipment,
        Dialogue,
        //Patrolling,
    }
}