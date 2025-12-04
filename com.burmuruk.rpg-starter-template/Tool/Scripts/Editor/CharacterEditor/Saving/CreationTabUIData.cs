using Burmuruk.RPGStarterTemplate.Editor.Saving.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    [Serializable]
    internal class CreationTabUIData : CreationData
    {
        public ElementType searchFilter;
        public float scrollPos;
        public List<TabUIData> elements;

        public CreationTabUIData(string name) : 
            base(name)
        {
        }

        public override JObject GetJson()
        {
            JObject json = base.GetJson();

            json["creationTab"] = JsonSerializerHelper.ConvertDynamicDataToJson(this.GetType(), this);

            return json;
        }

        override public void RestoreFromJson(JObject json)
        {
            var data = (CreationTabUIData)JsonSerializerHelper.FromJson((JObject)json["creationTab"]);

            Id = data.Id;
            searchFilter = data.searchFilter;
            scrollPos = data.scrollPos;
            elements = data.elements;
        }
    }

    [Serializable]
    internal struct TabUIData
    {
        public CreationData data;
        public ElementType type;
    }
}
