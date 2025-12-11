using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class BuffSettings : BaseInfoTracker, ISaveable
    {
        private NamedBuff _changesBuff;
        private string _id;

        public override string Id => _id;
        public FloatField Value { get; private set; }
        public FloatField Duration { get; private set; }
        public FloatField Rate { get; private set; }
        public Toggle Percentage { get; private set; }
        public FloatField Probability { get; private set; }
        public EnumField Stat { get; private set; }

        public override void Initialize(VisualElement container, CreationsBaseInfo name)
        {
            base.Initialize(container, name);

            Value = container.Q<FloatField>("ffValue");
            Duration = container.Q<FloatField>("ffDuration");
            Rate = container.Q<FloatField>("ffRate");
            Percentage = container.Q<Toggle>("ffPercentage");
            Probability = container.Q<FloatField>("ffProbability");
            Stat = container.Q<EnumField>("ffStat");

            Stat.Init(ModifiableStat.None);
            _nameControl.TxtName.RegisterValueChangedCallback((evt) =>
            {
                if (IsActive)
                    TempName = evt.newValue;
            });
        }

        public BuffData GetInfo() =>
            new BuffData()
            {
                name = TempName,
                value = Value.value,
                duration = Duration.value,
                rate = Rate.value,
                percentage = Percentage.value,
                probability = Probability.value,
                stat = (ModifiableStat)Stat.value
            };

        public void UpdateInfo(BuffData data)
        {
            Clear();

            UpdateUIData(data);
            _changesBuff = new NamedBuff(data.name, data);
        }

        public void UpdateUIData<T>(T args) where  T : struct
        {
            if (args is not BuffData data) return;

            if (string.IsNullOrEmpty(Id))
                _originalName = data.name;
            TempName = data.name;
            UpdateName();

            Value.value = data.value;
            Duration.value = data.duration;
            Rate.value = data.rate;
            Percentage.value = data.percentage;
            Probability.value = data.probability;
            Stat.value = data.stat;
        }

        public override void Clear()
        {
            foreach (var element in _highlighted)
                Utilities.UtilitiesUI.Set_Tooltip(element.Key, element.Value, false);

            Value.value = 0;
            Duration.value = 0;
            Rate.value = 0;
            Percentage.value = false;
            Probability.value = 0;
            Stat.value = ModifiableStat.None;
            _changesBuff = new("", null);
            _id = null;
            base.Clear();
        }

        public override void Load_Changes()
        {
            var data = _changesBuff.Data.Value;

            UpdateInfo(data);
        }

        public override ModificationTypes Check_Changes()
        {
            try
            {
                if (_changesBuff.Data == null) return CurModificationType = ModificationTypes.Add;

                CurModificationType = ModificationTypes.None;
                BuffData data = _changesBuff.Data.Value;

                if (_nameControl.Check_Changes() != ModificationTypes.None)
                    CurModificationType = ModificationTypes.Rename;

                if (data.stat != (ModifiableStat)Stat.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (data.value != Value.value)
                {
                    CurModificationType = ModificationTypes.EditData;

                }
                if (data.duration != Duration.value)
                {
                    CurModificationType = ModificationTypes.EditData;

                }
                if (data.rate != Rate.value)
                {
                    CurModificationType = ModificationTypes.EditData;

                }
                if (data.percentage != Percentage.value)
                {
                    CurModificationType = ModificationTypes.EditData;

                }
                if (data.probability != Probability.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }

                //if ((CurModificationType & ModificationTypes.EditData & ModificationTypes.Rename) != 0)
                //{
                //    if (string.IsNullOrEmpty(_changesBuff.Name) && data == default)
                //        return ModificationTypes.Add; 
                //}

                return CurModificationType;
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool isValid = true;
            bool result = true;

            result &= _nameControl.VerifyData(out errors);

            result &= isValid = Value.value <= 0;
            _highlighted[Value] = Value.tooltip;
            Utilities.UtilitiesUI.Set_ErrorTooltip(Value, "The number can't be less than 1", ref errors, isValid);

            result &= isValid = (ModifiableStat)Stat.value != ModifiableStat.None;
            _highlighted[Stat] = Stat.tooltip;
            Utilities.UtilitiesUI.Set_ErrorTooltip(Stat, "The stat can't be none", ref errors, isValid);

            return result;
        }

        public bool Save()
        {
            if (!VerifyData(out var errors))
            {
                if (errors.Count > 0)
                    Utilities.UtilitiesUI.Notify(errors[0], BorderColour.Error);
                else
                    Utilities.UtilitiesUI.Notify("Invalid Data", BorderColour.Error);
                return false;
            }

            try
            {
                if (_creationsState == CreationsState.Editing && Check_Changes() == ModificationTypes.None)
                {
                    Utilities.UtilitiesUI.Notify("No changes were found", BorderColour.HighlightBorder);
                    return false;
                }
                else
                    CurModificationType = ModificationTypes.Add;

                Utilities.UtilitiesUI.DisableNotification(NotificationType.Creation);
                var data = new BuffCreationData(_nameControl.TxtName.value.Trim(), GetInfo());

                return SavingSystem.SaveCreation(ElementType.Buff, _id, data, CurModificationType);
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }
        }

        public CreationData Load(ElementType type, string id)
        {
            var data = SavingSystem.Load(type, id);

            if (data == null) return default;

            BuffData newData = (data as BuffCreationData).Data;
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(newData);
            _id = id;
            return data;
        }

        public CreationData Load(string id)
        {
            var data = SavingSystem.Load(id);

            if (data == null) return default;

            BuffData newData = (data as BuffCreationData).Data;
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(newData);
            _id = id;
            return data;
        }

        public override void Remove_Changes()
        {
            _changesBuff = new("", null);
            _id = null;
        }

        CreationData IDataProvider.GetInfo()
        {
            return new BuffCreationData(_id, GetInfo());
        }

        public void UpdateInfo(CreationData cd)
        {
            var data = cd as BuffCreationData;
            
            if (string.IsNullOrEmpty(cd.Id))
            {
                _creationsState = CreationsState.Creating;
            }
            else
            {
                _creationsState = CreationsState.Editing;
                Load(cd.Id);
            }

            UpdateUIData(data.Data);
            _id = cd.Id;
        }
    }
}
