namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class LoadUnsavedChangesOperation
        : ISavingOperation<CreationTabUIData>
    {
        public CreationTabUIData Execute(
            SavingContext context)
        {
            return context.UnsavedChanges.Load();
        }
    }
}