using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class UIMenuCharacters : MonoBehaviour
    {
        #region Variables
        [Header("Extra references")]
        [SerializeField] CharacterProgress characterProgress;
        [SerializeField] PlayerCustomization customization;
        [SerializeField] ItemsList itemsList;
        [Space(), Header("Characters")]
        [SerializeField] GameObject characterModel;
        [SerializeField] GameObject charactersMenu;
        [SerializeField] Image[] playersImg;
        [SerializeField] Image mainPlayerImg;
        [SerializeField] GameObject colorsPanel;
        [Space(), Header("Elements")]
        [SerializeField] StackableLabel elementPanel;
        [SerializeField] TextMeshProUGUI txtExtraInfo;
        [SerializeField] float rotationVelocity;
        [SerializeField] MyItemButton[] WarningButtons;
        [SerializeField] Image[] btnModificationSlots;
        [SerializeField] TextMeshProUGUI txtWarning;
        [SerializeField] GameObject AmountPanel;
        [Space(), Header("Abilities")]
        [SerializeField] GameObject abilitiesMenu;
        [SerializeField] Image[] btnHumanAbilities;
        [SerializeField] Image[] btnAlienAbilities;
        [SerializeField] Image[] btnAbilitiesSlot;
        [SerializeField] Color selectedColor;
        [SerializeField] Sprite defaultBTNSprite;
        [SerializeField] TextMeshProUGUI txtAbilityInfo;
        [SerializeField] TextMeshProUGUI txtAbilityTitle;

        public State curState = State.None;
        Vector2 direction;
        int curPlayerIdx;
        int curTabIdx;
        bool showDescription = false;
        int curElementId = 0;
        int curBtnId = 0;
        int? curModificationSlot;
        int? curAbiltySlot;
        int curAbilityId;
        bool isAbilitySlotUsed = false;

        PlayerManager playerManager;
        List<AIGuildMember> players;
        InventoryTab curInventoryTab;
        IInventory inventory;
        WarningProblem curWarningProblem;
        Menu curMenu = Menu.Inventory;
        MyItemButton[] btnColors;
        Dictionary<int, Image> btnColorsDict;
        Dictionary<int, (Image image, int subType)> btnAbilitiesDict;
        Dictionary<int, (Image image, int subType)> btnAbilitiesSlotDict;
        Dictionary<int, (Image image, int subType)> btnModsDict;
        Dictionary<int, (StackableNode panel, EquipeableItem item)> curElementLabels = new();

        public event Action<int> OnMainPlayerChanged;

        public enum State
        {
            None,
            Notice,
            Loading
        }
        enum InventoryTab
        {
            None,
            Modifications,
            Weapons,
            Inventory
        }
        enum WarningProblem
        {
            None,
            EquipEquiped,
            RemoveEquiped
        }
        enum Menu
        {
            None,
            Inventory,
            Abilities,
        }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            elementPanel.Initialize();
            WarningButtons[0].onClick.AddListener(AceptWarning);
            WarningButtons[1].onClick.AddListener(CancelWarning);

            var first = elementPanel.Get();
            elementPanel.Release(first);
        }

        private void Start()
        {
            colorsPanel.SetActive(false);
        }

        private void Update()
        {
            Rotate();
        } 
        #endregion

        #region Elements panel
        public void ShowModifications()
        {
            if (curState != State.None) return;

            ShowElements(GetSortedItems(InventoryTab.Modifications));
            curInventoryTab = InventoryTab.Modifications;
        }

        public void ShowWeapons()
        {
            if (curState != State.None) return;

            ShowElements(GetSortedItems(InventoryTab.Weapons));
            curInventoryTab = InventoryTab.Weapons;
        }

        public void ShowInventory()
        {
            if (curState != State.None) return;

            ShowElements(GetSortedItems(InventoryTab.Inventory));
            curInventoryTab = InventoryTab.Inventory;
        }

        public void SwitchExtraData()
        {
            if (curState != State.None) return;

            showDescription = !showDescription;
            ShowExtraData(showDescription);
        }

        public void SelectElement(int id)
        {
            if (curState != State.None) return;

            curElementId = id;
            ShowExtraData(showDescription);
        }

        public void ShowExtraData(bool showInfo)
        {
            if (!curElementLabels.ContainsKey(curElementId)) return;

            if (showInfo)
            {
                txtExtraInfo.text = curElementLabels[curElementId].item.Description;
            }
            else
            {
                txtExtraInfo.text = curElementLabels[curElementId].item.Name;
            }
        }

        public void ExecuteElementAction(int idx)
        {
            switch ((curElementLabels[idx].item.Type))
            {
                case ItemType.Consumable:
                    ConsumeItem(curElementLabels[idx].item);
                    break;

                case ItemType.Ability:
                case ItemType.Weapon:
                case ItemType.Modification:
                case ItemType.Armor:

                    EquipItem(idx);
                    break;

                default:
                    break;
            }
        }

        private void ConsumeItem(EquipeableItem equipable)
        {
            var item = (equipable as ConsumableItem);
            int id = item.ID;

            item.Use(players[curPlayerIdx], null, TryRemoveItem);
        }

        public void ElementCancelAction(int idx)
        {
            switch ((curElementLabels[idx].item.Type))
            {
                case ItemType.Consumable:
                    break;

                case ItemType.Ability:
                case ItemType.Weapon:
                case ItemType.Modification:
                case ItemType.Armor:

                    UnEquipItem(idx);
                    break;

                default:
                    break;
            }

            txtWarning.transform.parent.gameObject.SetActive(false);
            curWarningProblem = WarningProblem.None;
            curState = State.None;
        }

        public void TryRemoveItem()
        {
            if (!curElementLabels.ContainsKey(curElementId)) return;

            var item = curElementLabels[curElementId].item;
            if (!inventory.Remove(item.ID)) return;

            if (inventory.GetItemCount(item.ID) > 0)
            {
                SetElementInfo(curElementLabels[curElementId].panel, item);
            }
            else
            {
                RemoveElement(curElementLabels[curElementId].panel);
            }
        }

        public void ChangeMenu()
        {
            switch (curMenu)
            {
                case Menu.None:
                    break;
                case Menu.Inventory:
                    ShowAbilitiesMenu();
                    break;

                case Menu.Abilities:
                    ShowInventoryMenu();
                    break;

                default:
                    break;
            }
        }

        public void AceptWarning()
        {
            switch (curWarningProblem)
            {
                case WarningProblem.None:
                    break;
                case WarningProblem.EquipEquiped:
                    ChangeEquiped();
                    goto default;

                case WarningProblem.RemoveEquiped:
                    RemoveItem();
                    goto default;

                default:
                    txtWarning.transform.parent.gameObject.SetActive(false);
                    curWarningProblem = WarningProblem.None;
                    curState = State.None;
                    break;
            }
        }

        private void ShowAbilitiesMenu()
        {
            abilitiesMenu.SetActive(true);
            characterModel.SetActive(false);

            charactersMenu.SetActive(false);
            curMenu = Menu.Abilities;
        }

        private void ShowInventoryMenu()
        {
            charactersMenu.SetActive(true);

            characterModel.SetActive(true);
            abilitiesMenu.SetActive(false);
            curMenu = Menu.Inventory;
        }

        private void ChangeEquiped()
        {
            var equippedItem = curElementLabels[curElementId].item;
            var lastPlayer = equippedItem.Characters.Last();
            var inventoryDecorator = (lastPlayer.Inventory as InventoryEquipDecorator);
            List<EquipeableItem> unequipped;

            inventoryDecorator.Unequip(lastPlayer, equippedItem);
            inventoryDecorator.TryEquip(players[curPlayerIdx], equippedItem, out unequipped);

            SetPlayersColors(unequipped);
            SetPlayersColors(equippedItem, curElementLabels[curElementId].panel);
            ShowCharacterModel();
            
            if (curAbiltySlot.HasValue)
            {
                if (isAbilitySlotUsed)
                {
                    UnequipCurrentAbility();
                    isAbilitySlotUsed = false;
                }

                SetAbilityInSlot(curAbilityId);
            }
        }

        private void SetPlayersColors(List<EquipeableItem> items)
        {
            if (items != null)
            {
                foreach (var equipable in items)
                {
                    var id = FindLabelById(equipable.ID);

                    if (id < 0) return;

                    SetPlayersColors(curElementLabels[id].item, curElementLabels[id].panel);
                }
            }
        }

        private int FindLabelById(int id)
        {
            foreach (var label in curElementLabels)
            {
                if (label.Value.item.ID == id)
                {
                    return label.Key;
                }
            }

            return -1;
        }

        private void RemoveItem()
        {
            var item = curElementLabels[curElementId].item;

            var lastPlayer = item.Characters.Last();
            var inventoryDecorator = (inventory as InventoryEquipDecorator);

            inventoryDecorator.Unequip(lastPlayer, item);
            ShowCharacterModel();

            inventory.Remove(item.ID);

            if (inventory.GetItemCount(item.ID) > 0)
            {
                SetElementInfo(curElementLabels[curElementId].panel, item);
            }
            else
            {
                RemoveElement(curElementLabels[curElementId].panel);
            }
        }

        private void EquipItem(int idx)
        {
            var curElementLabel = curElementLabels[idx];
            var item = curElementLabel.item;
            List<EquipeableItem> unequipped = null;
            (players[curPlayerIdx].Inventory as InventoryEquipDecorator).TryEquip(players[curPlayerIdx], item, out unequipped);

            SetPlayersColors(unequipped);
            SetPlayersColors(curElementLabel.item, curElementLabel.panel);
            ShowCharacterModel();
            UpdateModsSprites();
        }

        private void UnEquipItem(int idx)
        {
            var curElementLabel = curElementLabels[idx];
            var item = curElementLabel.item;
            (players[curPlayerIdx].Inventory as InventoryEquipDecorator).Unequip(players[curPlayerIdx], curElementLabel.item);

            SetPlayersColors(curElementLabel.item, curElementLabel.panel);
            ShowCharacterModel();
            UpdateModsSprites();
        }

        private void ShowElements(List<InventoryItem> items)
        {
            CleanElements();
            if (items.Count == 0) return;

            int i = 0;
            foreach (var item in items)
            {
                var panel = elementPanel.Get();

                var equippedItem = item as EquipeableItem;

                if (equippedItem == null)
                    continue;
                
                SetElementInfo(panel, equippedItem);

                int buttonId = i++;
                SubscribeToEvents(panel, buttonId);

                curElementLabels.Add(buttonId, (panel, equippedItem));
            }

            curElementId = 0;
            ShowExtraData(true);

            void SubscribeToEvents(StackableNode panel, int buttonId)
            {
                var button = panel.label.transform.parent.GetComponent<MyItemButton>();
                button.SetId(buttonId);
                button.onClick.AddListener(() => { ExecuteElementAction(buttonId); });
                button.OnRightClick += () => ElementCancelAction(buttonId);
                button.OnPointerEnterEvent += SelectElement;
            }
        }

        private void SetElementInfo(StackableNode panel, EquipeableItem item)
        {
            panel.label.text = item.Name;

            TextMeshProUGUI txtCount = null;

            foreach (var label in panel.label.transform.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (label.gameObject != panel.label.gameObject)
                {
                    txtCount = label;
                    break;
                }
            }

            txtCount.text = inventory.GetItemCount(item.ID).ToString();

            SetPlayersColors(item, panel);
        }

        private void SetPlayersColors(EquipeableItem equipedItem, StackableNode panel)
        {
            Image[] images = panel.image.transform.GetComponentsInChildren<Image>(true)
                .Where(image => image.transform != panel.image.transform)
                .OrderBy(i => i.transform.name)
                .ToArray();
            
            if (equipedItem.IsEquip)
            {
                List<Color> colors = new();

                foreach (var player in players)
                {
                    foreach (var character in equipedItem.Characters)
                    {
                        if (player.stats.color == character.stats.color)
                        {
                            colors.Add(character.stats.color);
                            break;
                        }
                    }
                }
                
                for (int i = 0; i < images.Count(); i++)
                {
                    if (i < colors.Count())
                    {
                        images[i].gameObject.SetActive(true);
                        images[i].color = colors[i];
                    }
                    else
                    {
                        images[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                foreach (var image in images)
                {
                    image.gameObject.SetActive(false);
                }
            }
        }

        private void UpdatePlayersColors()
        {
            foreach (var element in curElementLabels.Values)
            {
                SetPlayersColors(element.item, element.panel);
            }
        }

        private void CleanElements()
        {
            while (elementPanel.activeNodes.Count > 0)
            {
                RemoveElement(elementPanel.activeNodes[0]);
            }

            curElementLabels.Clear();
        }

        private void RemoveElement(StackableNode node)
        {
            node.label.transform.parent.GetComponent<MyItemButton>().onClick.RemoveAllListeners();
            elementPanel.Release(node);
        }

        private List<InventoryItem> GetSortedItems(InventoryTab tab)
        {
            List<InventoryItem> items = null;

            ItemType type = tab switch
            {
                InventoryTab.Modifications => ItemType.Modification,
                InventoryTab.Weapons => ItemType.Weapon,
                _ => ItemType.None
            };

            if (type != ItemType.None)
                items = inventory.GetList(type);
            else
            {
                items = inventory.GetList(ItemType.Modification);
                items.AddRange(inventory.GetList(ItemType.Consumable));
                items.AddRange(inventory.GetList(ItemType.Weapon));
                items.AddRange(inventory.GetList(ItemType.Armor));
            }

            items.OrderBy(item => item.Name);

            return items;
        }

        private void ShowEquipedWarning()
        {
            curState = State.Notice;
            if (txtWarning != null)
                txtWarning.text = "El objeto está actualmente equipado.\n¿Desea equiparlo?";
            else
            {
                txtWarning = GameObject.FindGameObjectsWithTag("Respawn")[0].GetComponent<TextMeshProUGUI>();
                txtWarning.text = "El objeto está actualmente equipado.\n¿Desea equiparlo?";
            }
            WarningButtons[0].transform.parent.gameObject.SetActive(true);
        }

        private void ShowDeleteEquipedWarning()
        {
            curState = State.Notice;
            txtWarning.text = "El objeto está actualmente equipado.\n¿Desea eliminarlo?";
            WarningButtons[0].transform.parent.gameObject.SetActive(true);
        }

        private void ShowAmountNotice()
        {

        }

        private void CancelWarning()
        {
            WarningButtons[0].transform.parent.gameObject.SetActive(false);
            curState = State.None;
            curWarningProblem = WarningProblem.None;
        }
        #endregion

        #region Characters panel
        public void ShowCharacters()
        {
            var mainPlayer = players[0].Leader;
            mainPlayerImg.gameObject.SetActive(false);

            playersImg[0].color = players[GetNextPlayerIdx(-1)].stats.color;
            playersImg[1].color = players[curPlayerIdx].stats.color;
            playersImg[2].color = players[GetNextPlayerIdx(1)].stats.color;

            int[] idxs = new int[3]
            {
                curPlayerIdx, -1, 1
            };
            for (int i = 0; i < 3; i++)
            {
                if (players[GetNextPlayerIdx(idxs[i])] == mainPlayer)
                {
                    int idx = 1 - i < 0 ? 2 : 1 - i;

                    SelectMainPlayerButton(playersImg[idx]);
                    break;
                }
            }

            ShowCharacterModel();
            UpdateModsSprites();

            if (curMenu == Menu.Abilities)
            {
                ShowCharacterAbilities();
            }
        }

        private void SelectMainPlayerButton(Image playerImg)
        {
            mainPlayerImg.rectTransform.position = playerImg.rectTransform.position;
            mainPlayerImg.rectTransform.sizeDelta = playerImg.rectTransform.sizeDelta + playerImg.rectTransform.sizeDelta / 10;
            mainPlayerImg.gameObject.SetActive(true);
        }

        public void ShowNextPlayer()
        {
            if (curState != State.None) return;

            curPlayerIdx = GetNextPlayerIdx(1);
            ShowCharacters();
        }

        public void ShowPreviourPlayer()
        {
            if (curState != State.None) return;

            curPlayerIdx = GetNextPlayerIdx(-1);
            ShowCharacters();
        }

        public void ChangePlayerColor()
        {
            colorsPanel.SetActive(!colorsPanel.activeSelf);
        }

        public void ChangeMainCharacter()
        {
            OnMainPlayerChanged?.Invoke(curPlayerIdx);
            ShowCharacters();
        }

        public void SelectBtnColor(int id) => curBtnId = id;

        private void ShowCharacterAbilities()
        {
            Ability[] abilities = (from ability in inventory.GetList(ItemType.Ability)
                             where ((EquipeableItem)ability).Characters.Contains(players[curPlayerIdx])
                             select (Ability)inventory.GetItem(ability.ID))
                             .ToArray();

            int j = 0;
            for (int i = 0; i < btnAbilitiesSlotDict.Values.Count; i++)
            {
                var slot = btnAbilitiesSlotDict[i];

                if (j < abilities.Length)
                {
                    slot.image.sprite = abilities[j].Sprite;
                    btnAbilitiesSlotDict[i] = (btnAbilitiesSlotDict[i].image, abilities[j].ID);
                    j++;
                    continue;
                }

                slot.image.sprite = defaultBTNSprite;
                btnAbilitiesSlotDict[i] = (btnAbilitiesSlotDict[i].image, -1);
            }
        }

        private void ChangeColor()
        {
            var lastColor = players[curPlayerIdx].stats.color;
            players[curPlayerIdx].stats.color = btnColorsDict[curBtnId].color;
            btnColorsDict[curBtnId].color = lastColor;

            print("ChangeColor");
            ShowCharacters();
            colorsPanel.SetActive(false);
            UpdatePlayersColors();
        }

        private void ShowCharacterModel()
        {
            RemovePreviousModel();
            var prefab = players[curPlayerIdx].Equipment.GetItem((int)EquipmentType.None); ;
            var inst = Instantiate(prefab, characterModel.transform);
            inst.transform.localPosition = Vector3.zero;

            void RemovePreviousModel()
            {
                if (characterModel.transform.childCount > 0)
                {
                    Destroy(characterModel.transform.GetChild(0).gameObject);
                    //while (characterModel.transform.childCount > 0)
                    //{
                    //}
                }
            }
        }

        private int GetNextPlayerIdx(int idx)
        {
            if (players.Count == 1)
            {
                return curPlayerIdx;
            }
            else if (players.Count == 2)
            {
                return curPlayerIdx == 0 ? 1 : 0;
            }
            else if (players.Count > 2)
            {
                if (idx < 0)
                    return curPlayerIdx == 0 ? players.Count - 1 : curPlayerIdx - 1;
                else
                    return curPlayerIdx == players.Count - 1 ? 0 : curPlayerIdx + 1;
            }

            return 0;
        }

        private void UpdateColorButtons(bool shouldInit)
        {
            btnColors = colorsPanel.GetComponentsInChildren<MyItemButton>(true);
            if (shouldInit) 
                btnColorsDict = new();

            for (int i = 0, btnId = 0; i < btnColors.Length; i++)
            {
                var btn = btnColors[i];

                if (i >= customization.Colors.Length || !VerifyColor(i))
                {
                    btn.gameObject.SetActive(false);
                    continue;
                }

                if (shouldInit)
                    InitColorButton(btn, in btnId);

                btnColorsDict[btn.GetId()].color = customization.Colors[i];
                btn.gameObject.SetActive(true);
                btnId++; 
            }
        }

        bool VerifyColor(int idx)
        {
            foreach (var player in players)
            {
                if (player.stats.color == customization.Colors[idx])
                    return false;
            }

            return true;
        }

        private void InitColorButton(MyItemButton btn, in int btnId)
        {
            btn.SetId(btnId);
            btn.OnPointerEnterEvent += SelectBtnColor;
            btn.onClick.AddListener(ChangeColor);
            btnColorsDict.Add(btnId, btn.GetComponent<Image>());
        }
        #endregion

        #region Initialization
        public void SetInventory(IInventory inventory)
        {
            this.inventory = inventory;

            foreach (var player in players)
            {
                var inventoryDecorator = (player.Inventory as InventoryEquipDecorator);

                inventoryDecorator.OnTryAlreadyEquiped += () =>
                {
                    curWarningProblem = WarningProblem.EquipEquiped;
                    ShowEquipedWarning();
                };
                inventoryDecorator.OnTryDeleteEquiped += () =>
                {
                    curWarningProblem = WarningProblem.RemoveEquiped;
                    ShowDeleteEquipedWarning();
                };
            }
            curInventoryTab = InventoryTab.Modifications; 

            ShowInventory();
            UpdateModsSprites();
            InitializeAbilityButtons();
        }

        public void SetPlayerManager(PlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        public void UnloadMenu()
        {
            foreach (var player in players)
            {
                var inventoryDecorator = (player.Inventory as InventoryEquipDecorator);

                inventoryDecorator.OnTryAlreadyEquiped -= () =>
                {
                    curWarningProblem = WarningProblem.EquipEquiped;
                    ShowEquipedWarning();
                };
                inventoryDecorator.OnTryDeleteEquiped -= () =>
                {
                    curWarningProblem = WarningProblem.RemoveEquiped;
                    ShowDeleteEquipedWarning();
                }; 
            }
        }

        public void SetPlayers(List<AIGuildMember> players)
        {
            this.players = players;

            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].enabled)
                {
                    curPlayerIdx = i;
                    break;
                }
            }

            ShowCharacters();
            UpdateColorButtons(true);
            //playersImg[1] = this.players[curPlayerIdx];
        }

        private void UpdateModsSprites()
        {
            if (inventory == null) return;

            var mods = (from mod in inventory.GetList(ItemType.Modification)
                       let equiped = (EquipeableItem)mod
                       where equiped != null && equiped.IsEquip && equiped.Characters.Contains(players[curPlayerIdx])
                       select (Modification)inventory.GetItem(mod.ID)
                       ).ToArray();

            int i = 0;
            foreach (var modSlot in btnModificationSlots)
            {
                if (i < mods.Length)
                {
                    modSlot.sprite = mods[i++].Sprite;
                }
                else
                {
                    modSlot.sprite = defaultBTNSprite;
                }
            }
        }
        #endregion

        #region Model
        public void RotatePlayer(Vector2 direction)
        {
            this.direction = direction;
        }

        private void Rotate()
        {
            characterModel.transform.Rotate(Vector3.up, direction.x * rotationVelocity * -1);
        } 
        #endregion

        #region Abilities
        private void InitializeAbilityButtons()
        {
            btnAbilitiesDict = new();
            btnAbilitiesSlotDict = new();
            txtAbilityInfo.text = "";
            txtAbilityTitle.text = "";

            for (int i = 0; i < btnAbilitiesSlot.Length; i++)
            {
                int idx = i;
                btnAbilitiesSlotDict.Add(idx, (btnAbilitiesSlot[i], -1));

                var button = btnAbilitiesSlot[i].transform.parent.GetComponent<MyItemButton>();
                button.onClick.AddListener(() => SelectAbilitySlot(idx));
                button.OnRightClick += () => UnequipAbilty(idx);
            }

            var collection = btnHumanAbilities;

            for (int i = 0, j = 0, k = 0; i < collection.Count() && j < 2; i++, k++)
            {
                int idx = k;
                AddAbilitiesImage(idx, collection[i]);

                if (i == btnHumanAbilities.Count() - 1)
                {
                    collection = btnAlienAbilities;
                    j++;
                    i = 0;
                }
            }
        }

        private bool AddAbilitiesImage(int idx, Image image)
        {
            string name = image.transform.parent.name;

            if (!Int32.TryParse(name.Substring(6, name.Length - 6), out var subType))
                return false;
            
            if (Enum.IsDefined(typeof(AbilityType), subType))
            {
                var id = GetAbilityId(subType);

                btnAbilitiesDict.Add(idx, (image, id));

                if (inventory.GetItem(id) is var item && item != null)
                {
                    var ability = (Ability)item;
                    image.sprite = ability.Sprite;
                    image.transform.parent.gameObject.SetActive(true);
                    return true;
                }
            }
            else
                btnAbilitiesDict.Add(idx, (image, -1));

            image.transform.parent.gameObject.SetActive(false);
            return false;
        }

        private int GetAbilityId(int type)
        {
            var abilities = itemsList.GetList(ItemType.Ability);

            foreach (var ability in abilities)
            {
                if (((int)ability.GetSubType()) == type)
                {
                    return ability.ID;
                }
            }

            return -1;
        }

        private void ShowAbilityInfo(int id)
        {
            var ability = (Ability)inventory.GetItem(id);

            txtAbilityTitle.text = ability.Name;
            txtAbilityInfo.text = ability.Description;
        }

        private void UnequipAbilty(int id)
        {
            if (curState == State.Notice) return;

            if (curAbiltySlot.HasValue)
            {
                ChangeSelectedColor();
                curAbiltySlot = null;
            }

            curAbiltySlot = id;
            UnequipCurrentAbility();
            curAbiltySlot = null;
        }

        private void SelectAbilitySlot(int idx)
        {
            if (curState == State.Notice) return;

            if (curAbiltySlot.HasValue)
            {
                ChangeSelectedColor();

                if (curAbiltySlot.Value == idx)
                {
                    curAbiltySlot = null;
                    return;
                }
                else if (btnAbilitiesSlotDict[curAbiltySlot.Value].subType >= 0)
                {
                    isAbilitySlotUsed = true;
                }
            }

            curAbiltySlot = idx;
            ChangeSelectedColor();
        }

        private void UnequipCurrentAbility()
        {
            var slot = btnAbilitiesSlotDict[curAbiltySlot.Value];

            var item = inventory.GetItem(slot.subType);
            (players[curPlayerIdx].Inventory as InventoryEquipDecorator).Unequip(players[curPlayerIdx], (EquipeableItem)item);

            slot.image.sprite = defaultBTNSprite;
            btnAbilitiesSlotDict[curAbiltySlot.Value] = (slot.image, -1);
        }

        public void SelectAbility(int idx)
        {
            if (curState == State.Notice) return;

            if (!curAbiltySlot.HasValue)
            {
                ShowAbilityInfo(idx);
                return;
            }

            curAbilityId = idx;
            EquipAbility(idx);

            ChangeSelectedColor();
            curAbiltySlot = null;
        }

        private void EquipAbility(int idx)
        {
            var item = itemsList.Get(btnAbilitiesDict[idx].subType);
            if ((players[curPlayerIdx].Inventory as InventoryEquipDecorator).TryEquip(players[curPlayerIdx], item, out _))
            {
                if (isAbilitySlotUsed)
                {
                    UnequipCurrentAbility();
                    isAbilitySlotUsed = false;
                }
                SetAbilityInSlot(idx);
            }
        }

        private void SetAbilityInSlot(int abiltyId)
        {
            var slotImage = btnAbilitiesSlotDict[curAbiltySlot.Value].image;
            slotImage.sprite = btnAbilitiesDict[abiltyId].image.sprite;

            btnAbilitiesSlotDict[curAbiltySlot.Value] = (slotImage, btnAbilitiesDict[abiltyId].subType);
        }

        private void ChangeSelectedColor()
        {
            var parentImage = btnAbilitiesSlotDict[curAbiltySlot.Value].image.transform.parent.GetComponent<Image>();
            var prevColor = parentImage.color;
            parentImage.color = selectedColor;

            selectedColor = prevColor;
        }
        #endregion
    }
}
