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
        
        enum Interactions
        {
            None,
            Pickable,
            Talk,
            Interact
        }

        public event Action<bool, string> OnPickableEnter;
        public event Action<bool, string> OnPickableExit;
        public event Action<string, Vector3> OnItemPicked;
        public event Action<bool, string> OnInteractableEnter;
        public event Action<bool, string> OnInteractableExit;
        public event Action OnInteract;

        public Character Target { get; private set; }
        public bool HavePickable
        {
            get
            {
                if (m_pickables.Count > 0)
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
        }

        void FixedUpdate()
        {
            if (m_shouldMove && player && GameManager.Instance.GameState == GameManager.State.Playing)
            {
                try
                {
                    if (player.Target)
                    {
                        if (detachRotation)
                            player.mover.DetachRotation = true;

                        if (player.Target != Target) //Asssigns new target
                            Target = player.Target.gameObject.GetComponent<Character>();

                        player.transform.LookAt(Target.transform);
                    }
                    else
                        detachRotation = false;

                    player.mover.MoveTo(player.transform.position + m_direction * 2, true);
                }
                catch (NullReferenceException)
                {

                    throw;
                }
            }

            DetectItems();
            DetectInteractables();
        }

        public void SetPlayer(Character player)
        {
            var vollider = player.GetComponent<CapsuleCollider>();
            this.player = player;
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
                var pickedUpItem = m_pickables.First().Value;
                player.Inventory.Add(pickedUpItem.ID);
                //var inventory = GetComponent<InventoryEquipDecorator>();
                //inventory.AddVariable(pickedUpItem.itemType, pickedUpItem);
                //inventory.TryEquip(player, pickedUpItem.itemType, pickedUpItem.GetSubType());
                //pickedUpItem.gameObject.SetActive(false);
                pickedUpItem.PickUp();
                //m_pickables.RemoveVariable(pickedUpItem.transform);

                var itemName = player.Inventory.GetItem(pickedUpItem.ID).Name;
                OnItemPicked?.Invoke(itemName, pickedUpItem.transform.position);
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

        protected void ConsumeItem()
        {
            var items = (player.Inventory as InventoryEquipDecorator).Equipped.GetItems((int)EquipmentLocation.Items);

            if (items == null || items.Count == 0) return;

            (items[0] as ConsumableItem).Use(player, null, null);
        }

        protected void ChangeItem(int v)
        {
            throw new NotImplementedException();
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
            var hadItem = m_pickables.Count > 0;
            //m_pickables.Clear();
            Dictionary<Transform, Pickup> newList = new();
            List<Pickup> newPickables = new();

            foreach (var item in items)
            {
                var cmp = item.GetComponent<Pickup>();
                if (cmp)
                {
                    if (!m_pickables.ContainsKey(cmp.transform))
                    {
                        newPickables.Add(cmp);
                    }
                    else
                    {
                        m_pickables.Remove(cmp.transform);
                    }    
                    
                    newList.Add(cmp.transform, cmp);
                }
            }

            if (newList.Count <= 0)
                foreach (var item in m_pickables)
                    OnPickableExit?.Invoke(false, "");

            foreach (var item in newPickables)
                OnPickableEnter?.Invoke(true, "Tomar"/* + m_items[0].modifiableStat.ToString()*/);

            m_pickables = newList;
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
                OnPickableExit?.Invoke(false, "");
            }
            else if (m_interactables.Count > 0)
            {
                OnPickableEnter?.Invoke(true, "Interact");
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
