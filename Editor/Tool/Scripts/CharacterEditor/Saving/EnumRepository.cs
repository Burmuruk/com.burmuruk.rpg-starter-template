using Burmuruk.RPGStarterTemplate.Editor.Saving.Json;
using Newtonsoft.Json.Linq;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class EnumRepository
    {
        private readonly JsonStore jsonStore;

        public EnumRegistry Registry { get; private set; }

        public EnumRepository(JsonStore jsonStore)
        {
            this.jsonStore = jsonStore;
        }

        public void Save(EnumRegistry registry)
        {
            if (registry == null)
                return;

            JObject root = jsonStore.Read();

            JObject enumJson = JObject.FromObject(registry, JsonSerializerHelper.Serializer);

            jsonStore.Set(root, JsonStore.ENUMS_KEY, enumJson);

            jsonStore.Write(root);
        }

        public EnumRegistry Load()
        {
            JObject root = jsonStore.Read();

            if (!jsonStore.TryGet(root, JsonStore.ENUMS_KEY, out JObject enumJson))
            {
                return Registry = new EnumRegistry();
            }

            return Registry = enumJson.ToObject<EnumRegistry>(JsonSerializerHelper.Serializer) ?? new EnumRegistry();
        }
    }
}