using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public static class GlobalIdRegistry
    {
        private static readonly Dictionary<string, UnityEngine.Object> Owners = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Owners.Clear();

        public static bool TryRegister(string id, UnityEngine.Object owner)
        {
            if (string.IsNullOrWhiteSpace(id) || owner == null)
                return false;
            if (Owners.TryGetValue(id, out var existing) && existing != null)
                return existing == owner;
            Owners[id] = owner;
            return true;
        }

        public static void Unregister(string id, UnityEngine.Object owner)
        {
            if (string.IsNullOrEmpty(id))
                return;
            if (Owners.TryGetValue(id, out var existing) && existing == owner)
                Owners.Remove(id);
        }

        public static bool Contains(string id, UnityEngine.Object owner)
        {
            return !string.IsNullOrEmpty(id) && owner != null &&
                Owners.TryGetValue(id, out var existing) && existing == owner;
        }
    }
}
