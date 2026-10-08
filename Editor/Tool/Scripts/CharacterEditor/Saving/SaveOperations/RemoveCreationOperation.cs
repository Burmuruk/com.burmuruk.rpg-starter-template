namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class RemoveCreationOperation
        : ISavingOperation<bool>
    {
        private readonly ElementType type;
        private readonly string id;

        public RemoveCreationOperation(
            ElementType type,
            string id)
        {
            this.type = type;
            this.id = id;
        }

        public bool Execute(
            SavingContext context)
        {
            return context.Creations.Remove(type, id);
        }
    }
}