using System;
using System.Collections.Generic;
using System.Linq;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    internal static class EnumScheduler
    {
        private static UIListScheduler<Type, EnumModificationData> scheduler;

        public static void Add(ModificationTypes modificationType, Type key, IUIListContainer<EnumModificationData> container)
        {
            scheduler ??= new();
            scheduler.AddContainer(modificationType, key, container);
        }

        public static void ChangeData(ModificationTypes modificationType, Type key)
        {
            scheduler.ChangeData(modificationType, key, default);
        }
    }

    internal static class CreationScheduler
    {
        public static Func<ElementType, Dictionary<string, string>> creationsNames;
        private static UIListScheduler<ElementType, BaseCreationInfo> scheduler;

        public static void Add(ModificationTypes modificationType, ElementType key, IUIListContainer<BaseCreationInfo> container)
        {
            scheduler ??= new();
            scheduler.AddContainer(modificationType, key, container);
        }

        public static void ChangeData(ModificationTypes modificationType, ElementType key, string id, BaseCreationInfo data)
        {
            var names = GetNames(key);

            scheduler.ChangeData(modificationType, key, in data);
        }

        public static Dictionary<string, string> GetNames(ElementType type)
        {
            return creationsNames(type);
        }
    }

    internal class UIListScheduler<T, U> where U : struct
    {
        Dictionary<ModificationTypes, Dictionary<T, List<IUIListContainer<U>>>> modifiers = new()
        {
            { ModificationTypes.Add, new() },
            { ModificationTypes.Remove, new() },
            { ModificationTypes.EditData, new() },
            { ModificationTypes.Rename, new() },
        };

        public void AddContainer(ModificationTypes modificationType, T key, IUIListContainer<U> container)
        {
            if (!modifiers[modificationType].ContainsKey(key))
                modifiers[modificationType].Add(key, new List<IUIListContainer<U>>());

            if (!(from c in modifiers[modificationType][key]
                  where c == container
                  select c).Any())
            {
                modifiers[modificationType][key].Add(container);
            }
        }

        public void ChangeData(ModificationTypes modificationType, T key, in U data)
        {
            if (modificationType == ModificationTypes.None)
                return; 

            foreach (ModificationTypes mod in Enum.GetValues(typeof(ModificationTypes)))
            {
                if ((modificationType & mod) == 0 || !modifiers[mod].ContainsKey(key))
                    continue;

                if (modifiers[mod].ContainsKey(key))
                {
                    MakeChange(data, mod, key);
                }
            }
        }

        private void MakeChange(in U data, ModificationTypes modificationType, T key)
        {
            foreach (var modifier in modifiers[modificationType][key])
            {
                switch (modificationType)
                {
                    case ModificationTypes.Remove:
                        modifier.RemoveData(in data);
                        break;
                    case ModificationTypes.Add:
                        modifier.AddData(in data);
                        break;
                    case ModificationTypes.EditData:
                        modifier.EditData(in data);
                        break;
                    case ModificationTypes.Rename:
                        modifier.RenameCreation(in data);
                        break;
                }
            }
        }
    }

    internal interface IUIListContainer<U> where U : struct
    {
        public virtual void AddData(in U newValue) { }

        public virtual void RenameCreation(in U newValue) { }

        public virtual void RemoveData(in U newValue) { }

        public virtual void EditData(in U newValue) { }
    }
}
