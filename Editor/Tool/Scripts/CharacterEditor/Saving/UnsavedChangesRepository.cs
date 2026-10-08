using Newtonsoft.Json.Linq;

namespace Burmuruk.RPGStarterTemplate.Editor.Saving
{
    public sealed class UnsavedChangesRepository
    {
        private readonly JsonStore jsonStore;

        public UnsavedChangesRepository(JsonStore jsonStore)
        {
            this.jsonStore = jsonStore;
        }

        public void Save(CreationTabUIData data)
        {
            if (data == null)
                return;

            JObject root = jsonStore.Read();

            jsonStore.Set(root, JsonStore.UNSAVED_CHANGES_KEY, data.GetJson());

            jsonStore.Write(root);
        }

        public CreationTabUIData Load()
        {
            JObject root = jsonStore.Read();

            if (!jsonStore.TryGet(root, JsonStore.UNSAVED_CHANGES_KEY, out JObject json))
            {
                return null;
            }

            var data = new CreationTabUIData(null);

            data.RestoreFromJson(json);

            return data;
        }

        public bool Clear()
        {
            JObject root = jsonStore.Read();

            if (!jsonStore.Remove(root, JsonStore.UNSAVED_CHANGES_KEY))
            {
                return false;
            }

            jsonStore.Write(root);

            return true;
        }
    }
}