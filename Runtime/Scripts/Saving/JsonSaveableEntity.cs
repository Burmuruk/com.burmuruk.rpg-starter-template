using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Burmuruk.RPGStarterTemplate.Saving
{
    [ExecuteAlways]
    public class JsonSaveableEntity : MonoBehaviour
    {
        // Preserve the serialized field name and the existing component's .meta file.
        [SerializeField] private string uniqueIdentifier = "";
        private string registeredIdentifier;
        private bool retiring;

        public bool IsRetiring => retiring;
        public string GetUniqueIdentifier() => uniqueIdentifier;

        private void OnEnable()
        {
            if (Application.IsPlaying(gameObject) && !retiring)
                SetUniqueIdentifier();
        }

        // Keep IDs reserved while an entity is disabled; release on destruction.
        private void OnDestroy() => GlobalIdRegistry.Unregister(registeredIdentifier, this);

        public void SetUniqueIdentifier()
        {
            if (retiring)
                return;
            string candidate = uniqueIdentifier;
            while (!GlobalIdRegistry.TryRegister(candidate, this))
                candidate = Guid.NewGuid().ToString();
            SetRegisteredIdentifier(candidate);
        }

        public void SetUniqueIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("A saved entity ID cannot be empty.", nameof(identifier));
            if (!GlobalIdRegistry.TryRegister(identifier, this))
                throw new InvalidOperationException($"Entity ID '{identifier}' already belongs to another object.");
            retiring = false;
            SetRegisteredIdentifier(identifier);
        }

        private void SetRegisteredIdentifier(string identifier)
        {
            if (registeredIdentifier != identifier)
                GlobalIdRegistry.Unregister(registeredIdentifier, this);
            registeredIdentifier = identifier;
            uniqueIdentifier = identifier;
        }

        // Call immediately before Destroy when replacing an entity in the same frame.
        public void ReleaseIdentifierForDestruction()
        {
            retiring = true;
            GlobalIdRegistry.Unregister(registeredIdentifier, this);
            registeredIdentifier = null;
        }

        // Uniform entity capture: stage name -> component type -> component data.
        public JObject CaptureStages()
        {
            SetUniqueIdentifier();
            var stages = new JObject();
            foreach (var component in GetComponents<IJsonSaveable>())
            {
                JToken token = component.CaptureAsJToken(out SavingExecution execution);
                if (token == null || token.Type == JTokenType.Null)
                    continue;
                string stageName = Enum.GetName(typeof(SavingExecution), execution);
                if (stageName == null)
                    throw new InvalidDataException($"Undefined SavingExecution value: {(int)execution}.");
                var stage = stages[stageName] as JObject;
                if (stage == null)
                    stages[stageName] = stage = new JObject();
                string typeName = component.GetType().FullName;
                if (stage.ContainsKey(typeName))
                    throw new InvalidDataException($"Multiple saveable components of type '{typeName}' on '{name}'. Give them separate entities.");
                stage[typeName] = token.DeepClone();
            }
            return stages;
        }

        // Compatibility with callers expecting the original two-part capture API.
        public JToken CaptureAsJtoken(out JObject UniqueItems)
        {
            UniqueItems = null;
            JObject entityStages = null;
            foreach (var entry in CaptureStages())
            {
                var execution = (SavingExecution)Enum.Parse(typeof(SavingExecution), entry.Key);
                if ((int)execution <= (int)SavingExecution.Instances)
                {
                    UniqueItems ??= new JObject();
                    UniqueItems[entry.Key] = entry.Value.DeepClone();
                }
                else
                {
                    entityStages ??= new JObject();
                    entityStages[entry.Key] = entry.Value.DeepClone();
                }
            }
            return entityStages;
        }

        public void RestoreFromJToken(JToken state, SavingExecution execution)
        {
            if (state is JObject stages && stages[execution.ToString()] is JObject components)
                RestoreComponents(components);
        }

        public void RestoreComponents(JObject components)
        {
            foreach (var component in GetComponents<IJsonSaveable>())
            {
                if (retiring || this == null)
                    break;
                if (component is UnityEngine.Object unityObject && unityObject == null)
                    continue;
                if (components.TryGetValue(component.GetType().FullName, out JToken token))
                    component.LoadAsJToken(token);
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.IsPlaying(gameObject) || string.IsNullOrEmpty(gameObject.scene.path))
                return;
            // SerializedObject records the new ID in scene/prefab overrides as before.
            var serialized = new SerializedObject(this);
            var property = serialized.FindProperty("uniqueIdentifier");
            string candidate = property.stringValue;
            while (!GlobalIdRegistry.TryRegister(candidate, this))
                candidate = Guid.NewGuid().ToString();
            if (property.stringValue != candidate)
            {
                property.stringValue = candidate;
                serialized.ApplyModifiedProperties();
            }
            SetRegisteredIdentifier(candidate);
        }
#endif
    }
}
