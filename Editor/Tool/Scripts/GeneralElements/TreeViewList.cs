using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class TreeViewList<T> : MyCustomList, IDataProvider, IChangesObserver where T : IClearable, IDataProvider, IChangesObserver, IVElement, IUpdatableUI, new()
    {
        protected LinkedList<T> _enabledElements = new();
        protected LinkedList<T> _disabledElements = new();
        private TreeViewListData _changes = null;

        public Action<T> OnElementCreated;
        public Action<T> OnElementAdded;
        public Action<T> OnElementRemoved;

        public TreeViewList(VisualElement container) : base(container)
        {
            BtnAdd.clicked += Add;
            BtnRemove.clicked += Remove;

            TxtCount.RegisterCallback<KeyUpEvent>(ChangeAmount);
        }

        protected void ChangeAmount(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;

            int amount = ((int)TxtCount.value) - _enabledElements.Count;

            if (amount == 0)
            {
                Clear();
            }
            else if (amount > 0)
            {
                while (amount > 0)
                {
                    Add();
                    --amount;
                }
            }
            else
            {
                while (amount < 0)
                {
                    Remove();
                    ++amount;
                }
            }
        }

        protected override void SetupFoldOut()
        {
            base.SetupFoldOut();
            Foldout.text = "Elements";
        }

        public virtual void Add()
        {
            T data;

            if (_disabledElements.Count > 0)
            {
                data = _disabledElements.First.Value;
                _disabledElements.RemoveFirst();
            }
            else
            {
                data = new T();
                OnElementCreated?.Invoke(data);
            }

            _enabledElements.AddLast(data);
            _elementsContainer.Add(data.Container);
            TxtCount.value = (uint)_enabledElements.Count;
            OnElementAdded?.Invoke(data);
            //if (data is DropElementData d)
            // {
            //     d.Drop = new obj
            // }
        }

        public virtual void Remove()
        {
            if (_enabledElements.Count <= 0) return;

            _disabledElements.AddLast(_enabledElements.Last.Value);
            _elementsContainer.Remove(_enabledElements.Last.Value.Container);
            _enabledElements.RemoveLast();

            OnElementRemoved?.Invoke(_disabledElements.Last.Value);
            _disabledElements.Last.Value.Clear();
            TxtCount.value = (uint)_enabledElements.Count;
            _disabledElements.Last.Value.Clear();
        }

        public virtual void Remove(LinkedListNode<T> node)
        {
            _enabledElements.Remove(node);
            _disabledElements.AddLast(node);
            _elementsContainer.Remove(node.Value.Container);

            node.Value.Clear();
            TxtCount.value = (uint)_enabledElements.Count;
        }

        public override void Clear()
        {
            _changes = null;
            _modificationType = ModificationTypes.None;
            DisableAllElements();
        }

        protected void DisableAllElements()
        {
            while (_enabledElements.Count > 0)
            {
                Remove(_enabledElements.First);
            }
        }

        #region Changes
        public virtual void Load_Changes() => UpdateInfo(_changes);

        public virtual void Remove_Changes() => _changes = null;

        public override ModificationTypes Check_Changes()
        {
            if (_changes == null)
                return ModificationTypes.None;

            var result = ModificationTypes.None;

            foreach (var element in _enabledElements)
            {
                if (element.Check_Changes() != ModificationTypes.None)
                    result = ModificationTypes.EditData;
            }

            return result;
        }

        public virtual bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = true;
            var current = _enabledElements.First;

            while (current != null)
            {
                if (!current.Value.VerifyData(out _))
                {
                    var next = current.Next;
                    Remove(current);
                    current = next;
                    continue;
                }

                current = current.Next;
            }

            return result;
        }

        public virtual CreationData GetInfo()
        {
            TreeViewListData data = new();

            VerifyData(out _);

            foreach (var element in _enabledElements)
            {
                data.Elements.Add(element.GetInfo());
            }

            return data;
        }

        public virtual void UpdateInfo(CreationData cd)
        {
            var data = cd as TreeViewListData;

            if (data == null) return;

            Clear();
            _changes = data;

            foreach (var element in data.Elements)
            {
                Add();
                _enabledElements.Last.Value.UpdateInfo(element);
            }
        }

        public void UpdateUIData<U>(U cd) where U : CreationData
        {
            var data = cd as TreeViewListData;

            if (data == null) return;

            DisableAllElements();

            foreach (var element in data.Elements)
            {
                Add();
                _enabledElements.Last.Value.UpdateUIData(element);
            }
        }
        #endregion
    }

    public interface IVElement
    {
        public VisualElement Container { get; }
    }

    public class TreeViewListData : CreationData
    {
        public List<CreationData> Elements;
        public TreeViewListData() : base(null)
        {
            Elements = new List<CreationData>();
        }
    }
}
