using System;
using System.Collections.Generic;
using Burmuruk.RPGStarterTemplate.Control;

namespace Burmuruk.RPGStarterTemplate.Stats
{
    public static class ModsList
    {
        private sealed class Variable
        {
            public Func<float> Get;
            public Action<float> Set;
            public float BaseValue;
            public float LastWritten;
            public readonly Dictionary<Guid, float> Modifications = new Dictionary<Guid, float>();

            // Preserve external additive changes, while retaining fractions lost by int setters.
            public void Sync() { float current = Get(); BaseValue += current - LastWritten; LastWritten = current; }
            public void Write()
            {
                float result = BaseValue;
                foreach (float delta in Modifications.Values)
                    result += delta;
                Set(result);
                LastWritten = Get();
            }
        }

        private static readonly Dictionary<Character, Dictionary<ModifiableStat, Variable>> variables =
            new Dictionary<Character, Dictionary<ModifiableStat, Variable>>();

        private static bool Find(Character character, ModifiableStat stat, out Variable variable)
        {
            variable = null;
            return !ReferenceEquals(character, null) && variables.TryGetValue(character, out var stats)
                && stats.TryGetValue(stat, out variable);
        }

        public static void AddVariable(Character character, ModifiableStat modType,
            Func<float> getter, Action<float> setter)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            if (getter == null)
                throw new ArgumentNullException(nameof(getter));

            if (setter == null)
                throw new ArgumentNullException(nameof(setter));

            if (!variables.TryGetValue(character, out var stats))
                variables.Add(character, stats = new Dictionary<ModifiableStat, Variable>());

            if (stats.ContainsKey(modType))
                return;

            float value = getter();
            stats.Add(modType, new Variable { Get = getter, Set = setter, BaseValue = value, LastWritten = value });
        }

        public static bool IsRegistered(Character character, ModifiableStat stat) => Find(character, stat, out _);

        public static bool TryGetValue(Character character, ModifiableStat stat, out float value)
        {
            value = 0;

            if (!Find(character, stat, out var variable))
                return false;

            value = variable.Get();
            return true;
        }

        public static bool TryGetBaseValue(Character character, ModifiableStat stat, out float value)
        {
            value = 0;

            if (!Find(character, stat, out var variable))
                return false;

            variable.Sync();
            value = variable.BaseValue;
            return true;
        }

        // Permanent change: never added to the reversible modifier list.
        public static bool ApplyChange(Character character, ModifiableStat stat, float delta)
        {
            if (!Find(character, stat, out var variable))
                return false;

            variable.Sync();
            variable.BaseValue += delta;
            variable.Write();
            return true;
        }

        public static bool AddModification(Character character, ModifiableStat modsStat, float modification)
            => AddModification(character, modsStat, modification, out _);

        public static bool AddModification(Character character, ModifiableStat stat, float modification, out Guid id)
        {
            id = Guid.Empty;

            if (!Find(character, stat, out var variable))
                return false;

            variable.Sync();
            id = Guid.NewGuid();
            variable.Modifications.Add(id, modification);
            variable.Write();
            return true;
        }

        public static bool RemoveModification(Character character, ModifiableStat stat, Guid id)
        {
            if (!Find(character, stat, out var variable) || !variable.Modifications.ContainsKey(id))
                return false;

            variable.Sync();
            variable.Modifications.Remove(id);
            variable.Write();
            return true;
        }

        public static bool RemoveModification(Character character, ModifiableStat modsStat, float modification)
        {
            if (!Find(character, modsStat, out var variable))
                return false;

            Guid found = Guid.Empty;

            foreach (var pair in variable.Modifications)
            {
                if (pair.Value.Equals(modification))
                { 
                    found = pair.Key; break; 
                }
            }

            return found != Guid.Empty && RemoveModification(character, modsStat, found);
        }

        public static void RemoveAllModifications(Character character)
        {
            if (ReferenceEquals(character, null) || !variables.TryGetValue(character, out var stats))
                return;

            foreach (var variable in stats.Values)
            {
                variable.Sync();
                variable.Modifications.Clear();
                variable.Write();
            }
        }

        public static void RemoveVariable(Character character, ModifiableStat modsStat)
        {
            if (!Find(character, modsStat, out var variable))
                return;

            variable.Sync();
            variable.Modifications.Clear();
            variable.Write();
            variables[character].Remove(modsStat);
        }

        public static bool RemoveCharacter(Character character)
        {
            if (ReferenceEquals(character, null))
                return false;

            if (character != null)
                RemoveAllModifications(character);

            return variables.Remove(character);
        }

        public static float TryGetRealValue(float value, Character character, ModifiableStat stat)
            => TryGetBaseValue(character, stat, out float result) ? result : value;
    }

    public enum ModifiableStat
    {
        None = 0,
        HP = 1,
        MaxHP = 2,
        Speed = 3,
        BaseDamage = 4,
        GunDamage = 5,
        GunFireRate = 6,
        MinDistance = 7,
    }

    public enum ModsType
    {
        None,
        Sum,
        Percentage,
    }
}
