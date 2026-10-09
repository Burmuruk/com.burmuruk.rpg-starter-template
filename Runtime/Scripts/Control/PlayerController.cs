using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Interaction;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
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
        protected Dictionary<Transform, Pickup> m_pickables = new ();
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

        public Character Target { get; private set; }
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
            if (!player) return;

            UpdateFacingTarget();

            if (m_shouldMove && player && GameManager.Instance.GameState == GameManager.State.Playing)
            {
                player.mover.MoveTo(player.transform.position + m_direction * 2, true);
            }

            DetectItems();
            DetectInteractables();
        }

        private void UpdateFacingTarget()
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

        public virtual void SetPlayer(Character player)
        {
            if (this.player != null && this.player.mover != null)
                this.player.mover.FacingTarget = null;

            SetSelectedPickup(null);
            m_pickables.Clear();
            var vollider = player.GetComponent<CapsuleCollider>();
            this.player = player;

            UpdateFacingTarget();
        }

        #region Inputs
        public void Move(InputAction.CallbackContext context)
        {
            if (!player) return;

            if (gameManager.GameState != GameManager.State.Playing)
                return;

            if (context.performed)
            {
                var dir = context.ReadValue<Vector2>();
                if (dir.magnitude <= 0)
                {
                    m_shouldMove = false;
                    return;
                }

                m_direction = new Vector3(dir.x, 0, dir.y).normalized;
                m_shouldMove = true;
            }
            else
            {
                m_direction = Vector3.zero;
                m_shouldMove = false;
            }
        }

        public void SelectTarget(InputAction.CallbackContext context)
        {
            if (!player) return;

            if (context.performed)
            {
                var enemy = DetectEnemyInMouse();

                if (enemy)
                {
                    var newTarget = enemy.GetComponent<Character>();

                    if (newTarget == null || newTarget.Health == null || !newTarget.Health.IsAlive) 
                        return;

                    var playerRef = (AIGuildMember)player;

                    if (Target != null && Target == newTarget)
                    {
                        Target.Deselect();
                        Target = null;
                        detachRotation = false;
                        playerRef.Retreat();
                    }
                    else if (enemy.CompareTag(player.EnemyTag))
                    {
                        Target = newTarget;
                        Target.Select();
                        //print(enemy.itemName);
                        playerRef.AutoAttackEnemy(Target);
                        //playerRef.AttackEnemy(Target);
                        detachRotation = true;
                    }

                    UpdateFacingTarget();
                }
            }
        }

        public void Interact(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (gameManager.GameState == GameManager.State.Cinematic)
            {
                OnInteract?.Invoke();
                return;
            }

            if (HavePickable)
            {
                var pickedUpItem = selectedPickup;

                if (!player.Inventory.Add(pickedUpItem.ID)) 
                    return;
                //var inventory = GetComponent<InventoryEquipDecorator>();
                //inventory.AddVariable(pickedUpItem.itemType, pickedUpItem);
                //inventory.TryEquip(player, pickedUpItem.itemType, pickedUpItem.GetSubType());
                //pickedUpItem.gameObject.SetActive(false);
                pickedUpItem.PickUp();
                //m_pickables.RemoveVariable(pickedUpItem.transform);

                var itemName = player.Inventory.GetItem(pickedUpItem.ID).Name;
                OnItemPicked?.Invoke(itemName, pickedUpItem.transform.position);
                DetectItems();
            }
            else if (m_interactables.Count > 0)
            {
                m_interactables[0].Interact();
            }
        }

        public void Pause(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (gameManager.GameState == GameManager.State.UI)
            {
                levelManager.ExitUI();
            }
            else
            {
                levelManager.Pause();
            }
        }

        public void UseAbility1(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            var abilities = (player.Inventory as InventoryEquipDecorator).Equipped.GetItems((int)EquipmentLocation.Abilities);

            if (abilities == null || abilities.Count <= 0)
                return;

            //(abilities[0] as Ability).Use(null, null);
        }

        public void UseAbility2(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
        }

        public void UseAbility3(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
        }

        public void UseAbility4(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
        }
        #endregion

        #region Actions and detections
        public void UseAbility(Ability ability)
        {
            if (gameManager.GameState != GameManager.State.Playing)
                return;

            if (ability == null) return;

            switch ((AbilityType)ability.GetSubType())
            { 
                case AbilityType.None:
                    break;
                case AbilityType.Dash:
                    //ability.Use();
                    break;
                case AbilityType.StealHealth:
                    break;
                case AbilityType.Jump:
                    break;
            }
        }

        protected Collider DetectEnemyInMouse()
        {
            if (!player || gameManager.GameState != GameManager.State.Playing) return null;

            Ray ray = GetRayFromMouseToWorld();
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 200, 1 << 10))
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

        private void OnTriggerEnter(Collider other)
        {
            //Physics.OverlapSphere(transform.position, .5f, 1<<11);
            //if (other.gameObject.GetComponent<Consumable>() is var itemType && itemType)
            //{
            //    player.inventory.AddVariable(Type.Consumable, itemType);
            //    Destroy(other.gameObject);
            //}
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

        //private void TakeItem()
        //{
        //    var pickedUpItem = m_pickables[0];
        //    var inventory = player.GetComponent<InventoryEquipDecorator>();
        //    inventory.AddVariable(pickedUpItem.itemType, pickedUpItem);
        //    //inventory.ExecuteElementAction(pickedUpItem.modifiableStat, pickedUpItem.Item);
        //    pickedUpItem.gameObject.SetActive(false);

        //    m_pickables.RemoveVariable(pickedUpItem);

        //    OnItemPicked?.Invoke(pickedUpItem.itemType.ToString(), pickedUpItem.transform.position);
        //}
        #endregion
    }
}
