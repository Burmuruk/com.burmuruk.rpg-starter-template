using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    internal static class JsonWritter
    {
        static string FilePath { get => Path.Combine(Application.persistentDataPath, "RPGSettings" + ".json"); }

        public static void WriteJson(JObject jObject)
        {
            File.WriteAllText(FilePath, jObject.ToString(Formatting.Indented));
        }

        public static bool ReadJson(out JObject json)
        {
            json = null;

            if (!JsonFileExists())
                return false;

            var text = File.ReadAllText(FilePath);
            json = JObject.Parse(text);
            return true;
        }

        public static bool JsonFileExists()
        {
            //if (!Directory.Exists(Application.persistentDataPath))
            //    Directory.CreateDirectory(Application.persistentDataPath);
            return File.Exists(FilePath);
        }
    }
}
