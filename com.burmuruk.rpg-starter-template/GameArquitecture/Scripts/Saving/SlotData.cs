namespace Burmuruk.RPGStarterTemplate.Saving
{
    public record SlotData (int Id, int BuildIdx, float PlayedTime)
    {
        public int MembersCount { get; init; }
    }
}

namespace System.Runtime.CompilerServices
{
    class IsExternalInit
    {

    }
}