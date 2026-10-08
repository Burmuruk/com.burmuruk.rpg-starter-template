namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class SaveCreationOperation
        : ISavingOperation<bool>
    {
        private readonly ElementType type;
        private readonly string id;
        private readonly CreationData data;
        private readonly ModificationTypes modificationType;

        public SaveCreationOperation(ElementType type, string id, CreationData data, ModificationTypes modificationType)
        {
            this.type = type;
            this.id = id;
            this.data = data;
            this.modificationType = modificationType;
        }

        public bool Execute(SavingContext context)
        {
            return context.Creations.Save(type, id, data, modificationType);
        }
    }
}