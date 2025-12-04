using Burmuruk.RPGStarterTemplate.Editor.Saving.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving.Json
{
    internal static class JsonSerializerHelper
    {
        private static JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new UnitySerializeFieldResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter(), new UnityObjectConverter<GameObject>() }
        });
        public static JsonSerializer Serializer => serializer;

        #region Serialization
        public static JToken ConvertDynamicDataToJson(Type type, object data)
        {
            return ConvertDynamicDataToJson(type, data, new HashSet<object>(ReferenceEqualityComparer.Instance));
        }

        public static JToken ConvertDynamicDataToJson(object data)
        {
            return JToken.FromObject(data, Serializer);
        }

        private static JToken ConvertDynamicDataToJson(Type type, object data, HashSet<object> visited)
        {
            if (data == null) return JValue.CreateNull();

            if (type.IsEnum)
            {
                return new JValue(Convert.ToInt32(data));
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid))
            {
                return new JValue(data);
            }

            if (Nullable.GetUnderlyingType(type) is Type underlying)
            {
                var underlyingVal = Convert.ChangeType(data, underlying);
                return ConvertDynamicDataToJson(underlying, underlyingVal, visited);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                var arr = new JArray();
                foreach (var item in (IEnumerable)data)
                {
                    if (item == null) { arr.Add(JValue.CreateNull()); continue; }
                    arr.Add(ConvertDynamicDataToJson(item.GetType(), item, visited));
                }
                return arr;
            }

            if (!type.IsValueType)
            {
                if (visited.Contains(data)) return JValue.CreateString("[CyclicRef]");
                visited.Add(data);
            }

            var json = new JObject();

            for (Type t = type; t != null && t != typeof(object) && t != typeof(UnityEngine.Object); t = t.BaseType)
            {
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (var f in fields)
                {
                    if (f.FieldType == typeof(System.Action)) continue;
                    if (f.Name.Contains("k__BackingField") && !f.IsInitOnly) continue;

                    object value;
                    try
                    {
                        value = f.GetValue(data);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!json.ContainsKey(t.AssemblyQualifiedName))
                        json[t.AssemblyQualifiedName] = new JObject();

                    Type fieldType = value != null ? value.GetType() : f.FieldType;

                    JToken token = ConvertDynamicDataToJson(fieldType, value, visited);
                    json[t.AssemblyQualifiedName][f.Name] = token;
                }
            }

            return json;
        }
        #endregion

        #region Deserealizaiton
        public static T FromJson<T>(JObject jo)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new UnitySerializeFieldResolver(),
            };
            return jo.ToObject<T>(JsonSerializer.Create(settings));
        }

        public static object FromJson(JObject jObject)
        {
            object item = null;

            foreach (var jo in jObject)
            {
                Type t = Type.GetType(jo.Key);
                item ??= CreateWithoutCtor(t);
                var dic = (IDictionary<string, JToken>)jo.Value;

                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (var f in fields)
                {
                    if (!dic.ContainsKey(f.Name)) continue;

                    if (f.FieldType.FullName.Contains("RPGStarterTemplate") && (dic[f.Name].Type == JTokenType.Object || dic[f.Name].Type == JTokenType.Array))
                    {
                        if (dic[f.Name] is JObject obj)
                        {
                            var result = FromJson(obj);
                            f.SetValue(item, result);
                        }
                        else if (dic[f.Name] is JArray array)
                        {
                            bool flowControl = ReadArray(item, f, array);

                            if (!flowControl) continue;
                        }
                    }
                    else if (dic[f.Name] is JArray array)
                    {
                        bool flowControl = ReadArray(item, f, array);

                        if (!flowControl) continue;
                    }
                    else
                    {
                        if (!dic.ContainsKey(f.Name)) continue;

                        f.SetValue(item, dic[f.Name].ToObject(f.FieldType, Serializer));
                    }
                }
            }

            return item;
        }

        private static bool ReadArray(object item, FieldInfo f, JArray array)
        {
            if (array.Count <= 0) return false;

            if (array.First is JValue jv)
            {
                f.SetValue(item, array.ToObject(f.FieldType, serializer));
                return true;
            }
            else if (!(array.First is JObject)) return false;

            string typeName = ((JObject)array.First).Properties().First().Name;
            Type lt = Type.GetType(typeName);

            if (lt.IsGenericType && lt.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                Type kType = lt.GenericTypeArguments[0];
                Type vType = lt.GenericTypeArguments[1];
                Type dictType = typeof(Dictionary<,>).MakeGenericType(kType, vType);
                IDictionary dic = (IDictionary)Activator.CreateInstance(dictType);

                foreach (var kwPair in array)
                {
                    var dicInfo = kwPair.First();

                    foreach (var kvWrapper in dicInfo)
                    {
                        if (kvWrapper == null) continue;

                        JObject kwjo = kvWrapper as JObject;
                        JObject valuePair = null;
                        Type valueType = null;

                        if (kwjo == null) continue;

                        if (kwjo.Property("key") != null && kwjo.Property("value") != null)
                        {
                            valuePair = kwjo["value"] as JObject;

                            if (valuePair == null)
                            {
                                if (kwjo["value"] != null)
                                    dic.Add(kwjo["key"].ToObject(kType), kwjo["value"].ToObject(vType, Serializer));
                                else
                                    dic.Add(kwjo["key"].ToObject(kType), null);

                                continue;
                            }

                            valueType = Type.GetType(valuePair.Properties().First().Name);
                        }

                        JToken keyToken = kwjo["key"];
                        JToken valueToken = valuePair.Properties().First().Value;

                        if (keyToken == null) continue;

                        object keyObj = keyToken.ToObject(kType);
                        object valueObj = valueToken != null ? FromJson(new JObject { { valueType.AssemblyQualifiedName, valueToken } }) :
                            (valueType.IsValueType ? Activator.CreateInstance(valueType) : null);

                        dic.Add(keyObj, valueObj);
                    } 
                }

                f.SetValue(item, dic);
            }
            else
            {
                Type listType = typeof(List<>).MakeGenericType(lt);
                var items = (IList)Activator.CreateInstance(listType);

                foreach (var a in array)
                {
                    if (a is JObject co)
                        items.Add(FromJson(co));
                    else if (a is JValue cv)
                        items.Add(cv.ToObject(lt));
                }

                MethodInfo toArrayMethod = listType.GetMethod("ToArray");
                Array resultArray = (Array)toArrayMethod.Invoke(items, null);

                if (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    f.SetValue(item, items);
                else
                    f.SetValue(item, resultArray);
            }
            return true;
        }

        private static object CreateWithoutCtor(Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }
        #endregion
    }

    #region ReferenceEqualityComparer helper
    // Comparador para HashSet que usa referencia de objeto (no Equals sobrecargado)
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        private ReferenceEqualityComparer() { }
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
    #endregion
}
