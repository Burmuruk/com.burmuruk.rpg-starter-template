using Burmuruk.RPGStarterTemplate.Inventory;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ArmourSetting : BaseItemSetting
    {
        public EnumModifierUI<EquipmentType> EquipmentPlace { get; private set; }

        public override void Initialize(VisualElement container, CreationsBaseInfo nameControl)
        {
            base.Initialize(container, nameControl);

            EquipmentPlace = new EnumModifierUI<EquipmentType>(container);
        }

        public override void UpdateInfo(InventoryItem data, ItemDataArgs args, ItemType type = ItemType.Armor)
        {
            _changes = new ArmourElement();
            _args = args;
            base.UpdateInfo(data, args, type);
            var armour = data as ArmourElement;

            //if (armour == null) return;

            EquipmentPlace.Value = (EquipmentType)armour.GetEquipLocation();
            (_changes as ArmourElement).UpdateInfo(EquipmentPlace.Value);
        }

        public override void UpdateUIData<T, U>(T data, U args)
        {
            base.UpdateUIData(data, args);
            var armour = data as ArmourElement;

            EquipmentPlace.Value = (EquipmentType)armour.GetEquipLocation();
        }

        public override (InventoryItem item, ItemDataArgs args) GetInfo(ItemDataArgs args)
        {
            ArmourElement armour = new ArmourElement();
            var (baseInfo, newArgs) = base.GetInfo(args);
            armour.Copy(baseInfo);

            armour.UpdateInfo((EquipmentType)EquipmentPlace.EnumField.value);

            return (armour, newArgs);
        }

        public override ModificationTypes Check_Changes()
        {
            try
            {
                if (_changes == null) return CurModificationType = ModificationTypes.Add;

                base.Check_Changes();
                var location = (EquipmentType)(_changes as ArmourElement).GetEquipLocation();

                if (location != EquipmentPlace.Value)
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
            var (data, args) = GetInfo(null);
            var creationData = new ItemCreationData(TxtName.value, data as ArmourElement, args);

            return SavingSystem.SaveCreation(ElementType.Armour, in _id, creationData, CurModificationType);
        }

        public override void Clear()
        {
            base.Clear();

            EquipmentPlace.Value = EquipmentType.None;
            _changes = null;
            _args = null;
        }

        public override void Load_Changes()
        {
            base.Load_Changes();

            var changes = _changes as ArmourElement;
            EquipmentPlace.Value = (EquipmentType)changes.GetEquipLocation();
        }
    }

    public record ArmourDataArgs : ItemDataArgs
    {
        public EquipmentType EquipmentPlace { get; private set; }

        public ArmourDataArgs(EquipmentType equipmentPlace, string modelPath, string imgGUID) : base(modelPath, imgGUID)
        {
            EquipmentPlace = equipmentPlace;
        }
    }
}
