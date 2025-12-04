using Burmuruk.RPGStarterTemplate.Stats;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class BuffAdderUI : MyCustomList, IUIListContainer<BaseCreationInfo>, IChangesObserver
    {
        public const string INVALIDNAME = "Custom";

        private List<NamedBuff?> _changes = new();
        private List<BuffsDataUI> buffs = new();
        private Dictionary<string, string> buffNames_Ids;

        public BuffAdderUI(VisualElement container) : base(container)
        {
            TxtCount.RegisterCallback<KeyUpEvent>(OnValueChanged_BuffsCount);
            CreationScheduler.Add(ModificationTypes.Rename, ElementType.Buff, this);
            CreationScheduler.Add(ModificationTypes.Add, ElementType.Buff, this);
            CreationScheduler.Add(ModificationTypes.Remove, ElementType.Buff, this);
            buffNames_Ids = CreationScheduler.GetNames(ElementType.Buff);
            buffNames_Ids ??= new();
        }

        public virtual void AddData(in BaseCreationInfo data)
        {
            var newBuffsNames = buffNames_Ids;
            newBuffsNames.TryAdd(data.Name, data.Id);

            foreach (var buff in buffs)
            {
                bool found = false;
                buffNames_Ids.TryGetValue(buff.DDBuff.value, out string selectedId);
                buff.DDBuff.choices.Clear();

                buff.DDBuff.choices.Add("Custom");
                buff.DDBuff.choices.AddRange(newBuffsNames.Keys);

                foreach (var newName in newBuffsNames)
                {
                    if (newName.Value == selectedId)
                    {
                        buff.DDBuff.value = newName.Key;
                        found = true;
                    }
                }

                if (!found)
                    buff.DDBuff.value = "None";
            }

            this.buffNames_Ids = newBuffsNames;
        }

        public virtual void RemoveData(in BaseCreationInfo newValue)
        {
            foreach (var buff in buffs)
            {
                buffNames_Ids.TryGetValue(buff.DDBuff.value, out string selectedId);

                if (selectedId == newValue.Id)
                {
                    buff.DDBuff.value = "None";
                }

                buff.DDBuff.choices.Remove(newValue.Name);
            }

            buffNames_Ids.Remove(newValue.Name);
        }

        public virtual void RenameCreation(in BaseCreationInfo newValue)
        {
            int? idx = null;
            string name = null;
            int i = 1;
            Dictionary<string, string> newNames = new();

            foreach (var buffData in buffNames_Ids)
            {
                if (buffData.Value == newValue.Id)
                {
                    idx = i;
                    name = buffData.Key;
                    newNames.Add(name, buffData.Value);
                }
                else
                    newNames.Add(buffData.Key, buffData.Value);

                ++i;
            }

            if (!idx.HasValue) return;

            foreach (var buff in buffs)
            {
                buff.DDBuff.choices[idx.Value] = newValue.Name;

                if (buff.DDBuff.value == name)
                {
                    buff.DDBuff.value = newValue.Name;
                }
            }

            buffNames_Ids = newNames;
        }

        private void OnValueChanged_BuffsCount(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;

            int amount = ((int)TxtCount.value) - buffs.Count;

            if (amount == 0)
            {
                buffs.ForEach(buff => { _elementsContainer.Remove(buff.Element); });
                buffs.Clear();
            }
            else if (amount > 0)
            {
                while (amount > 0)
                {
                    AddBuff();
                    --amount;
                }
            }
            else
            {
                while (amount < 0)
                {
                    RemoveBuff();
                    ++amount;
                }
            }
        }

        protected override void SetupFoldOut()
        {
            base.SetupFoldOut();

            Foldout.text = "Buffs";
            BtnAdd.clicked += () => AddBuff();
            BtnRemove.clicked += () => RemoveBuff();
        }

        private VisualElement AddBuff()
        {
            var buff = new BuffsDataUI();
            buff.SetValues(buffNames_Ids);

            buffs.Add(buff);
            _elementsContainer.Add(buff.Element);

            TxtCount.SetValueWithoutNotify((uint)buffs.Count);
            return buff.Element;
        }

        private void RemoveBuff()
        {
            if (buffs.Count == 0) return;

            var buff = buffs[buffs.Count - 1];

            _elementsContainer.Remove(buff.Element);
            buffs.RemoveAt(buffs.Count - 1);
            TxtCount.SetValueWithoutNotify((uint)buffs.Count);
        }

        public void UpdateData(List<(string id, BuffData? buff)> buffsData)
        {
            if (buffsData.Count <= 0)
            {
                Clear();
                return;
            }

            int max = Mathf.Min(buffs.Count, buffsData.Count);
            _changes = new();
            int i = 0;

            for (; i < max; i++)
            {
                if (!TryGetBuffName(buffsData[i].id, out string curName))
                    continue;

                buffs[i].UpdateData(curName, buffsData[i].buff);
                _changes.Add(new(curName, buffsData[i].buff));
            }

            if (buffsData.Count > buffs.Count)
            {
                for (int j = i; j < buffsData.Count; j++)
                {
                    if (!TryGetBuffName(buffsData[j].id, out string curName))
                        continue;

                    AddBuff();
                    buffs[buffs.Count - 1].UpdateData(curName, buffsData[j].buff);
                    _changes.Add(new(curName, buffsData[i].buff));
                }
            }
            else if (buffsData.Count < buffs.Count)
            {
                for (int j = i; j < buffs.Count; j++)
                {
                    RemoveBuff();
                }
            }
        }

        public virtual void UpdateUIData<T>(T buffsData) where T : List<(string id, BuffData? buff)>
        {
            if (buffsData.Count <= 0) return;

            int max = Mathf.Min(buffs.Count, buffsData.Count);
            int i = 0;

            for (; i < max; i++)
            {
                if (!TryGetBuffName(buffsData[i].id, out string curName))
                    continue;

                buffs[i].UpdateData(curName, buffsData[i].buff);
            }

            if (buffsData.Count > buffs.Count)
            {
                for (int j = i; j < buffsData.Count; j++)
                {
                    if (!TryGetBuffName(buffsData[j].id, out string curName))
                        continue;

                    AddBuff();
                    buffs[buffs.Count - 1].UpdateData(curName, buffsData[j].buff);
                }
            }
            else if (buffsData.Count < buffs.Count)
            {
                for (int j = i; j < buffs.Count; j++)
                {
                    RemoveBuff();
                }
            }
        }

        private bool TryGetBuffName(string id, out string newName)
        {
            newName = id switch
            {
                null => null,
                "" => INVALIDNAME,
                _ => GetNameById(id),
            };

            return newName is not null;
        }

        private string GetNameById(string id)
        {
            foreach (var value in buffNames_Ids)
            {
                if (value.Value == id)
                    return value.Key;
            }

            return null;
        }

        /// <summary>
        /// Returns the corresponding ids with data. Empty values are discarded.
        /// </summary>
        /// <returns></returns>
        public List<NamedBuff> GetBuffsData()
        {
            var buffsData = new List<NamedBuff>();

            foreach (var buff in buffs)
            {
                NamedBuff data = buff.GetInfo();

                if (data.name == null)
                {
                    continue;
                }

                if (data.name != "")
                    data.name = buffNames_Ids[data.name];

                buffsData.Add(data);
            }

            return buffsData;
        }

        public override void Clear()
        {
            base.Clear();
            buffs.Clear();
            _changes = null;
        }

        public void Remove_Changes()
        {
            _changes = null;
        }

        public bool VerifyData(out List<string> errors)
        {
            errors = new();
            return true;
        }

        public override ModificationTypes Check_Changes()
        {
            if (_changes == null) return ModificationTypes.None;
            
            var namedBuffs = GetBuffsData();
            Check_Names(namedBuffs);

            if (_changes.Count != namedBuffs.Count)
                return CurModificationType = ModificationTypes.EditData;

            //for (int i = 0; i < _changes.Count; i++)
            //{
            //    if (_changes[i].Value.Name != namedBuffs[i].Name)
            //        return CurModificationType = ModificationType.EditData;

            //    if (namedBuffs[i].Name == INVALIDNAME)
            //    {
            //        if (_changes[i].Value.Data != namedBuffs[i].Data)
            //            return CurModificationType = ModificationType.EditData;
            //    }
            //}

            return CurModificationType;
        }

        private void Check_Names(List<NamedBuff> namedBuffs)
        {
            foreach (var buff in namedBuffs)
            {
                if (buff.name != "")
                {
                    bool containsName = false;
                    foreach (var name in _changes)
                    {
                        if (name.HasValue && name.Value.name == buff.name)
                        {
                            containsName = true;
                            break;
                        }
                    }

                    if (!containsName)
                    {
                        CurModificationType = ModificationTypes.EditData;
                    }
                }
                else if (buff.name == "")
                {
                    bool hasData = false;

                    foreach (var change in _changes)
                    {
                        if (change.HasValue && change.Value.name == "")
                        {
                            if (change.Value.Data == buff.Data)
                            {
                                hasData = true;
                                break;
                            }
                        }
                    }

                    if (!hasData)
                        CurModificationType = ModificationTypes.EditData;
                }
            }
        }

        public void Load_Changes()
        {
            if (_changes == null)
            {
                Clear();
                return;
            }

            List<(string id, BuffData? data)> newData = new();

            foreach (var change in _changes)
            {
                if (!change.HasValue) continue;

                if (change.Value.name == INVALIDNAME)
                {
                    newData.Add(("", change.Value.Data));
                }
                else
                {
                    newData.Add((buffNames_Ids[change.Value.name], null));
                }
            }
            
            UpdateData(newData);
        }
    }
}
