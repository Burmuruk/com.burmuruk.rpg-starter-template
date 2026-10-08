namespace Burmuruk.RPGStarterTemplate.Editor
{
    public struct EnumModificationData
    {
        public string id;
        public string name;
        public int order;

        public EnumModificationData(string id, string name, int order)
        {
            this.id = id;
            this.name = name;
            this.order = order;
        }

        public void Deconstruct(out string id, out string name, out int order)
        {
            id = this.id;
            name = this.name;
            order = this.order;
        }
    }
}
