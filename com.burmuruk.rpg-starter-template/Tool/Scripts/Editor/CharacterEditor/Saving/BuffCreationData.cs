using Newtonsoft.Json.Linq;
using System;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    [Serializable]
    public class BuffCreationData : CreationData
    {
        public Stats.BuffData Data;

        public BuffCreationData(string name, Stats.BuffData data) : base(name)
        {
            Data = data;
        }

        public override JObject GetJson()
        {
            var status = base.GetJson();
            var BuffsData = new JObject()
            {
                { "name", Data.name },
                { "stat", (int)Data.stat },
                { "value", Data.value },
                { "duration", Data.duration },
                { "rate",  Data.rate },
                { "percentage", Data.percentage },
                { "probability", Data.probability }
            };
            status["buffData"] = BuffsData;

            return status;
        }

        public override void RestoreFromJson(JObject json)
        {
            base.RestoreFromJson(json);
            var data = json["buffData"];

            Data = new Stats.BuffData()
            {
                name = data["name"].ToObject<string>(),
                stat = (Stats.ModifiableStat)data["stat"].ToObject<int>(),
                value = data["value"].ToObject<int>(),
                duration = data["duration"].ToObject<float>(),
                rate = data["rate"].ToObject<float>(),
                percentage = data["percentage"].ToObject<bool>(),
                probability = data["probability"].ToObject<float>()
            };
        }
    }
}
