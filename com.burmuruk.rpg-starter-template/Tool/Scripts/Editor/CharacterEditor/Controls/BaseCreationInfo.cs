namespace Burmuruk.RPGStarterTemplate.Editor
{
    public struct BaseCreationInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly CreationData data;

        public BaseCreationInfo(string id, string name, CreationData data)
        {
            this.Id = id;
            this.Name = name;
            this.data = data;
        }
    }
}
