using Burmuruk.RPGStarterTemplate.Control;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Interaction
{
    public class CheckPoint : MonoBehaviour, IInteractable
    {
        protected GameManager gameManager;
        protected LevelManager levelManager;

        protected virtual void Start()
        {
            gameManager = FindObjectOfType<GameManager>();
            levelManager = FindObjectOfType<LevelManager>();
        }

        public virtual void Interact()
        {
            if (!gameManager.CanChangeToUI()) return;
        }

        protected bool RefreshReferences()
        {
            gameManager = GameManager.Instance;

            if (gameManager == null) 
                gameManager = FindObjectOfType<GameManager>();

            levelManager = gameManager != null ? gameManager.GetComponent<LevelManager>() : null;

            if (levelManager == null) 
                levelManager = FindObjectOfType<LevelManager>();

            return gameManager != null && levelManager != null;
        }
    }
}
