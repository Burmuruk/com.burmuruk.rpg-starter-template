using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System.Collections.Generic;
using System.Linq;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ItemBuffReader : BaseItemSetting
    {
        protected BuffsNamesDataArgs _changesBuffsIds;

        public BuffAdderUI BuffAdder { get; protected set; }

        public BuffsNamesDataArgs GetCreatedEnums(ElementType type)
        {
            var names = new List<string>();

            if (!SavingSystem.Data.creations.ContainsKey(type)) return new(null, null, null);

            foreach (var creation in SavingSystem.Data.creations[type])
            {
                names.Add(creation.Value.Id);
            }

            return new BuffsNamesDataArgs(names, null, null);
        }

        protected void UpdateBuffs(in BuffData[] buffs, BuffsNamesDataArgs buffArgs)
        {
            if (buffArgs == null || buffArgs.BuffsNames == null) return;

            List<(string id, BuffData? data)> buffsData = new();
            int consumableIdx = 0;

            foreach (var value in buffArgs.BuffsNames)
            {
                if (value == "")
                {
                    buffsData.Add((value, buffs[consumableIdx++]));
                    //continue;
                    //buffsData.Add((null, null));
                }
                else
                {
                    buffsData.Add((value, null));
                }
            }

            BuffAdder.UpdateData(buffsData);
        }

        public override ModificationTypes Check_Changes()
        {
            var result = base.Check_Changes();

            result = BuffAdder.Check_Changes();

            return result;
        }

        //protected bool CheckBuffChanges()
        //{
        //    var namedBuffs = BuffAdder.GetBuffsData();

        //    if (namedBuffs.Count != _changesBuffsIds.BuffsNames.Count) return true;

        //    for (int i = 0, j = 0; i < namedBuffs.Count && j < _changesBuffsIds.BuffsNames.Count; i++)
        //    {
        //        if (namedBuffs[i].name != _changesBuffsIds.BuffsNames[j])
        //            return true;
        //    }

        //    return true;
        //}

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = true;

            result &= base.VerifyData(out var baseErrors);

            if (baseErrors != null && baseErrors.Count > 0)
                errors.AddRange(baseErrors);

            result &= BuffAdder.VerifyData(out var buffErrors);

            if (buffErrors != null && buffErrors.Count > 0)
                errors.AddRange(buffErrors);

            return result;
        }

        protected List<BuffData> SelectCustomBuffs(List<NamedBuff> localBuffs)
        {
            List<BuffData> buffsData = new();

            foreach (NamedBuff curLocalBuff in localBuffs)
            {
                if (curLocalBuff.name == "")
                    buffsData.Add(curLocalBuff.Data.Value);
            }

            return buffsData;
        }

        public (List<BuffData> buffsList, BuffsNamesDataArgs NamesList) GetBuffsInfo()
        {
            List<NamedBuff> curBuffs = BuffAdder.GetBuffsData();
            List<BuffData> buffs = SelectCustomBuffs(curBuffs);

            return (buffs, new BuffsNamesDataArgs((from n in curBuffs select n.name).ToList(), null, null));
        }

        public override bool Save()
        {
            if (!VerifyData(out var errors))
            {
                Notify(errors.Count <= 0 ? "Invalid Data" : errors[0], BorderColour.Error);
                return false;
            }

            if (_creationsState == CreationsState.Editing && Check_Changes() == ModificationTypes.None)
            {
                Notify("No changes were found", BorderColour.HighlightBorder);
                return false;
            }
            else
                CurModificationType = ModificationTypes.Add;

            DisableNotification(NotificationType.Creation);
            var (item, args) = ((InventoryItem, BuffsNamesDataArgs))GetInfo(GetCreatedEnums(ElementType.Buff));
            var creationData = new BuffUserCreationData(_nameControl.TxtName.text.Trim(), item, args);

            return SavingSystem.SaveCreation(ElementType, in _id, creationData, CurModificationType);
        }

        public override CreationData Load(ElementType type, string id)
        {
            var result = SavingSystem.Load(type, id);

            if (result == null) return default;

            var data = result as BuffUserCreationData;
            _id = id;
            var (item, args) = (data, data.Names);
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(data.Data, args);

            return result;
        }

        public override CreationData Load(string id)
        {
            var result = SavingSystem.Load(id);

            if (result == null) return default;

            var data = result as BuffUserCreationData;
            _id = id;
            var (item, args) = (data, data.Names);
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(data.Data, args);

            return result;
        }

        public override void UpdateInfo(CreationData cd)
        {
            var data = cd as BuffUserCreationData;
            var (bItem, args) = (data, data.Names);

            if (string.IsNullOrEmpty(data.Id))
            {
                _creationsState = CreationsState.Creating;
            }
            else
            {
                Load(cd.Id);
                _creationsState = CreationsState.Editing;
            }

            _id = cd.Id;
            UpdateUIData(data.Data, args);
            _id = cd.Id;
        }

        public void UpdateUIBuffs(in BuffData[] buffs, BuffsNamesDataArgs buffArgs)
        {
            if (buffArgs == null || buffArgs.BuffsNames == null) return;

            List<(string id, BuffData? data)> buffsData = new();
            int consumableIdx = 0;

            foreach (var value in buffArgs.BuffsNames)
            {
                if (value == "")
                {
                    buffsData.Add((value, buffs[consumableIdx++]));
                    //continue;
                    //buffsData.Add((null, null));
                }
                else
                {
                    buffsData.Add((value, null));
                }
            }

            BuffAdder.UpdateUIData(buffsData);
        }

        public override CreationData GetInfo()
        {
            var (bItem, bArgs) = ((InventoryItem, BuffsNamesDataArgs))GetInfo(GetCreatedEnums(ElementType.Buff));

            return new BuffUserCreationData(_id, bItem, bArgs);
        }
    }
}
