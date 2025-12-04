using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public abstract class SubWindow : UnityEditor.Editor, IClearable, IChangesObserver, IEnableable, IUpdatableUI
    {
        protected VisualElement _container;
        protected VisualElement _instance;
        protected ModificationTypes _modificationType;
        protected Dictionary<VisualElement, string> _highlighted = new();

        public bool IsActive { get; set; }
        public VisualElement Container { get => _container; }
        public VisualElement Instance { get => _instance; }

        protected ModificationTypes CurModificationType
        {
            get => _modificationType;
            set
            {
                if (value == ModificationTypes.None || value == ModificationTypes.Add)
                {
                    _modificationType = value;
                    return;
                }
                else if (value == ModificationTypes.Rename)
                {
                    _modificationType = ModificationTypes.Rename;
                    return;
                }
                else if ((_modificationType | ModificationTypes.Rename) != 0)
                {
                    _modificationType |= value;
                    return;
                }

                _modificationType = value;
            }
        }

        public Action GoBack;

        public virtual void Initialize(VisualElement container)
        {
            _container = container;
        }

        public abstract bool VerifyData(out List<string> errors);

        public abstract ModificationTypes Check_Changes();

        public abstract void Clear();

        public abstract void Load_Changes();

        public virtual void Enable(bool enabled) => IsActive = enabled;

        public virtual void Remove_Changes() { }
    }

    public interface IEnableable
    {
        public bool IsActive { get; set; }
        public void Enable(bool enabled) => IsActive = enabled;
    }
}
