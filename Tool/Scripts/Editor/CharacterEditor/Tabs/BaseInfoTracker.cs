using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public abstract class BaseInfoTracker : SubWindow
    {
        protected CreationsBaseInfo _nameControl;
        protected string _originalName = "";
        protected CreationsState _creationsState = CreationsState.Creating;

        public string TempName { get; protected set; } = "";
        public TextField TxtName => _nameControl.TxtName;
        public abstract string Id { get; }
        public CreationsState CreationsState => _creationsState;

        public virtual void Initialize(VisualElement container, CreationsBaseInfo name)
        {
            _nameControl = name;
            _container = container;
            _nameControl.TxtName.RegisterValueChangedCallback((evt) =>
            {
                if (IsActive)
                    TempName = evt.newValue;
            });
            _nameControl.CreationsStateChanged += Set_CreationState;
        }

        public override void Clear()
        {
            TempName = "";
            _originalName = "";
            _nameControl.UpdateName(TempName, _originalName);
            
            foreach (var element in _highlighted)
                Utilities.UtilitiesUI.Set_Tooltip(element.Key, element.Value, false);
        }

        public override void Enable(bool enabled)
        {
            base.Enable(enabled);

            if (enabled)
            {
                UpdateName();
            }
        }

        public virtual void UpdateName()
        {
            _nameControl.UpdateName(TempName, _originalName);
            _nameControl.SetState(_creationsState);
        }

        public virtual void Set_CreationState(CreationsState state)
        {
            if (!IsActive) return;

            if (state == CreationsState.Creating)
                Remove_Changes();

            _creationsState = state;
        }

        public void Force_CreationState(CreationsState state) =>
            _creationsState = state;
    }
}
