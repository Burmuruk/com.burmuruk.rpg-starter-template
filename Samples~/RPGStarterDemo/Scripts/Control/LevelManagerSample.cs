using Burmuruk.RPGStarterTemplate.Movement.PathFindig;
using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.UI;
using Burmuruk.RPGStarterTemplate.UI.Samples;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class LevelManagerSample : LevelManager
    {
        [SerializeField] GameObject endScreen;
        [SerializeField] public string sceneName;
        protected UIMenuCharacters menuCharacters;

        protected override void Start()
        {
            base.Start();
            AddItemToDestroy(FindAnyObjectByType<SavingUI>(FindObjectsInactive.Include).gameObject);
            FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include)?.Init();
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
            RefreshRuntimeReferences();

            if (gameManager == null || !gameManager.CanChangeToUI() || playerManager == null || playerManager.CurPlayer == null)
                return;

            savingWrapper.AddNewAutoSaveSlot(CaptureLevelData(), false);

            gameManager.EnableUI(true);
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        public override void ToggleSavingOptions()
        {
            FindAnyObjectByType<SavingUI>().ToggleSlots();
        }

        public override void HideSavingOptions()
        {
            FindAnyObjectByType<SavingUI>().ShowSlots(false);
        }

        public override void ExitUI()
        {
            if (menuCharacters.curState != UIMenuCharacters.State.None) return;

            menuCharacters.UnloadMenu();
            GetComponentInChildren<Camera>(true).gameObject.SetActive(true);

            gameManager.ExitUI();
            StartCoroutine(SaveAfterLeavingInventory());
        }

        private System.Collections.IEnumerator SaveAfterLeavingInventory()
        {
            yield return new WaitForSecondsRealtime(.2f);

            RefreshRuntimeReferences();

            if (savingWrapper != null)
                savingWrapper.AddNewAutoSaveSlot(CaptureLevelData(), false);
        }

        public void EndGame()
        {
            if (gameManager.GameState != GameManager.State.Playing) return;

            if (gameManager.PauseGame())
            {
                endScreen?.SetActive(true);
                Time.timeScale = 0;
            }
        }

        //public override void Pause()
        //{
        //    if (gameManager.Continue())
        //    {
        //        pauseMenu.gameObject.SetActive(false);
        //        Time.timeScale = 1;

        //        HideSavingOptions();
        //    }
        //    else if (pauseMenu.gameObject.activeSelf)
        //    {
        //        pauseMenu.gameObject.SetActive(false);
        //        Time.timeScale = 1;

        //        HideSavingOptions();
        //    }
        //}

        protected override void LoadNavigationMap()
        {
#if UNITY_EDITOR
            NavSaver.Restart();
            // Resolve relative to this imported sample, including its versioned folder.
            string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(
                UnityEditor.MonoScript.FromMonoBehaviour(this));
            string assetsSamplePath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(scriptPath), "..", "..", "NavigationMaps"));

            if (!Directory.Exists(assetsSamplePath)) return;

            NavSaver.LoadNavMesh(assetsSamplePath);
            FindAnyObjectByType<LevelManager>()?.SetPaths();
#endif
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
                FindAnyObjectByType<LevelManager>().
                GetComponentInChildren<Camera>().gameObject.SetActive(false);
                var uiController = FindAnyObjectByType<UICharactersController>();

                foreach (var item in rootItems)
                {
                    menuCharacters = item.GetComponentInChildren<UIMenuCharacters>();

                    if (menuCharacters != null)
                    {
                        var pm = FindAnyObjectByType<PlayerManager>();
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
            HUDManager hud = FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);

            switch (state)
            {
                case GameManager.State.Playing:
                    hud?.gameObject.SetActive(true);
                    hud.SetVisible(true);
                    break;
                case GameManager.State.UI:
                    hud?.SetVisible(false);
                    break;
                default:
                    break;
            }
        }
    }
}
