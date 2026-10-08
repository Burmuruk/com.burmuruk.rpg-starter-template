namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class LoadCreationOperation
        : ISavingOperation<CreationData>
    {
        private readonly ElementType type;
        private readonly string id;

        public LoadCreationOperation(
            ElementType type,
            string id)
        {
            this.type = type;
            this.id = id;
        }

        public CreationData Execute(
            SavingContext context)
        {
            return context.Creations.Load(type, id);
        }
    }
}