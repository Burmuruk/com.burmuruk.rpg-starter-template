using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class CreditsExit : MonoBehaviour
    {
        [SerializeField] GameObject mainButtons;
        [SerializeField] GameObject credtis;

        public void HideCredits()
        {
            mainButtons.SetActive(true);
            credtis.SetActive(false);
        }
    }
}
