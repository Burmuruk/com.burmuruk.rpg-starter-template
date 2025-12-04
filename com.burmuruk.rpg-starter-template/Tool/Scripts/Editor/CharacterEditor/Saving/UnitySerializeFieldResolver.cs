using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class UnitySerializeFieldResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Incluye: públicos; y privados con [SerializeField]
            var fields = type.GetFields(flags)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);

            var props = fields
                .Select(f =>
                {
                    var p = base.CreateProperty(f, memberSerialization);
                    p.Readable = true;
                    p.Writable = true;
                    p.PropertyName = f.Name; // respeta el nombre del campo
                    return p;
                })
                .ToList();

            return props;
        }
    } 
}
