using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class CreationSerializer
    {
        private const string TYPE_KEY = "type";

        public JObject Serialize(
            ElementType type,
            CreationData data)
        {
            if (data == null)
                return null;

            return new JObject
            {
                [data.GetType().FullName] = data.GetJson(),
                [TYPE_KEY] = (int)type
            };
        }

        public bool TryDeserialize(
            JObject json,
            out ElementType elementType,
            out CreationData data)
        {
            elementType = ElementType.None;
            data = null;

            if (json == null)
                return false;

            if (json[TYPE_KEY] == null)
                return false;

            elementType = (ElementType)json[TYPE_KEY].ToObject<int>();

            // No asumimos que la primera property siempre sea el tipo.
            var dataProperty = json
                .Properties()
                .FirstOrDefault(p => p.Name != TYPE_KEY);

            if (dataProperty == null)
                return false;

            Type creationType = Type.GetType(dataProperty.Name);

            if (creationType == null)
                return false;

            if (!typeof(CreationData).IsAssignableFrom(creationType))
                return false;

            if (dataProperty.Value is not JObject creationJson)
                return false;

            data = (CreationData)FormatterServices
                .GetUninitializedObject(creationType);

            data.RestoreFromJson(creationJson);

            return true;
        }
    }
}