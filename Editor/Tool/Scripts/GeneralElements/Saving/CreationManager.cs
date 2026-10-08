using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public static class CreationManager
    {
        public static bool CreateEverything()
        {
            CreationSaver creationSaver = new();

            ElementType[] types = GetTypesInOrder();

            bool elementCreated = false;

            foreach (var creationType in types)
            {
                foreach (var creation in SavingSystem.Data.creations[creationType])
                {
                    switch (creationType)
                    {
                        case ElementType.Item:
                        case ElementType.Armour:
                            {
                                var itemData = creation.Value as ItemCreationData;

                                var item = itemData.Data;
                                var args = itemData.args;

                                var instance = CloneFakeScriptable(item);
                                creationSaver.SavetItem(instance, args);
                                elementCreated = true;
                                break;
                            }

                        case ElementType.Weapon:
                        case ElementType.Consumable:
                            {
                                var buffUserData = creation.Value as BuffUserCreationData;
                                var (buffUser, args) = (buffUserData.Data, buffUserData.Names);

                                ItemDataConverter.Update_BuffsInfo(buffUser as IBuffUser, args);
                                InventoryItem newBuff = CloneFakeScriptable(buffUser);

                                creationSaver.SavetItem(newBuff, args);
                                elementCreated = true;
                                break;
                            }

                        case ElementType.Character:
                            {
                                var data = creation.Value as CharacterCreationData;

                                creationSaver.SavePlayer(data.Data);
                                elementCreated = true;
                                break;
                            }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return elementCreated;
        }

        private static ElementType[] GetTypesInOrder()
        {
            LinkedList<ElementType> types = new();

            bool hasCharacter = false;

            foreach (var type in SavingSystem.Data.creations.Keys)
            {
                if (type == ElementType.Item)
                {
                    types.AddFirst(type);
                }
                else if (type == ElementType.Character)
                {
                    hasCharacter = true;
                }
                else
                {
                    types.AddLast(type);
                }
            }

            if (hasCharacter)
                types.AddLast(ElementType.Character);

            return types.ToArray();
        }

        private static T CloneFakeScriptable<T>(T source) where T : ScriptableObject
        {
            if (ReferenceEquals(source, null))
                return null;

            T clone = (T)ScriptableObject.CreateInstance(source.GetType().FullName);
            Type currentType = source.GetType();

            while (currentType != typeof(ScriptableObject))
            {
                var fields = currentType.GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                foreach (var field in fields)
                    field.SetValue(clone, field.GetValue(source));

                currentType = currentType.BaseType;
            }

            return clone;
        }
    }
}