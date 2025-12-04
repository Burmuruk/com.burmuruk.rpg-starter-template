using Burmuruk.RPGStarterTemplate.Inventory;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class BaseItemSetting : BaseInfoTracker, ISaveable
    {
        protected string _id = null;
        protected InventoryItem _changes;
        protected ItemDataArgs _args = null;

        public override string Id => _id;
        public TextField TxtDescription { get; private set; }
        public ObjectField OfSprite { get; private set; }
        public ObjectField OfPickup { get; private set; }
        public UnsignedIntegerField UfCapacity { get; private set; }
        public virtual ElementType ElementType { get; }

        public override void Initialize(VisualElement container, CreationsBaseInfo nameControl)
        {
            base.Initialize(container, nameControl);

            TxtDescription = container.Q<TextField>("txtDescription");
            OfSprite = container.Q<ObjectField>("opSprite");
            OfPickup = container.Q<ObjectField>("opPickup");
            UfCapacity = container.Q<UnsignedIntegerField>("txtCapacity");

            OfSprite.objectType = typeof(Sprite);
            OfPickup.objectType = typeof(GameObject);
            _nameControl.TxtName.RegisterValueChangedCallback((evt) =>
            {
                if (IsActive)
                    TempName = evt.newValue;
            });
        }

        public virtual void UpdateInfo(InventoryItem data, ItemDataArgs args, ItemType type = ItemType.Consumable)
        {
            TempName = data.Name;
            _originalName = data.Name;
            UpdateName();

            TxtDescription.value = data.Description;
            OfSprite.value = args?.GetSprite();
            OfPickup.value = args?.GetPickupPrefab();
            UfCapacity.value = (uint)data.Capacity;

            _changes ??= new InventoryItem();
            _changes.UpdateInfo(data.Name, data.Description, type, (Sprite)OfSprite.value, null, unchecked((int)UfCapacity.value));
            _args = args;
        }

        public virtual void UpdateInfo(CreationData cd)
        {
            var data = cd as ItemCreationData;
            
            if (!string.IsNullOrEmpty(data.Id))
            {
                _creationsState = CreationsState.Editing;
                Load(data.Id);
            }
            else
            {
                _creationsState = CreationsState.Creating;
            }
            
            _id = cd.Id;
            UpdateUIData(data.Data, data.args);
        }

        public virtual void UpdateUIData<T, U>(T data, U args) where T : InventoryItem where U : ItemDataArgs
        {
            if (string.IsNullOrEmpty(_id))
                _originalName = data.Name;
            TempName = data.Name;
            UpdateName(); 
            
            TxtDescription.value = data.Description;
            OfSprite.value = args?.GetSprite();
            OfPickup.value = args?.GetPickupPrefab();
            UfCapacity.value = (uint)data.Capacity;
        }

        public virtual (InventoryItem item, ItemDataArgs args) GetInfo(ItemDataArgs args)
        {
            var data = new InventoryItem();
            ItemDataArgs newArgs;

            newArgs = new ItemDataArgs(
                SavingSystem.GetAssetReference(OfPickup.value),
                SavingSystem.GetAssetReference(OfSprite.value));

            data.UpdateInfo(
                TempName,
                TxtDescription.value,
                ItemType.None,
                (Sprite)OfSprite.value,
                null,
                unchecked((int)UfCapacity.value)
                );

            return (data, newArgs);
        }

        public virtual CreationData GetInfo()
        {
            var (item, args) = GetInfo(null);
            return new ItemCreationData(_id, item, args);
        }


        public override void Clear()
        {
            TxtDescription.value = "";
            OfSprite.value = null;
            OfPickup.value = null;
            UfCapacity.value = 0;
            CurModificationType = ModificationTypes.None;
            _changes = null;
            _args = null;
            _id = null;
            base.Clear();
        }

        protected void ClearItemInfo()
        {
            _changes.UpdateInfo("", "", _changes.Type, null, null, 0);
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = true;

            result &= _nameControl.VerifyData(out errors);

            return result;
        }

        public override ModificationTypes Check_Changes()
        {
            try
            {
                if (_changes == null) return CurModificationType = ModificationTypes.Add;

                CurModificationType = ModificationTypes.None;

                if (_nameControl.Check_Changes() != ModificationTypes.None)
                    CurModificationType = ModificationTypes.Rename;

                if (TxtDescription.value != _changes.Description)
                    CurModificationType = ModificationTypes.EditData;

                if (OfSprite.value != _changes.Sprite)
                    CurModificationType = ModificationTypes.EditData;

                if (OfPickup.value != _args?.GetPickupPrefab())
                    CurModificationType = ModificationTypes.EditData;

                if (UfCapacity.value != _changes.Capacity)
                    CurModificationType = ModificationTypes.EditData;

                return CurModificationType;
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }
        }

        public virtual bool Save()
        {
            if (!VerifyData(out var errors))
            {
                Utilities.UtilitiesUI.Notify(errors.Count <= 0 ? "Invalid Data" : errors[0], BorderColour.Error);
                return false;
            }

            CurModificationType = Check_Changes();
            if (_creationsState == CreationsState.Editing && Check_Changes() == ModificationTypes.None)
            {
                Utilities.UtilitiesUI.Notify("No changes were found", BorderColour.HighlightBorder);
                return false;
            }
            else
                CurModificationType = ModificationTypes.Add;

            Utilities.UtilitiesUI.DisableNotification(NotificationType.Creation);
            var (data, args) = GetInfo(null);
            var creationData = new ItemCreationData(_nameControl.TxtName.value, data, args);

            return SavingSystem.SaveCreation(ElementType.Item, in _id, creationData, CurModificationType);
        }

        public virtual CreationData Load(ElementType type, string id)
        {
            var result = SavingSystem.Load(type, id);

            if (result == null) return null;

            _id = id;
            var item = (result as ItemCreationData);
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(item.Data, item.args);

            return result;
        }

        public virtual CreationData Load(string id)
        {
            CreationData result = SavingSystem.Load(id);

            if (result == null) return null;

            _id = id;
            var item = (result as ItemCreationData);
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(item.Data, item.args);

            return result;
        }

        public override void Load_Changes()
        {
            foreach (var element in _highlighted)
                Utilities.UtilitiesUI.Set_Tooltip(element.Key, element.Value, false);

            TempName = _changes.name;
            UpdateName();
            TxtDescription.value = _changes.Description;
            OfSprite.value = _changes.Sprite;
            OfPickup.value = _args?.GetPickupPrefab();
            UfCapacity.value = (uint)_changes.Capacity;
            CurModificationType = ModificationTypes.None;
        }

        public override void Remove_Changes()
        {
            _changes = null;
            _args = null;
            _id = null;
        }
    }

    public interface IClearable
    {
        public abstract void Clear();
    }

    public record ItemDataArgs
    {
        public string PickupPath { get; init; }
        public string ImgGUID { get; init; }

        public ItemDataArgs(string pickupPath, string imgGUID)
        {
            this.PickupPath = pickupPath;
            ImgGUID = imgGUID;
        }

        public Sprite GetSprite() => SavingSystem.GetAsset<Sprite>(ImgGUID);

        public GameObject GetPickupPrefab() => SavingSystem.GetAsset<GameObject>(PickupPath);


    }
}

namespace System.Runtime.CompilerServices
{
    public interface IsExternalInit { }
}
