using Newtonsoft.Json.Linq;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public record SlotData (int Id, int BuildIdx, float PlayedTime, int MembersAmount)
    {
        public const string SlotKey = "Slot";
        public const string BuildIndexKey = "BuildIdx";
        public const string TimePlayedKey = "TimePlayed";
        public const string MembersAmountKey = "MembersCount";
        public const string ImageKey = "Image";

        public int MembersCount { get; init; }
    }
}

namespace System.Runtime.CompilerServices
{
    class IsExternalInit
    {

    }
}