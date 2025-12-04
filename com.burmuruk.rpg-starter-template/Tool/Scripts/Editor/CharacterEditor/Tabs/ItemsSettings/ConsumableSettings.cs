using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ConsumableSettings : ItemBuffReader
    {
        public FloatField ConsumptionTime { get; private set; }
        public FloatField AreaRadious { get; private set; }
        public override ElementType ElementType { get => ElementType.Consumable; }

        public override void Initialize(VisualElement container, CreationsBaseInfo name)
        {
            base.Initialize(container, name);

            BuffAdder = new BuffAdderUI(container);
            ConsumptionTime = container.Q<FloatField>("ffConsumptionTime");
            AreaRadious = container.Q<FloatField>("ffAreaRadious");
        }

        public override void UpdateInfo(InventoryItem data, ItemDataArgs args, ItemType type = ItemType.Consumable)
        {
            _changes = new ConsumableItem();
            _args = args;
            
            base.UpdateInfo(data, args, type);

            //if (consumable == null) return;

            var consumable = data as ConsumableItem;
            var buffArgs = args as BuffsNamesDataArgs;
            ConsumptionTime.value = consumable.ConsumptionTime;
            AreaRadious.value = consumable.AreaRadious;
            (_changes as ConsumableItem).UpdateInfo(consumable.Buffs, ConsumptionTime.value, AreaRadious.value);

            UpdateBuffs(consumable.Buffs, buffArgs);
        }

        public override void UpdateUIData<T, U>(T data, U args)
        {
            base.UpdateUIData(data, args);

            var consumable = data as ConsumableItem;
            var buffArgs = args as BuffsNamesDataArgs;
            ConsumptionTime.value = consumable.ConsumptionTime;
            AreaRadious.value = consumable.AreaRadious;
            UpdateUIBuffs(consumable.Buffs, buffArgs);
        }

        public override (InventoryItem item, ItemDataArgs args) GetInfo(ItemDataArgs args)
        {
            ConsumableItem newItem = new();
            var (baseInfo, baseArgs) = base.GetInfo(args);
            newItem.Copy(baseInfo);

            (var buffs, var buffsNames) = GetBuffsInfo();
            buffsNames = buffsNames with
            {
                PickupPath = baseArgs?.PickupPath,
                ImgGUID = baseArgs?.ImgGUID
            };

            newItem.UpdateInfo(
                buffs.ToArray(),
                ConsumptionTime.value,
                AreaRadious.value
                );

            return (newItem, buffsNames);
        }

        public override void Clear()
        {
            base.Clear();
            BuffAdder.Clear();
            ConsumptionTime.value = 0;
            AreaRadious.value = 0;

            _changes = null;
            _args = null;
        }

        public override void Remove_Changes()
        {
            base.Remove_Changes();
            BuffAdder.Remove_Changes();
        }

        #region Saving
        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = true;

            result &= base.VerifyData(out var baseErrors);

            if (baseErrors != null && baseErrors.Count > 0)
                errors.AddRange(baseErrors);

            result &= ConsumptionTime.Verify_NegativaValue(errors, _highlighted);
            result &= AreaRadious.Verify_NegativaValue(errors, _highlighted);

            return result;
        }

        public override ModificationTypes Check_Changes()
        {
            try
            {
                if (_changes == null) return CurModificationType = ModificationTypes.Add;

                CurModificationType = ModificationTypes.None;
                var lastData = _changes as ConsumableItem;
                base.Check_Changes();

                if (BuffAdder.Check_Changes() != ModificationTypes.None)
                {
                    CurModificationType = ModificationTypes.EditData;
                }

                if (ConsumptionTime.value != lastData.ConsumptionTime)
                {
                    CurModificationType = ModificationTypes.EditData;
                }

                if (AreaRadious.value != lastData.AreaRadious)
                {
                    CurModificationType = ModificationTypes.EditData;
                }

                return CurModificationType;
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }
        }

        public override void Load_Changes()
        {
            base.Load_Changes();

            var changes = _changes as ConsumableItem;

            BuffAdder.Load_Changes();
            ConsumptionTime.value = changes.ConsumptionTime;
            AreaRadious.value = changes.AreaRadious;
        }
        #endregion
    }

    public record BuffsNamesDataArgs : ItemDataArgs
    {
        public List<string> BuffsNames { get; init; }

        public BuffsNamesDataArgs(List<string> buffs, string modelPath, string imgGUID) : base(modelPath, imgGUID)
        {
            BuffsNames = buffs;
        }
    }

    public record CreatedBuffsDataArgs : ItemDataArgs
    {
        public CreationData[] Buffs { get; init; }

        public CreatedBuffsDataArgs(CreationData[] buffs, string modelPath, string imgGUID) : base(modelPath, imgGUID)
        {
            Buffs = buffs;
        }
    }

    public struct NamedBuff
    {
        public string name;
        public BuffData? Data { get; init; }

        public NamedBuff(string name, BuffData? data)
        {
            this.name = name;
            Data = data;
        }
    }
}