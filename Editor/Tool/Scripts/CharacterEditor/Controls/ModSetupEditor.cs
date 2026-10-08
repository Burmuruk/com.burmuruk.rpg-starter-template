using Burmuruk.RPGStarterTemplate.Editor.Saving;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ModSetupEditor
    {
        private const string MethodName = "SetUpMods";

        public static List<ModEntry> ExtractAllMods(string scriptText)
        {
            var registry = SavingSystem.LoadEnumRegistry();
            return ModStatReferences.Read(scriptText).Select(call => new ModEntry
            {
                VariableName = call.VariableName,
                ModifiableStat = registry.GetName<ModifiableStat>(
                    ModStatReferences.ResolveId(call.Value, registry))
            }).ToList();
        }

        public static string AddMods(string scriptText, List<ModEntry> newMods)
        {
            if (newMods.Count == 0)
                return scriptText;

            string code = ModStatReferences.MaskTrivia(scriptText);
            var methodMatch = Regex.Match(code, $@"\bvoid\s+{MethodName}\s*\(\s*\)\s*\{{");
            if (!methodMatch.Success)
                throw new InvalidOperationException($"Cannot find {MethodName} to add buff registrations.");
            int depth = 1;
            int close = methodMatch.Index + methodMatch.Length;
            for (; close < code.Length && depth > 0; close++)
            {
                if (code[close] == '{')
                    depth++;
                else if (code[close] == '}')
                    depth--;
            }
            if (depth != 0)
                throw new InvalidOperationException($"Incomplete {MethodName} body.");
            close--;
            string additions = string.Empty;

            var registry = SavingSystem.LoadEnumRegistry();
            var existing = new HashSet<string>(ModStatReferences.Read(scriptText).Select(c => c.VariableName));
            foreach (var entry in newMods)
            {
                if (!existing.Add(entry.VariableName))
                    continue;
                var option = registry.GetEntry(typeof(ModifiableStat), entry.ModifiableStat);
                if (option == null || option.Id == EnumRegistry.NoneId)
                    throw new InvalidOperationException($"Invalid buff stat: {entry.ModifiableStat}");
                string floatCast = entry.isFloat ? "" : "(int)";
                var newLine = $"            ModsList.AddVariable((Character)this, (ModifiableStat){option.Id}, () => stats.{entry.VariableName}, (value) => {{ stats.{entry.VariableName} = {floatCast}value; }});";
                additions += newLine + "\n";
            }

            if (additions.Length == 0)
                return scriptText;
            int lineStart = scriptText.LastIndexOf('\n', close);
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            int insertion = string.IsNullOrWhiteSpace(scriptText.Substring(lineStart, close - lineStart))
                ? lineStart : close;
            return scriptText.Insert(insertion, "\n" + additions);
        }

        public static string RemoveMods(string scriptText, List<string> variableNames)
        {
            var names = new HashSet<string>(variableNames);
            foreach (var call in ModStatReferences.Read(scriptText).OrderByDescending(c => c.Start))
                if (names.Contains(call.VariableName))
                    scriptText = scriptText.Remove(call.Start, call.Length);
            return scriptText;
        }

        public static string RenameModChanges(string scriptText, List<ModChange> changes)
        {
            // Get_Changes can emit both a type change and a name change for one variable.
            var grouped = changes.GroupBy(c => c.OldName).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var call in ModStatReferences.Read(scriptText).OrderByDescending(c => c.Start))
            {
                if (!grouped.TryGetValue(call.VariableName, out var edits))
                    continue;
                int id = (int)edits[0].Type;
                if (id == EnumRegistry.NoneId)
                    scriptText = scriptText.Remove(call.Start, call.Length);
                else
                    scriptText = scriptText.Remove(call.ValueStart, call.ValueLength)
                        .Insert(call.ValueStart, $"(ModifiableStat){id}");
            }

            // A missing NewName means only the buff type changed.
            // Replace in one pass to avoid cascading A->B, B->C renames.
            return Regex.Replace(scriptText, @"\b(?<owner>stats|basicStats)\.(?<name>@?\w+)", match =>
            {
                if (!grouped.TryGetValue(match.Groups["name"].Value, out var edits))
                    return match.Value;
                string newName = edits.LastOrDefault(c => !string.IsNullOrEmpty(c.NewName)).NewName;
                return string.IsNullOrEmpty(newName) ? match.Value : match.Groups["owner"].Value + "." + newName;
            });
        }
    }

    public struct ModEntry
    {
        public string VariableName;
        public string ModifiableStat;
        public bool isFloat;

        public override string ToString() => $"{VariableName} => {ModifiableStat}";
    }

    public struct ModChange
    {
        public string Header;
        public string OldName;
        public string NewName;
        public ModifiableStat Type;
        public VariableType VariableType;
    }
}
