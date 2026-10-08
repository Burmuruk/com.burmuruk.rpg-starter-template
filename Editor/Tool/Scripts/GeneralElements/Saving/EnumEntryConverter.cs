using Burmuruk.RPGStarterTemplate.Editor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving.Json.Converters
{
    public sealed class EnumEntryConverter : JsonConverter<EnumEntry>
    {
        public override void WriteJson(JsonWriter writer, EnumEntry value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(nameof(EnumEntry.Id));
            writer.WriteValue(value.Id);

            writer.WritePropertyName(nameof(EnumEntry.Name));
            writer.WriteValue(value.Name);

            writer.WritePropertyName(nameof(EnumEntry.Order));
            writer.WriteValue(value.Order);

            writer.WriteEndObject();
        }

        public override EnumEntry ReadJson(JsonReader reader, Type objectType, EnumEntry existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject json = JObject.Load(reader);

            int id = json.Value<int>(nameof(EnumEntry.Id));
            string name = json.Value<string>(nameof(EnumEntry.Name));
            int order = json.Value<int>(nameof(EnumEntry.Order));

            return new EnumEntry(id, name, order);
        }
    } 
}