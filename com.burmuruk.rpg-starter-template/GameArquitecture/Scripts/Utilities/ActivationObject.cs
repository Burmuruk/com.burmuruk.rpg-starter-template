using Burmuruk.RPGStarterTemplate.Saving;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Utilities
{
    public class ActivationObject : MonoBehaviour, IJsonSaveable, ISerializationCallbackReceiver
    {
        [SerializeField] protected int _id = 0;
        [SerializeField] bool _enable = false;

        public bool Enabled
        {
            get
            {
                LoadTemporalInfo();

                return _enable;
            }
            set
            {
                Enable(value);
            }
        }

        public int Id
        {
            get
            {
                if (_id == 0)
                    _id = GetHashCode();

                return _id;
            }
        }

        public void Enable(bool shouldEnable)
        {
            _enable = true;
            TemporalSaver.Save(Id, true);
        }

        private void LoadTemporalInfo()
        {
            if (TemporalSaver.TryLoad(_id, out object data))
                _enable = (bool)data;
        }

        public JToken CaptureAsJToken(out SavingExecution execution)
        {
            execution = SavingExecution.Admin;
            JObject state = new JObject();

            state["Enabled"] = _enable;

            return state;
        }

        public void LoadAsJToken(JToken state)
        {
            _enable = (state as JObject)["Enabled"].ToObject<bool>();
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (_id == 0)
                _id = GetHashCode();
#endif
        }

        public void OnAfterDeserialize() { }
    }
}
