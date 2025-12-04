using Burmuruk.RPGStarterTemplate.Control;
using System;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    public static class ModsList
    {
        static Dictionary<Character, BuffData> buffs = new();
        public struct BuffVariables
        {
            public Func<float> get;
            public Action<float> set;
            public List<float> modifications;

            public BuffVariables(Func<float> get, Action<float> set)
            {
                this.get = get;
                this.set = set;
                this.modifications = new();
            }
        }

        private struct BuffData
        {
            public Character Character;
            public Dictionary<ModifiableStat, BuffVariables> mods;

            public List<float> this[ModifiableStat type] => mods[type].modifications;

            public BuffData(Character character, ModifiableStat modsType, Func<float> get, Action<float> set)
            {
                this.Character = character;
                mods = new()
                {
                    { modsType, new BuffVariables(get, set) }
                };
            }

            public void AddModification(ModifiableStat stat, float modification)
            {
                mods[stat].modifications.Add(modification);
                mods[stat].set(mods[stat].get() + modification);
            }

            public void RemoveModification(ModifiableStat type, float modification)
            {
                for (int i = 0; i < mods[type].modifications.Count; i++)
                {
                    if (mods[type].modifications[i] == modification)
                    {
                        UpdateResult(type, modification);
                        mods[type].modifications.RemoveAt(i);
                        break;
                    }
                }

            }
            private void UpdateResult(ModifiableStat type, float modification)
            {
                mods[type].set(mods[type].get() - modification);
            }

            public void RemoveVariable(ModifiableStat type, Action<float> resultAssigner)
            {
                int i = 0;
                while (mods[type].modifications.Count > 0)
                {
                    RemoveModification(type, mods[type].modifications[i]);
                }
            }

            public void RemoveAllMods(Character character)
            {
                foreach (var key in mods.Keys)
                {
                    float totalAdded = 0;

                    foreach (var mod in mods[key].modifications)
                    {
                        totalAdded += mod;
                    }

                    mods[key].modifications.Clear();
                    mods[key].set(mods[key].get() - totalAdded);
                }
            }
        }

        public static void AddVariable(Character character, ModifiableStat modType, Func<float> getter, Action<float> setter)
        {
            if (!buffs.ContainsKey(character))
            {
                buffs.Add(character, new BuffData(character, modType, getter, setter));
            }
            else if (!buffs[character].mods.ContainsKey(modType))
            {
                buffs[character].mods.Add(modType, new BuffVariables(getter, setter));
            }
        }

        public static void RemoveVariable(Character character, ModifiableStat modsStat)
        {
            if (!buffs.ContainsKey(character)) return;


            buffs[character].mods.Remove(modsStat);
        }

        public static bool AddModification(Character character, ModifiableStat modsStat, float modification)
        {
            if (!buffs.ContainsKey(character) || !buffs[character].mods.ContainsKey(modsStat))
                return false;

            buffs[character].AddModification(modsStat, modification);
            return true;
        }

        public static bool RemoveModification(Character character, ModifiableStat modsStat, float modificaiton)
        {
            if (!buffs.ContainsKey(character) || !buffs[character].mods.ContainsKey(modsStat))
                return false;

            buffs[character].RemoveModification(modsStat, modificaiton);
            return true;
        }

        public static void RemoveAllModifications(Character character)
        {
            buffs[character].RemoveAllMods(character);
        }

        public static bool RemoveCharacter(Character character)
        {
            buffs.Remove(character);
            return true;
        }

        public static float TryGetRealValue(float value, Character character, ModifiableStat stat)
        {
            if (!buffs.ContainsKey(character) || !buffs[character].mods.ContainsKey(stat)) return value;

            List<float> mods = buffs[character][stat];

            mods.ForEach(mod => value -= mod);

            return value;
        }
    }

    public enum ModifiableStat
    {
        None,
        HP,
        Speed,
        BaseDamage,
        GunDamage,
        GunFireRate,
        MinDistance
    }

    public enum ModsType
    {
        None,
        Sum,
        Percentage,
    }
}
