namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class ClearUnsavedChangesOperation
        : ISavingOperation<bool>
    {
        public bool Execute(
            SavingContext context)
        {
            return context.UnsavedChanges.Clear();
        }
    }
}