using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public sealed class SavingStageDraft
    {
        public string OriginalName;
        public string Name;
        public bool Removed;
        public SavingStageDraft Replacement;
        public bool IsSystem => OriginalName == "System" || OriginalName == "Database" || OriginalName == "Manager";
    }

    public static class SavingStageEditor
    {
        private static readonly HashSet<string> Keywords = new((
            "abstract as base bool break byte case catch char checked class const continue decimal default delegate " +
            "do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int " +
            "interface internal is lock long namespace new null object operator out override params private protected " +
            "public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true " +
            "try typeof uint ulong unchecked unsafe ushort using virtual void volatile while")
            .Split(' '));

        public static bool ValidName(string name)
        {
            return name != null &&
            Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$") && !Keywords.Contains(name);
        }

        public static string Mask(string source)
        {
            return Regex.Replace(source,
            @"//[^\r\n]*|/\*[\s\S]*?\*/|@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'",
            m => new string(m.Value.Select(c => c == '\n' || c == '\r' ? c : ' ').ToArray()));
        }

        private static string[] ScriptPaths()
        {
            return AssetDatabase.FindAssets("t:Script")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
            .Distinct().ToArray();
        }

        private static string FindDeclaration(string typeName, IEnumerable<string> paths)
        {
            string[] matches = paths.Where(p => Regex.IsMatch(Mask(File.ReadAllText(p)),
                $@"\b(?:enum|class)\s+{Regex.Escape(typeName)}\b")).ToArray();

            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected one declaration of {typeName}; found {matches.Length}.");

            return matches[0];
        }

        public static Dictionary<string, string> BuildChanges(IReadOnlyList<string> baseline,
            List<SavingStageDraft> drafts)
        {
            var active = drafts.Where(d => !d.Removed).ToList();
            if (active.Count < 3 || active[0].Name != "System" || active[1].Name != "Database" || active[2].Name != "Manager")
                throw new InvalidOperationException("System, Database and Manager must remain first and unchanged.");

            if (active.Any(d => !ValidName(d.Name)) || active.Select(d => d.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != active.Count)
                throw new InvalidOperationException("Stage names must be valid, unique C# identifiers.");

            var historical = SavingExecutionAliases.GetMappings();

            foreach (var row in active)
                if (row.Name != row.OriginalName && (baseline.Contains(row.Name) || historical.ContainsKey(row.Name)))
                    throw new InvalidOperationException($"'{row.Name}' is reserved by an existing or historical stage. Use a new name.");

            var changes = new Dictionary<string, string>();

            foreach (string original in baseline)
            {
                var draft = drafts.Single(d => d.OriginalName == original);
                var seen = new HashSet<SavingStageDraft>();

                while (draft != null && draft.Removed)
                {
                    if (!seen.Add(draft))
                        throw new InvalidOperationException("Circular stage replacements.");

                    draft = draft.Replacement;
                }
                changes.Add(original, draft?.Name);
            }

            return changes;
        }

        public static string RewriteReferences(string source, Dictionary<string, string> changes, string path)
        {
            string code = Mask(source);
            var changed = changes.Where(p => p.Value != p.Key).ToDictionary(p => p.Key, p => p.Value);

            if (changed.Count == 0)
                return source;

            if (Regex.IsMatch(code, @"\busing\s+(?:static\s+[^;]*\bSavingExecution|\w+\s*=\s*[^;]*\bSavingExecution)\s*;"))
                throw new InvalidOperationException($"{path}: replace the SavingExecution alias/static import with explicit SavingExecution.Option references first.");

            foreach (Match literal in Regex.Matches(source, @"//[^\r\n]*|/\*[\s\S]*?\*/|@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'"))
            {
                if (literal.Value.StartsWith("//") || literal.Value.StartsWith("/*"))
                    continue;

                string prefix = source.Substring(Math.Max(0, literal.Index - 2), Math.Min(2, literal.Index));

                if (!(prefix.EndsWith("$") || prefix.EndsWith("$@")))
                    continue;

                foreach (string old in changed.Keys)
                    if (Regex.IsMatch(literal.Value, $@"\bSavingExecution\s*\.\s*{Regex.Escape(old)}\b"))
                        throw new InvalidOperationException($"{path}: move the SavingExecution expression outside the interpolated string before applying.");
            }

            var edits = new List<(int start, int length, string value)>();

            foreach (Match match in Regex.Matches(code, @"\bSavingExecution\s*\.\s*(?<member>@?\w+)"))
            {
                var member = match.Groups["member"];
                string old = member.Value.TrimStart('@');

                if (!changed.TryGetValue(old, out string target))
                    continue;

                if (target == null)
                    throw new InvalidOperationException($"{path}: SavingExecution.{old} is used. Select a replacement before removing it.");
                edits.Add((member.Index, member.Length, target));
            }

            if (edits.Count > 0 && changes.Where(c => c.Value != null).GroupBy(c => c.Value).Any(g => g.Count() > 1) &&
                Regex.IsMatch(code, @"\bswitch\s*\{"))
                throw new InvalidOperationException($"{path}: review the switch expression before merging stages; duplicate arms require a manual edit.");

            foreach (var edit in edits.OrderByDescending(e => e.start))
                source = source.Remove(edit.start, edit.length).Insert(edit.start, edit.value);

            ValidateSwitchLabels(Mask(source), path);

            return source;
        }

        private static void ValidateSwitchLabels(string code, string path)
        {
            var scopes = new Stack<HashSet<string>>();
            scopes.Push(null);
            bool pendingSwitch = false;

            foreach (Match token in Regex.Matches(code, @"\bswitch\b|\{|\}|\bcase\s+(?:(?:global::)?[\w.]+\.)?SavingExecution\s*\.\s*(?<name>\w+)\s*:"))
            {
                if (token.Value == "switch")
                    pendingSwitch = true;

                else if (token.Value == "{")
                {
                    scopes.Push(pendingSwitch ? new HashSet<string>() : null);
                    pendingSwitch = false;
                }
                else if (token.Value == "}")
                {
                    if (scopes.Count > 1)
                        scopes.Pop();
                }
                else
                {
                    var labels = scopes.FirstOrDefault(s => s != null);

                    if (labels != null && !labels.Add(token.Groups["name"].Value))
                        throw new InvalidOperationException($"{path}: replacement would duplicate a switch case. Choose a different stage or merge the case logic manually.");
                }
            }
        }

        public static int Apply(IReadOnlyList<string> baseline, List<SavingStageDraft> drafts)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Apply stages outside Play Mode and compilation.");

            var changes = BuildChanges(baseline, drafts);
            string[] paths = ScriptPaths();
            string enumPath = FindDeclaration("SavingExecution", paths);
            string aliasPath = FindDeclaration("SavingExecutionAliases", paths);
            var original = new Dictionary<string, string>();
            var updated = new Dictionary<string, string>();

            foreach (string path in paths)
            {
                string text = File.ReadAllText(path);

                if (!text.Contains("SavingExecution"))
                    continue;

                string next = RewriteReferences(text, changes, path);

                if (next != text)
                {
                    original[path] = text;
                    updated[path] = next;
                }
            }

            string enumText = updated.TryGetValue(enumPath, out string modified) ? modified : File.ReadAllText(enumPath);
            string enumCode = Mask(enumText);
            var declaration = Regex.Match(enumCode, @"\benum\s+SavingExecution\b[^\{]*\{");
            int start = declaration.Index + declaration.Length;
            int end = enumCode.IndexOf('}', start);

            if (!declaration.Success || end < 0)
                throw new InvalidOperationException("Cannot locate SavingExecution body.");

            string body = enumCode.Substring(start, end - start);
            string[] currentNames = body.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0)
                .Select(v => v.Split('=')[0].Trim()).ToArray();

            if (!currentNames.SequenceEqual(baseline))
                throw new InvalidOperationException("SavingExecution.cs changed outside this window. Cancel and reopen after compilation.");

            string values = string.Join("\n", drafts.Where(d => !d.Removed).Select((d, i) => $"        {d.Name} = {i},"));
            updated[enumPath] = enumText.Substring(0, start) + "\n" + values + "\n    " + enumText.Substring(end);
            original[enumPath] = File.ReadAllText(enumPath);

            var aliases = SavingExecutionAliases.GetMappings();

            foreach (string old in aliases.Keys.ToArray())
                if (aliases[old] != null && changes.TryGetValue(aliases[old], out string target))
                    aliases[old] = target;

            foreach (var change in changes)
                if (change.Key != change.Value)
                    aliases[change.Key] = change.Value;

            string aliasText = File.ReadAllText(aliasPath);
            string literal = JsonConvert.SerializeObject(aliases).Replace("\"", "\"\"");
            var dataPattern = new Regex("private const string MigrationJson = @\"(?:\"\"|[^\"])*\";");

            if (dataPattern.Matches(aliasText).Count != 1)
                throw new InvalidOperationException("Cannot locate runtime migration data.");

            updated[aliasPath] = dataPattern.Replace(aliasText, _ => "private const string MigrationJson = @\"" + literal + "\";");
            original[aliasPath] = aliasText;

            foreach (string path in updated.Keys.ToArray())
            {
                if (updated[path] == original[path])
                {
                    updated.Remove(path);
                    continue;
                }
                
                if (path.StartsWith("Packages/", StringComparison.Ordinal))
                    throw new InvalidOperationException($"{path}: move the package source into Assets before applying (package cache writes are not supported).");

                if ((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
                    throw new InvalidOperationException($"Read-only script: {path}");
            }

            if (updated.Count == 0)
                return 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var file in updated)
                    File.WriteAllText(file.Key, file.Value);
            }
            catch
            {
                foreach (var file in updated)
                    File.WriteAllText(file.Key, original[file.Key]);

                throw;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();

            return updated.Count;
        }
    }
}
