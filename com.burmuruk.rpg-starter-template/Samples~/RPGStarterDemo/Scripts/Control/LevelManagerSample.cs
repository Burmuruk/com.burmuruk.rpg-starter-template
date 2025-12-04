using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.UI;
using Burmuruk.RPGStarterTemplate.UI.Samples;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class LevelManagerSample : LevelManager
    {
        [SerializeField] public string sceneName;
        protected UIMenuCharacters menuCharacters;

        protected override void Start()
        {
            base.Start();
            AddItemToDestroy(FindObjectOfType<SavingUI>().gameObject);
        }

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.K))
            {
                var data = CaptureLevelData();

                savingWrapper.Save(data["Slot"].ToObject<int>(), data);
            }

            if (Input.GetKeyUp(KeyCode.L))
            {
                TemporalSaver.RemoveAllData();
                savingWrapper.Load(GetSlotData().Id);
            }
        }

        public void ChangeMenu()
        {
            savingWrapper.AddNewAutoSaveSlot(CaptureLevelData(), true);

            gameManager.EnableUI(true);
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        public override void ToggleSavingOptions()
        {
            FindObjectOfType<SavingUI>().ToggleSlots();
        }

        public override void HideSavingOptions()
        {
            FindObjectOfType<SavingUI>().ShowSlots(false);
        }

        public override void ExitUI()
        {
            if (menuCharacters.curState != UIMenuCharacters.State.None) return;

            menuCharacters.UnloadMenu();
            GetComponentInChildren<Camera>(true).gameObject.SetActive(true);

            gameManager.ExitUI();
            Task.Delay(200).GetAwaiter().OnCompleted(() => savingWrapper.AddNewAutoSaveSlot(CaptureLevelData(), false));
        }

        protected override void VerifyScene(Scene scene, LoadSceneMode mode)
        {
            base.VerifyScene(scene, mode);

            if (scene.buildIndex == 1)
            {
                onUILoaded?.Invoke();
                Time.timeScale = 0;
                SceneManager.SetActiveScene(scene);
                var rootItems = SceneManager.GetSceneByBuildIndex(1).GetRootGameObjects();
                FindObjectOfType<LevelManager>().
                GetComponentInChildren<Camera>().gameObject.SetActive(false);
                var uiController = FindObjectOfType<UICharactersController>();

                foreach (var item in rootItems)
                {
                    menuCharacters = item.GetComponentInChildren<UIMenuCharacters>();

                    if (menuCharacters != null)
                    {
                        var pm = FindObjectOfType<PlayerManager>();
                        menuCharacters.SetPlayers(pm.Players);
                        menuCharacters.SetInventory(pm.MainInventory);
                        menuCharacters.SetPlayerManager(pm);

                        menuCharacters.OnMainPlayerChanged += playerManager.SetPlayerControl;
                        uiController.menuCharacters = menuCharacters;
                        uiController.gameManager = gameManager;
                        break;
                    }
                }
            }
        }

        protected override void UpdateGameState(GameManager.State state)
        {
            base.UpdateGameState(state);

            switch (state)
            {
                case GameManager.State.Playing:
                    FindObjectOfType<HUDManager>(true).gameObject.SetActive(true);
                    break;
                case GameManager.State.UI:
                    FindObjectOfType<HUDManager>().gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
        }
    }
}
