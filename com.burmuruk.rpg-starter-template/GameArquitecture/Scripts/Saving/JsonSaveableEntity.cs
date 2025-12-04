using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    [ExecuteAlways]
    public class JsonSaveableEntity : MonoBehaviour
    {
        [SerializeField] string uniqueIdentifier = "";

        static Dictionary<string, JsonSaveableEntity> globalLookup = new();

        public string GetUniqueIdentifier()
        {
            return uniqueIdentifier;
        }

        public JToken CaptureAsJtoken(out JObject UniqueItems)
        {
            UniqueItems = null;
            JObject state = null;
            SortedDictionary<int, JObject> sortedDict = null;
            SortedDictionary<int, JObject> sortedUniqueDict = null;

            foreach (var jsonSaveable in GetComponents<IJsonSaveable>())
            {
                JToken token = jsonSaveable.CaptureAsJToken(out SavingExecution execution);

                if (token == null) continue;

                if (((int)execution) <= (int)SavingExecution.Organization)
                {
                    sortedUniqueDict ??= new();

                    if (!sortedUniqueDict.ContainsKey((int)execution))
                        sortedUniqueDict.Add((int)execution, new JObject());

                    sortedUniqueDict[(int)execution][jsonSaveable.GetType().ToString()] = token;
                }
                else
                {
                    sortedDict ??= new();

                    if (!sortedDict.ContainsKey((int)execution))
                        sortedDict.Add((int)execution, new JObject());

                    sortedDict[(int)execution][jsonSaveable.GetType().ToString()] = token;
                }
            }

            ConvertSortedToNormalDic(sortedDict, ref state);
            ConvertSortedToNormalDic(sortedUniqueDict, ref UniqueItems);

            void ConvertSortedToNormalDic(SortedDictionary<int, JObject> sorted, ref JObject target)
            {
                if (sorted == null) return;
                target ??= new();

                foreach (var chunk in sorted)
                {
                    target[((SavingExecution)chunk.Key).ToString()] = chunk.Value;
                }
            }

            return state;
        }

        public void RestoreFromJToken(JToken s, SavingExecution execution)
        {
            JObject state = s.ToObject<JObject>();

            if (!state.ContainsKey(execution.ToString())) return;

            JObject executionState = (JObject)state[execution.ToString()];

            foreach (IJsonSaveable jsonSaveable in GetComponents<IJsonSaveable>())
            {
                string component = jsonSaveable.GetType().ToString();
                
                if (executionState.ContainsKey(component))
                {
                    jsonSaveable.LoadAsJToken(executionState[component]);
                }
            }
        }

        public void SetUniqueIdentifier(string identifier)
        {
            if (globalLookup.ContainsKey(identifier))
            {
                //var lastItem = globalLookup[identifier];
                globalLookup.Remove(identifier);
            }

            uniqueIdentifier = identifier;
            globalLookup[identifier] = this;
        }

        public void SetUniqueIdentifier()
        {
            if (string.IsNullOrEmpty(uniqueIdentifier) || !IsUnique(uniqueIdentifier))
            {
                uniqueIdentifier = System.Guid.NewGuid().ToString();
            }

            globalLookup[uniqueIdentifier] = this;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.IsPlaying(gameObject)) return;
            if (string.IsNullOrEmpty(gameObject.scene.path)) return;

            SerializedObject serializedObject = new(this);
            SerializedProperty property = serializedObject.FindProperty("uniqueIdentifier");

            if (string.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue))
            {
                property.stringValue = System.Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            globalLookup[property.stringValue] = this;
        }
#endif

        private bool IsUnique(string candidate)
        {
            if (!globalLookup.ContainsKey(candidate)) return true;

            if (globalLookup[candidate] == this) return true;

            if (globalLookup[candidate] == null)
            {
                globalLookup.Remove(candidate);
                return true;
            }
            
            if (globalLookup[candidate].GetUniqueIdentifier() != candidate)
            {
                globalLookup.Remove(candidate);
                return true;
            }

            return false;
        }
    }
}