using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Control.AI;

namespace Burmuruk.RPGStarterTemplate.Interaction
{
    public class NPCDialogue : Interactable
    {
        AIGuildMember member;
        PlayerManager playerManager;
        AIGuildMember mainPlayer;

        public override void Interact()
        {
            base.Interact();

            playerManager = FindObjectOfType<PlayerManager>();
            var levelManager = FindObjectOfType<LevelManager>();

            member = playerManager.CreatePlayer();
            playerManager.AddMember(member);
            levelManager.SetPathToPlayer(member);
            //mainPlayer = playerManager.CurPlayer;

            Invoke("AddMember", 1);
        }

        private void AddMember()
        {
            //int i = 0;
            //foreach (var player in playerManager.Players)
            //{
            //    if (player == mainPlayer)
            //    {
            //        playerManager.SetPlayerControl(i);
            //        break;
            //    }
            //    ++i;
            //}

            member.MoveCloseToPlayer();
        }
    }
}
