using Burmuruk.RPGStarterTemplate.Stats;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class BuffsDataUI
    {
        VisualElement dataContainer;
        private string buffId;

        public VisualElement Element { get; private set; }
        public DropdownField DDBuff { get; private set; }
        public FloatField Value { get; private set; }
        public FloatField Duration { get; private set; }
        public FloatField Rate { get; private set; }
        public Toggle Percentage { get; private set; }
        public FloatField Probability { get; private set; }

        public BuffsDataUI()
        {
            VisualTreeAsset ElementTag = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Controls/BuffsAdder.uxml");
            StyleSheet basicStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/BasicSS.uss");
            StyleSheet lineStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/Styles/LineTags.uss");
            Element = ElementTag.Instantiate();
            Element.styleSheets.Add(basicStyleSheet);
            Element.styleSheets.Add(lineStyleSheet);

            DDBuff = Element.Q<DropdownField>("buffType");
            dataContainer = Element.Q<VisualElement>("dataContainer");
            Value = Element.Q<FloatField>("ffValue");
            Duration = Element.Q<FloatField>("ffDuration");
            Rate = Element.Q<FloatField>("ffRate");
            Percentage = Element.Q<Toggle>("tglPercentage");
            Probability = Element.Q<FloatField>("ffProbability");

            DDBuff.choices.Clear();
            DDBuff.SetValueWithoutNotify("None");
            DDBuff.RegisterValueChangedCallback(OnValueChanged_BuffType);
        }

        private void OnValueChanged_BuffType(ChangeEvent<string> evt)
        {
            bool shouldEnable = string.IsNullOrEmpty(evt.newValue) || evt.newValue == "Custom";
            EnableDataContainer(shouldEnable);
        }

        private void EnableDataContainer(bool shouldEnable)
        {
            EnableContainer(dataContainer, shouldEnable);
        }

        public void SetValues(Dictionary<string, string> newNames)
        {
            DDBuff.choices.Add("Custom");

            if (newNames != null)
                DDBuff.choices.AddRange(newNames.Keys);
        }

        /// <summary>
        /// Returns buffs's names and info. "" for custom values and None for empty values.
        /// </summary>
        /// <returns></returns>
        public NamedBuff GetInfo()
        {
            if (DDBuff.value == "Custom")
            {
                BuffData buff = new BuffData()
                {
                    value = Value.value,
                    duration = Duration.value,
                    rate = Rate.value,
                    percentage = Percentage.value,
                    probability = Probability.value,
                };

                return new("", buff);
            }
            else if (DDBuff.value == "None")
                return default;

            return new(DDBuff.value, null);
        }

        public void UpdateData(string name, BuffData? data)
        {
            if (data.HasValue)
            {
                Value.value = data.Value.value;
                Duration.value = data.Value.duration;
                Rate.value = data.Value.rate;
                Percentage.value = data.Value.percentage;
                Probability.value = data.Value.probability;
            }
            else
            {
                ClearValues();
                EnableDataContainer(false);
            }

            DDBuff.value = name;
        }

        public void UpdateData(float value, float duration, float rate, bool percentage, float probability)
        {
            Value.value = value;
            Duration.value = duration;
            Rate.value = rate;
            Percentage.value = percentage;
            Probability.value = probability;
        }

        public void ClearValues()
        {
            DDBuff.SetValueWithoutNotify("None");

            Value.value = 0;
            Duration.value = 0;
            Rate.value = 0;
            Percentage.value = false;
            Probability.value = 0;
        }
    }
}
