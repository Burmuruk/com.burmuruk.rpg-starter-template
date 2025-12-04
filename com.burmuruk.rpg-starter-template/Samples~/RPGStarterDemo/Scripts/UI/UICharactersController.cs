using Burmuruk.RPGStarterTemplate.Control;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class UICharactersController : MonoBehaviour
    {
        public UIMenuCharacters menuCharacters;
        public GameManager gameManager;

        private void Start()
        {
            menuCharacters = FindObjectOfType<UIMenuCharacters>();
        }

        public void RotatePlayer(InputAction.CallbackContext context)
        {
            if (GameManager.Instance.GameState != GameManager.State.UI)
                return;

            var value = context.ReadValue<Vector2>();

            menuCharacters.RotatePlayer(value);
            //if (context.performed)
            //{
            //    var dir = context.ReadValue<Vector2>();
            //    if (dir.magnitude <= 0)
            //    {
            //        m_shouldMove = false;
            //        return;
            //    }

            //    levelManager.RotatePlayer(dir);
            //    m_shouldMove = true;
            //}
            //else
            //{
            //    levelManager.RotatePlayer(Vector2.zero);
            //    m_shouldMove = false;
            //}
        }

        public void ShowMoreOptions(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (gameManager.GameState == GameManager.State.UI)
            {
                menuCharacters.SwitchExtraData();
            }
        }

        public void Remove(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (gameManager.GameState == GameManager.State.UI)
            {
                menuCharacters.TryRemoveItem();
            }
        }

        public void ChangeMenu(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (gameManager.GameState == GameManager.State.UI)
            {
                menuCharacters.ChangeMenu();
            }
        }

        public void ChangeCharacter(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (context.ReadValue<float>() > 0)
                menuCharacters.ShowNextPlayer();
            else
                menuCharacters.ShowPreviourPlayer();
        }
    }
}
