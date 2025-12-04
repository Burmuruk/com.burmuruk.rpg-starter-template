using UnityEngine;
using UnityEngine.Events;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameObject mainMenu;
        [SerializeField] Animator creditsAnimator;
        [SerializeField] UnityEvent onMenuOpened;

        private void OnEnable()
        {
            onMenuOpened?.Invoke();
        }

        public void ShowMenu(bool shouldShow)
        {
            mainMenu?.SetActive(shouldShow);
        }

        public void HideCredits()
        {
            creditsAnimator.enabled = false;
        }

        public void LoadGame(int idx)
        {

        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void ShowControls(GameObject controlsPanel)
        {
            controlsPanel.SetActive(!controlsPanel.activeSelf);
        }

        public void ShowSavingTab(GameObject savingPanel)
        {
            savingPanel.SetActive(!savingPanel.activeSelf);
        }
    }
}
