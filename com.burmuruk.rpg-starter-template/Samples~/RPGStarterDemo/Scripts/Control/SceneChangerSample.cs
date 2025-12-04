using Burmuruk.RPGStarterTemplate.Control.AI;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class SceneChangerSample : SceneChanger
    {
        [SerializeField] public string sceneName;

        protected override void OnTriggerEnter(UnityEngine.Collider other)
        {
            if (other.gameObject.TryGetComponent(out AIGuildMember member))
            {
                OnTriggered?.Invoke();

                if (member == FindObjectOfType<PlayerManager>().CurPlayer)
                {
                    FindObjectOfType<GameManager>().ChangeScene(sceneName);
                }
            }
        }
    }
}
