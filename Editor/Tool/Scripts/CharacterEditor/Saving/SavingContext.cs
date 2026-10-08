namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class SavingContext
    {
        public CreationRepository Creations { get; }
        public UnsavedChangesRepository UnsavedChanges { get; }
        public EnumRepository Enums { get; }

        public SavingContext(CreationRepository creations, UnsavedChangesRepository unsavedChanges, EnumRepository enums)
        {
            Creations = creations;
            UnsavedChanges = unsavedChanges;
            Enums = enums;
        }
    }
}