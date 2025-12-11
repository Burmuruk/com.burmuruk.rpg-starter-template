using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving.Json.Converters
{
    public class UnityObjectConverter<T> : JsonConverter where T : UnityEngine.Object
    {
        public override bool CanConvert(Type objectType) => typeof(T).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.StartObject)
            {
                var jo = JObject.Load(reader);
                if (!jo.HasValues)
                    return null;
            }

            return serializer.Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
