using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public static class TemporalSaver
    {
        static Dictionary<int, object> data;

        public static void Save(int id, object args)
        {
            data ??= new();
            data[id] = args;
        }

        public static bool TryLoad(int id, out object args)
        {
            args = null;

            if (data == null) { return false; }

            if (data.ContainsKey(id))
            {
                args = data[id];
                return true;
            }

            return false;
        }

        public static void RemoveAllData()
        {
            data?.Clear();
        }
    }
}
