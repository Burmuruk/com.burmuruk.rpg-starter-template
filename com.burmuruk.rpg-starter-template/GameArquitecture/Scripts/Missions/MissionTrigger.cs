using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Utilities;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Missions
{
    public class MissionTrigger : ActivationObject
    {
        [SerializeField] Mission mission;



        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out AIGuildMember member) && member.IsControlled)
            {
                FindObjectOfType<MissionManager>().AddMission(mission);

                Enable(true);
            }
        }
    }
}
