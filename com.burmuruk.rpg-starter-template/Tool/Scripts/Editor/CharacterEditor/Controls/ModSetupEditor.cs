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
            var mods = new List<ModEntry>();

            var methodMatch = Regex.Match(scriptText, $@"(?<=void\s+{MethodName}\s*\(\)\s*\{{)(.*?)(?=\}}[^\)])", RegexOptions.Singleline);
            if (!methodMatch.Success) return mods;

            var methodBody = methodMatch.Groups[1].Value;

            var matches = Regex.Matches(methodBody,
                @"ModsList\.AddVariable\(\(Character\)this,\s*ModifiableStat\.([a-zA-Z0-9_]+),\s*\(\)\s*=>\s*(.*?),.*?value\s*?\)?\s*?=>",
                RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count >= 3)
                {
                    mods.Add(new ModEntry
                    {
                        ModifiableStat = match.Groups[1].Value.Trim(),
                        VariableName = match.Groups[2].Value.Trim().Split('.').Last().Replace(";", "")
                    });
                }
            }

            return mods;
        }

        public static string AddMods(string scriptText, List<ModEntry> newMods)
        {
            if (newMods.Count == 0) return scriptText;

            var methodMatch = Regex.Match(scriptText, $@"(void\s+{MethodName}\s*\(\)\s*\{{)(.*?)(?=\}}[^\)])", RegexOptions.Singleline);
            if (!methodMatch.Success) return scriptText;

            var methodStart = methodMatch.Groups[1].Value.TrimEnd();
            var bodyLines = methodMatch.Groups[2].Value.Split("\r\n");
            var methodBody = string.Join("\r\n", bodyLines.Where(line => !string.IsNullOrWhiteSpace(line)));
            var methodEnd = methodMatch.Groups[3].Value.TrimStart();

            foreach (var entry in newMods)
            {
                var newLine = $"            ModsList.AddVariable((Character)this, ModifiableStat.{entry.ModifiableStat}, () => stats.{entry.VariableName}, (value) => {{ stats.{entry.VariableName} = value; }});";
                bool containsLine = false;

                foreach (var line in bodyLines)
                {
                    if (line.Contains(entry.ModifiableStat) && line.Contains(entry.VariableName))
                    {
                        containsLine = true;
                        break;
                    }
                }

                if (!containsLine)
                    methodBody += "\r\n" + newLine;
            }

            return scriptText.Replace(methodMatch.Value, methodStart + "\r\n" + methodBody + "\r\n" + methodEnd);
        }

        public static string RemoveMods(string scriptText, List<string> variableNames)
        {
            if (variableNames.Count == 0) return scriptText;

            var methodMatch = Regex.Match(scriptText, $@"(void\s+{MethodName}\s*\(\)\s*\{{)(.*?)(?=\}}[^\)])", RegexOptions.Singleline);
            if (!methodMatch.Success) return scriptText;

            var methodStart = methodMatch.Groups[1].Value;
            var methodBody = methodMatch.Groups[2].Value;
            var methodEnd = methodMatch.Groups[3].Value;

            var lines = methodBody.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("//"))
                {
                    filteredLines.Add(line);
                    continue;
                }

                bool containsAny = false;
                foreach (var variable in variableNames)
                {
                    if (Regex.IsMatch(line, $@"\b{Regex.Escape(variable)}\b"))
                    {
                        containsAny = true;
                        break;
                    }
                }

                if (!containsAny)
                    filteredLines.Add(line);
            }

            return scriptText.Replace(methodMatch.Value, methodStart + "\r\n" + string.Join("\r\n", filteredLines) + "\r\n" + methodEnd);
        }

        public static string RenameModChanges(string scriptText, List<ModChange> changes)
        {
            if (changes.Count == 0) return scriptText;
            string result = scriptText;

            foreach (ModChange change in changes)
            {
                result = Regex.Replace(result, $@"\b{Regex.Escape(change.OldName)}\b", change.NewName); 
            }

            return result;
        }
    }

    public struct ModEntry
    {
        public string VariableName;
        public string ModifiableStat;

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
