using System.IO;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class VariablesAdder
    {
        string path;

        public VariablesAdder(string path)
        {
            this.path = path;
        }

        public bool Modify(string filePath, (VariableType type, string name)[] values, out string error)
        {
            error = "";

            if (Application.isPlaying)
            {
                error = "Can't proceed when the application is running.";
                return false;
            }

            string[] lines = File.ReadAllLines(filePath);

            bool inVariables = false;
            bool added = false;

            using (var writer = new StreamWriter(filePath))
            {
                foreach (var line in lines)
                {
                    if (added)
                    {
                        writer.WriteLine(line);
                        continue;
                    }
                    else if (line.Contains("Space("))
                    {
                        if (!inVariables)
                        {
                            inVariables = true;
                        }
                        else
                        {
                            inVariables = false;
                            added = true;

                            WriteProperties(writer, values);
                        }
                    }
                    else if (inVariables && line.Contains(";"))
                    {
                        WriteVariables(writer, values);

                        added = true;
                    }

                    writer.WriteLine(line);
                }
            }

            return true;
        }

        private void WriteVariables(StreamWriter writer, (VariableType type, string name)[] values)
        {
            int i = 0;

            while (i < values.Length)
            {
                string variable = string.Concat("\tprivate ", values[i].type.ToString(), " ", values[i].name, ";");
                writer.WriteLine(variable);
                ++i;
            }
        }

        private void WriteProperties(StreamWriter writer, (VariableType type, string name)[] values)
        {
            int i = 0;

            while (i < values.Length)
            {
                string propName = char.ToUpper(values[i].name[0]) + values[i].name.Substring(1, values[i].name.Length - 1);
                string property = string.Concat(
                    "\tpublic ",
                    values[i].type.ToString(), " ",
                    propName,
                    " { get => ", values[i].name,
                    "; set => ", values[i].name,
                    " = value; }");
                writer.WriteLine(property);
                ++i;
            }
        }

        private bool HasSpecialCharacter(string value)
        {
            return value.Any(chr => !char.IsLetterOrDigit(chr));
        }
    }

    public enum VariableType
    {
        None,
        @string,
        @int,
        @float,
        @double,
        @bool,
        @UnityEngine_Color,
        @UnityEngine_Vector2,
        @UnityEngine_Vector3,
    }
}
