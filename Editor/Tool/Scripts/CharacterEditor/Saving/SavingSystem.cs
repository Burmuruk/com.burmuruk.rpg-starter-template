using Burmuruk.RPGStarterTemplate.Editor.Saving;
using System;
using System.Collections.Generic;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public static class SavingSystem
    {
        private static SavingContext context;
        private static bool initialized;

        public static event Action<ModificationTypes, ElementType, string, CreationData> OnCreationModified;

        public static CreationDatabase Data
        {
            get
            {
                EnsureInitialized();
                return context.Creations.Database;
            }
        }

        #region Initialization
        public static void ForceInit()
        {
            initialized = false;
            Initialize();
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            var jsonStore = new JsonStore();
            var database = new CreationDatabase();
            var creationSerializer = new CreationSerializer();
            var creationRepository = new CreationRepository(database, jsonStore, creationSerializer);
            var unsavedRepository = new UnsavedChangesRepository(jsonStore);
            var enumRepository = new EnumRepository(jsonStore);
            creationRepository.CreationModified += HandleCreationModified;

            context =
                new SavingContext(
                    creationRepository,
                    unsavedRepository,
                    enumRepository);

            creationRepository.LoadAll();
            enumRepository.Load();

            initialized = true;
        }

        private static void EnsureInitialized()
        {
            if (!initialized)
                Initialize();
        }

        private static void HandleCreationModified(ModificationTypes modification, ElementType type, string id, CreationData data)
        {
            OnCreationModified?.Invoke(modification, type, id, data);

            var info = new BaseCreationInfo(id, data?.Id, data);

            CreationScheduler.ChangeData(modification, type, id, info);
        }
        #endregion

        #region OPERATIONS
        public static TResult Execute<TResult>(ISavingOperation<TResult> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            EnsureInitialized();

            return operation.Execute(context);
        }
        #endregion

        #region COMPATIBILITY WRAPPERS
        public static bool SaveCreation(ElementType type, in string id, in CreationData data, ModificationTypes modificationType)
        {
            return Execute(new SaveCreationOperation(type, id, data, modificationType));
        }

        public static CreationData Load(ElementType type, string id)
        {
            return Execute(new LoadCreationOperation(type, id));
        }

        public static CreationData Load(string id)
        {
            return Execute(new LoadCreationByIdOperation(id));
        }

        public static bool Remove(ElementType type, string id)
        {
            return Execute(new RemoveCreationOperation(type, id));
        }

        public static CreationData GetCreation(ElementType type, string id)
        {
            return Load(type, id);
        }
        #endregion


        #region RELOAD
        public static bool ReloadFromDisk()
        {
            return Execute(new ReloadCreationsOperation());
        }

        /*
         * Mantiene el comportamiento anterior:
         * las creaciones ya están cargadas en memoria
         * y simplemente notificamos a la UI.
         */
        public static void LoadCreations()
        {
            EnsureInitialized();

            bool loadedAnything = false;

            foreach (var type in Data.creations)
            {
                foreach (var creation in type.Value)
                {
                    HandleCreationModified(
                        ModificationTypes.Add,
                        type.Key,
                        creation.Key,
                        creation.Value
                    );

                    loadedAnything = true;
                }
            }

            if (loadedAnything)
                Notify("Creations loaded", BorderColour.Success);
        }
        #endregion

        #region ASSET COMPATIBILITY
        public static string GetAssetReference(UnityEngine.Object asset)
        {
            return AssetReferenceProvider.GetReference(asset);
        }

        public static T GetAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetReferenceProvider.GetAsset<T>(path);
        }
        #endregion

        #region UNSAVED CHANGES
        public static bool SaveUnsavedChanges(CreationTabUIData data)
        {
            return Execute(new SaveUnsavedChangesOperation(data));
        }

        public static CreationTabUIData LoadUnsavedChanges()
        {
            return Execute(new LoadUnsavedChangesOperation());
        }

        public static bool ClearUnsavedChanges()
        {
            return Execute(new ClearUnsavedChangesOperation());
        }
        #endregion

        #region ENUMS
        public static void SaveEnumRegistry(EnumRegistry registry)
        {
            Execute(new SaveEnumRegistryOperation(registry));
        }

        public static EnumRegistry LoadEnumRegistry()
        {
            EnsureInitialized();

            return context.Enums.Registry;
        } 
        #endregion
    }

    public interface ISaveable : IDataVerifiable, IDataProvider
    {
        bool Save();
        CreationData Load(ElementType type, string id);
        CreationData Load(string id);
    }

    public interface IDataProvider
    {
        CreationData GetInfo();
        void UpdateInfo(CreationData creationData);
    }

    [Flags]
    public enum ModificationTypes
    {
        None = 0,
        Add = 1,
        Remove = 1 << 1,
        EditData = 1 << 2,
        Rename = 1 << 3,
        ColourReasigment = 1 << 4
    }

    public interface IChangesObserver : IDataVerifiable
    {
        ModificationTypes Check_Changes();
        void Load_Changes();
        void Remove_Changes();
    }

    public interface IUpdatableUI
    {
        void UpdateUIData<T>(T arg1) { }

        void UpdateUIData<T, U>(T arg1, U arg2) { }

        void UpdateUIData<T, U, R>(T arg1, U arg2, R arg3) { }
    }

    public interface IDataVerifiable
    {
        bool VerifyData(out List<string> errors);
    }

    public enum CreationsState
    {
        None,
        Creating,
        Editing,
    }
}