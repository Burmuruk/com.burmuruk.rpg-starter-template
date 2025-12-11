using Burmuruk.RPGStarterTemplate.Control.AI;
using UnityEngine;
using UnityEngine.Events;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class SceneChanger : MonoBehaviour
    {
        [SerializeField] protected int nextSceneBuildIdx;
        [SerializeField] protected UnityEvent OnTriggered;
        private int _id = 0;

        public void DisableAndSave()
        {

        }

        public void OnAfterDeserialize() { }

        public void OnBeforeSerialize()
        {
            if (_id == 0)
                _id = GetHashCode();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out AIGuildMember member))
            {
                OnTriggered?.Invoke();

                if (member == FindObjectOfType<PlayerManager>().CurPlayer)
                {
                    FindObjectOfType<GameManager>().ChangeScene(nextSceneBuildIdx);
                }
            }
        }
    }
}
