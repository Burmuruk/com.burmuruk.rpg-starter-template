namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class ReloadCreationsOperation
        : ISavingOperation<bool>
    {
        public bool Execute(
            SavingContext context)
        {
            return context.Creations.LoadAll();
        }
    }
}