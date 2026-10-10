using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Burmuruk.RPGStarterTemplate.Control.Samples
{
    public class PlayerControllerSample : PlayerController
    {
        protected Animator playerAnimator;
        protected ConsumableItem selectedItem;

        public event Action<bool> OnFormationHold;
        public event Action<Vector2, object> OnFormationChanged;

        #region Unity methods
        private void OnEnable()
        {
            OnItemPicked += ItemPickedTrigger;
            OnInteract += ItemInteractTrigger;
            OnPickableEnter += SelectPickup;
            OnPickableExit += SelectPickup;

            if (selectedPickup != null)
                Select(selectedPickup.gameObject, true);
        }

        private void OnDisable()
        {
            if (selectedPickup != null)
                Select(selectedPickup.gameObject, false);

            OnItemPicked -= ItemPickedTrigger;
            OnInteract -= ItemInteractTrigger;
            OnPickableEnter -= SelectPickup;
            OnPickableExit -= SelectPickup;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (playerAnimator != null && player != null)
            {
                playerAnimator.SetInteger("Health", player.Health.HP);
                playerAnimator.SetFloat("Speed", player.mover.Veloctiy.magnitude);
            }
        }
        #endregion

        #region Animations and interactions
        private void SelectPickup(bool shouldSelect, string label, GameObject item)
        {
            Select(item, shouldSelect);
        }

        private void ItemInteractTrigger()
        {
            if (playerAnimator != null && player != null)
            {
                playerAnimator.SetTrigger("Interact");
            }
        }

        private void ItemPickedTrigger(string arg1, Vector3 vector)
        {
            if (playerAnimator != null && player != null)
            {
                playerAnimator.SetTrigger("PickUp");
            }
        }

        public void Select(GameObject item, bool shouldSelect)
        {
            if (item == null)
                return;

            foreach (Renderer rend in item.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in rend.materials)
                {
                    if (!material.shader.name.Contains("Outliner"))
                        continue;

                    material.SetFloat("_Enabled", shouldSelect ? 1 : 0);
                    break;
                }
            }
        }
        #endregion

        public override void SetPlayer(Character player)
        {
            base.SetPlayer(player);

            if (this.player != null && this.player.mover != null)
                this.player.mover.FacingTarget = null;

            playerAnimator = player.gameObject.GetComponent<Animator>();
            UpdateFacingTarget();
        }

        #region Inputs
        public void DisplayFormations(InputAction.CallbackContext context)
        {
            if (!player)
                return;

            if (context.performed)
            {
                m_canChangeFormation = true;

                OnFormationHold?.Invoke(true);
            }
            else
            {
                if (m_canChangeFormation)
                    OnFormationHold?.Invoke(false);

                m_canChangeFormation = false;
            }
        }

        public void ChangeFormation(InputAction.CallbackContext context)
        {
            if (!player || gameManager.GameState != GameManager.State.Playing)
                return;

            if (context.performed && m_canChangeFormation)
            {
                var dir = context.ReadValue<Vector2>();

                if (dir.y == -1 && Target == null)
                    return;

                object args = dir switch
                {
                    { y: -1 } => Target,
                    _ => null
                };

                OnFormationChanged?.Invoke(dir, args);
            }
        }

        public void Cross(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (gameManager.GameState != GameManager.State.Playing || !player)
                return;

            var value = context.ReadValue<Vector2>();

            switch (value)
            {
                case { y: < 0 }:
                    ConsumeItem();
                    break;

                case { x: < 0 }:
                    ChangeItem(-1);
                    break;

                case { x: > 0 }:
                    ChangeItem(1);
                    break;

                case { y: > 0 }:
                    //ShowItems()
                    break;

                default:
                    break;
            }
        }

        public void SelectTarget(InputAction.CallbackContext context)
        {
            if (!player)
                return;

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

        public void Move(InputAction.CallbackContext context)
        {
            if (!player)
                return;

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

        public void Interact(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (gameManager.GameState == GameManager.State.Cinematic)
            {
                CallOnInteract();
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
                CallOnItemPicked(itemName, pickedUpItem.transform.position);
                DetectItems();
            }
            else if (m_interactables.Count > 0)
            {
                m_interactables[0].Interact();
            }
        }

        public void Pause(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (gameManager.GameState == GameManager.State.UI)
            {
                levelManager.ExitUI();
            }
            else
            {
                levelManager.Pause();
            }
        }

        #endregion

        #region Abilities
        public override void UseAbility(Ability ability)
        {
            if (gameManager.GameState != GameManager.State.Playing)
                return;

            if (ability == null)
                return;

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

        public void UseAbility1(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            var abilities = (player.Inventory as InventoryEquipDecorator).Equipped.GetItems((int)EquipmentLocation.Abilities);

            if (abilities == null || abilities.Count <= 0)
                return;

            //(abilities[0] as Ability).Use(null, null);
        }

        public void UseAbility2(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;
        }

        public void UseAbility3(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;
        }

        public void UseAbility4(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;
        }
        #endregion

        protected void ConsumeItem()
        {
            var items = (player.Inventory as InventoryEquipDecorator).Equipped.GetItems((int)EquipmentLocation.Items);

            if (items == null || items.Count == 0)
                return;

            (items[0] as ConsumableItem).Use(player, null, null);
        }

        protected void ChangeItem(int v)
        {
            if (player.Inventory is null)
                return;

            var items = (player.Inventory as InventoryEquipDecorator).Equipped.GetItems((int)EquipmentLocation.Items);
        }
    }
}
