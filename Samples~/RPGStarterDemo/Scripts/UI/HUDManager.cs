using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Control;
using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Control.Samples;
using Burmuruk.RPGStarterTemplate.Dialogue;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class HUDManager : MonoBehaviour
    {
        private static HUDManager instance;
        [Header("References")]
        [SerializeField] Camera mainCamera;
        CanvasGroup hudCanvasGroup;
        CanvasGroup dialogueCanvasGroup;
        PlayerControllerSample playerController;
        PlayerManagerSample playerManager;
        GameManager gameManager;
        Missions.MissionManager missionsManager;

        [Space()]
        [Header("Abilities"), Space()]
        [SerializeField] GameObject pActiveAbilities;
        [SerializeField] GameObject pPasiveAbilities;
        [SerializeField] Sprite defaultAbilityIMG;
        [Header("Formations"), Space()]
        [SerializeField] GameObject pFormationInfo;
        [SerializeField] StackableLabel pFormationState;
        [Header("Interactables"), Space()]
        [SerializeField] StackableLabel pInteractable;
        [Header("Notifications"), Space()]
        [SerializeField] StackableLabel pNotifications;
        [Header("Missions"), Space()]
        [SerializeField] StackableLabel pMissions;
        [Header("Dialogues"), Space()]
        [SerializeField] GameObject pDialogue;
        [SerializeField] TextMeshProUGUI pDialogueText;
        [SerializeField] TextMeshProUGUI pDialogueTitle;
        [Header("Life"), Space()]
        [SerializeField] StackableLabel pLife;
        [SerializeField] bool alwaysShowHealthBars = false;
        bool requestedVisible = true;
        GameManager.State currentGameState;

        [Space, Header("Saving")]
        [SerializeField] Image imgSaving;

        State state;
        //PopUps activePopUps;
        Coroutine savingNotification;
        CoolDownAction cdChangeFormation;
        CoolDownAction cdFormationInfo;
        Queue<CoolDownAction> cdMissions;
        List<(StackableNode node, CoolDownAction coolDown)> cdNotifications = new();
        public FormationState formationState = FormationState.None;
        bool hasInitialized = false;
        private readonly List<Action> removeSubscriptions = new();
        private readonly List<Action> removeHealthSubscriptions = new();

        Dictionary<AIGuildMember, StackableNode> playersLife;

        enum State
        {
            None,
            HUD,
            MainMenu,
            Inventory,
        }
        public enum FormationState
        {
            None,
            Showing,
            Explaining,
            Changing
        }

        [Flags]
        enum HUDElements
        {
            None,
            Formation,
            Interactable,
            Life,
            Abilities,
            Notification,
            Mission,
            Damage,
            Effects
        }
        enum LifeBarType
        {
            None,
            Player,
            Guild,
            HurtedGuild,
            Enemies
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            instance = this;
            hudCanvasGroup = GetComponent<CanvasGroup>();
            if (pDialogue != null)
            {
                // Render dialogue independently while the gameplay HUD is hidden.
                var canvas = pDialogue.GetComponent<Canvas>();
                if (canvas == null) canvas = pDialogue.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 32759;
                dialogueCanvasGroup = pDialogue.GetComponent<CanvasGroup>();
                if (dialogueCanvasGroup == null)
                    dialogueCanvasGroup = pDialogue.AddComponent<CanvasGroup>();
                dialogueCanvasGroup.ignoreParentGroups = true;
                pDialogue.SetActive(false);
            }
            playerController = FindAnyObjectByType<PlayerControllerSample>();
            playerManager = FindAnyObjectByType<PlayerManagerSample>();
            gameManager = FindAnyObjectByType<GameManager>();
            missionsManager = FindAnyObjectByType<Missions.MissionManager>();
            if (missionsManager != null)
                missionsManager.OnMissionStarted += HandleMissionStarted;
            if (imgSaving != null)
            {
                var canvas = imgSaving.GetComponent<Canvas>();
                if (canvas == null) canvas = imgSaving.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 32760;
                var group = imgSaving.GetComponent<CanvasGroup>();
                if (group == null) group = imgSaving.gameObject.AddComponent<CanvasGroup>();
                group.ignoreParentGroups = true;
                group.interactable = false;
                group.blocksRaycasts = false;
                imgSaving.gameObject.SetActive(false);
            }
        }

        private void HandleMissionStarted(Missions.Mission mission) => ShowMission(mission.Description);

        public void SetVisible(bool visible)
        {
            requestedVisible = visible;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            bool visible = requestedVisible && currentGameState == GameManager.State.Playing;
            if (dialogueCanvasGroup != null)
            {
                bool dialogueVisible = requestedVisible &&
                    (currentGameState == GameManager.State.Playing ||
                     currentGameState == GameManager.State.Cinematic);
                dialogueCanvasGroup.alpha = dialogueVisible ? 1f : 0f;
                dialogueCanvasGroup.interactable = dialogueVisible;
                dialogueCanvasGroup.blocksRaycasts = dialogueVisible;
            }

            if (hudCanvasGroup == null)
                return;

            hudCanvasGroup.alpha = visible ? 1f : 0f;
            hudCanvasGroup.interactable = visible;
            hudCanvasGroup.blocksRaycasts = visible;
        }

        private void HandleGameState(GameManager.State newState)
        {
            currentGameState = newState;
            ApplyVisibility();

            if (newState == GameManager.State.Loading)
                ShowSavingIcon(0);
            else
                StopSavingNotification();

            if (newState == GameManager.State.Playing && hasInitialized)
            {
                CreateHPPlayersBar();
                ShowAbilities(playerManager != null && playerManager.IsInCombat);
            }
        }

        private void InitializeStackables()
        {
            pFormationState.Initialize();
            pInteractable.Initialize();
            pNotifications.Initialize();
            pMissions.Initialize();
            pLife.Initialize();
        }

        public void CreateHPPlayersBar()
        {
            if (playerManager == null) return;
            playersLife ??= new();
            foreach (var player in playersLife.Keys.ToArray())
                if (player == null || !playerManager.Players.Contains(player))
                {
                    pLife.Release(playersLife[player]);
                    playersLife.Remove(player);
                }
            
            foreach (var player in playerManager.Players)
            {
                if (player == null || player.Health == null)
                    continue;

                if (!playersLife.TryGetValue(player, out var lifeBar))
                {
                    lifeBar = pLife.Get();
                    playersLife.Add(player, lifeBar);
                }
                UpdateHealth(player.Health.HP, player);
            }
        }

        private void OnEnable()
        {
            if (hasInitialized) UpdateSubscripttions();

            var savingWrapper = FindAnyObjectByType<JsonSavingWrapper>();

            if (savingWrapper)
            {
                savingWrapper.OnSaving += ShowSavingIcon;
                savingWrapper.OnLoading += ShowLoadingIcon;
                savingWrapper.OnLoaded += HideLoadingIcon;
            }
        }

        private void OnDisable()
        {
            RemoveSubscripttions();
            var savingWrapper = FindAnyObjectByType<JsonSavingWrapper>();
            if (savingWrapper)
            {
                savingWrapper.OnSaving -= ShowSavingIcon;
                savingWrapper.OnLoading -= ShowLoadingIcon;
                savingWrapper.OnLoaded -= HideLoadingIcon;
            }
        }

        private void LateUpdate()
        {
            if (hasInitialized && currentGameState == GameManager.State.Playing)
            {
                CreateHPPlayersBar();
                UpdateHealthPosition();
            }
        }

        private void UpdateSubscripttions()
        {
            RemoveSubscripttions();

            var manager = playerManager;
            var controller = playerController;

            if (gameManager != null)
            {
                var states = gameManager;
                states.onStateChange += HandleGameState;
                removeSubscriptions.Add(() => states.onStateChange -= HandleGameState);
                HandleGameState(states.GameState);
            }

            if (manager != null)
            {
                manager.OnCombatEnter += EnableHPPlayersBar;
                manager.OnCombatEnter += ShowAbilities;
                manager.OnFormationChanged += ChangeFormation;
                manager.OnPlayerAdded += HandlePlayerAdded;

                removeSubscriptions.Add(() =>
                {
                    manager.OnCombatEnter -= EnableHPPlayersBar;
                    manager.OnCombatEnter -= ShowAbilities;
                    manager.OnFormationChanged -= ChangeFormation;
                    manager.OnPlayerAdded -= HandlePlayerAdded;
                });
            }

            if (controller != null)
            {
                controller.OnFormationHold += ShowFormations;
                controller.OnPickableEnter += ShowInteractionButton;
                controller.OnPickableExit += ShowInteractionButton;
                controller.OnInteractableEnter += ShowInteractionButton;
                controller.OnInteractableExit += ShowInteractionButton;
                controller.OnItemPicked += ShowNotification;

                removeSubscriptions.Add(() =>
                {
                    controller.OnFormationHold -= ShowFormations;
                    controller.OnPickableEnter -= ShowInteractionButton;
                    controller.OnPickableExit -= ShowInteractionButton;
                    controller.OnInteractableEnter -= ShowInteractionButton;
                    controller.OnInteractableExit -= ShowInteractionButton;
                    controller.OnItemPicked -= ShowNotification;
                });

                if (controller.TryGetComponent<PlayerConversant>(out var conversant))
                {
                    conversant.OnConversationUpdated += ShowDialogues;
                    conversant.OnConversationEnded += HideDialogues;

                    removeSubscriptions.Add(() =>
                    {
                        conversant.OnConversationUpdated -= ShowDialogues;
                        conversant.OnConversationEnded -= HideDialogues;
                    });
                }
            }

            RefreshHealthSubscriptions();
        }

        private void RemoveSubscripttions()
        {
            foreach (var unsubscribe in removeSubscriptions)
                unsubscribe();

            removeSubscriptions.Clear();

            foreach (var unsubscribe in removeHealthSubscriptions)
                unsubscribe();

            removeHealthSubscriptions.Clear();
        }

        private void RefreshHealthSubscriptions()
        {
            foreach (var unsubscribe in removeHealthSubscriptions)
                unsubscribe();

            removeHealthSubscriptions.Clear();

            if (playerManager == null)
                return;

            foreach (var player in playerManager.Players)
            {
                if (player == null || player.Health == null)
                    continue;

                var health = player.Health;

                void OnDamaged(int hp)
                {
                    UpdateHealth(hp, player);
                }

                health.OnDamaged += OnDamaged;

                removeHealthSubscriptions.Add(() => health.OnDamaged -= OnDamaged);
            }
        }

        private void HandlePlayerAdded(Character player)
        {
            if (!hasInitialized)
                return;

            RestartPlayersTags();
            RefreshHealthSubscriptions();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            RemoveSubscripttions();
            if (missionsManager != null)
                missionsManager.OnMissionStarted -= HandleMissionStarted;
        }

        private void UpdateHealthPosition()
        {
            if (playerManager == null || playerManager.Players == null || playersLife == null)
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            foreach (var player in playerManager.Players)
            {
                if (player == null)
                    continue;

                if (!playersLife.TryGetValue(player, out var bar))
                    continue;

                if (bar.label == null)
                    continue;

                var barTransform = bar.label.transform.parent;
                if (barTransform == null)
                    continue;

                var screenPosition = mainCamera.WorldToScreenPoint(
                    player.transform.position + Vector3.up * 2);

                barTransform.position = Vector3.Lerp(
                    barTransform.position,
                    screenPosition,
                    Time.deltaTime * 20);
            }
        }

        public void Init()
        {
            if (instance != null && instance != this) return;
            RemoveSubscripttions();

            playerController = FindAnyObjectByType<PlayerControllerSample>();
            playerManager = FindAnyObjectByType<PlayerManagerSample>();
            gameManager = FindAnyObjectByType<GameManager>();
            mainCamera = Camera.main;

            cdChangeFormation = new CoolDownAction(2, (value) =>
            {
                formationState = FormationState.Showing;
                ShowFormations(!value);
            });
            cdFormationInfo = new CoolDownAction(.5f, EnableFormationsInfo, true);
            cdMissions = new Queue<CoolDownAction>();
            mainCamera = Camera.main;


            if (!hasInitialized)
                InitializeStackables();
            CreateHPPlayersBar();
            hasInitialized = true;

            UpdateSubscripttions();
            DontDestroyOnLoad(transform.root);
        }

        public void RestartPlayersTags()
        {
            CreateHPPlayersBar();
        }

        private void UpdateHealth(float hp, AIGuildMember player)
        {
            if (player == null || player.Health == null || playersLife == null ||
                !playersLife.TryGetValue(player, out var bar))
                return;

            if (bar.image == null)
                return;

            float maxHp = player.Health.MaxHp;
            bar.image.fillAmount = maxHp > 0
                ? Mathf.Clamp01(hp / maxHp)
                : 0f;

            bool visible = player.gameObject.activeInHierarchy && hp > 0 &&
                (alwaysShowHealthBars || (playerManager != null && playerManager.IsInCombat) ||
                 (maxHp > 0 && hp * 100f < maxHp * 15f));
            bar.label.transform.parent.gameObject.SetActive(visible);
        }

        private void ShowFormations(bool value)
        {
            if (value)
            {
                if (formationState == FormationState.Changing)
                {
                    cdChangeFormation.Restart();
                    StopCoroutine(cdChangeFormation.CoolDown());

                    return;
                }

                pFormationState.container.SetActive(true);
                UpdateFormationText();
                pFormationInfo.SetActive(false);

                formationState = FormationState.Showing;
            }
            else
            {
                if (formationState == FormationState.Changing) return;

                pFormationState.container.SetActive(false);
                pFormationState.Release();

                formationState = FormationState.None;
            }

            cdFormationInfo.Restart();

            if (!cdFormationInfo.CanUse)
                StartCoroutine(cdFormationInfo.CoolDown());
        }

        private void ChangeFormation()
        {
            if (formationState == FormationState.Changing)
            {
                cdChangeFormation.Restart();
            }

            if (!cdChangeFormation.CanUse)
                StartCoroutine(cdChangeFormation.CoolDown());

            UpdateFormationText();
            formationState = FormationState.Changing;
        }

        private void EnableFormationsInfo(bool value)
        {
            if (formationState == FormationState.Changing)
                return;
            
            pFormationInfo.SetActive(value);
        }

        private void UpdateFormationText()
        {
            var newText = playerManager.CurFormation.value switch
            {
                Formation.Protect => "Protect",
                Formation.Free => "Free",
                Formation.LockTarget => "Lock Target",
                Formation.Follow => "Follow",
                _ => "Free"
            };

            StackableNode node;

            if (pFormationState.activeNodes.Count > 0)
            {
                node = pFormationState.activeNodes[0];
            }
            else
            {
                node = pFormationState.Get();
            }

            node.label.text = newText;
        }

        private void ShowMission(params string[] missions)
        {
            if (missions == null || missions.Length > 0 || (pMissions.maxAmount - pMissions.activeNodes.Count) <= 0) 
                return;

            if (!pMissions.container.activeSelf)
                pMissions.container.SetActive(true);

            foreach (var mission in missions)
            {
                var node = pMissions.Get();

                node.label.text = mission;
                var coolDown = new CoolDownAction(5, ReleaseMission);
                cdMissions.Enqueue(coolDown);

                StartCoroutine(coolDown.CoolDown());
            }
        }

        private void ReleaseMission(bool value)
        {
            if (!value) return;

            for (int i = 0; i < pMissions.activeNodes.Count; i++)
            {
                pMissions.Release();
            }

            if (pMissions.activeNodes.Count <= 0)
            {
                pMissions.container.SetActive(false);
            }
        }

        private void ShowDialogues(DialogueNode dialogue)
        {
            pDialogue.SetActive(true);
            pDialogueText.text = dialogue.Message;
            pDialogueTitle.text = dialogue.characterName;
        }

        private void HideDialogues()
        {
            pDialogue.SetActive(false);
            pDialogueText.text = string.Empty;
            pDialogueTitle.text = string.Empty;
        }

        private void ShowInteractionButton(bool shouldShow, string name, GameObject pickup)
        {
            ShowInteractionButton(shouldShow, name);
        }

        private void ShowInteractionButton(bool shouldShow, string name)
        {
            if (playerController != null && playerController.HavePickable)
            {
                shouldShow = true;
                name = "Pick up";
            }
            if (shouldShow)
            {
                StackableNode node;

                if (pInteractable.activeNodes.Count < 1)
                {
                    node = pInteractable.Get();
                }
                else
                    node = pInteractable.activeNodes[0];

                node.label.text = name;
            }
            else
            {
                pInteractable.Release();
            }
        }

        private void ShowAbilities(bool shouldShow)
        {
            if (pActiveAbilities == null) return;
            shouldShow &= playerManager != null && playerManager.CurPlayer != null && playerManager.MainInventory != null;
            if (shouldShow)
            {
                var inventory = playerManager.MainInventory;
                var curPlayer = playerManager.CurPlayer.transform.GetComponent<Character>();
                Ability[] abilities = GetEquippedAbilitites(inventory, curPlayer);

                for (int i = 0, j = 0; i < pActiveAbilities.transform.childCount; i++)
                {
                    var imgAbilty = pActiveAbilities.transform.GetChild(i).GetComponent<Image>();

                    if (j < abilities.Length)
                    {
                        SetupAbilityButton(abilities, j++, imgAbilty);

                        continue;
                    }

                    imgAbilty.sprite = defaultAbilityIMG;
                    imgAbilty.gameObject.SetActive(false);
                }
            }

            pActiveAbilities.transform.parent.gameObject.SetActive(shouldShow);

            void SetupAbilityButton(Ability[] abilities, int j, Image imgAbilty)
            {
                int id = abilities[j].ID;
                var button = imgAbilty.GetComponent<MyItemButton>();

                imgAbilty.sprite = abilities[j].Sprite;

                imgAbilty.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => UseAbility(id));
            }

            Ability[] GetEquippedAbilitites(IInventory inventory, Character curPlayer)
            {
                return (from ability in inventory.GetList(ItemType.Ability)
                        where ((EquipableItem)ability).Characters.Contains(curPlayer)
                        select (Ability)inventory.GetItem(ability.ID))
                                             .ToArray();
            }
        }

        private void UseAbility(int id)
        {
            playerManager.UseItem(id);
        }

        private void EnableHPOnDamage()
        {

        }

        private void EnableHPPlayersBar(bool enable)
        {
            CreateHPPlayersBar();
        }

        private void ShowNotification(string itemName, Vector3 itemPosition)
        {
            if (!pNotifications.container.activeSelf)
            {
                pNotifications.container.transform.position = mainCamera.WorldToScreenPoint(itemPosition);
                pNotifications.container.SetActive(true);
            }
            
            var panel = pNotifications.Get();
            panel.label.text = itemName;

            var coolDown = new CoolDownAction(pNotifications.showingTime,
                (_) => { HideNotification(panel); });

            cdNotifications.Add((panel, coolDown));

            StartCoroutine(coolDown.CoolDown());
        }

        private void HideNotification(StackableNode node)
        {
            pNotifications.Release(node);

            if (pNotifications.activeNodes.Count == 0)
                pNotifications.container.SetActive(false);
        }

        private void ShowLoadingIcon(float slot)
        {
            if (savingNotification == null)
                ShowSavingIcon(0);
        }

        private void HideLoadingIcon(SlotData data)
        {
            if (currentGameState != GameManager.State.Loading)
                StopSavingNotification();
            if (hasInitialized) CreateHPPlayersBar();
        }

        private void ShowSavingIcon(float progress)
        {
            if (!isActiveAndEnabled || imgSaving == null ||
                (currentGameState != GameManager.State.Playing && currentGameState != GameManager.State.Loading)) return;

            if (progress >= 1 && currentGameState != GameManager.State.Loading)
            {
                Invoke("StopSavingNotification", .5f);
                return;
            }
            
            CancelInvoke(nameof(StopSavingNotification));
            if (savingNotification == null)
                savingNotification = StartCoroutine(RotateSavingImage());
        }

        private void StopSavingNotification()
        {
            if (currentGameState == GameManager.State.Loading) return;
            CancelInvoke(nameof(StopSavingNotification));
            if (savingNotification != null) StopCoroutine(savingNotification);
            if (imgSaving == null) return;
            imgSaving.gameObject.SetActive(false);
            imgSaving.transform.rotation = Quaternion.identity;
            
            savingNotification = null;
        }
            

        private IEnumerator RotateSavingImage()
        {
            float maxTime = 30;
            float curTime = 0;

            imgSaving.gameObject.SetActive(true);

            while (curTime < maxTime)
            {
                yield return null;

                imgSaving.transform.Rotate(Vector3.forward, 2);
            }
        }
    }
}
