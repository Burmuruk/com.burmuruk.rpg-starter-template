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
        [Header("References")]
        [SerializeField] PlayerManagerSample playerManager;
        [SerializeField] Camera mainCamera;
        PlayerControllerSample playerController;
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
            playerController = FindObjectOfType<PlayerControllerSample>();
            playerManager = FindObjectOfType<PlayerManagerSample>();
            gameManager = FindObjectOfType<GameManager>();
            missionsManager = FindAnyObjectByType<Missions.MissionManager>();
            missionsManager.OnMissionStarted += (m) => ShowMission( m.Description );

            var savingWrapper = FindObjectOfType<JsonSavingWrapper>();
            if (savingWrapper)
            {
                savingWrapper.OnSaving += ShowSavingIcon;
                savingWrapper.OnLoading += ShowSavingIcon;
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
            if (playersLife != null && playersLife.Count > 0)
            {
                foreach (var item in playersLife.Values)
                {
                    pLife.Release(item);
                }
            }
            playersLife = new();
            
            foreach (var player in playerManager.Players)
            {
                StackableNode lifeBar = pLife.Get();
                playersLife.Add(player, lifeBar);

                lifeBar.label.transform.parent.position = mainCamera.WorldToScreenPoint(player.transform.position);
                UpdateHealth(player.Health.HP, player);
                lifeBar.label.transform.parent.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            //if (!hasInitialized) { return; }

            //UpdateSubscripttions();
        }

        private void OnDisable()
        {
            //if (!hasInitialized) { return; }

            //playerManager.OnCombatEnter -= EnableHPPlayersBar;
            //playerManager.OnCombatEnter -= ShowAbilities;
            //playerManager.OnFormationChanged -= ChangeFormation;
            //playerController.OnFormationHold -= ShowFormations;
            //playerController.OnPickableEnter -= ShowInteractionButton;
            //playerController.OnPickableExit -= ShowInteractionButton;
            //playerController.OnItemPicked -= ShowNotification;

            //foreach (var player in playerManager.Players)
            //{
            //    player.Health.OnDamaged -= (hp) => { UpdateHealth(hp, player); };
            //}
        }

        private void LateUpdate()
        {
            if (gameManager.GameState == GameManager.State.Playing)
            {
                UpdateHealthPosition();
            }
        }

        private void UpdateSubscripttions()
        {
            RemoveSubscripttions();
            playerManager.OnCombatEnter += EnableHPPlayersBar;
            playerManager.OnCombatEnter += ShowAbilities;
            playerManager.OnFormationChanged += ChangeFormation;
            playerManager.OnPlayerAdded += (_) => RestartPlayersTags();
            playerController.OnFormationHold += ShowFormations;
            playerController.OnPickableEnter += ShowInteractionButton;
            playerController.OnPickableExit += ShowInteractionButton;
            playerController.OnItemPicked += ShowNotification;
            if (playerController.TryGetComponent<PlayerConversant>(out var conversarnt))
            {
                conversarnt.OnConversationUpdated += ShowDialogues;
                conversarnt.OnConversationEnded += HideDialogues; 
            }
            //playerManager. combat mode -> abilities

            foreach (var player in playerManager.Players)
            {
                player.Health.OnDamaged += (hp) => { UpdateHealth(hp, player); };
            }
        }

        private void RemoveSubscripttions()
        {
            playerManager.OnCombatEnter -= EnableHPPlayersBar;
            playerManager.OnCombatEnter -= ShowAbilities;
            playerManager.OnFormationChanged -= ChangeFormation;
            playerManager.OnPlayerAdded -= (_) => RestartPlayersTags();
            playerController.OnFormationHold -= ShowFormations;
            playerController.OnPickableEnter -= ShowInteractionButton;
            playerController.OnPickableExit -= ShowInteractionButton;
            playerController.OnItemPicked -= ShowNotification;
            if (playerController.TryGetComponent<PlayerConversant>(out var conversarnt))
            {
                conversarnt.OnConversationUpdated -= ShowDialogues;
                conversarnt.OnConversationEnded -= HideDialogues;
            }
            //playerManager. combat mode -> abilities

            foreach (var player in playerManager.Players)
            {
                player.Health.OnDamaged -= (hp) => { UpdateHealth(hp, player); };
            }
        }

        private void UpdateHealthPosition()
        {
            if (playersLife.Count <= 0) return;

            foreach (var player in playerManager.Players)
            {
                var position = Vector3.Lerp(playersLife[player].label.transform.parent.position,
                    mainCamera.WorldToScreenPoint(player.transform.position + Vector3.up * 2),
                    Time.deltaTime * 20);

                playersLife[player].label.transform.parent.position = position;
            }
        }

        public void Init()
        {
            cdChangeFormation = new CoolDownAction(2, (value) =>
            {
                formationState = FormationState.Showing;
                ShowFormations(!value);
            });
            cdFormationInfo = new CoolDownAction(.5f, EnableFormationsInfo, true);
            cdMissions = new Queue<CoolDownAction>();
            mainCamera = Camera.main;


            InitializeStackables();
            CreateHPPlayersBar();
            hasInitialized = true;

            UpdateSubscripttions();
            DontDestroyOnLoad(transform.parent.gameObject);
        }

        public void RestartPlayersTags()
        {
            //pFormationState.Initialize();
            //pInteractable.Initialize();
            //pNotifications.Initialize();
            //pMissions.Initialize();
            //pLife.Initialize();
            CreateHPPlayersBar();
        }

        private void UpdateHealth(float hp, AIGuildMember player)
        {
            playersLife[player].image.fillAmount = hp / player.Health.MaxHp;

            if (hp <= player.Health.MaxHp )
                playersLife[player].image.transform.parent.parent.gameObject.SetActive(true);
        } 

        private void ShowFormations(bool value)
        {
            if (value)
            {
                if (formationState == FormationState.Changing)
                {
                    StopCoroutine(cdChangeFormation.CoolDown());
                    cdChangeFormation.Restart();

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

            StopCoroutine(cdFormationInfo.CoolDown());
            cdFormationInfo.Restart();
            StartCoroutine(cdFormationInfo.CoolDown());
        }

        private void ChangeFormation()
        {
            if (formationState == FormationState.Changing)
            {
                StopCoroutine(cdChangeFormation.CoolDown());
                cdChangeFormation.Restart();
            }

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
                Formation.Protect => "Protejer",
                Formation.Free => "Libre",
                Formation.LockTarget => "Fija objetivo",
                Formation.Follow => "Seguir",
                _ => "Libre"
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

        private void ShowInteractionButton(bool shouldShow, string name)
        {
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
                        where ((EquipeableItem)ability).Characters.Contains(curPlayer)
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
            foreach (var bar in playersLife)
            {
                bar.Value.label.transform.parent.gameObject.SetActive(enable);
            }
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

        private void ShowSavingIcon(float progress)
        {
            if (!gameObject.activeSelf) return;

            if (savingNotification != null && progress < 0)
                return;
            else if (progress >= 1)
            {
                Invoke("StopSavingNotification", 2);
                return;
            }
            
            savingNotification = StartCoroutine(RotateSavingImage());
        }

        private void StopSavingNotification()
        {
            if (savingNotification == null) return;

            StopCoroutine(savingNotification);
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
                yield return new WaitForEndOfFrame();

                imgSaving.transform.Rotate(Vector3.forward, 2);
            }
        }
    }
}
