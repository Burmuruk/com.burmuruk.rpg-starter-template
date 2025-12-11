using Burmuruk.RPGStarterTemplate.Saving;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Dialogue
{
    public class DialogueGraphController : ScriptableObject
    {
        private const string SETTINGS_TAB_NAME = "Settings";
        private const string PINS_TAB_NAME = "Pins";
        private const string OF_CHARACTER_NAME = "OFCharacter";
        private const string TXT_ID = "txtId";
        private const string TXT_NICKNAME_NAME = "txtNickName";
        private const string CF_NODE_COLOUR_NAME = "NodeColour";
        private RPGStarterTemplate.Dialogue.DialogueBlock _result;
        [SerializeField] private List<string> _pins = new();
        [SerializeField] private BaseNode _selectedNode;
        [SerializeField] public List<CharacterData> characters = new();
        private string _path;

        public string dialogueGUID;
        public Dictionary<string, BaseNode> nodes = new();

        public event Action OnChange;
        public event Action OnSave;
        public event Action<string> Notify;

        [Serializable]
        public struct CharacterData
        {
            public string id;
            public string name;
            public Color Color;
        }

        public VisualElement SettingsContainer { get; private set; }
        public VisualElement PinsContainer { get; private set; }
        public VisualElement SavingContainer { get; private set; }
        public ObjectField OFCharacter { get; private set; }
        public TextField TxtId { get; private set; }
        public TextField TxtCharacterName { get; private set; }
        public ColorField CFNodeColour { get; private set; }
        public VisualElement Container { get; private set; }
        public TextField TxtDialogueName { get; private set; }
        private List<string> StartNodes
        {
            get
            {
                var values = nodes.Values
                    .Where(n => n.GraphViewNode != null && n.IsStartNode && n.IsExecutable)
                    .Select(n => n.Id).ToList();

                return values is null ? new() : values;
            }
        }

        private RPGStarterTemplate.Dialogue.DialogueBlock Result
        {
            get
            {
                if (_result == null && !string.IsNullOrEmpty(_path))
                {
                    _result = AssetDatabase.LoadAssetAtPath<RPGStarterTemplate.Dialogue.DialogueBlock>(_path);
                }
                return _result;
            }

            set
            {
                _result = value;
                if (_result != null)
                {
                    _path = AssetDatabase.GetAssetPath(_result);
                }
            }
        }

        public void Initialize()
        {
            //_graphData = controller;
            Container = new VisualElement()
            {
                style =
                {
                    position = Position.Absolute,
                    flexDirection = FlexDirection.Row,
                    width = new Length(100, LengthUnit.Percent),
                    flexGrow = 1
                }
            };
            OnChange = null;
            CreateSettingsTab();
            CreatePinsTab();
            //CreateSaveButton();
            Container.Add(SettingsContainer);
            Container.Add(PinsContainer);
            //Container.Add(SavingContainer);

            SettingsContainer.style.visibility = Visibility.Hidden;
            EnableContainer(PinsContainer, false);
            Set_Result(null);
        }

        public Dictionary<string, BaseNode> GetNodes()
        {
            nodes = new();

            var values = AssetDatabase.LoadAllAssetRepresentationsAtPath(
                    AssetDatabase.GetAssetPath(this));
            return values.ToDictionary(i => i.name, n => (BaseNode)n);
        }

        public void AddNode(BaseNode node)
        {
            nodes.Add(node.Id, node);
            node.Set_DefaultStatusButton();
            node.OnStartPointCreated += OnStartNodeChanged;
            node.OnExecutionChanged += Node_OnExecutionChanged;
            node.OnExecutionChanged += (n, v) =>
            {
                if (!v) Notify?.Invoke("Unrachable node detected.");
            };
            node.OnSelected += SetTargetNode;
            node.OnDeselected += SetTargetNode;

            OnChange?.Invoke();
        }

        private void Node_OnExecutionChanged(BaseNode node, bool value)
        {
            foreach (var id in node.Children)
            {
                var cur = nodes[id];
                bool state = cur.IsExecutable;
                BaseNode.UpdateExecution(cur, false);

                if (state != value)
                    Node_OnExecutionChanged(cur, value);
            }
        }

        private bool VeryfyExecution(BaseNode node, out bool result)
        {
            result = node.GraphViewNode.input.connections.
                Any(c => (c.output.node as GraphViewNode).Parent.IsExecutable);

            bool changed = node.IsExecutable == result;

            return changed;
        }

        public void RemoveNode(GraphViewNode node)
        {
            if (nodes.ContainsKey(node.Parent.Id))
            {
                var ids = new List<string>(node.Parent.Children);

                node.parent.schedule.Execute(() =>
                {
                    RemoveCharacterData(node.Parent);
                    nodes.Remove(node.Parent.Id);

                    foreach (var id in ids)
                    {
                        if (nodes.ContainsKey(id) && VeryfyExecution(nodes[id], out var change))
                        {
                            nodes[id].IsExecutable = change;
                        }
                    }

                    OnChange?.Invoke();
                }).ExecuteLater(100);
            }
        }

        private void RemoveCharacterData(BaseNode node)
        {
            bool isCharacter = this.nodes.Values.Any(n => n.characterID == node.characterID && n.Id != node.Id);

            if (isCharacter) return;

            string characterID = node.characterID;

            for (int i = 0; i < characters.Count(); i++)
            {
                if (characters[i].id == characterID)
                    {
                    characters.RemoveAt(i);
                    return;
                }
            }
        }

        public BaseNode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (nodes.ContainsKey(id))
            {
                return nodes[id];
            }
            return null;
        }

        private void Set_Result(RPGStarterTemplate.Dialogue.DialogueBlock dialogue)
        {
            if (dialogue == null)
            {
                _result = CreateInstance<RPGStarterTemplate.Dialogue.DialogueBlock>();
                _path = null;
            }
            else
            {
                Result = dialogue;
            }
        }

        private void CreateSettingsTab()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/DialogueEditor/Elements/Node.uxml");
            var instance = visualTree.Instantiate();
            instance.style.position = Position.Absolute;
            //instance.style.right = 4;
            //instance.style.top = 4;

            SettingsContainer = instance.Q<VisualElement>(SETTINGS_TAB_NAME);
            TxtId = SettingsContainer.Q<TextField>(TXT_ID);
            TxtCharacterName = SettingsContainer.Q<TextField>(TXT_NICKNAME_NAME);
            //TxtId.RegisterValueChangedCallback(SetNodesIds);
            TxtCharacterName.RegisterValueChangedCallback(SetNodesNames);
            TxtDialogueName = SettingsContainer.Q<TextField>("txtDialogueName");
            TxtDialogueName.RegisterValueChangedCallback(OnChanged_TxtDialogueName);

            CFNodeColour = new ColorField();
            CFNodeColour.style.flexBasis = 20;
            SettingsContainer.Q<VisualElement>(CF_NODE_COLOUR_NAME).Add(CFNodeColour);
            CFNodeColour.RegisterValueChangedCallback(ChangeNodesColor);

            OFCharacter = new ObjectField();
            OFCharacter.style.flexBasis = 20;
            SettingsContainer.Q<VisualElement>(OF_CHARACTER_NAME).Add(OFCharacter);
            OFCharacter.objectType = typeof(RPGStarterTemplate.Dialogue.AIConversant);
            OFCharacter.RegisterValueChangedCallback(VerifyCharacterSelection);
        }

        public void Load_CharacterData()
        {
            if (characters.Count <= 0) return;

            foreach (var node in nodes.Values)
            {
                foreach (var character in characters)
                {
                    if (character.id == node.characterID)
                    {
                        node.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor = character.Color;
                        node.GraphViewNode.title = character.name;
                        break;
                    }
                }
            }
        }

        private void OnChanged_TxtDialogueName(ChangeEvent<string> evt)
        {
            _selectedNode.dialogueName = evt.newValue;
            OnChange?.Invoke();
        }

        private void SetNodesNames(ChangeEvent<string> evt)
        {
            if (_selectedNode == null) return;

            if (characters.Any(c => c.name.Equals(evt.newValue, StringComparison.OrdinalIgnoreCase)))
            {
                TxtCharacterName.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            var nodes = this.nodes.Values.Except(new BaseNode[] { _selectedNode });

            foreach (var node in nodes)
            {
                if (node.characterID == _selectedNode.characterID)
                {
                    node.Title = evt.newValue;
                }
            }

            _selectedNode.Title = evt.newValue;
            SaveCharacterData(evt.newValue, _selectedNode.characterID, CFNodeColour.value);
            OnChange?.Invoke();
        }

        private void SetNodesIds(ChangeEvent<string> evt)
        {
            if (_selectedNode == null) return;

            var nodes = this.nodes.Values.Except(new BaseNode[] { _selectedNode });

            foreach (var node in nodes)
            {
                if (node.characterID == _selectedNode.characterID)
                {
                    node.characterID = TxtId.value;
                }
            }

            _selectedNode.characterID = evt.newValue;
        }

        private void ChangeNodesColor(ChangeEvent<Color> evt)
        {
            if (_selectedNode == null) return;

            var nodes = this.nodes.Values.Except(new BaseNode[] { _selectedNode });

            foreach (var node in this.nodes.Values)
            {
                if (node.characterID == _selectedNode.characterID)
                {
                    node.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor = evt.newValue;
                }
            }

            _selectedNode.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor = evt.newValue;
            SaveCharacterData(TxtCharacterName.value, _selectedNode.characterID, CFNodeColour.value);
            OnChange?.Invoke();
        }

        public void SetTargetNode(BaseNode node)
        {
            _selectedNode = node;
            TxtId.SetValueWithoutNotify(node.characterID);
            TxtCharacterName.SetValueWithoutNotify(node.Title);
            CFNodeColour.SetValueWithoutNotify(node.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor.value);

            var conversants = SceneAsset.FindObjectsByType<RPGStarterTemplate.Dialogue.AIConversant>(FindObjectsSortMode.None);
            bool found = false;
            foreach (var conversant in conversants)
            {
                if (conversant.gameObject.TryGetComponent<JsonSaveableEntity>(out var saveableEntity))
                {
                    if (saveableEntity.GetUniqueIdentifier() == node.characterID)
                    {
                        OFCharacter.SetValueWithoutNotify(conversant);
                        found = true;
                        break;
                    }
                }
            }
            if (!found) OFCharacter.SetValueWithoutNotify(null);
        }

        private void VerifyCharacterSelection(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue is not RPGStarterTemplate.Dialogue.AIConversant conversant)
            {
                TxtId.value = null;
                TxtCharacterName.value = null;
                OnChange?.Invoke();
                return;
            }

            if (conversant.gameObject.TryGetComponent<JsonSaveableEntity>(out var saveableEntity))
            {
                var id = saveableEntity.GetUniqueIdentifier();
                SetPreviousSettings(id);

                var data = characters.Where(c => c.id == _selectedNode.Id);
                CharacterData cur = default;

                if (data.Count() > 0)
                {
                    cur = data.First();
                }
                else
                {
                    SaveCharacterData(conversant.GetName(), id, CFNodeColour.value);
                    cur = characters[^1];
                }

                UpdateSettingsValues(cur);
                OnChange?.Invoke();
            }
            else
            {
                OFCharacter.SetValueWithoutNotify(null);
                Highlight(OFCharacter, 500, BorderColour.Error);
            }
        }

        private void UpdateSettingsValues(CharacterData cur)
        {
            TxtId.SetValueWithoutNotify(cur.id);
            TxtCharacterName.SetValueWithoutNotify(cur.name);
            CFNodeColour.SetValueWithoutNotify(cur.Color);
        }

        private void SaveCharacterData(string name, string id, Color color)
        {
            var data = new CharacterData()
            {
                id = id,
                name = name,
                Color = color
            };

            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].id == id)
                {
                    characters[i] = data;
                    return;
                }
            }

            characters.Add(data);
        }

        private void SetPreviousSettings(string newId)
        {
            var nodes = this.nodes.Values.Except(new BaseNode[] { _selectedNode });

            foreach (var node in nodes)
            {
                if (node.characterID == newId)
                {
                    _selectedNode.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor =
                        node.GraphViewNode.Q<VisualElement>("node-border").style.backgroundColor;
                    break;
                }
            }
        }

        private void CreatePinsTab()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/DialogueEditor/PinsTab.uxml");
            var instance = visualTree.Instantiate();
            instance.style.position = Position.Absolute;
            instance.style.right = 10;
            instance.style.top = 4;

            PinsContainer = instance.Q<VisualElement>(PINS_TAB_NAME);
        }

        private void CreateSaveButton()
        {
            //var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/DialogueEditor/PinsTab.uxml");
            //var instance = visualTree.Instantiate();
            Button button = new Button(Save);
            button.style.position = Position.Absolute;
            button.style.right = 10;
            button.style.bottom = 4;
            button.style.width = 100;
            button.style.height = 100;
            button.text = "Save";

            SavingContainer = button;

            button.style.visibility = (AssetDatabase.GetAssetPath(this) == "") switch
            {
                true => Visibility.Visible,
                false => Visibility.Hidden
            };
        }

        public void OnPortConnected(Port from, Port to)
        {
            var fromN = (from.node as GraphViewNode).Parent;
            var toN = (to.node as GraphViewNode).Parent;
            fromN.OnPortConnected(fromN, toN, from, to);
            OnChange?.Invoke();
        }

        public void OnPortDisconnected(Port from, Port to)
        {
            var fromN = (from.node as GraphViewNode).Parent;
            var toN = (to.node as GraphViewNode).Parent;
            fromN.OnPortDisconnected(fromN, toN, from, to);
            OnChange?.Invoke();
        }

        private void OnStartNodeChanged(string id, bool value)
        {
            if (value)
            {
                if (StartNodes.Contains(id)) return;

                StartNodes.Add(id);
            }
            else
            {
                if (!StartNodes.Contains(id)) return;

                StartNodes.Remove(id);
            }
        }
        #region SettingsTab

        #endregion

        #region Pins
        public void AddPin(BaseNode node)
        {
            if (_pins.Contains(node.Id))
                return;

            _pins.Add(node.Id);

            if (_pins.Count >= 1)
            {
                EnableContainer(PinsContainer, true);
            }

            OnChange?.Invoke();
        }

        public void RemovePin(BaseNode node)
        {
            _pins.Remove(node.Id);

            if (_pins.Count == 0)
            {
                EnableContainer(PinsContainer, false);
            }

            OnChange?.Invoke();
        }
        #endregion

        #region Saving
        public void SaveResults()
        {
            if (string.IsNullOrEmpty(_path) || Result == null)
            {
                if (!GenerateResultAsset()) return;
            }

            dialogueGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Result));
            UpdateDialogues();
            EditorUtility.SetDirty(Result);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private bool GenerateResultAsset()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save dialogue",
                Application.dataPath,
                "New dialogue",
                "asset"
            );

            string relativePath = GetRelativePath(path);
            if (!string.IsNullOrEmpty(relativePath))
            {
                AssetDatabase.CreateAsset(CreateInstance<RPGStarterTemplate.Dialogue.DialogueBlock>(), relativePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Result = AssetDatabase.LoadAssetAtPath<RPGStarterTemplate.Dialogue.DialogueBlock>(relativePath);
                return true;
            }

            return false;
        }

        private static string GetRelativePath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = FileUtil.GetProjectRelativePath(path);

                if (relativePath.StartsWith("Assets"))
                {
                    return relativePath;
                }
                else
                {
                    Debug.LogError("El archivo debe guardarse dentro de la carpeta Assets/");
                }
            }

            return null;
        }

        private void UpdateDialogues()
        {
            var dialoguesToRemove = Result.dialogues.Where(n => !StartNodes.Contains(n.id)).Select(n => n.id);

            foreach (var id in StartNodes)
            {
                if (nodes.TryGetValue(id, out var node))
                {
                    RPGStarterTemplate.Dialogue.Dialogue dialogue = GetDialogueData(id, node);
                    Result[id] = dialogue;
                    dialogue.UpdateDialogue(node.GetNodeData(null));
                }
            }

            foreach (var item in dialoguesToRemove)
            {
                Result.RemoveDialogue(item);
            }
        }

        private RPGStarterTemplate.Dialogue.Dialogue GetDialogueData(string id, BaseNode node)
        {
            var dialogue = ScriptableObject.CreateInstance<RPGStarterTemplate.Dialogue.Dialogue>();
            dialogue.id = id;
            dialogue.name = string.IsNullOrEmpty(node.dialogueName) ? id : node.dialogueName;
            List<string> characters = default;
            GetCharacterInDialogue(id, ref characters);
            dialogue.characters = characters.Select(c =>
            {
                var data = GetCharacterData(c);
                return data == null ? c : data.Value.name;
            }).ToList();
            return dialogue;
        }

        private CharacterData? GetCharacterData(string id)
        {
            foreach (var character in characters)
            {
                if (character.id == id)
                    return character;
            }

            return null;
        }

        private void GetCharacterInDialogue(string startId, ref List<string> characters)
        {
            characters ??= new();

            if (nodes.TryGetValue(startId, out var node))
            {
                if (!characters.Contains(node.characterID))
                    characters.Add(node.characterID);

                foreach (var id in node.Children)
                {
                    GetCharacterInDialogue(id, ref characters);
                }
            }
        }

        public void Save()
        {
            SaveGraphData();
        }

        private void SaveGraphData()
        {
            if (AssetDatabase.GetAssetPath(this) == "")
                GenerateGraphAsset();

            foreach (var node in nodes.Values)
            {
                node.Save();
            }
            AttachNodes();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            OnSave?.Invoke();
            //string path = Path.Combine("Assets", "Test_DialogueGraphController.asset");
            //AssetDatabase.CreateAsset(this, path);
            //AssetDatabase.SaveAssets();
            //AssetDatabase.Refresh();
            //Debug.Log($"Asset saved at: {path}");
        }

        private void GenerateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save dialogue Graph",
                Application.dataPath,
                "New dialogue graph",
                "asset"
            );

            string relativePath = GetRelativePath(path);
            if (!string.IsNullOrEmpty(relativePath))
            {
                AssetDatabase.CreateAsset(this, relativePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Result = AssetDatabase.LoadAssetAtPath<RPGStarterTemplate.Dialogue.DialogueBlock>(relativePath);
            }
        }

        public void AttachNodes()
        {
            if (AssetDatabase.GetAssetPath(this) == "") return;

            var currentNodes = AssetDatabase.LoadAllAssetRepresentationsAtPath(
                    AssetDatabase.GetAssetPath(this))
                    .Where(n => !nodes.ContainsKey(n.name))
                    .Select(n => (BaseNode)n).ToList();

            foreach (var node in currentNodes)
                AssetDatabase.RemoveObjectFromAsset(node);

            foreach (var node in nodes)
            {
                if (AssetDatabase.GetAssetPath(node.Value) == "")
                {
                    AssetDatabase.AddObjectToAsset(node.Value, this);
                }
            }
        }
        #endregion
    }
}
