using Newtonsoft.Json.Linq;
using System;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    [Serializable]
    public class CreationData
    {
        public string Id;

        public CreationData(string id)
        {
            Id = id;
        }

        public virtual JObject GetJson()
        {
            var status = new JObject();
            status["id"] = Id;
            return status;
        }

        public virtual void RestoreFromJson(JObject json)
        {
            Id = json["id"].ToObject<string>();
        }
    }
}
