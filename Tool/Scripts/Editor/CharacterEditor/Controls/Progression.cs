using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using Burmuruk.RPGStarterTemplate.Stats;
using Burmuruk.RPGStarterTemplate.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ProgressionUIManager : SubWindow
    {
        public Button BtnBaseInfo { get; private set; }
        public Button BtnGeneralProgression { get; private set; }
        public Toggle TglApplyForAllLevels { get; private set; }
        public Button BtnAdd { get; private set; }
        public Button BtnRemove { get; private set; }
        public VisualElement StatsContainer { get; private set; }
        public ScrollView LevelButtonsScrollView { get; private set; }
        public VisualElement LevelButtonsContainer { get => LevelButtonsScrollView.contentContainer; }

        private List<Button> _levelButtons = new();
        private List<BasicStats> _statsPerLevel = new();
        private BasicStats _baseInfo;
        private BasicStats _increment;
        private BasicStats? _changesBaseInfo = null;

        private Button _selectedButton = null;
        private bool _applyForAllLevels = false;
        private CharacterProgress _changes = null;
        private CharacterType _changesCharacterType;
        private CharacterType _characterType;

        private BasicStats? CurrentData
        {
            get
            {
                if (_selectedButton == null) return null;

                if (_selectedButton == BtnBaseInfo)
                {
                    return _baseInfo;
                }
                else if (_selectedButton == BtnGeneralProgression)
                {
                    return _increment;
                }
                else
                {
                    int idx = _levelButtons.IndexOf(_selectedButton);
                    return _statsPerLevel[idx];
                }
            }

            set
            {
                if (_selectedButton == null) return;

                if (_selectedButton == BtnBaseInfo)
                {
                    _baseInfo = value.Value;
                }
                else if (_selectedButton == BtnGeneralProgression)
                {
                    _increment = value.Value;
                }
                else
                {
                    int idx = _levelButtons.IndexOf(_selectedButton);
                    _statsPerLevel[idx] = value.Value;
                }
            }
        }

        private Func<BasicStats> _getStats;
        private Action<BasicStats> _setStats;

        public void Initialize(VisualElement container, Func<BasicStats> getStats, Action<BasicStats> setStats)
        {
            base.Initialize(container);
            _getStats = getStats;
            _setStats = setStats;

            _instance = container.Q<VisualElement>("Progression");
            BtnBaseInfo = _instance.Q<Button>("btnBaseInfo");
            BtnGeneralProgression = _instance.Q<Button>("btnGeneralProgression");
            BtnAdd = _instance.Q<Button>("btnAddLevel");
            BtnRemove = _instance.Q<Button>("btnRemoveLevel");
            StatsContainer = container.Q<VisualElement>("statsContainer");
            LevelButtonsScrollView = _instance.Q<ScrollView>("levelButtons");
            TglApplyForAllLevels = _instance.Q<Toggle>("tglApplyAll");

            BtnBaseInfo.clicked += () => SwitchView(BtnBaseInfo);
            BtnGeneralProgression.clicked += () => SwitchView(BtnGeneralProgression);
            TglApplyForAllLevels.RegisterValueChangedCallback((e) => OnTglValueChanged(e.newValue));
            BtnAdd.clicked += OnClickedLevelButton;
            BtnRemove.clicked += RemoveLevel;

            SwitchView(BtnBaseInfo);
            EnableContainer(StatsContainer, true);
        }

        private void OnClickedLevelButton()
        {
            AddLevel();
            var button = _levelButtons[_levelButtons.Count - 1];
            SwitchView(button);
            LevelButtonsScrollView.schedule.Execute(() =>
            {
                LevelButtonsScrollView.ScrollTo(button);
            }).ExecuteLater(80);
        }

        public void Set_CharacterType(CharacterType type) => _characterType = type;

        private void SwitchView(Button newButton)
        {
            SaveCurrentStats();

            if (_selectedButton == newButton)
            {
                Highlight(_selectedButton, false);
                EnableContainer(StatsContainer, false);
                _selectedButton = null;
                return;
            }

            if (_selectedButton != null)
            {
                Highlight(_selectedButton, false);
                CurrentData = _getStats();
            }

            Highlight(newButton, true);
            _selectedButton = newButton;

            EnableContainer(StatsContainer, true);

            if (newButton == BtnBaseInfo)
            {
                _setStats(_baseInfo);
            }
            else if (newButton == BtnGeneralProgression)
            {
                _setStats(_increment);
            }
            else
            {
                int index = _levelButtons.IndexOf(newButton);
                _setStats(_statsPerLevel[index]);
            }
        }

        private void OnTglValueChanged(bool value)
        {
            ToggleLevelButtons(!value);
            BtnGeneralProgression.SetEnabled(value);
        }

        private void SaveCurrentStats()
        {
            if (_selectedButton == null) return;

            if (_selectedButton == BtnBaseInfo)
            {
                _baseInfo = _getStats();
            }
            else if (_selectedButton == BtnGeneralProgression)
            {
                _increment = _getStats();
            }
            else
            {
                int index = _levelButtons.IndexOf(_selectedButton);
                _statsPerLevel[index] = _getStats();
            }
        }

        private void AddLevel()
        {
            var levelIndex = _levelButtons.Count;
            Button newButton = null;
            newButton = new Button(() => SwitchView(newButton))
            {
                text = levelIndex.ToString()
            };

            _levelButtons.Add(newButton);
            _statsPerLevel.Add(new BasicStats());
            newButton.AddToClassList("LineExtraButton");
            LevelButtonsContainer.Add(newButton);

            BtnRemove.SetEnabled(true);
        }

        private void RemoveLevel()
        {
            if (_levelButtons.Count <= 0) return;

            if (_levelButtons.Count == 1)
                SwitchView(BtnBaseInfo);
            else
                SwitchView(_levelButtons[_levelButtons.Count - 2]);

            int lastIndex = _levelButtons.Count - 1;
            LevelButtonsContainer.Remove(_levelButtons[lastIndex]);
            _levelButtons.RemoveAt(lastIndex);
            _statsPerLevel.RemoveAt(lastIndex);

            BtnRemove.SetEnabled(_levelButtons.Count > 0);
        }

        private void ToggleLevelButtons(bool enable)
        {
            LevelButtonsContainer.SetEnabled(enable);
            ToggleAdditionButtons(enable);
        }

        private void ToggleAdditionButtons(bool enable)
        {
            BtnAdd.SetEnabled(enable);
            BtnRemove.SetEnabled(enable && _levelButtons.Count > 0);
        }

        public void LoadStats(CharacterProgress progress, BasicStats baseInfo, CharacterType type)
        {
            _changes = progress;
            _changesBaseInfo = baseInfo;
            _changesCharacterType = type;
            //if (progress == null)
            //    _changes = CreateInstance<CharacterProgress>();
            UpdateUIData(progress, baseInfo, type);
        }

        public void UpdateUIData(CharacterProgress progress, BasicStats baseInfo, CharacterType type)
        {
            _baseInfo = baseInfo;
            _increment = default;
            _characterType = type;
            _selectedButton = null;

            _statsPerLevel.Clear();
            LevelButtonsContainer.Clear();
            _levelButtons.Clear();

            if (progress.ApplyForAll(type))
            {
                _applyForAllLevels = true;
                _increment = progress.GetDataByLevel(type, -1).Value;
            }
            else
            {
                _applyForAllLevels = false;
                int i = 0;
                BasicStats? data;

                do
                {
                    data = progress.GetDataByLevel(type, i);
                    if (data.HasValue)
                    {
                        AddLevel();
                        _statsPerLevel[i] = data.Value;
                    }
                    i++;
                } while (data.HasValue);
            }

            TglApplyForAllLevels.value = _applyForAllLevels;
            OnTglValueChanged(_applyForAllLevels);
            SwitchView(BtnBaseInfo);
        }

        public override ModificationTypes Check_Changes()
        {
            ModificationTypes changes = ModificationTypes.None;

            if (_characterType != _changesCharacterType)
                changes |= ModificationTypes.EditData;

            SaveCurrentStats();
            FieldInfo[] fields = typeof(BasicStats).GetFields();

            if (_changesBaseInfo.HasValue && HasChanges(fields, _changesBaseInfo.Value, _baseInfo))
                changes |= ModificationTypes.EditData;

            if (_applyForAllLevels != TglApplyForAllLevels.value)
                return changes |= ModificationTypes.EditData;

            if (_changes == null) return changes;

            if (_applyForAllLevels)
            {
                if (HasChanges(fields, _increment, _changes.GetDataByLevel(_characterType, -1).Value))
                    changes |= ModificationTypes.EditData;
            }
            else
            {
                for (int i = 0; i < _statsPerLevel.Count; i++)
                {
                    var prev = _changes.GetDataByLevel(_changesCharacterType, i);
                    if (!prev.HasValue || HasChanges(fields, prev.Value, _statsPerLevel[i]))
                    {
                        changes |= ModificationTypes.EditData;
                        break;
                    }
                }
            }

            return changes;

            bool HasChanges(FieldInfo[] fields, BasicStats a, BasicStats b)
            {
                foreach (var field in fields)
                {
                    if (!Equals(field.GetValue(a), field.GetValue(b)))
                        return true;
                }
                return false;
            }
        }

        public void Get_Info(out CharacterProgress progress, out BasicStats baseInfo)
        {
            SaveCurrentStats();
            baseInfo = _baseInfo;
            progress = new CharacterProgress();
            List<CharacterProgress.LevelData> levels = new();

            if (TglApplyForAllLevels.value)
            {
                levels.Add(new CharacterProgress.LevelData
                {
                    level = -1,
                    stats = _increment
                });
            }
            else
            {
                for (int i = 0; i < _statsPerLevel.Count; i++)
                {
                    levels.Add(new CharacterProgress.LevelData
                    {
                        level = i,
                        stats = _statsPerLevel[i]
                    });
                }
            }

            progress.SetData(_characterType, TglApplyForAllLevels.value, levels);
        }

        public override void Clear()
        {
            _setStats?.Invoke(default);
            SwitchView(BtnBaseInfo);

            _selectedButton = null;
            _applyForAllLevels = false;
            _changes = null;
            _changesCharacterType = CharacterType.None;
            _changesBaseInfo = null;
            _baseInfo = default;

            _statsPerLevel.Clear();
            _levelButtons.ForEach(b => b.RemoveFromHierarchy());
            _levelButtons.Clear();
            BtnRemove.SetEnabled(false);
            _setStats?.Invoke(default);
            TglApplyForAllLevels.value = false;
            EnableContainer(StatsContainer, false);
            Clear_Highlights();
        }

        public override void Remove_Changes()
        {
            _changes = null;
            _changesCharacterType = CharacterType.None;
            _changesBaseInfo = null;
        }

        public override bool VerifyData(out List<string> errors)
        {
            Clear_Highlights();
            bool result = true;
            errors = new();

            if (_baseInfo != null)
                result &= Verify_Stats(_baseInfo);

            if (_applyForAllLevels)
            {
                foreach (var stats in _statsPerLevel)
                {
                    result &= Verify_Stats(stats);
                }
            }

            return result;
        }

        private bool Verify_Stats(in BasicStats stats)
        {
            bool result = true;

            var fields = typeof(BasicStats).GetFields();
            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(DisallowNegativeAttribute)))
                {
                    if (DisallowNegativeAttribute.ValidateUsage(field))
                    {
                        var value = float.Parse(field.GetValue(stats).ToString());
                        var stat = Container.Q<VisualElement>($"Stat_{field.Name}");

                        if (field.FieldType == typeof(int) || field.FieldType == typeof(Int32))
                        {
                            var statField = stat.Q<IntegerField>();
                            result &= statField.Verify_NegativaValue(null, _highlighted);
                        }
                        else
                        {
                            var statField = stat.Q<FloatField>();
                            result &= statField.Verify_NegativaValue(null, _highlighted);
                        }
                    }
                }
            }

            return result;
        }

        //private FloatField GetStatField()
        //{
        //    Container.Query<TextValueField>
        //}

        private void Clear_Highlights()
        {
            foreach (var element in _highlighted)
            {
                Set_Tooltip(element.Key, element.Value, false, BorderColour.Error);
            }
            _highlighted.Clear();
        }

        public override void Load_Changes()
        {
            LoadStats(_changes, _changesBaseInfo.Value, _characterType);
        }
    }
}
