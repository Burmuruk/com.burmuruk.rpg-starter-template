using Burmuruk.RPGStarterTemplate.Editor.Dialogue;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.TabCharacterEditor;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class CharacterSettings : BaseInfoTracker, ISaveable, ISubWindowsContainer
    {
        #region Variables
        VisualElement _parent;
        const string STATS_CONTAINER_NAME = "infoStats";
        Dictionary<CharacterTab, SubWindow> subTabs = new();
        CharacterData? _characterData;
        private string _id;
        private Dictionary<int, StatDataUI> _stats = new();
        private Dictionary<int, StatChange> _statsChanges = new();
        private Dictionary<int, PropertyField> _statsTypes = new();
        private int? _selectedStat = null;
        private List<ModEntry> allMods;
        private Dictionary<string, StatNameData> _statNames;
        private Dictionary<string, Type> _selectableClasses;
        private ProgressionUIManager _progression;
        //private BasicStats basicStats;

        CharacterTab _lastTab;
        CharacterTab curTab = CharacterTab.None;
        VisualElement statsContainer;
        StatsVisualizer basicStats = null;

        string[] _defaultVariables = new string[]
        {
            "speed",
            "damage",
            "damageRate",
            "color",
            "eyesRadious",
            "earsRadious",
            "minDistance",
        };
        string[] _defaultHeaders = new string[]
        {
            "Basic stats",
            "Detection",
        };
        private SerializedObject _serializedStats;

        enum CharacterTab
        {
            None,
            Inventory,
            Equipment,
            Health
        }

        private struct ModChanges
        {
            public List<string> remove;
            public List<ModChange> edit;
            public List<ModEntry> add;

            public void Initialize()
            {
                remove = new List<string>();
                edit = new List<ModChange>();
                add = new List<ModEntry>();
            }
        }

        private struct StatChanges
        {
            public List<string> remove;
            public List<ModChange> edit;
            public List<BasicStatsEditor.VariableEntry> add;

            public bool HasChanges()
            {
                return remove.Count > 0 || edit.Count > 0 || add.Count > 0;
            }

            public void Initialize()
            {
                remove = new List<string>();
                edit = new List<ModChange>();
                add = new List<BasicStatsEditor.VariableEntry>();
            }
        }

        class StatDataUI
        {
            public VisualElement extraSpace;
            public Toggle toggle;
            public VisualElement editButtons;
            StatData data = new();

            public string Name { get => data.name; set => data.name = value; }
            public ModifiableStat Type { get => data.type; set => data.type = value; }
            public bool Enabled { get => data.enabled; set => data.enabled = value; }
        }

        class StatChange
        {
            public ModifiableStat? type;
            public string name;
            public bool removed;

            public bool HasChanges()
            {
                return type.HasValue || name != null || removed;
            }

            public StatChange(ModifiableStat? type, string name)
            {
                this.type = type;
                this.name = name;
            }
        }
        #endregion

        #region Properties
        public override string Id => _id;
        public Toggle TglSave { get; private set; }
        public ComponentsListUI<ElementComponent> ComponentsList { get; private set; }
        public EnumModifierUI<CharacterType> EMCharacterType { get; private set; }
        public DropdownField DDFEnemyTag { get; private set; }
        public TextField TxtTagName { get; private set; }
        public ObjectField OFModel { get; private set; }
        public TreeViewList<DropElementData> DropsList { get; private set; }
        public PopupField<string> PUBaseClass { get; private set; }
        public VariablesAdderUI VariableAdder { get; private set; }
        public EquipmentSettings EquipmentS { get => (EquipmentSettings)subTabs[CharacterTab.Equipment]; }
        public InventorySettings InventoryS { get => (InventorySettings)subTabs[CharacterTab.Inventory]; }
        public VisualElement StatEditorContainer { get; private set; }
        private EnumModifierUI<ModifiableStat> EMStatType { get; set; }
        private Button BtnApplyStats { get; set; }
        #endregion

        public void Initialize(VisualElement container, CreationsBaseInfo name, VisualElement parent)
        {
            _parent = parent;
            Initialize(container, name);
        }

        #region Initialization
        public override void Initialize(VisualElement container, CreationsBaseInfo name)
        {
            base.Initialize(container, name);
            _instance = container;

            TglSave = container.Q<Toggle>("TglSave");
            VisualElement pBaseClass = container.Q<VisualElement>("PBaseClass");
            BtnApplyStats = container.Q<Button>("btnApplyStats");
            BtnApplyStats.clicked += OnClick_ApplyStats;
            DDFEnemyTag = container.Q<VisualElement>("ddfEnemyTag").Q<DropdownField>();
            OFModel = container.Q<ObjectField>("ofModel");
            TxtTagName = container.Q<VisualElement>("txtTagName").Q<TextField>();
            Setup_Model();
            Setup_DropsList();
            Setup_EnemyTag();
            Setup_PUBaseClass(pBaseClass);

            ComponentsList = new ComponentsListUI<ElementComponent>(container);
            Create_StatModifier();
            Create_StatEditor();

            Setup_ComponentsList();
            Setup_EMCharacterType();
            CreateSubTabs();
            Setup_Stats(container);
            Setup_Progression(container);
        }

        private void Setup_Progression(VisualElement container)
        {
            _progression = CreateInstance<ProgressionUIManager>();
            _progression.Initialize(container,
                () => basicStats.stats,
                bs =>
                {
                    basicStats.stats = bs;
                    var so = new SerializedObject(basicStats);
                    so.Update();
                });
        }

        private void Setup_DropsList()
        {
            DropsList = new TreeViewList<DropElementData>(Container);
            DropsList.Foldout.text = "Drops";
        }

        private void Setup_Model()
        {
            OFModel.objectType = typeof(GameObject);
            OFModel.RegisterValueChangedCallback(OnModelChanged);
        }

        private void OnModelChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue != null)
            {
                OFModel.tooltip = "";
                Highlight(OFModel, false);
            }
        }

        private void Setup_EnemyTag()
        {
            TxtTagName.RegisterCallback<KeyUpEvent>(OnValueChanged_TagName);
            DDFEnemyTag.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "New")
                {
                    EnableContainer(TxtTagName.parent, true);
                    TxtTagName.SetValueWithoutNotify(string.Empty);
                    TxtTagName.Focus();
                }
                else
                {
                    EnableContainer(TxtTagName.parent, false);
                }
            });
            DDFEnemyTag.choices.Clear();
            DDFEnemyTag.choices.Add("New");

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                DDFEnemyTag.choices.Add(t.stringValue);
            }
        }

        private void OnValueChanged_TagName(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return) return;

            if (VerifyVariableName(TxtName.value))
            {
                string tagName = TxtTagName.value.Trim();
                if (string.IsNullOrEmpty(tagName))
                {
                    Notify("Tag name cannot be empty", BorderColour.Error);
                    return;
                }
                if (!DDFEnemyTag.choices.Contains(tagName))
                {
                    DDFEnemyTag.choices.Add(tagName);
                    DDFEnemyTag.SetValueWithoutNotify(tagName);
                    Notify($"Tag '{tagName}' added", BorderColour.Success);
                    EnableContainer(TxtTagName.parent, false);
                }
                else
                {
                    Notify($"Tag '{tagName}' already exists", BorderColour.Error);
                }
            }
            else
            {
                Notify("Invalid tag name", BorderColour.Error);
            }
        }

        private void Create_StatEditor()
        {
            VisualTreeAsset element = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/StatEditor.uxml");
            StatEditorContainer = element.Instantiate();
            TextField textField = StatEditorContainer.Q<TextField>("txtStatName");
            textField.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    Rename_StatName(textField.text, textField);
                }
            });
        }

        private void Rename_StatName(string name, TextField textField)
        {
            if (_selectedStat == null) return;

            DisableNotification(NotificationType.Creation);
            Highlight(textField, false);
            string lower = name.ToLowerInvariant();

            if (!Verify_NewStatName(name, textField, lower))
                return;

            _statsChanges[_selectedStat.Value].name = name;

            VariableAdder.RequestEnable_ApplyButton(true);
            Highlight_StatChange(true);
            Disable_StatNameEditor(_selectedStat.Value);

            int idx = _selectedStat.Value;
            if (_statsChanges[idx].removed)
            {
                Enable_RemoveStat(idx, false);
            }
        }

        private bool Verify_NewStatName(string name, TextField textField, string lower)
        {
            if (string.IsNullOrEmpty(name))
            {
                _statsChanges[_selectedStat.Value].name = null;
                Disable_StatNameEditor(_selectedStat.Value);
                Highlight_StatChange(false);
                Verify_ModChanges();
                return false;
            }
            else if (name.Length < 3)
            {
                Delete_NewStatName("The name must be at least 3 characters long", textField);
                Highlight_StatChange(false);
                return false;
            }

            if (lower != _stats[_selectedStat.Value].Name.ToLower())
            {
                if (!VerifyVariableName(name))
                {
                    Notify("Invalid name", BorderColour.Error);
                    Highlight(textField, true, BorderColour.Error);
                    return false;
                }
            }
            else
            {
                Delete_NewStatName("The name must be different", textField);
                Highlight(textField, false);
                Highlight_StatChange(false);
                return false;
            }

            return true;
        }

        private void Highlight_StatChange(bool highlight)
        {
            var button = _stats[_selectedStat.Value].editButtons.Q<Button>("btnEditStat");
            Highlight(button, highlight, BorderColour.SpecialChange);
        }

        private void Delete_NewStatName(string message, TextField textField)
        {
            Notify(message, BorderColour.Error);
            Highlight(textField, true, BorderColour.Error);
            _statsChanges[_selectedStat.Value].name = null;
            Verify_ModChanges();
        }

        private void Disable_StatNameEditor(int idx)
        {
            if (_stats[idx].extraSpace.Contains(StatEditorContainer))
            {
                _stats[idx].extraSpace.Remove(StatEditorContainer);
            }

            if (_stats[idx].extraSpace.Contains(EMStatType.Container))
                return;

            EnableContainer(_stats[idx].extraSpace, false);
        }

        private void OnClick_ApplyStats()
        {
            var classes = GetClasses();
            var newStats = VariableAdder.GetInfo();
            string updatedText = string.Empty;
            string path = null;

            Get_Changes(newStats, out ModChanges mods, out StatChanges stats);
            DisableNotification(NotificationType.Creation);

            foreach (var name in classes)
            {
                path = GetCharacterScriptPath(name);

                if (string.IsNullOrEmpty(path))
                {
                    Notify($"{PUBaseClass.value} class was not found", BorderColour.Error);
                    return;
                }

                var text = File.ReadAllText(path);

                updatedText = ModSetupEditor.RemoveMods(text, mods.remove);
                updatedText = ModSetupEditor.RenameModChanges(updatedText, mods.edit);
                updatedText = ModSetupEditor.AddMods(updatedText, mods.add);
                File.WriteAllText(path, updatedText);
                AssetDatabase.SaveAssets();
            }


            Change_StatsNames(stats);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void Get_Changes(List<ModChange> newStats, out ModChanges mods, out StatChanges stats)
        {
            mods = new ModChanges();
            mods.Initialize();
            stats = new StatChanges();
            stats.Initialize();

            foreach (var stat in newStats)
            {
                if (stat.NewName != null)
                {
                    stats.add.Add(new BasicStatsEditor.VariableEntry
                    {
                        Name = stat.NewName,
                        Type = stat.VariableType.ToString(),
                        Header = stat.Header,
                    });

                    if (stat.Type != ModifiableStat.None && stat.VariableType == VariableType.@float)
                    {
                        mods.add.Add(new ModEntry
                        {
                            VariableName = stat.NewName,
                            ModifiableStat = stat.Type.ToString(),
                        });
                    }
                }
            }

            foreach (var item in _statsChanges)
            {
                if (!item.Value.HasChanges()) continue;

                StatChange change = item.Value;

                if (change.type.HasValue)
                {
                    if (_stats[item.Key].Type == ModifiableStat.None)
                    {
                        mods.add.Add(new ModEntry
                        {
                            VariableName = _stats[item.Key].Name,
                            ModifiableStat = change.type.Value.ToString(),
                        });
                    }
                    else if (change.type == ModifiableStat.None)
                    {
                        mods.remove.Add(_stats[item.Key].Name);
                    }
                    else
                    {
                        mods.edit.Add(new ModChange
                        {
                            OldName = _stats[item.Key].Name,
                            NewName = change.name == null ? null : change.name,
                            Type = change.type.Value,
                        });
                    }
                }

                if (change.name != null)
                {
                    mods.edit.Add(new ModChange
                    {
                        OldName = _stats[item.Key].Name,
                        NewName = change.name,
                        Type = _stats[item.Key].Type,
                    });
                    stats.edit.Add(new ModChange
                    {
                        OldName = _stats[item.Key].Name,
                        NewName = change.name,
                        Type = _stats[item.Key].Type,
                    });
                }

                if (change.removed)
                {
                    stats.remove.Add(_stats[item.Key].Name);
                    mods.remove.Add(_stats[item.Key].Name);
                }
            }
        }

        private void Change_StatsNames(StatChanges changes)
        {
            if (!changes.HasChanges()) return;

            string className = typeof(BasicStats).Name;
            string[] guids = AssetDatabase.FindAssets($"t:Script {className}");
            string path = null;

            foreach (string guid in guids)
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                string content = File.ReadAllText(path);

                if (Regex.IsMatch(content, $@"(?m)public\s+struct\s+\b{className}\b"))
                    break;

                path = null;
            }

            if (path == null) return;

            var text = File.ReadAllText(path);

            var updatedText = BasicStatsEditor.RemoveVariables(text, changes.remove);
            updatedText = BasicStatsEditor.AddVariables(updatedText, changes.add);

            foreach (var editedStat in changes.edit)
            {
                updatedText = BasicStatsEditor.RenameVariable(updatedText, editedStat.OldName, editedStat.NewName);
            }

            File.WriteAllText(path, updatedText);
        }

        private void Setup_PUBaseClass(VisualElement pBaseClass)
        {
            var derivedTypes = TypeCache.GetTypesDerivedFrom<RPGStarterTemplate.Control.Character>();
            var baseClasses = derivedTypes.Select(t => t.FullName).ToList();
            var shortNames = baseClasses.Select(name => name.Split(".").Last()).ToList();

            PUBaseClass = new PopupField<string>("Base class", shortNames, 0);
            PUBaseClass.style.flexBasis = new Length(98, LengthUnit.Percent);
            PUBaseClass.style.flexShrink = 1;
            PUBaseClass.RegisterValueChangedCallback(OnValueChanged_BaseClass);

            foreach (var name in shortNames)
            {
                if (name.Contains("GuildMember"))
                {
                    PUBaseClass.SetValueWithoutNotify(name);
                    break;
                }
            }

            _selectableClasses = new Dictionary<string, Type>();
            int i = 0;
            foreach (var name in shortNames)
            {
                _selectableClasses[name] = derivedTypes[i++];
            }
            pBaseClass.Add(PUBaseClass);
        }

        private void OnValueChanged_BaseClass(ChangeEvent<string> evt)
        {
            Read_StatsModifications();

            foreach (var stat in _stats.Values)
            {
                stat.Type = IsStatModified(stat.Name);
            }

            if (_selectedStat.HasValue)
            {
                Disable_Stat(_selectedStat.Value);
            }
        }

        private void Create_StatModifier()
        {
            VisualTreeAsset element = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/GeneralElements/TypeAdder.uxml");
            EMStatType = new EnumModifierUI<ModifiableStat>(element.Instantiate());
            EMStatType.EnumField.RegisterValueChangedCallback(OnSelected_StatType);
            EMStatType.Name.text = "Modification name";
        }

        private void Setup_Stats(VisualElement container)
        {
            var instance = ScriptableObject.CreateInstance<StatsVisualizer>();
            statsContainer = container.Q<VisualElement>(STATS_CONTAINER_NAME);
            statsContainer.Clear();
            basicStats = instance;
            Read_StatsModifications();
            AddStats();

            statsContainer.schedule.Execute(() =>
            {
                VisualElement adderUI = container.Q<VisualElement>("VariblesAdder");
                VariableAdder = new(adderUI, Get_Headers(), _statNames);
                VariableAdder.OnChange += value => EnableContainer(BtnApplyStats, value);
            }).ExecuteLater(200);
        }

        private List<string> Get_Headers()
        {
            var properties = statsContainer.Query<PropertyField>().ToList();
            List<string> labels = new();

            foreach (var property in properties)
            {
                var newLabels = property.Query<Label>().ToList();

                if (newLabels.Count > 1)
                {
                    labels.Add(newLabels[0].text);

                    if (_statNames.ContainsKey(newLabels[1].text))
                    {
                        _statNames[newLabels[1].text].header = newLabels[1].text;
                    }
                }
            }

            return labels;
        }

        private void Setup_ComponentsList()
        {
            ComponentsList.DDFElement.RegisterValueChangedCallback(OnElementSelected);
            ComponentsList.OnElementCreated += Setup_ComponentRemoveButton;
            ComponentsList.OnComponentClicked += OpenComponentSettings;
            ComponentsList.CreationValidator += ContainsCreation;
            ComponentsList.AddElementExtraData += Setup_ComponentButton;
            ComponentsList.OnElementRemoved += Clear_ComponentData;
            ComponentsList.DeletionValidator += Validate_ComponentRemoval;

            ComponentsList.DDFElement.choices.Clear();

            foreach (var name in Enum.GetNames(typeof(ComponentType)))
            {
                ComponentsList.DDFElement.choices.Add(name);
            }

            ComponentsList.DDFElement.value = "None";
        }

        private bool Validate_ComponentRemoval(int idx)
        {
            if (IsDisabled(ComponentsList[idx].element))
                return false;

            if (((ComponentType)ComponentsList[idx].Type) == ComponentType.Inventory &&
                ComponentsList.Contains(ComponentType.Equipment.ToString()))
            {
                Notify("Remove equipment before removing inventory component", BorderColour.Error);
                return false;
            }

            return true;
        }

        private void OnElementSelected(ChangeEvent<string> evt)
        {
            ComponentsList.AddComponent(evt);

            ComponentsList.DDFElement.schedule.Execute(() =>
            {
                UpdateComponentChoices();
            }).ExecuteLater(90);
        }

        private void Setup_ComponentRemoveButton(ElementComponent component)
        {
            component.RemoveButton.clicked += () => ComponentsList.RemoveComponent(component.idx);
        }

        private void Setup_EMCharacterType()
        {
            EMCharacterType = new EnumModifierUI<CharacterType>(_instance.Q<VisualElement>(EnumModifierUI<CharacterType>.ContainerName));
            EMCharacterType.Name.text = "Character Type";
            EMCharacterType.EnumField.Init(CharacterType.None);
            EMCharacterType.EnumField.RegisterValueChangedCallback(evt =>
            {
                if ((CharacterType)evt.newValue == CharacterType.None)
                {
                    Highlight(EMCharacterType.EnumField, true, BorderColour.Error);
                    return;
                }

                Highlight(EMCharacterType.EnumField, false);
                _progression.Set_CharacterType((CharacterType)evt.newValue);
            });
        }

        private void CreateSubTabs()
        {
            subTabs.Add(CharacterTab.None, this);

            subTabs.Add(CharacterTab.Inventory, CreateInstance<InventorySettings>());
            var inventory = (InventorySettings)subTabs[CharacterTab.Inventory];
            inventory.Initialize(_parent);
            inventory.GoBack += () => ChangeWindow(CharacterTab.None);
            inventory.OnElementClicked += ChangeTab;
            EnableContainer(inventory.Instance, false);

            subTabs.Add(CharacterTab.Equipment, CreateInstance<EquipmentSettings>());
            var equipment = subTabs[CharacterTab.Equipment];
            equipment.Initialize(_parent);
            equipment.GoBack += () => ChangeWindow(CharacterTab.None);
            EnableContainer(equipment.Instance, false);

            subTabs.Add(CharacterTab.Health, CreateInstance<HealthSettings>());
            subTabs[CharacterTab.Health].Initialize(_parent);
            subTabs[CharacterTab.Health].GoBack += () => ChangeWindow(CharacterTab.None);
            EnableContainer(subTabs[CharacterTab.Health].Instance, false);
        }
        #endregion

        #region Stats visualization
        private void AddStats()
        {
            _statNames = new Dictionary<string, StatNameData>();
            _stats.Clear();
            var members = typeof(BasicStats).GetFields();
            _serializedStats = new SerializedObject(basicStats);

            foreach (var member in members)
            {
                var prop = _serializedStats.FindProperty("stats");

                _statNames.Add(member.Name, new StatNameData
                {
                    variableType = member.GetType().ToString(),
                });

                bool modifiable = member.FieldType == typeof(float) || member.FieldType == typeof(int);

                AddStatUI(statsContainer, prop.FindPropertyRelative(member.Name), member.Name, modifiable);
            }

            _serializedStats.ApplyModifiedProperties();
        }

        private void Read_StatsModifications()
        {
            var classes = GetClasses();
            allMods = new();

            foreach (var name in classes)
            {
                string path = GetCharacterScriptPath(name);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($"{name} class was not found");
                    return;
                }

                var text = File.ReadAllText(path);
                allMods.AddRange(ModSetupEditor.ExtractAllMods(text));
            }

            return;
        }

        private List<string> GetClasses()
        {
            var classes = new List<string>()
            {
                "Character",
            };

            if (PUBaseClass.value != "Character")
            {
                Type curent = _selectableClasses[PUBaseClass.value];
                List<string> baseClasses = new();

                while (curent != null && curent.Name != "Character")
                {
                    baseClasses.Add(PUBaseClass.value);
                    curent = curent.BaseType;
                }

                classes.AddRange(baseClasses);
            }

            return classes;
        }

        public Type GetTypeByName(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    Type[] types = null;
                    try { types = assembly.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                    return types;
                })
                .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
        }

        string GetCharacterScriptPath(string className)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Script {className}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string content = File.ReadAllText(path);

                if (content.Contains($"public class {className}"))
                {
                    return path;
                }
            }

            return null;
        }

        private void OnSelected_StatType(ChangeEvent<Enum> evt)
        {
            if (!_selectedStat.HasValue) return;

            int idx = _selectedStat.Value;
            var newType = (ModifiableStat)evt.newValue;

            if (newType != _stats[idx].Type)
            {
                _statsChanges[idx].type = newType;
                VariableAdder.RequestEnable_ApplyButton(true);
                Highlight(_stats[idx].toggle.Q<VisualElement>("unity-checkmark"), true, BorderColour.SpecialChange);
                Enable_RemoveStat(idx, false);
            }
            else
            {
                _statsChanges[idx].type = null;
                Verify_ModChanges();

                _stats[idx].toggle.SetValueWithoutNotify(newType != ModifiableStat.None);
                Highlight(_stats[idx].toggle.Q<VisualElement>("unity-checkmark"), false);
            }

            Disable_Stat(idx);
            _selectedStat = null;
        }

        private void OnEnable_StatToggle(ChangeEvent<bool> evt, int idx)
        {
            Verify_LastToggleValue();

            if (!evt.newValue)
            {
                Disable_Stat(idx);
                _selectedStat = null;

                if (_stats[idx].Type == ModifiableStat.None)
                {
                    _statsChanges[idx].type = null;
                    Verify_ModChanges();
                    Highlight(_stats[idx].toggle.Q<VisualElement>("unity-checkmark"), false);
                }
                else
                {
                    _statsChanges[idx].type = ModifiableStat.None;
                    VariableAdder.RequestEnable_ApplyButton(true);
                    Highlight(_stats[idx].toggle.Q<VisualElement>("unity-checkmark"), true, BorderColour.SpecialChange);
                }
                return;
            }

            if (_statsChanges[idx].type.HasValue)
                EMStatType.EnumField.SetValueWithoutNotify(_statsChanges[idx].type.Value);
            else
                EMStatType.EnumField.SetValueWithoutNotify(_stats[idx].Type);
            _selectedStat = idx;

            _stats[idx].extraSpace.Add(EMStatType.Container);
            EnableContainer(_stats[idx].extraSpace, true);
        }

        private void Verify_LastToggleValue()
        {
            if (!_selectedStat.HasValue) return;

            var idx = _selectedStat.Value;

            if (_statsChanges[idx].type.HasValue && _statsChanges[idx].type != ModifiableStat.None)
            {
                _stats[idx].toggle.SetValueWithoutNotify(true);
                Highlight(_stats[idx].toggle.Q<VisualElement>("unity-checkmark"), true, BorderColour.SpecialChange);
                Enable_RemoveStat(idx, false);
            }
            else
            {
                Disable_Stat(idx);
                _stats[idx].toggle.SetValueWithoutNotify(false);
            }
        }

        private void Verify_ModChanges()
        {
            foreach (var chage in _statsChanges.Values)
            {
                if (chage.HasChanges())
                {
                    VariableAdder.RequestEnable_ApplyButton(true);
                    return;
                }
            }

            VariableAdder.RequestEnable_ApplyButton(false);
        }

        private void Disable_Stat(int idx)
        {
            if (_stats[idx].extraSpace.Contains(EMStatType.Container))
            {
                _stats[idx].extraSpace.Remove(EMStatType.Container);
            }

            if (_stats[idx].extraSpace.Contains(StatEditorContainer))
                return;

            EnableContainer(_stats[idx].extraSpace, false);
        }

        void AddStatUI(VisualElement parent, SerializedProperty property, string name, bool modifiable)
        {
            var row = GetRow();
            var field = new PropertyField(property, name);
            field.Bind(property.serializedObject);
            field.style.flexGrow = 1;
            field.name = $"Stat_{name}";

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.FlexStart;
            container.style.flexGrow = 0;

            var extraRow = GetRow();
            extraRow.style.flexGrow = 1;
            extraRow.style.marginBottom = 6;

            int idx = _stats.Count;
            var toggleColumn = Create_Toggle(idx, out Toggle toggle);

            toggle.SetEnabled(modifiable);

            row.Add(field);
            row.Add(toggleColumn);
            container.Add(row);
            container.Add(extraRow);
            parent.Add(container);

            EnableContainer(extraRow, false);
            _stats.Add(idx, new StatDataUI()
            {
                toggle = toggle,
                extraSpace = extraRow,
                Name = name,
                Type = IsStatModified(name),
            });
            _statsTypes[idx] = field;

            _statsChanges ??= new();
            _statsChanges[idx] = new StatChange(null, null);

            toggle.SetValueWithoutNotify(_stats[idx].Type != ModifiableStat.None);
            _statNames[name].type = _stats[idx].Type;

            if (_defaultVariables.Contains(name))
            {
                _statNames[name].editable = false;
                return;
            }

            _statNames[name].editable = true;

            //Add buttons to edit and remove the stat
            Add_EditButtons(row, idx);
        }

        private void Add_EditButtons(VisualElement row, int idx)
        {
            VisualTreeAsset editButtons = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/BtnsEditStat.uxml");
            VisualElement buttonsContainer = editButtons.Instantiate();
            buttonsContainer.style.flexGrow = 1;
            buttonsContainer.style.maxWidth = new Length(45, LengthUnit.Pixel);
            buttonsContainer.style.flexDirection = FlexDirection.Column;

            row.Add(buttonsContainer);
            _stats[idx].editButtons = row.Q<VisualElement>("EditButtons");
            Button edit = _stats[idx].editButtons.Q<Button>("btnEditStat");
            Button remove = _stats[idx].editButtons.Q<Button>("btnRemoveStat");

            edit.clicked += () => OnClick_EditStat(idx);
            remove.clicked += () => OnClick_RemoveStat(idx);
        }

        private void OnClick_RemoveStat(int idx)
        {
            bool removed = !_statsChanges[idx].removed;
            _statsChanges[idx].removed = removed;

            Enable_RemoveStat(idx, removed);

            if (removed)
            {
                _selectedStat = idx;
                Highlight(_stats[idx].toggle.Q<VisualElement>("unity-checkmark"), false);
                Highlight_StatChange(false);
                _statsChanges[idx].name = null;
                _statsChanges[idx].type = _stats[idx].Type == ModifiableStat.None ? null : _stats[idx].Type;
                _stats[idx].toggle.value = _stats[idx].Enabled;
                Disable_Stat(idx);
                Disable_StatNameEditor(idx);

                VariableAdder.RequestEnable_ApplyButton(true);
            }
            else
            {
                Verify_ModChanges();
            }

            _selectedStat = null;
        }

        private void Enable_RemoveStat(int idx, bool value)
        {
            Highlight(_stats[idx].editButtons.Q<Button>("btnRemoveStat"), value, BorderColour.SpecialChange);
            _statsChanges[idx].removed = value;
        }

        private void OnClick_EditStat(int idx)
        {
            if (!_stats[idx].extraSpace.Contains(StatEditorContainer))
            {
                _stats[idx].extraSpace.Add(StatEditorContainer);
                EnableContainer(_stats[idx].extraSpace, true);
                _selectedStat = idx;
                string name = _statsChanges[idx].name == null ? _stats[idx].Name : _statsChanges[idx].name;
                StatEditorContainer.Q<TextField>("txtStatName").SetValueWithoutNotify(name);
            }
            else
            {
                Disable_StatNameEditor(idx);
                _selectedStat = null;
            }
        }

        private ModifiableStat IsStatModified(string name)
        {
            var nameLower = name.ToLower();

            foreach (var mod in allMods)
            {
                if (mod.VariableName.ToLower() == nameLower)
                {
                    if (Enum.TryParse(mod.ModifiableStat, out ModifiableStat result))
                        return result;
                }
            }

            return ModifiableStat.None;
        }

        private VisualElement Create_Toggle(int idx, out Toggle toggle)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Column;
            row.style.flexGrow = 1;
            row.style.maxWidth = 20;
            var column = new VisualElement();
            column.style.flexDirection = FlexDirection.ColumnReverse;
            column.style.flexGrow = 1;

            toggle = new Toggle()
            {
                text = "",
                tooltip = $"Acción personalizada para {name}"
            };

            toggle.RegisterValueChangedCallback(evt => OnEnable_StatToggle(evt, idx));

            toggle.style.width = 24;
            toggle.style.marginLeft = 4;

            row.Add(column);
            column.Add(toggle);
            return row;
        }

        private VisualElement GetRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexGrow = 1,
                    minWidth = new Length(98, LengthUnit.Percent),
                    flexShrink = 0,
                    flexWrap = Wrap.Wrap
                }
            };

            return row;
        }
        #endregion

        #region Public methods
        public override void Clear()
        {
            foreach (var element in _highlighted)
                Utilities.UtilitiesUI.Set_Tooltip(element.Key, element.Value, false);

            if (curTab != CharacterTab.None)
            {
                subTabs[curTab].Clear();
                return;
            }

            ComponentsList.Clear();
            subTabs[CharacterTab.Inventory].Clear();
            subTabs[CharacterTab.Equipment].Clear();
            subTabs[CharacterTab.Health].Clear();
            CloseWindows();
            _lastTab = CharacterTab.None;
            EMCharacterType.Value = CharacterType.None;
            DDFEnemyTag.value = null;
            TglSave.value = false;
            OFModel.SetValueWithoutNotify(null);
            DropsList.Clear();
            _progression.Clear();
            _characterData = null;
            _id = null;
            base.Clear();
        }

        public override bool VerifyData(out List<string> errors)
        {
            bool result = true;
            bool isValid = false;
            errors = new();

            result &= _nameControl.VerifyData(out errors);

            //Character type
            result &= isValid = EMCharacterType.Value != CharacterType.None;
            _highlighted[EMCharacterType.EnumField] = EMCharacterType.EnumField.tooltip;
            Set_ErrorTooltip(EMCharacterType.EnumField, "Ivalid value", ref errors, isValid);

            //Enemy tag
            result &= isValid = DDFEnemyTag.value != "New" || !string.IsNullOrEmpty(DDFEnemyTag.value.Trim());
            _highlighted[DDFEnemyTag] = DDFEnemyTag.tooltip;
            Set_ErrorTooltip(DDFEnemyTag, "Invalid tag", ref errors, isValid);

            //Model
            result &= isValid = OFModel.value != null;
            _highlighted[OFModel] = OFModel.tooltip;
            Set_ErrorTooltip(OFModel, "A model is required", ref errors, isValid);

            //Drops
            result &= isValid = DropsList.VerifyData(out var dropErrors);
            (errors ??= new()).AddRange(dropErrors);

            //Progression
            result &= _progression.VerifyData(out var progressErrors);
            errors.AddRange(progressErrors);

            //Components
            Verify_ComponentsData(errors, ref result, ref isValid);

            return result;
        }

        private void Verify_ComponentsData(List<string> errors, ref bool result, ref bool isValid)
        {
            foreach (var component in ComponentsList.Components)
            {
                if (!IsDisabled(component.element))
                {
                    var tabType = Get_TabType((ComponentType)component.Type);

                    if (tabType == CharacterTab.None) continue;

                    if (subTabs.ContainsKey(tabType) && subTabs[tabType] != null)
                    {
                        result &= isValid = subTabs[tabType].VerifyData(out var tabErrors);
                        SetComponent_ErrorBorder(component, !isValid);
                        errors.AddRange(tabErrors);
                    }
                }
            }
        }

        public override ModificationTypes Check_Changes()
        {
            try
            {
                if (_characterData == null) return CurModificationType = ModificationTypes.Add;

                CurModificationType = ModificationTypes.None;

                //Name
                if (_nameControl.Check_Changes() != ModificationTypes.None)
                {
                    CurModificationType = ModificationTypes.Rename;
                }

                //Progresssion
                if (_progression.Check_Changes() != ModificationTypes.None)
                    CurModificationType = ModificationTypes.EditData;

                //Type
                if (_characterData.Value.characterType != EMCharacterType.Value)
                    CurModificationType = ModificationTypes.EditData;

                //Model
                if (OFModel.value != SavingSystem.GetAsset<GameObject>(_characterData.Value.model))
                    CurModificationType = ModificationTypes.EditData;

                //Drops
                Check_DropsChanges();

                //Components
                Check_ComponentsChanges();

                //saving
                if (_characterData.Value.shouldSave != TglSave.value)
                    CurModificationType = ModificationTypes.EditData;

                //Enemy
                if (DDFEnemyTag.value != _characterData.Value.enemyTag)
                    CurModificationType = ModificationTypes.EditData;

                return CurModificationType;
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }
        }

        private void Check_DropsChanges()
        {
            if (_characterData.Value.drops == null)
            {
                if (DropsList.TxtCount.value > 0)
                    CurModificationType = ModificationTypes.EditData;
            }
            else if (_characterData.Value.drops.Count != DropsList.TxtCount.value)
                CurModificationType = ModificationTypes.EditData;
            else
            {
                var newDropData = DropsList.GetInfo() as TreeViewListData;

                for (int i = 0; i < _characterData.Value.drops.Count; i++)
                {
                    var cur = _characterData.Value.drops[i];
                    var newDrop = newDropData.Elements[i] as DropData;

                    if (cur != newDrop.Name)
                    {
                        CurModificationType = ModificationTypes.EditData;
                        break;
                    }
                }
            }
        }

        private void Check_ComponentsChanges()
        {
            if (!(_characterData.Value.components == null ^ ComponentsList.Components != null))
            {
                CurModificationType = ModificationTypes.EditData;
            }
            else if (ComponentsList?.Components != null)
            {
                if (_characterData.Value.components.Count == ComponentsList.EnabledCount)
                {
                    foreach (var component in ComponentsList.EnabledComponents)
                    {
                        if (!_characterData.Value.components.ContainsKey((ComponentType)component.Type))
                        {
                            CurModificationType = ModificationTypes.EditData;
                            break;
                        }

                        var tabType = Get_TabType((ComponentType)component.Type);

                        if (tabType == CharacterTab.None) continue;

                        if (subTabs.ContainsKey(tabType) && subTabs[tabType] != null)
                        {
                            if (subTabs[tabType].Check_Changes() != ModificationTypes.None)
                            {
                                CurModificationType = ModificationTypes.EditData;
                                break;
                            }
                        }
                    }
                }
                else
                    CurModificationType = ModificationTypes.EditData;
            }
        }

        public bool Save()
        {
            if (!VerifyData(out var errors))
            {
                string error = errors.Count <= 0 ? "Invalid data" : errors[0];
                Notify(error, BorderColour.Error);

                return false;
            }

            if (_creationsState == CreationsState.Editing && Check_Changes() == ModificationTypes.None)
            {
                Notify("No changes were found", BorderColour.HighlightBorder);
                return false;
            }
            else
                CurModificationType = ModificationTypes.Add;

            DisableNotification(NotificationType.Creation);
            var newData = GetInfo();
            return SavingSystem.SaveCreation(ElementType.Character, _id, new CharacterCreationData(newData.characterName, newData), CurModificationType);
        }

        public CreationData Load(ElementType type, string id)
        {
            var result = SavingSystem.Load(ElementType.Character, id);

            if (result == null) return null;

            var data = (CharacterCreationData)result;
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(data.Data, id);

            return data;
        }

        public CreationData Load(string id)
        {
            var result = SavingSystem.Load(id);

            if (result == null) return null;

            var data = (CharacterCreationData)result;
            Set_CreationState(CreationsState.Editing);
            UpdateInfo(data.Data, id);

            return data;
        }

        public override void Load_Changes()
        {
            if (curTab != CharacterTab.None)
            {
                subTabs[curTab].Load_Changes();
                return;
            }

            _lastTab = CharacterTab.None;
            CharacterData newInfo = _characterData.Value;
            UpdateInfo(newInfo, _id);
        }

        public void CloseWindows()
        {
            for (int i = 1; i < Enum.GetValues(typeof(CharacterTab)).Length; i++)
            {
                EnableContainer(subTabs[(CharacterTab)i].Instance, false);
            }

            curTab = CharacterTab.None;
        }

        public override void Enable(bool enabled)
        {
            base.Enable(enabled);

            if (enabled)
                ChangeWindow(_lastTab);
            else
                CloseWindows();
        }

        CreationData IDataProvider.GetInfo()
        {
            return new CharacterCreationData(_id, GetInfo());
        }

        public void UpdateInfo(CreationData cd)
        {
            var data = cd as CharacterCreationData;

            if (string.IsNullOrEmpty(cd.Id))
                _creationsState = CreationsState.Creating;
            else
            {
                _creationsState = CreationsState.Editing;
                Load(cd.Id);
            }

            _id = cd.Id;
            UpdateUIData(data.Data);
        }
        #endregion

        #region Events
        private void OpenComponentSettings(int componentIdx)
        {
            var type = (ComponentType)ComponentsList[componentIdx].Type;

            switch (type)
            {
                case ComponentType.Equipment:
                    Load_InventoryItemsInEquipment();
                    break;

                case ComponentType.Dialogue:
                    if (DialogueEditor.window == null)
                        DialogueEditor.ShowEditorWindow();

                    DialogueEditor.window.Show();
                    DialogueEditor.window.Focus();
                    DialogueEditor.window.ShowCharacterDialogues(_id);
                    break;

                default: break;
            }

            CharacterTab newTab = Get_TabType(type);

            if (newTab == CharacterTab.None)
                return;

            ChangeWindow(newTab);
        }

        private int? ContainsCreation(IList list, string name)
        {
            var components = (List<ElementComponent>)list;
            int i = 0;
            int emptyIdx = -1;

            foreach (var component in components)
            {
                if (component.element.ClassListContains("Disable"))
                {
                    emptyIdx = i;
                    break;
                }

                if (component.NameButton.text == name)
                {
                    return null;
                }

                ++i;
            }

            return emptyIdx;
        }

        /// <summary>
        /// Setup element by setting id, changing colour or enable its button.
        /// </summary>
        /// <param name="element"></param>
        private void Setup_ComponentButton(ElementComponent element)
        {
            switch ((ComponentType)element.Type)
            {
                case ComponentType.Equipment:
                    AddEquipment(element);
                    goto case ComponentType.Inventory;

                case ComponentType.Dialogue:
                case ComponentType.Inventory:
                case ComponentType.Health:
                    SetClickableButtonColour(element, true);
                    break;

                default:
                    SetClickableButtonColour(element, false);
                    break;
            }

            SetComponent_ErrorBorder(element, false);
        }

        private void Clear_ComponentData(ElementComponent element)
        {
            var tabType = Get_TabType((ComponentType)element.Type);
            ComponentsList.DDFElement.schedule.Execute(() =>
                UpdateComponentChoices()).ExecuteLater(90);

            if (tabType == CharacterTab.None) return;

            if (!subTabs.ContainsKey(tabType)) return;

            subTabs[tabType].Clear();
        }

        private void UpdateComponentChoices()
        {
            var names = new List<string>(Enum.GetNames(typeof(ComponentType)));

            for (int i = 0; i < names.Count; i++)
            {
                if (ComponentsList.Contains(names[i]))
                    names.RemoveAt(i--);
            }

            ComponentsList.DDFElement.choices = names;
        }
        #endregion

        /// <summary>
        /// Adds an inventory if the new element is an equipment and the inventory it's not in the list.
        /// </summary>
        /// <param name="element">Equipment component</param>
        private void AddEquipment(ElementComponent element)
        {
            if ((ComponentType)element.Type != ComponentType.Equipment) return;

            var comps = (from c in ComponentsList.Components
                         where (ComponentType)c.Type == ComponentType.Inventory && !c.element.ClassListContains("Disable")
                         select c).ToArray();

            if (comps == null || comps.Length == 0)
            {
                ComponentsList.AddElement(ComponentType.Inventory.ToString());
            }
        }

        private void SetClickableButtonColour(ElementComponent element, bool clickable)
        {
            //var (white, whiteLight) = ("WhiteBorder", "whiteBorder-light");
            var (white, whiteLight) = ("WhiteBorder", "WhiteBorder");
            var (newClass, lastClass) = clickable ? (white, whiteLight) : (whiteLight, white);

            //if (!element.NameButton.ClassListContains(newClass))
            element.NameButton.AddToClassList(newClass);

            if (element.NameButton.ClassListContains(lastClass))
                element.NameButton.RemoveFromClassList(lastClass);

            element.NameButton.SetEnabled(clickable);
        }

        private void SetComponent_ErrorBorder(ElementComponent component, bool shouldSet)
        {
            if (!component.NameButton.ClassListContains("error-border"))
            {
                if (shouldSet)
                {
                    component.NameButton.AddToClassList("error-border");
                }
            }
            else if (!shouldSet)
            {
                component.NameButton.RemoveFromClassList("error-border");
            }
        }

        #region Tab control
        private void ChangeTab(ComponentType type)
        {
            if (type == ComponentType.Health)
                ChangeWindow(CharacterTab.Health);
        }

        private void ChangeWindow(CharacterTab newTab)
        {
            if (curTab == newTab) return;

            EnableContainer(subTabs[curTab].Instance, false);
            EnableContainer(subTabs[newTab].Instance, true);
            _lastTab = newTab;
            curTab = newTab;
        }
        #endregion

        private CharacterTab Get_TabType(ComponentType type) =>
            type switch
            {
                ComponentType.Equipment => CharacterTab.Equipment,
                ComponentType.Health => CharacterTab.Health,
                ComponentType.Inventory => CharacterTab.Inventory,
                _ => CharacterTab.None
            };

        private void Load_InventoryItemsInEquipment()
        {
            var items = ((InventorySettings)subTabs[CharacterTab.Inventory]).MClInventoryElements;
            //var inventory = (subTabs[CharacterTab.Inventory] as InventorySettings).GetInventory();
            var equipment = ((EquipmentSettings)subTabs[CharacterTab.Equipment]);
            equipment.Load_EquipmentFromList(items);
            equipment.Set_Model((GameObject)OFModel.value);
        }

        public CharacterData GetInfo()
        {
            CharacterData newData = new();
            newData.characterName = TempName;
            newData.shouldSave = TglSave.value;
            newData.className = _selectableClasses[PUBaseClass.value].AssemblyQualifiedName;
            newData.components ??= new();
            AddCharacterComponents(ref newData);
            newData.characterType = (CharacterType)EMCharacterType.EnumField.value;
            newData.model = SavingSystem.GetAssetReference(OFModel.value);
            newData.enemyTag = DDFEnemyTag.value;
            newData.drops = Get_DropsInfo();
            _progression.Get_Info(out newData.progress, out newData.basicStats);

            return newData;
        }

        public void UpdateInfo(in CharacterData newData, string id)
        {
            Clear();
            _characterData = newData;
            TempName = _characterData.Value.characterName;
            _originalName = _characterData.Value.characterName;
            UpdateName();

            string className = Find_SelectableClass(newData.className);
            PUBaseClass.value = string.IsNullOrEmpty(className) ? "" : className;
            OFModel.value = SavingSystem.GetAsset<GameObject>(_characterData.Value.model);
            LoadCharacterComponents(in newData);
            _progression.LoadStats(newData.progress, newData.basicStats, newData.characterType);
            DropsList.UpdateInfo(Convert_DropsInfo(_characterData.Value.drops));
            TglSave.value = _characterData.Value.shouldSave;
            DDFEnemyTag.value = _characterData.Value.enemyTag ?? "";
            EMCharacterType.Value = _characterData.Value.characterType;
            ComponentsList.DDFElement.value = "None";
            UpdateComponentChoices();
            _id = id;

        }

        private string Find_SelectableClass(string className)
        {
            Type searchType = Type.GetType(className);

            foreach (var item in _selectableClasses)
            {
                if (item.Value == searchType)
                {
                    return item.Key;
                }
            }

            return null;
        }

        private void UpdateUIData<T>(in T arg1) where T : struct
        {
            if (!(arg1 is CharacterData newData)) return;

            if (string.IsNullOrEmpty(Id))
                _originalName = newData.characterName;
            TempName = newData.characterName;
            UpdateName();

            string className = Find_SelectableClass(newData.className);
            PUBaseClass.value = string.IsNullOrEmpty(className) ? "" : className;
            OFModel.value = SavingSystem.GetAsset<GameObject>(newData.model);
            UpdateComponentsUI(in newData);
            _progression.UpdateUIData(newData.progress, newData.basicStats, newData.characterType);
            DropsList.UpdateUIData(Convert_DropsInfo(newData.drops));

            TglSave.value = newData.shouldSave;
            DDFEnemyTag.value = newData.enemyTag ?? "";
            EMCharacterType.Value = newData.characterType;
            ComponentsList.DDFElement.value = "None";
            UpdateComponentChoices();
        }

        private List<string> Get_DropsInfo()
        {
            var data = DropsList.GetInfo() as TreeViewListData;
            var drops = new List<string>();

            data.Elements.ForEach(e => drops.Add((e as DropData).Name));

            return drops;
        }

        private CreationData Convert_DropsInfo(List<string> drops)
        {
            TreeViewListData data = new();

            if (drops == null) return data;

            foreach (var drop in drops)
            {
                data.Elements.Add(new DropData(drop));
            }

            return data;
        }

        private void AddCharacterComponents(ref CharacterData characterData)
        {
            var components = from comp in ComponentsList.Components
                             where !comp.element.ClassListContains("Disable")
                             select comp;

            foreach (var component in components)
            {
                switch ((ComponentType)component.Type)
                {
                    case ComponentType.Health:
                        Health health = ((HealthSettings)subTabs[CharacterTab.Health]).GetInfo();

                        characterData.components[ComponentType.Health] = health;
                        break;

                    case ComponentType.Inventory:
                        AddInventoryComponent(ref characterData);
                        break;

                    case ComponentType.Equipment:
                        AddInventoryComponent(ref characterData);

                        var inventory = (Inventory)characterData.components[ComponentType.Inventory];
                        var equipment = EquipmentS.GetEquipment(in inventory);
                        characterData.components[ComponentType.Equipment] = equipment;
                        equipment.modelPath = SavingSystem.GetAssetReference(OFModel.value);
                        break;

                    case ComponentType.None:
                        break;

                    //case ComponentType.Dialogue:
                    //    break;

                    default:
                        characterData.components[(ComponentType)component.Type] = null;
                        break;
                }
            }
        }

        private void AddInventoryComponent(ref CharacterData characterData)
        {
            if (characterData.components.ContainsKey(ComponentType.Inventory))
                return;

            var inventory = ((InventorySettings)subTabs[CharacterTab.Inventory]).GetInventory();
            characterData.components[ComponentType.Inventory] = inventory;
        }

        private void UpdateComponentsUI(in CharacterData data)
        {
            ComponentsList.Clear();

            if (data.components == null) return;

            foreach (var component in data.components)
            {
                switch (component.Key)
                {
                    case ComponentType.None:
                        continue;

                    case ComponentType.Health:
                        ((HealthSettings)subTabs[CharacterTab.Health]).UpdateUIData((Health)component.Value);
                        break;

                    case ComponentType.Inventory:
                        ((InventorySettings)subTabs[CharacterTab.Inventory]).UpdateUIData((Inventory)component.Value);
                        var items = ((InventorySettings)subTabs[CharacterTab.Inventory]).MClInventoryElements;

                        if (data.components.ContainsKey(ComponentType.Equipment))
                        {
                            var equipment = data.components[ComponentType.Equipment];
                            EquipmentS.UpdateUIData((Equipment)equipment, OFModel.value as GameObject, items);
                            ComponentsList.AddElement(ComponentType.Equipment.ToString());
                        }
                        break;

                    //case ComponentType.Equipment:
                    //    EquipmentS.UpdateUIData((Equipment)component.Value, OFModel.value as GameObject);
                    //    break;

                    default:
                        break;
                }

                ComponentsList.AddElement(component.Key.ToString());
            }
        }

        private void LoadCharacterComponents(in CharacterData data)
        {
            ComponentsList.Clear();

            if (data.components == null) return;

            foreach (var component in data.components)
            {
                switch (component.Key)
                {
                    case ComponentType.None:
                        continue;

                    case ComponentType.Health:
                        ((HealthSettings)subTabs[CharacterTab.Health]).LoadInfo((Health)component.Value);
                        break;

                    case ComponentType.Inventory:
                        ((InventorySettings)subTabs[CharacterTab.Inventory]).LoadInventoryItems((Inventory)component.Value);
                        var items = ((InventorySettings)subTabs[CharacterTab.Inventory]).MClInventoryElements;

                        if (data.components.ContainsKey(ComponentType.Equipment))
                        {
                            var equipment = data.components[ComponentType.Equipment];
                            EquipmentS.LoadEquipment((Equipment)equipment, OFModel.value as GameObject, items);
                            ComponentsList.AddElement(ComponentType.Equipment.ToString());
                        }
                        break;

                    default:
                        break;
                }

                ComponentsList.AddElement(component.Key.ToString());
            }
        }

        public override void Remove_Changes()
        {
            _characterData = null;
            _progression.Remove_Changes();
            DropsList.Remove_Changes();
            //foreach (var tab in subTabs)
            //    tab.Value?.Remove_Changes();
            subTabs[CharacterTab.Equipment]?.Remove_Changes();
            subTabs[CharacterTab.Inventory].Remove_Changes();
            subTabs[CharacterTab.Health].Remove_Changes();
            _id = null;
        }
    }

    public interface ISubWindowsContainer
    {
        public void CloseWindows();
    }

    public class StatData
    {
        public string name;
        public ModifiableStat type;
        public bool enabled;
    }

    public class StatNameData
    {
        public string header;
        public ModifiableStat type;
        public bool editable;
        public string variableType;
    }
}
