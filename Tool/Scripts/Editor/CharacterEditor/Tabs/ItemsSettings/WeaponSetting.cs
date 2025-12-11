using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Inventory;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class WeaponSetting : ItemBuffReader
    {
        public UnsignedIntegerField Damage { get; private set; }
        public FloatField RateDamage { get; private set; }
        public FloatField MinDistance { get; private set; }
        public FloatField MaxDistance { get; private set; }
        public FloatField ReloadTime { get; private set; }
        public IntegerField MaxAmmo { get; private set; }
        public override ElementType ElementType => ElementType.Weapon;

        public EnumField EFBodyPart { get; private set; }
        public EnumModifierUI<WeaponType> EMWeaponType { get; private set; }

        public override void Initialize(VisualElement container, CreationsBaseInfo name)
        {
            base.Initialize(container, name);

            Damage = container.Q<UnsignedIntegerField>("txtDamage");
            RateDamage = container.Q<FloatField>("txtRateDamage");
            MinDistance = container.Q<FloatField>("MinDistance");
            MaxDistance = container.Q<FloatField>("MaxDistance");
            ReloadTime = container.Q<FloatField>("txtReloadTime");
            MaxAmmo = container.Q<IntegerField>("txtMaxAmmo");

            EFBodyPart = container.Q<EnumField>("efBodyPart");
            EFBodyPart.Init(EquipmentType.None);
            //var bodyPart = container.Q<EnumField>("efBodyPart");
            //bodyPart.Init(EquipmentPlace.None);

            var typeAdder = container.Q<VisualElement>("TypeAdderWeapon");
            EMWeaponType = new EnumModifierUI<WeaponType>(typeAdder);
            EMWeaponType.Name.text = "Weapon type";
            //var weaponType = container.Q<VisualElement>("TypeAdderWeapon");
            //weaponType.Q<Label>().text = "Weapon type";
            //weaponType.Q<EnumField>().Init(WeaponType.None);

            BuffAdder = new BuffAdderUI(container);
        }

        public override void UpdateInfo(InventoryItem data, ItemDataArgs args, ItemType type = ItemType.Weapon)
        {
            _changes = new Weapon();
            _args = args;
            base.UpdateInfo(data, args, type);

            var weapon = data as Weapon;
            var buffArgs = args as BuffsNamesDataArgs;

            //if (weapon == null) return;

            EFBodyPart.value = weapon.BodyPart;
            Damage.value = (uint)weapon.Damage;
            RateDamage.value = weapon.DamageRate;
            MinDistance.value = weapon.MinDistance;
            MaxDistance.value = weapon.MaxDistance;
            ReloadTime.value = weapon.ReloadTime;
            MaxAmmo.value = weapon.MaxAmmo;
            EMWeaponType.Value = (WeaponType)weapon.GetSubType();

            (_changes as Weapon).UpdateInfo(
                weapon.BodyPart, EMWeaponType.Value, weapon.Damage, weapon.DamageRate,
                weapon.MinDistance, weapon.MaxDistance, weapon.ReloadTime, weapon.MaxAmmo, weapon.Buffs);

            UpdateBuffs(weapon.Buffs, buffArgs);
        }

        public override void UpdateUIData<T, U>(T data, U args)
        {
            base.UpdateUIData(data, args);

            var weapon = data as Weapon;
            var buffArgs = args as BuffsNamesDataArgs;

            EFBodyPart.value = weapon.BodyPart;
            Damage.value = (uint)weapon.Damage;
            RateDamage.value = weapon.DamageRate;
            MinDistance.value = weapon.MinDistance;
            MaxDistance.value = weapon.MaxDistance;
            ReloadTime.value = weapon.ReloadTime;
            MaxAmmo.value = weapon.MaxAmmo;
            EMWeaponType.Value = (WeaponType)weapon.GetSubType();

            UpdateUIBuffs(weapon.Buffs, buffArgs);
        }

        public override (InventoryItem item, ItemDataArgs args) GetInfo(ItemDataArgs args)
        {
            Weapon weapon = new Weapon();
            var (baseInfo, baseArgs) = base.GetInfo(args);
            weapon.Copy(baseInfo);

            (var buffs, var buffsNames) = GetBuffsInfo();
            buffsNames = buffsNames with
            {
                PickupPath = baseArgs?.PickupPath,
                ImgGUID = baseArgs?.ImgGUID
            };

            weapon.UpdateInfo(
                (EquipmentType)EFBodyPart.value,
                EMWeaponType.Value,
                (int)unchecked(Damage.value),
                RateDamage.value,
                MinDistance.value,
                MaxDistance.value,
                ReloadTime.value,
                MaxAmmo.value,
                buffs.ToArray()
                );

            return (weapon, buffsNames);
        }

        public override void Clear()
        {
            base.Clear();
            Damage.value = 0;
            RateDamage.value = 0;
            MinDistance.value = 0;
            MaxDistance.value = 0;
            ReloadTime.value = 0;
            MaxAmmo.value = 0;
            EFBodyPart.value = EquipmentType.None;
            EMWeaponType.Clear();
            BuffAdder.Clear();

            _changes = null;
            _args = null;
        }

        public override void Remove_Changes()
        {
            base.Remove_Changes();
            BuffAdder.Remove_Changes();
        }

        public override ModificationTypes Check_Changes()
        {
            try
            {
                if (_changes == null) return CurModificationType = ModificationTypes.Add;

                base.Check_Changes();
                var _changesWeapon = _changes as Weapon;

                if (_changesWeapon.Damage != Damage.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (_changesWeapon.DamageRate != RateDamage.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (_changesWeapon.MinDistance != MinDistance.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (_changesWeapon.MaxDistance != MaxDistance.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (_changesWeapon.ReloadTime != ReloadTime.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (_changesWeapon.MaxAmmo != MaxAmmo.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if (_changesWeapon.BodyPart != (EquipmentType)EFBodyPart.value)
                {
                    CurModificationType = ModificationTypes.EditData;
                }
                if ((WeaponType)_changesWeapon.GetSubType() != EMWeaponType.Value)
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

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = true;

            result &= base.VerifyData(out var baseErrors);

            if (baseErrors != null && baseErrors.Count > 0)
                errors.AddRange(baseErrors);

            result &= RateDamage.Verify_NegativaValue(errors, _highlighted);
            result &= MinDistance.Verify_NegativaValue(errors, _highlighted);
            result &= MaxDistance.Verify_NegativaValue(errors, _highlighted);
            result &= ReloadTime.Verify_NegativaValue(errors, _highlighted);
            result &= MaxAmmo.Verify_NegativaValue(errors, _highlighted);

            return result;
        }

        public override CreationData Load(string id)
        {
            var result = SavingSystem.Load(id);

            if (result == null) return null;

            var weapon = result as BuffUserCreationData;
            _id = id;
            (var item, var args) = (weapon.Data, weapon.Names);
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(item, args);

            return weapon;
        }

        //public override void UpdateInfo(CreationData cd)
        //{
        //    var data = cd as BuffUserCreationData;
        //    var (bItem, args) = (data, data.Names);

        //    if (string.IsNullOrEmpty(cd.Id))
        //    {
        //        _creationsState = CreationsState.Creating;
        //    }
        //    else
        //    {
        //        _creationsState = CreationsState.Editing;
        //        Load()
        //    }

        //}

        public override void Load_Changes()
        {
            base.Load_Changes();

            var changes = _changes as Weapon;

            Damage.value = unchecked((uint)changes.Damage);
            RateDamage.value = changes.DamageRate;
            MinDistance.value = changes.MinDistance;
            MaxDistance.value = changes.MaxDistance;
            ReloadTime.value = changes.ReloadTime;
            MaxAmmo.value = changes.MaxAmmo;
            EFBodyPart.value = changes.BodyPart;
            EMWeaponType.Value = (WeaponType)changes.GetSubType();
            BuffAdder.Load_Changes();
        }
    }
}
