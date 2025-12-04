using Burmuruk.RPGStarterTemplate.Combat;
using Burmuruk.RPGStarterTemplate.Control.AI;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Saving;
using Burmuruk.RPGStarterTemplate.Stats;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Burmuruk.RPGStarterTemplate.Control
{
    public class PlayerManager : MonoBehaviour, IJsonSaveable
    {
        [SerializeField] protected CharacterProgress progress;
        [SerializeField] protected PlayerCustomization customization;
        [SerializeField] protected GameObject PlayerPrefab;
        [SerializeField] protected string curPlayerName;

        protected GameObject playersParent;
        protected List<AIGuildMember> players;
        protected PlayerController playerController;
        protected int? m_CurPlayer;
        protected (Formation value, object args) curFormation = default;

        public event Action OnPlayerChanged;
        public event Action<bool> OnCombatEnter;
        public event Action<Character> OnPlayerAdded;

        public List<AIGuildMember> Players
        {
            get
            {
                return players;
            }
        }
        public IInventory playerInventory
        {
            get
            {
                if (!m_CurPlayer.HasValue)
                {
                    return null;
                }

                return players[m_CurPlayer.Value].Inventory;
            }
        }
        public IInventory MainInventory { get; private set; }
        public AIGuildMember CurPlayer
        {
            get
            {
                if (m_CurPlayer.HasValue)
                {
                    return players[m_CurPlayer.Value];
                }

                return null;
            }
        }
        public (Formation value, object args) CurFormation { get => curFormation; }
        public GameObject PlayersParent
        {
            get
            {
                if (playersParent == null)
                {
                    playersParent = new GameObject("Players");
                    DontDestroyOnLoad(playersParent);
                }

                return playersParent;
            }
        }

        protected virtual void Awake()
        {
            playerController = FindObjectOfType<PlayerController>();
            var mainInventory = GetComponent<Inventory.Inventory>();
            MainInventory = mainInventory;

            DestroyPlayers();
            var member = CreatePlayer();
            AddMember(member);
            SetUpPlayer(member);
            SetPlayerControl(0);
            //AddMember(CreatePlayer());
            //SetPlayerControl(0);
        }

        private void Start()
        {
            SetPlayerControl();
        }

        public void UpdateLeaderPosition()
        {
            var playerSpawner = FindObjectOfType<PlayerSpawner>();

            if (playerSpawner && playerSpawner.Enabled)
            {
                CurPlayer.SetPosition(playerSpawner.transform.position);
            }
        }

        public void SetPlayerControl(int idx)
        {
            if (players != null && idx < players.Count)
            {
                EnableIAInRestOfPlayers(idx);

                players[idx].enabled = false;
                players[idx].EnableControll();
                players[idx].SetMainPlayer(players[idx]);
                playerController.SetPlayer(players[idx]);

                m_CurPlayer = idx;
                curPlayerName = players[idx].name;

                OnPlayerChanged?.Invoke();
            }
        }

        public void UseItem(int id)
        {
            var item = MainInventory.GetItem(id);

            if (item == null) return;

            if (item.Type == ItemType.Ability)
            {
                playerController.UseAbility((Ability)item);
            }
        }

        public AIGuildMember CreatePlayer()
        {
            Vector3 position = default;
            foreach (var spawner in FindObjectsOfType<PlayerSpawner>())
            {
                if (spawner.Enabled)
                {
                    position = spawner.transform.position;
                }
            }

            var instance = Instantiate(PlayerPrefab, position, Quaternion.identity, PlayersParent.transform);

            var player = instance.GetComponent<AIGuildMember>();
            instance.GetComponent<JsonSaveableEntity>().SetUniqueIdentifier();
            SetUpPlayer(player);

            return player;
        }

        private void SetUpPlayer(AIGuildMember player)
        {
            (player.Inventory as InventoryEquipDecorator).SetInventory((Inventory.Inventory)MainInventory);

            //var lastColor = member.stats.color;
            var stats = progress.GetDataByLevel(CharacterType.Player, 0);
            if (stats.HasValue)
                player.SetStats(stats.Value);
            else
                player.SetDefaultStats();
            //member.stats.color = lastColor;

            SetColor(player);
            player.SetUpMods();
        }

        private void DestroyPlayers()
        {
            var players = FindObjectsOfType<AIGuildMember>();

            if (players.Length == 0) return;

            foreach (var player in players)
            {
                RemoveMember(player);
                Destroy(player.gameObject);
            }

            this.players = new();

            m_CurPlayer = null;
            //foreach (var member in players)
            //{
            //    AddMember(member);
            //}
        }

        private void EnableIAInRestOfPlayers(int mainPlayerIdx)
        {
            players.ForEach(p =>
            {
                p.enabled = true;
                p.DisableControll();
                p.SetMainPlayer(players[mainPlayerIdx]);
                p.Fellows = players.Where(fellow => fellow != p && p != players[mainPlayerIdx]).ToArray();
            });
        }

        private void SetPlayerControl()
        {
            SetPlayerControl(0);
        }

        private void EnterToCombatMode(bool shouldEnter)
        {
            PlayerState state = shouldEnter ? PlayerState.Combat : PlayerState.None;

            players.ForEach((player) => { player.PlayerState = state; });

            OnCombatEnter?.Invoke(shouldEnter);
        }

        public void AddMember(AIGuildMember member)
        {
            member.OnCombatStarted += EnterToCombatMode;
            member.SetFormation(curFormation.value, curFormation.args);

            AIGuildMember mainPlayer = null;
            if (players != null && players.Count > 0)
            {
                if (m_CurPlayer.HasValue)
                    mainPlayer = CurPlayer;
                else
                {
                    m_CurPlayer = 0;
                    mainPlayer = CurPlayer;
                }
            }

            players.Add(member);
            OnPlayerAdded?.Invoke(member);

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == mainPlayer)
                {
                    SetPlayerControl(i);
                    break;
                }
            }

            if (m_CurPlayer.HasValue)
                member.SetMainPlayer(CurPlayer);
        }

        public void RemoveMember(AIGuildMember member)
        {
            member.OnCombatStarted -= EnterToCombatMode;
            if (players != null)
            {
                if (players.Contains(member))
                    players.Remove(member);

                if (players.Count <= 0)
                    m_CurPlayer = null;
            }
        }

        private void SetColor(AIGuildMember member)
        {
            if (customization.DefaultColors.Length <= 0) return;

            Color? newColor = default;

            foreach (var color in customization.DefaultColors)
            {
                bool hasColor = false;

                foreach (var player in players)
                {
                    if (player.stats.color == color)
                    {
                        hasColor = true;
                        break;
                    }
                }

                if (!hasColor)
                {
                    newColor = color;
                    break;
                }
            }

            if (!newColor.HasValue) return;

            member.stats.color = newColor.Value;

            //List<Color> usedColors = new();

            //checkedPlayers.ForEach(p => usedColors.AddVariable(p.statsList.Color));

            //var availableColors = (from color in playerColors where !usedColors.Contains(color) select color).ToList();

            //var selectedColorIdx = UnityEngine.Random.Range(0, availableColors.MaxCount);

            //member.statsList.Color = playerColors[selectedColorIdx];
        }

        public JToken CaptureAsJToken(out SavingExecution execution)
        {
            execution = SavingExecution.Organization;
            JObject state = new JObject();

            state["Players"] = CapturePlayers();
            state["Inventory"] = CaptureInventory();

            return state;
        }

        public void LoadAsJToken(JToken jToken)
        {
            DestroyPlayers();
            RestorePlayers(jToken["Players"]);
            RestoreInventory(jToken["Inventory"]);
        }

        private JToken CapturePlayers()
        {
            if (players.Count == 0) return null;

            JObject playersState = new JObject();
            var leaderIdentifier = players[0].Leader.GetComponent<JsonSaveableEntity>().GetUniqueIdentifier();

            for (int i = 0; i < players.Count; i++)
            {
                JObject state = new JObject();

                string identifier = players[i].GetComponent<JsonSaveableEntity>().GetUniqueIdentifier();
                state["ID"] = identifier;

                if (leaderIdentifier == identifier)
                    state["Leader"] = i;

                playersState[i.ToString()] = state;
            }

            playersState["Formation"] = (int)curFormation.value;

            return playersState;
        }

        private void RestorePlayers(JToken jToken)
        {
            IDictionary<string, JToken> state = (JObject)jToken;

            AIGuildMember[] members = new AIGuildMember[state.Count - 1];
            int leaderIdx = 0;

            for (int i = 0; i < state.Count - 1; i++)
            {
                var newPlayer = CreatePlayer();
                newPlayer.GetComponent<JsonSaveableEntity>().SetUniqueIdentifier((state[i.ToString()] as JObject)["ID"].ToString());
                members[i] = newPlayer;

                if ((state[i.ToString()] as JObject).ContainsKey("Leader"))
                    leaderIdx = i;

                OnPlayerAdded?.Invoke(newPlayer);
            }

            //for (int i = 0; i < players.Count; i++)
            //{
            //    string identifier = players[i].GetComponent<JsonSaveableEntity>().GetUniqueIdentifier();

            //    members[state[identifier]["ID"].ToObject<int>()] = players[i];
            //}

            players.Clear();

            foreach (var member in members)
                AddMember(member);

            DontDestroyOnLoad(players[0].gameObject.transform.parent);

            curFormation = ((Formation)state["Formation"].ToObject<int>(), null);
            SetPlayerControl(leaderIdx);
        }

        private JToken CaptureInventory()
        {
            if (Players.Count == 0) return null;

            JObject state = new JObject();
            int i = 0;

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                var items = MainInventory.GetList(type);

                if (items == null) continue;


                for (int j = 0; j < items.Count; j++)
                {
                    JObject itemState = new();

                    itemState["Id"] = items[j].ID;
                    itemState["Count"] = MainInventory.GetItemCount(items[j].ID);

                    JObject equipmentState = new JObject();
                    for (int k = 0; k < Players.Count; k++)
                    {
                        if (items[j] is EquipeableItem equipeable && equipeable.Characters.Contains(Players[k]))
                        {
                            equipmentState[k.ToString()] = 1;
                        }
                    }

                    itemState["Equipped"] = equipmentState;
                    state[i++.ToString()] = itemState;
                }
            }

            return state;
        }

        private void RestoreInventory(JToken jToken)
        {
            if (Players.Count == 0) return;

            if (!(jToken is JObject state && state != null)) return;

            int i = 0;

            while (state.ContainsKey(i.ToString()))
            {
                var itemState = (JObject)state[i.ToString()];
                int id = itemState["Id"].ToObject<int>();
                int count = itemState["Count"].ToObject<int>();

                for (int j = 0; j < count; j++)
                {
                    players[0].Inventory.Add(id);
                }

                for (int j = 0; j < Players.Count; j++)
                {
                    if ((itemState["Equipped"] as JObject).ContainsKey(j.ToString()))
                    {
                        (players[0].Inventory as InventoryEquipDecorator).TryEquip(Players[j], players[0].Inventory.GetItem(id), out _);
                    }
                }

                ++i;
            }
        }
    }

    public interface IPlayable
    {
        bool IsControlled { get; set; }

        void EnableControll();
        void DisableControll();
    }

    public enum PlayerState
    {
        None,
        Paused,
        Combat,
        FollowPlayer,
        Patrol,
        Teleporting,
        Dead
    }
}
