using Burmuruk.RPGStarterTemplate.Control.Samples;

namespace Burmuruk.RPGStarterTemplate.Interaction.Samples
{
    public class CheckPointSample : CheckPoint
    {
        new LevelManagerSample levelManager;

        override protected void Start()
        {
            base.Start();
            levelManager = FindObjectOfType<LevelManagerSample>();
        }

        override public void Interact()
        {
            levelManager.ChangeMenu();
        }
    }
}
