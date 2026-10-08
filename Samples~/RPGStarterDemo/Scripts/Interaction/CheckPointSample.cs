using Burmuruk.RPGStarterTemplate.Control.Samples;

namespace Burmuruk.RPGStarterTemplate.Interaction.Samples
{
    public class CheckPointSample : CheckPoint
    {
        override public void Interact()
        {
            if (!RefreshReferences() || !gameManager.CanChangeToUI()) 
                return;

            if (levelManager is LevelManagerSample currentLevel)
                currentLevel.ChangeMenu();
        }
    }
}
