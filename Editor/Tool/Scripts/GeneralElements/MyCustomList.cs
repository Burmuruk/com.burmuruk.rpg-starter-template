using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class MyCustomList : IClearable, IUpdatableUI
    {
        protected ModificationTypes _modificationType;
        protected VisualElement _elementsContainer;
        protected ScrollView _scrollView;

        public VisualElement Container { get; protected set; }
        public Foldout Foldout { get; protected set; }
        public UnsignedIntegerField TxtCount { get; protected set; }
        public Button BtnAdd { get; protected set; }
        public Button BtnRemove { get; protected set; }
        protected ModificationTypes CurModificationType
        {
            get => _modificationType;
            set
            {
                if (value == ModificationTypes.None)
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

        public MyCustomList(VisualElement container)
        {
            Container = container;

            Foldout = container.Q<Foldout>("mainFoldOut");
            BtnAdd = container.Q<Button>("btnAdd");
            BtnRemove = container.Q<Button>("btnRemove");
            TxtCount = container.Q<UnsignedIntegerField>("uiAmount");
            SetupFoldOut();
        }

        protected virtual void SetupFoldOut()
        {
            _elementsContainer = new VisualElement();

            _scrollView = new ScrollView();
            _scrollView.style.maxHeight = 180;
            _scrollView.style.flexGrow = 1;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _scrollView.Add(_elementsContainer);
            _scrollView.RegisterCallback<WheelEvent>(evt =>
            {
                if (_scrollView.verticalScroller.enabledSelf)
                    evt.StopPropagation();
            });
            Foldout.Add(_scrollView);

            var buttons = Container.Q<VisualElement>("buttonsContainer");
            buttons.parent.Remove(buttons);
            Foldout.Add(buttons);
        }

        public virtual void Clear()
        {
            _elementsContainer.Clear();
            TxtCount.value = 0;
            CurModificationType = ModificationTypes.None;
        }

        public virtual ModificationTypes Check_Changes() =>
            ModificationTypes.None;
    }
}
