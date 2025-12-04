namespace Burmuruk.RPGStarterTemplate.Editor
{
    internal class InvalidExeption : System.Exception
    {
        public override string Message => _message;
        private readonly string _message;

        public InvalidExeption(string name)
        {
            _message = name;
        }
    }

    internal class InvalidNameExeption : InvalidExeption
    {
        public InvalidNameExeption(string name = "Invalid name") : base(name)
        {
        }
    }

    internal class InvalidDataExeption : InvalidExeption
    {
        public InvalidDataExeption(string name = "Invalid information") : base(name)
        {
        }
    }
}
