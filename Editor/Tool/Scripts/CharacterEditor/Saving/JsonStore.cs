using Newtonsoft.Json.Linq;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class JsonStore
    {
        public const string CREATIONS_KEY = "creations";
        public const string UNSAVED_CHANGES_KEY = "unsavedChanges";
        public const string ENUMS_KEY = "enums";

        public JObject Read()
        {
            if (!JsonWritter.ReadJson(out JObject json))
                return new JObject();

            return json ?? new JObject();
        }

        public void Write(JObject root)
        {
            JsonWritter.WriteJson(root);
        }

        public JObject GetOrCreateObject(JObject root, string key)
        {
            if (root[key] is JObject obj)
                return obj;

            obj = new JObject();
            root[key] = obj;

            return obj;
        }

        public void Set(JObject root, string key, JToken value)
        {
            root[key] = value;
        }

        public bool TryGet<T>(JObject root, string key, out T token) where T : JToken
        {
            token = root[key] as T;
            return token != null;
        }

        public bool Remove(JObject root, string key)
        {
            return root.Remove(key);
        }
    }
}