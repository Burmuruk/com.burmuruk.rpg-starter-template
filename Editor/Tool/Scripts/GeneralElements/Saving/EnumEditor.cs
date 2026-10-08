using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public class EnumEditor
    {
        public bool AddValue(string enumName, string filePath, string value)
        {
            if (Application.isPlaying || !File.Exists(filePath)) return false;

            if (HasSpecialCharacter(enumName) || HasSpecialCharacter(value))
                return false;

            string text = File.ReadAllText(filePath);

            string find = @"(?<=\benum\b\s+" + enumName + @"\s*?{(?s).*)(?'lastVal'\w+)\s*?,?(?=\s*?})";
            string repleace = @"${lastVal}," + "\r\n\t" + value + ",";
            string result = Regex.Replace(text, find, repleace);

            File.WriteAllText(filePath, result);
            return true;
        }

        public bool SetValues(
    string enumName,
    string filePath,
    IEnumerable<EnumEntry> entries)
        {
            if (Application.isPlaying)
                throw new InvalidDataException(
                    "Can't modify enums while playing.");

            if (!File.Exists(filePath))
                return false;

            EnumEntry[] values = entries
                .OrderBy(x => x.Order)
                .ToArray();

            string text = File.ReadAllText(filePath);

            Match enumMatch = Regex.Match(
                text,
                $@"\benum\s+{Regex.Escape(enumName)}\b");

            if (!enumMatch.Success)
                return false;

            int openBraceIndex =
                text.IndexOf('{', enumMatch.Index + enumMatch.Length);

            if (openBraceIndex < 0)
                return false;

            int closeBraceIndex =
                FindClosingBrace(text, openBraceIndex);

            if (closeBraceIndex < 0)
                return false;

            string indentation =
                GetIndentation(text, openBraceIndex);

            string entryIndentation =
                indentation + "\t";

            string newValues = string.Join(
                System.Environment.NewLine,
                values.Select(entry =>
                    $"{entryIndentation}{entry.Name} = {entry.Id},"));

            string newEnumBody =
                "{" +
                System.Environment.NewLine +
                newValues +
                System.Environment.NewLine +
                indentation +
                "}";

            string result =
                text.Substring(0, openBraceIndex) +
                newEnumBody +
                text.Substring(closeBraceIndex + 1);

            File.WriteAllText(filePath, result);

            return true;
        }

        private static int FindClosingBrace(
    string text,
    int openBraceIndex)
        {
            int depth = 0;

            for (int i = openBraceIndex; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '{':
                        depth++;
                        break;

                    case '}':
                        depth--;

                        if (depth == 0)
                            return i;

                        break;
                }
            }

            return -1;
        }

        private static string GetIndentation(
            string text,
            int position)
        {
            int lineStart =
                text.LastIndexOf('\n', position);

            lineStart =
                lineStart < 0
                    ? 0
                    : lineStart + 1;

            int index = lineStart;

            while (index < text.Length &&
                   (text[index] == ' ' ||
                    text[index] == '\t'))
            {
                index++;
            }

            return text.Substring(
                lineStart,
                index - lineStart);
        }

        public bool Rename(string filePath, string enumName, string oldName, string newName)
        {
            if (Application.isPlaying)
            {
                throw new InvalidDataException("Can't continue when running application.");
            }

            string text = File.ReadAllText(filePath);
            string find = @"(?<=\benum\b\s+" + enumName + @"\s*?{.*)" + oldName + @"\s*?,?(?=.*?})";
            string repleace = newName + ",";
            string result = Regex.Replace(text, find, repleace, RegexOptions.Singleline);

            File.WriteAllText(filePath, result);

            return true;
        }

        public bool RemoveOption(string filePath, string optionName)
        {
            if (Application.isPlaying)
            {
                throw new InvalidDataException("Can't continue when running application.");
            }
            if (!File.Exists(filePath)) return false;

            var lines = File.ReadAllLines(filePath);
            string lowerName = char.ToLower(optionName[0]) + optionName.Substring(1, optionName.Length - 1);
            string upperName = char.ToUpper(optionName[0]) + optionName.Substring(1, optionName.Length - 1);

            Regex lowRegex = new Regex(lowerName);
            Regex upperRegex = new Regex(upperName);

            using (var writer = new StreamWriter(filePath))
            {
                foreach (var line in lines)
                {
                    if (lowRegex.IsMatch(line) || upperRegex.IsMatch(line))
                        continue;

                    writer.WriteLine(line);
                }
            }

            return true;
        }

        private bool HasSpecialCharacter(string value)
        {
            return value.Any(chr => !char.IsLetterOrDigit(chr));
        }
    }
}
