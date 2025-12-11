using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        State state;
        [SerializeField] PlayerInput playerInput;

        public event Action<State> onStateChange;

        public State GameState
        {
            get => state;
            private set
            {
                if (state != value)
                    onStateChange?.Invoke(value);

                state = value;
            }
        }

        public enum State
        {
            Playing,
            Pause,
            UI,
            Loading,
            Cinematic
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool EnableUI(bool shouldEnable)
        {
            if (GameState != State.Playing)
                return false;

            GameState = State.UI;
            //playerInput.SwitchCurrentActionMap("UI");
            playerInput.actions.FindActionMap("UI").Enable();

            return true;
        }

        public bool FinishLevel()
        {
            return true;
        }

        public bool SetState(State GameState)
        {
            this.GameState = GameState;
            return true;
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(0);
        }

        public bool Continue()
        {
            if (GameState != State.Pause) return false;

            GameState = State.Playing;
            return true;
        }

        public bool PauseGame()
        {
            if (GameState != State.Playing) return false;

            GameState = State.Pause;

            return true;
        }

        public void ChangeScene(int idx)
        {
            GameState = State.Loading;
            SceneManager.LoadScene(idx);
        }

        public void ChangeScene(string sceneName)
        {
            GameState = State.Loading;
            SceneManager.LoadScene(sceneName);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void StartGame()
        {
            SceneManager.LoadScene(2);
        }

        public void StartCinematic(bool start)
        {
            if (start)
            {
                if (state != State.Playing) return;

                GameState = State.Cinematic;
            }
            else
            {
                if (state != State.Cinematic) return;

                GameState = State.Playing;
            }
        }
        
        public bool CanChangeToUI()
        {
            if (GameState != State.Playing)
                return false;

            return true;
        }

        public void ExitUI()
        {
            if (GameState != State.UI) return;

            if (SceneManager.GetSceneByBuildIndex(1).isLoaded)
            {
                SceneManager.UnloadSceneAsync(1);
            }
            else
            {

            }

            //playerInput.SwitchCurrentActionMap("Player");
            GameState = State.Playing;
        }

        public void NotifyLevelLoaded()
        {
            if (GameState == State.Loading)
                GameState = State.Playing;
        }
    }
}
