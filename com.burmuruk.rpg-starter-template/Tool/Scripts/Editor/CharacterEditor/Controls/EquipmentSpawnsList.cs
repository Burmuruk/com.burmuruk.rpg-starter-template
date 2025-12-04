using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class EquipmentSpawnsList : TreeViewList<SpawnElementData>
    {
        Dictionary<string, (Transform transform, EquipmentType place)> _changes;

        public Action<List<string>> OnChoicesChanged;

        public EquipmentSpawnsList(VisualElement container) : base(container)
        {
            TxtCount.RegisterCallback<DragPerformEvent>(OnBoneDropped);
            OnElementCreated += (element) =>
            {
                element.place.choices = GetChoices();
                element.place.value = EquipmentType.None.ToString();
                element.place.RegisterValueChangedCallback(evt =>
                {
                    var newChoices = GetChoices();
                    
                    foreach (var item in _enabledElements)
                    {
                        item.place.choices = newChoices;
                    }

                    OnChoicesChanged?.Invoke(newChoices);
                });
            };
        }

        public new List<(Transform transform, EquipmentType type)> GetInfo()
        {
            var spawnPoints = new List<(Transform transform, EquipmentType type)>();

            foreach (var item in _enabledElements)
            {
                if (item.place.value == EquipmentType.None.ToString() ||
                    item.transform == null)
                    continue;

                Transform transform = null;
                if (item.transform.value is GameObject obj)
                {
                    transform = obj.transform;
                }
                else if (item.transform.value is Transform t)
                {
                    transform = t.transform;
                }

                spawnPoints.Add((transform, (EquipmentType)Enum.Parse(typeof(EquipmentType), item.place.value)));
            }

            return spawnPoints;
        }

        private List<string> GetChoices()
        {
            var selected = new List<string>();
            var newNames = new List<string>(Enum.GetNames(typeof(EquipmentType)));

            foreach (var element in _enabledElements)
            {
                if (element.place.value == "none") continue;

                selected.Add(element.place.value);
            }

            return newNames.Where(name => !selected.Contains(name)).ToList();
        }

        private void OnBoneDropped(DragPerformEvent evt)
        {
            var values = DragAndDrop.GetGenericData("DraggedNode") as UnityEngine.Object[];

            if (values != null && values.Length > 0)
            {
                Add();
                _enabledElements.Last.Value.transform.value = values[0];
            }
        }

        protected override void SetupFoldOut()
        {
            base.SetupFoldOut();

            Foldout.text = "Spawn points";
        }

        public void LoadInfo(List<(Transform transform, EquipmentType place)> newData)
        {
            Clear();
            _changes = new();

            if (newData == null) return;

            foreach (var item in newData)
            {
                Add();
                _enabledElements.Last.Value.place.value = item.place.ToString();
                _enabledElements.Last.Value.transform.value = item.transform;

                _changes.Add(item.transform.name, item);
            }
        }


        public new void UpdateUIData<T>(T newData) where T : List<(Transform transform, EquipmentType place)>
        {
            if (newData == null) return;

            DisableAllElements();

            foreach (var item in newData)
            {
                Add();
                _enabledElements.Last.Value.place.value = item.place.ToString();
                _enabledElements.Last.Value.transform.value = item.transform;
            }
        }

        public override void Clear()
        {
            base.Clear();
            _changes = null;
        }

        public override ModificationTypes Check_Changes()
        {
            if (_changes == null) return ModificationTypes.None;

            foreach (SpawnElementData element in _enabledElements)
            {
                if (_changes.ContainsKey(element.transform.value.name))
                {
                    //place
                    if (_changes[element.transform.value.name].place.ToString() != element.place.value)
                        return ModificationTypes.EditData;

                    //transform
                    if (element.transform.value is GameObject go)
                    {
                        if (_changes[element.transform.value.name].transform.gameObject != go)
                            return ModificationTypes.EditData;
                    }
                    else if (element.transform.value is Transform t)
                    {
                        if (_changes[element.transform.value.name].transform != t)
                            return ModificationTypes.EditData;
                    }
                }
                else
                {
                    return ModificationTypes.EditData;
                }
            }

            return ModificationTypes.None;
        }

        //public void Load_Changes()
        //{
        //    throw new System.NotImplementedException();
        //}

        //public void Remove_Changes()
        //{
        //    throw new System.NotImplementedException();
        //}
    }
}
