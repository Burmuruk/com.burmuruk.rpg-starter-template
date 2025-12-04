
using Newtonsoft.Json.Linq;

namespace Burmuruk.RPGStarterTemplate.Saving
{
    public interface IJsonSaveable
    {
        JToken CaptureAsJToken(out SavingExecution execution);
        void LoadAsJToken(JToken state);
    }
}
