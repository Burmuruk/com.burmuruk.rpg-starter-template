namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class LoadCreationByIdOperation
        : ISavingOperation<CreationData>
    {
        private readonly string id;

        public LoadCreationByIdOperation(string id)
        {
            this.id = id;
        }

        public CreationData Execute(
            SavingContext context)
        {
            return context.Creations.Load(id);
        }
    }
}