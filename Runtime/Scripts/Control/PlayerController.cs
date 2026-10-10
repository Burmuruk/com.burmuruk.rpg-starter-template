using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Interaction;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        protected Character player;
        protected GameManager gameManager;
        protected LevelManager levelManager;

        protected bool m_shouldMove = false;
        protected Vector3 m_direction = default;
        protected bool m_canChangeFormation = false;
        protected Dictionary<Transform, Pickup> m_pickables = new();
        protected List<IInteractable> m_interactables = new List<IInteractable>();
        protected int interactableIdx = 0;
        protected bool detachRotation = false;
        protected Pickup selectedPickup;

        enum Interactions
        {
            None,
            Pickable,
            Talk,
            Interact
        }

        public event Action<bool, string, GameObject> OnPickableEnter;
        public event Action<bool, string, GameObject> OnPickableExit;
        public event Action<string, Vector3> OnItemPicked;
        public event Action<bool, string> OnInteractableEnter;
        public event Action<bool, string> OnInteractableExit;
        public event Action OnInteract;

        public Character Target { get; protected set; }
        public bool HavePickable
        {
            get
            {
                if (selectedPickup != null && !selectedPickup.IsPicked)
                {
                    return true;
                }

                return false;
            }
        }

        #region Unity events
        private void Start()
        {
            gameManager = GetComponent<GameManager>();
            levelManager = GetComponent<LevelManager>();

            gameManager.onStateChange += OnGameStateChanged;
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.onStateChange -= OnGameStateChanged;
        }

        protected virtual void OnGameStateChanged(GameManager.State state)
        {
            if ((state == GameManager.State.Cinematic || state == GameManager.State.Playing) && player != null)
            {
                player.mover.CancelAll();
                player.mover?.ResetRoute();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!player)
                return;

            UpdateFacingTarget();

            if (m_shouldMove && player && GameManager.Instance.GameState == GameManager.State.Playing)
            {
                player.mover.MoveTo(player.transform.position + m_direction * 2, true);
            }

            DetectItems();
            DetectInteractables();
        }
        #endregion

        protected void CallOnItemPicked(string message, Vector3 position) => OnItemPicked?.Invoke(message, position);
        protected void CallOnInteract() => OnInteract?.Invoke();
        protected void CallOnPickableEnter(bool isActive, string message, GameObject item) => OnPickableEnter?.Invoke(isActive, message, item);
        protected void CallOnInteractableEnter(bool isActive, string message) => OnInteractableEnter?.Invoke(isActive, message);

        public virtual void SetPlayer(Character player)
        {
            if (this.player != null && this.player.mover != null)
                this.player.mover.FacingTarget = null;

            SetSelectedPickup(null);
            m_pickables.Clear();
            var vollider = player.GetComponent<CapsuleCollider>();
            this.player = player;
        }

        #region Actions and detections
        protected void UpdateFacingTarget()
        {
            var target = player.Target;
            Target = target != null && target.gameObject.activeInHierarchy &&
                target.TryGetComponent<Health>(out var targetHealth) && targetHealth.IsAlive
                ? target.GetComponent<Character>() : null;

            player.mover.FacingTarget = Target != null &&
                (GameManager.Instance == null || GameManager.Instance.GameState == GameManager.State.Playing)
                ? Target.transform : null;

            if (Target == null)
                detachRotation = false;
        }

        public virtual void UseAbility(Ability ability)
        {
            if (gameManager.GameState != GameManager.State.Playing)
                return;

            if (ability == null)
                return;

            switch ((AbilityType)ability.GetSubType())
            {
                default:
                    break;
            }
        }

        protected Collider DetectEnemyInMouse()
        {
            if (!player || gameManager.GameState != GameManager.State.Playing)
                return null;

            Ray ray = GetRayFromMouseToWorld();
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 200, 1 << LayerMask.NameToLayer("Character")))
            {
                return hit.collider;
            }

            return null;

            Ray GetRayFromMouseToWorld()
            {
                Vector3 mousePos = Mouse.current.position.ReadValue();
                var cam = mainCamera;

                Vector3 screenPos = new(mousePos.x, cam.pixelHeight - mousePos.y, cam.nearClipPlane);

                return cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            }
        }

        protected void DetectItems()
        {
            var items = Physics.OverlapSphere(player.transform.position, 1.5f, 1 << 11);
            m_pickables.Clear();
            Pickup first = null;

            foreach (var item in items)
            {
                var pickup = item.GetComponent<Pickup>();

                if (pickup == null || pickup.IsPicked)
                    continue;

                m_pickables[pickup.transform] = pickup;

                if (first == null)
                    first = pickup;
            }

            if (selectedPickup != null && m_pickables.ContainsKey(selectedPickup.transform))
                return;

            SetSelectedPickup(first);
        }

        private void SetSelectedPickup(Pickup pickup)
        {
            if (ReferenceEquals(selectedPickup, pickup))
                return;

            var previous = selectedPickup;
            selectedPickup = pickup;

            if (!ReferenceEquals(previous, null))
                OnPickableExit?.Invoke(false, "", previous != null ? previous.gameObject : null);

            if (pickup != null)
                OnPickableEnter?.Invoke(true, "Pick up", pickup.gameObject);
        }

        protected void DetectInteractables()
        {
            var items = Physics.OverlapSphere(player.transform.position, 1f, 1 << 11);
            var hadItem = m_interactables.Count > 0;
            m_interactables.Clear();

            foreach (var item in items)
            {
                var cmp = item.GetComponent<IInteractable>();

                if (cmp != null)
                {
                    m_interactables.Add(cmp);
                }
            }

            if (hadItem && m_interactables.Count <= 0)
            {
                OnInteractableExit?.Invoke(false, "");
            }
            else if (m_interactables.Count > 0)
            {
                OnInteractableEnter?.Invoke(true, "Interact");
            }
        }
        #endregion
    }
}
