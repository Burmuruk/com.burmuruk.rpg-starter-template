namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class SaveEnumRegistryOperation
        : ISavingOperation<bool>
    {
        private readonly EnumRegistry registry;

        public SaveEnumRegistryOperation(
            EnumRegistry registry)
        {
            this.registry = registry;
        }

        public bool Execute(
            SavingContext context)
        {
            if (registry == null)
                return false;

            context.Enums.Save(registry);

            return true;
        }
    }
}