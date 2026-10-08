namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class SaveUnsavedChangesOperation
        : ISavingOperation<bool>
    {
        private readonly CreationTabUIData data;

        public SaveUnsavedChangesOperation(
            CreationTabUIData data)
        {
            this.data = data;
        }

        public bool Execute(
            SavingContext context)
        {
            if (data == null)
                return false;

            context.UnsavedChanges.Save(data);

            return true;
        }
    }
}