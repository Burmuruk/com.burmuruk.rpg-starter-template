using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    // Written by the stage editor. This is runtime migration data, not EnumRegistry.
    public static class SavingExecutionAliases
    {
        private const string MigrationJson = @"{}";
        public static Dictionary<string, string> GetMappings() =>
            JsonConvert.DeserializeObject<Dictionary<string, string>>(MigrationJson)
            ?? new Dictionary<string, string>();

        public static JObject NormalizeSlot(JObject source)
        {
            var aliases = GetMappings();
            if (aliases.Count == 0)
                return source;
            var result = (JObject)source.DeepClone();
            if (result["SaveFormatVersion"] != null)
            {
                if (result["Stages"] is JObject stages)
                    result["Stages"] = NormalizeStages(stages, aliases);
                return result;
            }

            // Original format: global stage keys plus entity-ID -> stage keys.
            var stageNames = new HashSet<string>(Enum.GetNames(typeof(SavingExecution)));
            var globals = new JObject();
            foreach (var entry in source)
            {
                if (entry.Key == "SlotData")
                    continue;
                if (stageNames.Contains(entry.Key) || aliases.ContainsKey(entry.Key))
                {
                    globals[entry.Key] = entry.Value.DeepClone();
                    result.Remove(entry.Key);
                }
                else if (entry.Value is JObject entity)
                    result[entry.Key] = NormalizeStages(entity, aliases);
            }
            foreach (var entry in NormalizeStages(globals, aliases))
                result[entry.Key] = entry.Value.DeepClone();
            return result;
        }

        private static JObject NormalizeStages(JObject source, Dictionary<string, string> aliases)
        {
            var result = new JObject();
            foreach (var entry in source)
            {
                string name = aliases.TryGetValue(entry.Key, out var target) ? target : entry.Key;
                if (name == null)
                    continue; // Deleted, explicitly without a replacement.
                if (result[name] == null)
                    result[name] = entry.Value.DeepClone();
                else
                    Merge(result[name], entry.Value, name);
            }
            return result;
        }

        private static void Merge(JToken target, JToken source, string path)
        {
            if (JToken.DeepEquals(target, source))
                return;
            if (target is JObject into && source is JObject from)
            {
                foreach (var entry in from)
                {
                    if (into[entry.Key] == null)
                        into[entry.Key] = entry.Value.DeepClone();
                    else
                        Merge(into[entry.Key], entry.Value, path + "/" + entry.Key);
                }
                return;
            }
            throw new InvalidOperationException(
                $"Conflicting saved data after merging stages at '{path}'. Choose distinct stages or migrate that save explicitly.");
        }
    }
}
