using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class HealthSettings : SubWindow
    {
        const string INFO_HEALTH_SETTINGS_NAME = "HealthSettings";
        private Health _changes = null;
        private IVisualElementScheduledItem pkeyDownTimeOut = null;

        public IntegerField IFMaxHealth { get; set; }
        public IntegerField IFHealth { get; private set; }
        public Button btnBackHealthSettings { get; private set; }

        public override void Initialize(VisualElement container)
        {
            base.Initialize(container);

            _instance = UtilitiesUI.CreateDefaultTab(INFO_HEALTH_SETTINGS_NAME);
            container.hierarchy.Add(_instance);
            IFMaxHealth = container.Q<IntegerField>("maxHealth");
            IFHealth = container.Q<IntegerField>("health");
            IFHealth.value = 0;
            btnBackHealthSettings = _instance.Q<Button>();

            IFMaxHealth.RegisterValueChangedCallback(OnValueChanged_MaxHealth);
            IFHealth.RegisterValueChangedCallback(OnValueChanged_FFHealthValue);
            btnBackHealthSettings.clicked += () => GoBack?.Invoke();
        }

        private void OnValueChanged_MaxHealth(ChangeEvent<int> evt)
        {
            if (IFHealth.value > evt.newValue)
            {
                pkeyDownTimeOut?.Pause();
                pkeyDownTimeOut = null;

                pkeyDownTimeOut = IFHealth.schedule.Execute(() => 
                {
                    IFHealth.SetValueWithoutNotify(IFMaxHealth.value);
                    Debug.Log("Value Changed");
                });
                pkeyDownTimeOut.ExecuteLater(1000);
            }
            else
            {
                pkeyDownTimeOut?.Pause();
                pkeyDownTimeOut = null;
            }
        }

        public void UpdateHealth(in Health value)
        {
            IFHealth.value = value.HP;
            IFMaxHealth.value = value.MaxHP;
            _changes = value;
        }

        public void UpdateUIData<T>(T data) where T : Health
        {
            IFHealth.value = data.HP;
            IFMaxHealth.value = data.MaxHP;
        }

        private void OnValueChanged_FFHealthValue(ChangeEvent<int> evt)
        {
            if (evt.newValue > IFMaxHealth.value)
            {
                IFHealth.SetValueWithoutNotify(IFMaxHealth.value);
            }

            //AddComponentData(ComponentType.Health);
        }

        private void AddComponentData(ComponentType type)
        {
            object item = null;

            item = type switch
            {
                ComponentType.Health => new Health()
                {
                    HP = (int)IFHealth.value
                },
                _ => null
            };

            //characterData.components.TryAdd(type, item);
        }

        public void LoadInfo(in Health value)
        {
            UpdateHealth(value);
        }

        public Health GetInfo()
        {
            return new Health
            {
                HP = IFHealth.value,
                MaxHP = IFMaxHealth.value
            };
        }

        public override void Clear()
        {
            _changes = null;
            IFHealth.value = 0;
            IFMaxHealth.value = 100;
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            return true;
        }

        public override ModificationTypes Check_Changes()
        {
            var modificationType = ModificationTypes.None;

            if (_changes == null) return ModificationTypes.None;

            if (IFHealth.value != _changes.HP)
                modificationType = ModificationTypes.EditData;

            if (IFMaxHealth.value != _changes.MaxHP)
                modificationType = ModificationTypes.EditData;

            return modificationType;
        }

        public override void Load_Changes()
        {
            IFHealth.value = _changes.HP;
            IFMaxHealth.value = _changes.MaxHP;
        }

        public override void Remove_Changes()
        {
            _changes = null;
        }
    }
}
