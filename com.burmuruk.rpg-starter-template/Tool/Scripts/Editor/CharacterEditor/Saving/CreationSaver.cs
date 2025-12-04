using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Burmuruk.RPGStarterTemplate.Inventory.Equipment;
using static Burmuruk.RPGStarterTemplate.Inventory.InventoryEquipDecorator;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class CreationSaver
    {
        const string BASE_PICKUP_PATH = "Assets/com.burmuruk.rpg-starter-template/GameArquitecture/Game/Prefabs/Pickables/PickUpBase.prefab";
        const string ITEMS_LIST_NAME = "GeneralItemsList.asset";
        const string PROGRESS_NAME = "CharactersProgress.asset";
        const string ASSET_EXTENSION = ".asset";
        const string RESULT_PATH = "RPGResults";
        const string ITEMS_FOLDER = "Items";
        const string ARMOUR_FOLDER = "Armour";
        const string PICKUPS_FOLDER = "Pickups";
        const string CHARACTERS_FOLDER = "Characters";
        const string WEAPONS_FOLDER = "Weapons";
        ItemsList _itemsList;
        List<GameObject> garbage = new();
        CharacterProgress _progress;

        CharacterProgress Progress
        {
            get
            {
                if (_progress == null)
                {
                    _progress = AssetDatabase.LoadAssetAtPath<CharacterProgress>(Path + "/" + PROGRESS_NAME);

                    if (_progress == null)
                    {
                        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CharacterProgress>(), Path + "/" + PROGRESS_NAME);
                        AssetDatabase.Refresh();
                        _progress = AssetDatabase.LoadAssetAtPath<CharacterProgress>(Path + "/" + PROGRESS_NAME);
                    }
                }

                return _progress;
            }
        }

        ItemsList ItemsList
        {
            get
            {
                if (_itemsList == null)
                {
                    _itemsList = AssetDatabase.LoadAssetAtPath<ItemsList>(Path + "/" + ITEMS_LIST_NAME);

                    if (_itemsList == null)
                    {
                        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ItemsList>(), Path + "/" + ITEMS_LIST_NAME);
                        AssetDatabase.Refresh();
                        _itemsList = AssetDatabase.LoadAssetAtPath<ItemsList>(Path + "/" + ITEMS_LIST_NAME);
                    }
                }

                return _itemsList;
            }
        }
        public string Path
        {
            get
            {
                var resultsPath = PlayerPrefs.GetString("CreationPath");

                if (string.IsNullOrEmpty(resultsPath) || !AssetDatabase.IsValidFolder(resultsPath))
                {
                    if (!AssetDatabase.IsValidFolder(resultsPath))
                    {
                        string defaultPath = "Assets";
                        AssetDatabase.CreateFolder(defaultPath, RESULT_PATH);
                        AssetDatabase.Refresh();
                    }
                    else
                        return resultsPath;

                    if (!AssetDatabase.IsValidFolder(resultsPath))
                        return null;
                }

                return resultsPath;
            }
        }

        private bool VerifyFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(Path + "/" + path))
            {
                AssetDatabase.CreateFolder(Path, path);
                AssetDatabase.Refresh();
            }

            return AssetDatabase.IsValidFolder(Path + "/" + path);
        }

        public void SavetItem(InventoryItem item, ItemDataArgs args)
        {
            string subFolder = Get_ItemSubFolder(item);
            if (!VerifyFolder(subFolder)) return;

            string itemPath = Path + "/" + subFolder + "/" + item.Name + ASSET_EXTENSION;
            if (AssetDatabase.LoadAssetAtPath<InventoryItem>(itemPath) != null)
                AssetDatabase.DeleteAsset(itemPath);


            var copy = ScriptableObject.Instantiate(item);
            copy.Pickup = CreatePickUp(item, args, subFolder);
            AssetDatabase.CreateAsset(copy, itemPath);
            AssetDatabase.Refresh();
            var newItem = AssetDatabase.LoadAssetAtPath<InventoryItem>(itemPath);

            ItemsList.AddItem(newItem);
            EditorUtility.SetDirty(ItemsList);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var createdItem = AssetDatabase.LoadAssetAtPath<InventoryItem>(itemPath);
            copy.Pickup.inventoryItem = newItem;
            EditorUtility.SetDirty(copy.Pickup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RemoveGarbage();
            return;
        }

        private Pickup CreatePickUp(InventoryItem item, ItemDataArgs args, string subFolder)
        {
            if (!VerifyFolder(PICKUPS_FOLDER)) return null;

            string pickupPath = Path + "/" + PICKUPS_FOLDER + "/" + item.Name + "PickUp" + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<Pickup>(pickupPath) != null)
                AssetDatabase.DeleteAsset(pickupPath);

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PICKUP_PATH);
            GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            garbage.Add(baseInstance);

            SetPickUpModel(in args, baseInstance);
            GameObject variant = PrefabUtility.SaveAsPrefabAsset(baseInstance, pickupPath);

            PrefabUtility.SetPropertyModifications(variant, PrefabUtility.GetPropertyModifications(baseInstance));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return variant.GetComponent<Pickup>();
        }

        private void SetPickUpModel(in ItemDataArgs args, GameObject parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(args.PickupPath);

            if (model == null) return;
            GameObject modelInstance = GameObject.Instantiate(model, parent.transform);
            garbage.Add(modelInstance);

            var pickup = parent.GetComponent<Pickup>();
            pickup.prefab = model;
        }

        private string Get_ItemSubFolder(InventoryItem item) =>
            item switch
            {
                Weapon => WEAPONS_FOLDER,
                ArmourElement => ARMOUR_FOLDER,
                _ => ITEMS_FOLDER,
            };

        public void SavePlayer(CharacterData data)
        {
            if (!VerifyFolder(CHARACTERS_FOLDER)) return;

            GameObject player = new GameObject("Player", Get_Components(in data));
            garbage.Add(player);

            foreach (var component in data.components)
                Setup_Components(component.Key, player, component.Value, in data);

            player.GetComponent<Rigidbody>().freezeRotation = true;
            Setup_Character(player, in data);
            Set_Progression(in data);

            GameObject instance = PrefabUtility.SaveAsPrefabAsset(player, Path + "/" + CHARACTERS_FOLDER + "/" + data.characterName + ".prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RemoveGarbage();
        }

        private void Set_Progression(in CharacterData data)
        {
            var levelsF = typeof(CharacterProgress).GetField("statsList", BindingFlags.Instance | BindingFlags.NonPublic);

            if (levelsF == null) return;

            var stats = levelsF.GetValue(Progress);
            List<CharacterProgress.CharacterData> oldProgress = null;

            if (stats == null)
                oldProgress = new();
            else
                oldProgress = ((CharacterProgress.CharacterData[])stats).ToList();

            var newProgress = (CharacterProgress.CharacterData[])levelsF.GetValue(data.progress);

            if (newProgress == null) return;

            for (int i = 0; i < newProgress.Length; i++)
            {
                bool hasType = false;

                for (int j = 0; j < oldProgress.Count; j++)
                {
                    if (oldProgress[j].characterType == newProgress[i].characterType)
                    {
                        hasType = true;
                        var oldData = oldProgress[j];
                        oldData.levelData = newProgress[i].levelData;
                        oldProgress[j] = oldData;
                        break;
                    }
                }

                if (!hasType)
                    oldProgress.Add(newProgress[i]);
            }

            levelsF.SetValue(Progress, oldProgress.ToArray());
            EditorUtility.SetDirty(Progress);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void Setup_Character(GameObject player, in CharacterData characterData)
        {
            player.GetComponent<RPGStarterTemplate.Control.Character>();

            var typeF = typeof(RPGStarterTemplate.Control.Character).GetField("characterType", BindingFlags.Instance | BindingFlags.NonPublic);
            if (typeF != null)
                typeF.SetValue(player.GetComponent<RPGStarterTemplate.Control.Character>(), characterData.characterType);

            Setup_Drops(player, characterData);
            Setup_BasicStats(player, characterData);
            Set_DetectionPoints(player, characterData);
            Set_Enemy(player, characterData.enemyTag);
        }

        private void Setup_Drops(GameObject player, CharacterData characterData)
        {
            if (!player.TryGetComponent<RPGStarterTemplate.Control.Character>(out var playerComp)) return;

            Type curType = playerComp.GetType();

            while (curType != typeof(Control.Character))
            {
                FieldInfo field = curType.GetField("itemsToDrop");
                if (field != null)
                {
                    field.SetValue(playerComp, characterData.drops);
                    break;
                }

                curType = curType.BaseType;
            }
        }

        private void Setup_BasicStats(GameObject player, CharacterData characterData)
        {
            player.GetComponent<RPGStarterTemplate.Control.Character>().stats = characterData.basicStats;
        }

        private void Set_Enemy(GameObject player, in string enemyTag)
        {
            AddTag(enemyTag);
            player.tag = enemyTag;
        }

        public void AddTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag)) return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Checks if tag already exists
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(newTag)) return; // Ya existe
            }

            // Adds new tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = newTag;

            tagManager.ApplyModifiedProperties();
            Debug.Log($"Tag \"{newTag}\" añadido exitosamente.");
        }

        private void Set_DetectionPoints(GameObject player, CharacterData characterData)
        {
            var (hasEars, hasEyes) = (false, false);
            int i = 0;
            //while (characterData.progress.GetDataByLevel(characterData.characterType, i++) is var data && data != null)
            //{
            //    if (data.Value.eyesRadious != 0)
            //        hasEyes = true;
            //    if (data.Value.earsRadious != 0)
            //        hasEars = true;
            //}
            if (characterData.basicStats.farDectection != 0)
                hasEyes = true;
            if (characterData.basicStats.closeDetection != 0)
                hasEars = true;

            var eyesF = typeof(RPGStarterTemplate.Control.Character).GetField("farPercept", BindingFlags.Instance | BindingFlags.NonPublic);
            var earsF = typeof(RPGStarterTemplate.Control.Character).GetField("closePercept", BindingFlags.Instance | BindingFlags.NonPublic);
            var hasEyesF = typeof(RPGStarterTemplate.Control.Character).GetField("hasFarPerception", BindingFlags.Instance | BindingFlags.NonPublic);
            var hasEarsF = typeof(RPGStarterTemplate.Control.Character).GetField("hasClosePerception", BindingFlags.Instance | BindingFlags.NonPublic);

            var character = player.GetComponent<RPGStarterTemplate.Control.Character>();
            hasEyesF?.SetValue(character, hasEyes);
            hasEarsF?.SetValue(character, hasEars);

            if (hasEyes)
            {
                Add_DetectionPoint(player, "EyesDetection", out var eyesPoint);
                eyesF?.SetValue(character, eyesPoint.transform);
            }
            if (hasEars)
            {
                Add_DetectionPoint(player, "EarsDetection", out var earsPoint);
                earsF?.SetValue(character, earsPoint.transform);
            }
        }

        private void Add_DetectionPoint(GameObject player, string name, out GameObject detectionPoint)
        {
            detectionPoint = new GameObject(name);
            detectionPoint.transform.parent = player.transform;
            garbage.Add(detectionPoint);
        }

        public void RemoveGarbage() => garbage.ForEach(e => GameObject.DestroyImmediate(e));

        private Type[] Get_Components(in CharacterData characterData)
        {
            List<Type> components = new()
            {
                Type.GetType(characterData.className),
                typeof(CapsuleCollider),
                typeof(Rigidbody),
            };

            if (characterData.shouldSave)
                components.Add(typeof(JsonSaveableEntity));

            bool containsEquipment = characterData.components.ContainsKey(ComponentType.Equipment);

            foreach (var component in characterData.components)
            {
                if (component.Key == ComponentType.None) continue;

                if (component.Key == ComponentType.Inventory && containsEquipment) continue;

                components.Add(Get_ComponentType(component.Key));
            }

            return components.ToArray();
        }

        private void Setup_Components(ComponentType type, GameObject player, object component, in CharacterData character)
        {
            switch (type)
            {
                case ComponentType.Health:
                    Setup_Health(player, (Health)component);
                    break;

                case ComponentType.Inventory:
                    if (character.components.ContainsKey(ComponentType.Equipment))
                        return; // Equipment already handled in Setup_Equipment

                    var inventory = (Inventory)character.components[ComponentType.Inventory];
                    Setup_Inventory(player, inventory);
                    break;

                case ComponentType.Equipment:
                    var inventory2 = (Inventory)character.components[ComponentType.Inventory];
                    Setup_Inventory(player, inventory2);

                    var equipment = (Equipment)character.components[ComponentType.Equipment];
                    Setup_Equipment(player, equipment, (Inventory)character.components[ComponentType.Inventory]);
                    break;

                //case ComponentType.Dialogue:
                //    break;

                //case ComponentType.Patrolling:
                //    break;

                case ComponentType.None:
                    break;

                default:
                    break;
            }
        }

        private Type Get_ComponentType(ComponentType type) =>
            type switch
            {
                ComponentType.Mover => typeof(Movement.Movement),
                ComponentType.Fighter => typeof(Fighter),
                ComponentType.Inventory => typeof(RPGStarterTemplate.Inventory.Inventory),
                ComponentType.Equipment => typeof(RPGStarterTemplate.Inventory.InventoryEquipDecorator),
                ComponentType.Health => typeof(RPGStarterTemplate.Stats.Health),
                _ => null
            };

        private void Setup_Health(GameObject instance, Health health)
        {
            var inventory = instance.GetComponent<RPGStarterTemplate.Stats.Health>();

            FieldInfo maxHealthF = typeof(RPGStarterTemplate.Stats.Health).GetField("_maxHp", BindingFlags.Instance | BindingFlags.NonPublic);
            maxHealthF.SetValue(inventory, health.MaxHP);
            FieldInfo healthF = typeof(RPGStarterTemplate.Stats.Health).GetField("_hp", BindingFlags.Instance | BindingFlags.NonPublic);
            healthF.SetValue(inventory, health.HP);
            //var character = instance.GetComponents<Tesis.Control.Character>();

            //FieldInfo characterHealthF = typeof(Tesis.Control.Character).GetField("maxHp", BindingFlags.Instance | BindingFlags.NonPublic);
            //characterHealthF.SetValue(character, health);
        }

        private void Setup_Inventory(GameObject instance, Inventory inventory)
        {
            if (inventory?.items == null || !inventory.addInventory) return;
            
            if (!instance.TryGetComponent<RPGStarterTemplate.Inventory.Inventory>(out var inventoryComp))
                inventoryComp = instance.AddComponent<RPGStarterTemplate.Inventory.Inventory>();

            FieldInfo initialItemsF = typeof(RPGStarterTemplate.Inventory.Inventory).GetField("startingItems", BindingFlags.Instance | BindingFlags.NonPublic);
            var initalItems = new List<InventoryItem>();

            FieldInfo itemsF = typeof(ItemsList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
            var items = (List<InventoryItem>)itemsF.GetValue(ItemsList);

            foreach (var itemData in inventory.items)
            {
                if (!SavingSystem.Data.TryGetCreation(itemData.Key, out var data, out var type))
                    continue;

                string name = data.Id;

                foreach (var item in items)
                {
                    if (item.name == name)
                    {
                        initalItems.Add(item);

                        break;
                    }
                }
            }

            initialItemsF.SetValue(inventoryComp, initalItems.ToArray());
        }

        private void Setup_Equipment(GameObject instance, Equipment equipment, in Inventory inventory)
        {
            var equipper = instance.GetComponent<InventoryEquipDecorator>();

            FieldInfo initialItemsF = typeof(InventoryEquipDecorator).GetField("_initialItems", BindingFlags.Instance | BindingFlags.NonPublic);
            List<InitalEquipedItemData> initialItems = new();

            FieldInfo itemsF = typeof(ItemsList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
            var items = (List<InventoryItem>)itemsF.GetValue(ItemsList);

            foreach (var itemData in equipment.equipment)
            {
                string name = SavingSystem.Data.creations[itemData.Value.type][itemData.Key].Id;

                foreach (var item in items)
                {
                    if (item.name == name)
                    {
                        var initialItem = new InitalEquipedItemData();
                        int amount = inventory.items[itemData.Key];

                        initialItem.Initilize(item, amount, itemData.Value.equipped);
                        initialItems.Add(initialItem);
                        break;
                    }
                }
            }

            initialItemsF.SetValue(equipper, initialItems);

            SetPlayerModel(instance, equipper, in equipment);
        }


        private void SetPlayerModel(GameObject player, InventoryEquipDecorator inventory, in Equipment equipment)
        {
            var model = SavingSystem.GetAsset<GameObject>(equipment.modelPath);
            var body = GameObject.Instantiate(model, Vector3.zero, Quaternion.identity, player.transform);
            garbage.Add(body);

            Dictionary<string, EquipmentType> names = new();
            equipment.spawnPoints.ForEach(s =>
            {
                names.TryAdd(s.path, s.type);
            });

            var spawnPoints = GetSpawnPoints(body, names);

            FieldInfo bodyF = typeof(RPGStarterTemplate.Inventory.Equipment).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo spawnPointsF = typeof(RPGStarterTemplate.Inventory.Equipment).GetField("spawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo equipmentF = typeof(InventoryEquipDecorator).GetField("_equipment", BindingFlags.Instance | BindingFlags.NonPublic);
            RPGStarterTemplate.Inventory.Equipment newEquipment = new();

            bodyF.SetValue(newEquipment, body);
            spawnPointsF.SetValue(newEquipment, spawnPoints.ToArray());
            equipmentF.SetValue(inventory, newEquipment);
        }

        private List<SpawnPointData> GetSpawnPoints(GameObject model, Dictionary<string, EquipmentType> names)
        {
            var items = new List<SpawnPointData>();

            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name.Key)) continue;

                items.Add(new SpawnPointData()
                {
                    spawnType = (int)name.Value,
                    spawnPoint = model.transform.Find(name.Key)
                });
            }

            //for (int i = 0; i < model.transform.childCount; i++)
            //{
            //    var child = model.transform.GetChild(i);
            //    (string cur, string parent)? key = null;

            //    foreach (var item in names)
            //    {
            //        if (child.transform.name == item.Key.cur)
            //        {
            //            if (child.transform.parent.transform.name == item.Key.parent)
            //            {
            //                key = item.Key;
            //                break;
            //            }
            //        }

            //    }

            //    if (key.HasValue)
            //    {
            //        var data = new SpawnPointData()
            //        {
            //            spawnType = (int)names[key.Value],
            //            spawnPoint = child,
            //        };
            //        items.Add(data);
            //        names.Remove(key.Value);
            //    }

            //    if (child.transform.childCount > 0)
            //        items.AddRange(GetSpawnPoints(child.gameObject, ref names));
            //}

            return items;
        }
    }
}
