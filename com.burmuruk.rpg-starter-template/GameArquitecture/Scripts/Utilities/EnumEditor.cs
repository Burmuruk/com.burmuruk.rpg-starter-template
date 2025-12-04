using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Utilities
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

        public bool SetValues(string enumName, string filePath, params string[] values)
        {
            if (Application.isPlaying)
            {
                throw new InvalidDataException("Can't continue when running application.");
            }

            if (HasSpecialCharacter(enumName) || values.Any(chr => HasSpecialCharacter(chr)))
            {
                throw new InvalidDataException("Special characteres are not allowed.");
            }

            if (!File.Exists(filePath)) return false;

            var lines = File.ReadAllLines(filePath);

            bool containsEnum = false;
            bool elementAdded = false;

            if (!RewriteFile())
                return false;

            return containsEnum && elementAdded;

            bool RewriteFile()
            {
                bool inValues = false;

                using (var writer = new StreamWriter(filePath))
                {
                    int i = 0;

                    foreach (var line in lines)
                    {
                        if (line.Contains("{") && containsEnum)
                        {
                            if (line.Contains("}") || line.Contains(",")) return false;

                            inValues = true;
                        }
                        else if (line.Contains("}"))
                        {
                            inValues = false;
                        }
                        else if (inValues)
                        {
                            while (i < values.Length)
                            {
                                writer.WriteLine(values[i] + ",");
                                ++i;
                            }

                            continue;
                        }

                        writer.WriteLine(line);

                        if (line.Contains($"enum {enumName}"))
                            containsEnum = true;
                    }
                }

                return true;
            }
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

        public void RecompileScripts()
        {
            CompilationPipeline.RequestScriptCompilation();
        }

        private bool HasSpecialCharacter(string value)
        {
            return value.Any(chr => !char.IsLetterOrDigit(chr));
        }
    }
}
