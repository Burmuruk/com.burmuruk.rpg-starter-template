using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using Burmuruk.RPGStarterTemplate.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class EquipmentSettings : SubWindow, IUIListContainer<BaseCreationInfo>
    {
        const string INFO_EQUIPMENT_SETTINGS_NAME = "EquipmentSettings";
        Equipment _changes = null;
        ComponentsList<ListElementUI<ElementType>> _inventory;
        Queue<string> _itemsIds = new();
        bool _isLoading = false;
        EnumRegistry _enumRegistry;

        public Button BTNBackEquipmentSettings { get; private set; }
        public ComponentsList<EquipmentListElement> MClEquipmentElements { get; private set; }
        public EnumModifierUI<EquipmentType> EMBodyPart { get; private set; }
        public VisualElement InfoBodyPlacement { get; private set; }
        public ObjectField OFModel { get; private set; }
        public TreeView TVBodyParts { get; private set; }
        public EquipmentSpawnsList UIParts { get; private set; }

        public override void Initialize(VisualElement container)
        {
            _instance = UtilitiesUI.CreateDefaultTab(INFO_EQUIPMENT_SETTINGS_NAME);
            container.hierarchy.Add(_instance);
            base.Initialize(_instance);
            _enumRegistry = SavingSystem.LoadEnumRegistry();

            BTNBackEquipmentSettings = _container.Q<Button>();
            BTNBackEquipmentSettings.clicked += () => GoBack?.Invoke();

            EMBodyPart = new EnumModifierUI<EquipmentType>(_instance.Q<VisualElement>(EnumModifierUI<EquipmentType>.ContainerName));
            EMBodyPart.Name.text = "Body Part";

            InfoBodyPlacement = _instance.Q<VisualElement>("infoBodySplit");
            CreateSplitViewEquipment(InfoBodyPlacement);

            MClEquipmentElements = new(_instance.Q<VisualElement>(ComponentsList.CONTAINER_NAME));
            UIParts.OnChoicesChanged += _ => VerifyEquippedItems();
            Setup_ComponentsList();
            RegisterToChanges();
        }

        private bool VerifyEquippedItems()
        {
            if (_isLoading)
                return true;

            bool result = true;

            foreach (var element in MClEquipmentElements.EnabledComponents)
            {
                var field = element.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>();

                if (field == null)
                    continue;

                var item = ItemDataConverter.GetItem((ElementType)element.Type, element.Id) as EquipableItem;

                var requiredPlace = (EquipmentType)item.GetEquipLocation();

                if (requiredPlace == EquipmentType.None)
                {
                    requiredPlace = (EquipmentType)field.DynamicEnumField.SelectedId;
                }

                result &= Verify_Placement(element, requiredPlace);
            }

            return result;
        }

        private void Setup_ComponentsList()
        {
            MClEquipmentElements.OnElementCreated += Setup_ElementComponent;
            MClEquipmentElements.AddElementExtraData += Set_Id;
            MClEquipmentElements.OnElementAdded += Setup_EquipmentElementButton;
            MClEquipmentElements.OnElementRemoved += ClearElement;
        }

        private void ClearElement(EquipmentListElement creation)
        {
            creation.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>()?.DynamicEnumField.SetEnabled(true);
            creation.Toggle.value = false;

            Set_Tooltip(creation.element, _highlighted, highlight: false);
        }

        private void Set_Id(EquipmentListElement creation)
        {
            if (_itemsIds.Count <= 0)
                return;

            creation.Id = _itemsIds.Dequeue();
        }

        #region Changes traker
        private void RegisterToChanges()
        {
            ElementType[] elements = new ElementType[]
            {
                ElementType.Item,
                ElementType.Consumable,
                ElementType.Weapon,
                ElementType.Armour,
            };

            foreach (var element in elements)
                CreationScheduler.Add(ModificationTypes.Rename, element, this);

            foreach (var element in elements)
                CreationScheduler.Add(ModificationTypes.Remove, element, this);
        }

        public virtual void RenameCreation(in BaseCreationInfo newValue)
        {
            foreach (var component in MClEquipmentElements.Components)
            {
                if (component.Id == newValue.Id)
                {
                    component.NameButton.text = newValue.Name;
                    return;
                }
            }
        }

        public virtual void RemoveData(in BaseCreationInfo newValue)
        {
            for (int i = 0; i < MClEquipmentElements.Components.Count; i++)
            {
                if (MClEquipmentElements[i].Id == newValue.Id)
                {
                    MClEquipmentElements.RemoveComponent(i);
                    return;
                }
            }
        }
        #endregion

        private void Setup_ElementComponent(EquipmentListElement element)
        {
            //EnableContainer(MClEquipmentElements[componentIdx].IFAmount, false);
            var elementRef = element;
            EnableContainer(element.element.Q<Button>("btnPin"), false);
            EnableContainer(element.RemoveButton, false);
            element.Toggle.RegisterValueChangedCallback((evt) => OnValueChanged_TglEquipment(evt.newValue, elementRef));
            var equipmentField = element.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>();
            equipmentField.DynamicEnumField.SelectionChanged += id => OnValueChanged_EFEquipment(id, element);
            element.NameButton.SetEnabled(false);
            element.NameButton.style.marginRight = 15;
            equipmentField.EnumField.style.marginLeft = 15;
        }

        private void Setup_EquipmentElementButton(EquipmentListElement element)
        {
            var type = (ElementType)element.Type;
            var equipmentField = element.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>();
            var item = ItemDataConverter.GetItem(type, element.Id);
            bool isEquipable = item is EquipableItem;
            EnableContainer(element.Toggle, isEquipable);
            EnableContainer(equipmentField.EnumField, isEquipable); //displays the element

            if (item is EquipableItem equipable)
            {
                try
                {
                    var place = (EquipmentType)equipable.GetEquipLocation();
                    equipmentField.DynamicEnumField.SetEnabled(place == EquipmentType.None && element.Toggle.value); //disables functionallity

                    if (place != EquipmentType.None && equipmentField.DynamicEnumField.SelectedId != (int)place)
                        equipmentField.DynamicEnumField.SetValueWithoutNotify((int)place);
                    else
                        Verify_Placement(element, (EquipmentType)equipmentField.DynamicEnumField.SelectedId);
                }
                catch (NullReferenceException) { }
            }
        }

        private void OnValueChanged_EFEquipment(int newValue, EquipmentListElement element)
        {
            if (_isLoading) return;

            Verify_Placement(element, (EquipmentType)newValue);

            if (newValue == EnumRegistry.NoneId)
            {
                element.Toggle.SetValueWithoutNotify(false);
                element.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>()?.DynamicEnumField.SetEnabled(false);
            }
        }

        private void OnValueChanged_TglEquipment(bool newValue, EquipmentListElement element)
        {
            if (!SavingSystem.Data.TryGetCreation(element.Id, out var data, out var type))
            {
                Notify("The item was not found", BorderColour.Error);
                return;
            }

            var item = ItemDataConverter.GetItem(type, element.Id) as EquipableItem;
            var place = (EquipmentType)item.GetEquipLocation();

            Set_Tooltip(element.element, _highlighted, highlight: false);

            element.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>()?.
                DynamicEnumField.SetEnabled(newValue && (int)(EquipmentType)item.GetEquipLocation() == EnumRegistry.NoneId);

            Verify_Placement(element, place);
        }

        private bool Verify_Placement(EquipmentListElement element, EquipmentType place)
        {
            if (_isLoading) return true;

            Set_Tooltip(element.element, _highlighted, highlight: false);

            if (place == EquipmentType.None)
                return true;

            bool exists = UIParts.GetInfo().Any(point => point.transform != null && (int)point.type == (int)place);

            if (exists)
                return true;

            string placeName = _enumRegistry.GetName<EquipmentType>((int)place) ?? $"ID {(int)place}";

            Set_Tooltip(element.element, _highlighted, "There's no spawn point for: " + placeName);

            //Notify(element.element.tooltip, BorderColour.HighlightBorder);
            return false;
        }

        public void Load_EquipmentFromList(ComponentsList<ListElementUI<ElementType>> inventory)
        {
            MClEquipmentElements.Components.ForEach(c => EnableContainer(c.element, false));

            foreach (var component in inventory.Components)
            {
                if (IsDisabled(component.element))
                    continue;

                _itemsIds.Enqueue(component.Id);
                if (!MClEquipmentElements.AddElement(component.NameButton.text, component.Type))
                    _itemsIds.Dequeue();
            }
        }

        private void CreateSplitViewEquipment(VisualElement container)
        {
            TwoPaneSplitView splitView = new TwoPaneSplitView();
            splitView.orientation = TwoPaneSplitViewOrientation.Horizontal;
            splitView.fixedPaneInitialDimension = 215;
            splitView.AddToClassList("SplitViewStyle");

            var bodVis = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/BodyVisualizer.uxml");
            var leftSide = bodVis.Instantiate();
            var spawnFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/BodySpawnPoint.uxml");
            UIParts = new EquipmentSpawnsList(spawnFile.Instantiate());
            OFModel = leftSide.Q<ObjectField>();

            TVBodyParts = leftSide.Q<TreeView>();
            Setup_LeftSide(leftSide);
            Setup_TreeView();

            splitView.Insert(0, leftSide);
            splitView.Insert(1, UIParts.Container);
            container.Add(splitView);
        }

        private void Setup_LeftSide(VisualElement side)
        {
            OFModel.objectType = typeof(GameObject);
            OFModel.RegisterValueChangedCallback(evt => ShowBodyTree(evt.newValue));
            OFModel.SetEnabled(false);

            var scroll = side.Q<ScrollView>();
            scroll.RegisterCallback<WheelEvent>(evt =>
            {
                if (scroll.verticalScroller.enabledSelf)
                    evt.StopPropagation();
            });
        }

        public void Set_Model(GameObject model)
        {
            if (model == OFModel.value)
                return;

            OFModel.value = model;

            if (_changes != null)
                Update_SpawnPoints(_changes, model);
            else
                UIParts.TxtCount.value = 0;
        }

        //private void StopScroll(WheelEvent evt, ScrollView scroll)
        //{
        //    //bool scrollingDown = evt.delta.y > 0;
        //    //float scrollOffset = scroll.scrollOffset.y;
        //    //float contentHeight = scroll.contentContainer.layout.height;
        //    //float viewHeight = scroll.layout.height;

        //    //bool canScrollDown = scrollOffset + viewHeight < contentHeight;
        //    //bool canScrollUp = scrollOffset > 0;

        //    //if ((scrollingDown && canScrollDown) || (!scrollingDown && canScrollUp))
        //    //{
        //    //    // Si B aún puede desplazarse, evitamos que el scroll se propague al padre (A)
        //    //    evt.StopPropagation();
        //    //}
        //}

        private void Setup_TreeView()
        {
            TVBodyParts.SetEnabled(false);
            TVBodyParts.makeItem = () => new Label();

            TVBodyParts.bindItem = (element, i) =>
            {
                var data = TVBodyParts.GetItemDataForIndex<TransformNode>(i);
                var label = element as Label;
                label.text = data.name;
                label.userData = data;

                label.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                label.RegisterCallback<PointerDownEvent>(OnPointerDown);
            };
            TVBodyParts.fixedItemHeight = 16;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            var label = evt.target as Label;

            if (label == null || evt.button != 0)
                return;

            var data = label.userData as TransformNode;
            var go = data.transform?.gameObject;

            if (go == null || DragAndDrop.objectReferences.Length > 0)
                return;

            evt.StopPropagation();

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { go };
            DragAndDrop.StartDrag($"Dragging {go.name}");
        }

        private void ShowBodyTree(UnityEngine.Object evt)
        {
            GameObject selected = evt as GameObject;

            if (selected == null)
                return;

            int idCounter = 0;
            var rootNode = BuildTree(selected.transform, ref idCounter);
            var rootTreeItem = BuildTreeItem(rootNode);

            if (rootTreeItem.children.Count() <= 0)
            {
                TVBodyParts.Clear();
                TVBodyParts.SetEnabled(false);
                return;
            }

            var rootItems = new List<TreeViewItemData<TransformNode>> { rootTreeItem };
            TVBodyParts.SetRootItems(rootItems);
            TVBodyParts.Rebuild();
            TVBodyParts.SetEnabled(true);

            EditorApplication.delayCall += () => CollapseAll(rootItems);
        }

        void CollapseAll(IEnumerable<TreeViewItemData<TransformNode>> items)
        {
            foreach (var item in items)
            {
                TVBodyParts.CollapseItem(item.id);

                if (item.children != null && item.children.Count() > 0)
                {
                    CollapseAll(item.children);
                }
            }
        }

        void FlattenTree(TransformNode node, List<TransformNode> list)
        {
            list.Add(node);

            foreach (var child in node.children)
                FlattenTree(child, list);
        }

        TreeViewItemData<TransformNode> BuildTreeItem(TransformNode node)
        {
            var children = node.children.Select(child => BuildTreeItem(child)).ToList();
            return new TreeViewItemData<TransformNode>(node.id, node, children);
        }

        private TransformNode BuildTree(Transform transform, ref int idCounter)
        {
            var node = new TransformNode
            {
                id = idCounter++,
                name = transform.name,
                transform = transform,
            };

            for (int i = 0; i < transform.childCount; i++)
            {
                node.children.Add(BuildTree(transform.GetChild(i), ref idCounter));
            }

            return node;
        }

        private List<TreeViewItemData<string>> GetChilds(Transform transform, ref int idx)
        {
            var subItemData = new List<TreeViewItemData<string>>();

            for (; idx < transform.childCount; idx++)
            {
                if (transform.GetChild(idx).childCount > 0)
                {
                    int cur = idx++;
                    var childs = GetChilds(transform.GetChild(cur), ref idx);

                    subItemData.Add(new TreeViewItemData<string>(cur, transform.GetChild(cur).name, childs));
                }
                else
                    subItemData.Add(new TreeViewItemData<string>(idx, transform.GetChild(idx).name));
            }

            return subItemData;
        }

        public Equipment GetEquipment(in Inventory inventory)
        {
            var equipment = new Equipment()
            {
                spawnPoints = (from sp in UIParts.GetInfo()
                               let path = GetTransformPath(sp.transform)
                               where path != null
                               select (path, sp.type)).ToList(),
            };

            for (int i = 0; i < MClEquipmentElements.Components.Count; i++)
            {
                if (IsDisabled(MClEquipmentElements[i].element))
                    continue;

                EquipmentType place = EquipmentType.None;
                bool equipped = false;

                var equipmentField = MClEquipmentElements[i].GetComponent<ListElementTypedUI<ElementType, EquipmentType>>();

                if (equipmentField == null || IsDisabled(equipmentField.EnumField))
                    continue;

                place = (EquipmentType)equipmentField.DynamicEnumField.SelectedId;
                equipped = MClEquipmentElements[i].Toggle.value;

                equipment.equipment.TryAdd(MClEquipmentElements[i].Id, new EquipData()
                {
                    type = (ElementType)MClEquipmentElements[i].Type,
                    place = place,
                    equipped = place == EquipmentType.None ? false : equipped,
                });
            }

            return equipment;
        }

        private string GetTransformPath(Transform t)
        {
            var model = OFModel.value as GameObject;

            if (model == null || t == null)
                return null;

            var names = new List<string>();
            Transform current = t;

            while (current != null && current != model.transform)
            {
                names.Add(current.name);
                current = current.parent;
            }

            if (current == null)
                return null;

            names.Reverse();
            return string.Join("/", names);
        }

        public void LoadEquipment(in Equipment equipment, in GameObject model, ComponentsList<ListElementUI<ElementType>> inventory)
        {
            if (equipment == null)
                return;

            _isLoading = true;
            try
            {
                _changes = equipment;
                _inventory = inventory;
                if (model is GameObject go)
                    Update_SpawnPoints(equipment, go);

                Add_Equipment(equipment, inventory, true);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void UpdateUIData<T, U, R>(T equipment, U arg2, R inventory) where T : Equipment where R : ComponentsList<ListElementUI<ElementType>>
        {
            if (equipment == null)
                return;

            _isLoading = true;
            try
            {
                if (arg2 is GameObject go)
                    Update_SpawnPoints(equipment, go);

                Add_Equipment(equipment, inventory, false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void Update_SpawnPoints(Equipment equipment, GameObject model)
        {
            OFModel.value = model;

            if (equipment?.spawnPoints != null)
            {
                var transform = model.transform;
                UIParts.UpdateUIData(equipment.spawnPoints.Select(p => (FindChildByPath(transform, p.path), p.type)).ToList());
            }
        }

        private void Add_Equipment(Equipment equipment, ComponentsList<ListElementUI<ElementType>> inventory, bool save)
        {
            _itemsIds.Clear();
            MClEquipmentElements.RestartValues();

            if (inventory == null)
                return;

            equipment.equipment ??= new();
            Dictionary<string, EquipData> newItems = new();

            foreach (var item in inventory.EnabledComponents)
            {
                EquipData? equipFound = null;

                foreach (var equipable in equipment.equipment)
                {
                    if (item.Id == equipable.Key)
                    {
                        equipFound = equipable.Value;
                        break;
                    }
                }

                void EditData(EquipmentListElement e)
                {
                    if (equipFound.HasValue)
                    {
                        e.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>()
                            ?.DynamicEnumField.SetValueWithoutNotify((int)equipFound.Value.place);
                        e.Toggle.SetValueWithoutNotify(equipFound.Value.equipped);
                    }
                }

                MClEquipmentElements.AddElementExtraData += EditData;
                _itemsIds.Enqueue(item.Id);

                if (!MClEquipmentElements.AddElement(item.NameButton.text, item.Type))
                {
                    _itemsIds.Dequeue();
                }
                else if (save)
                {
                    var newItem = MClEquipmentElements.EnabledComponents.Last();
                    newItems.Add(item.Id, new EquipData()
                    {
                        equipped = newItem.Toggle.value,
                        place = (EquipmentType)newItem.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>().DynamicEnumField.SelectedId,
                        type = (ElementType)item.Type
                    });
                }

                MClEquipmentElements.AddElementExtraData -= EditData;
            }

            if (save)
                _changes.equipment = newItems;
        }

        Transform FindChildByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            return root.Find(path);
        }


        public override void Clear()
        {
            bool wasLoading = _isLoading;
            _isLoading = true;

            try
            {
                MClEquipmentElements.Clear();
                _itemsIds.Clear();

                foreach (var item in MClEquipmentElements.Components)
                {
                    item.Toggle.SetValueWithoutNotify(false);

                    item.GetComponent<ListElementTypedUI<ElementType, EquipmentType>>()?.DynamicEnumField.Clear();
                }

                OFModel.SetValueWithoutNotify(null);
                UIParts.Clear();
                EMBodyPart.Clear();
                TVBodyParts.Clear();

                _changes = null;
                _inventory = null;

                foreach (var item in _highlighted.ToList())
                    Set_Tooltip(item.Key, item.Value, false);

                _highlighted.Clear();
            }
            finally
            {
                _isLoading = wasLoading;
            }
        }

        public override void Remove_Changes()
        {
            _changes = null;
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            bool result = OFModel.value != null;

            _highlighted[OFModel] = OFModel.tooltip;
            Set_ErrorTooltip(OFModel, "There must to be a model to equip items on", ref errors, result);

            result &= UIParts.VerifyData(out var partsErrors);
            errors.AddRange(partsErrors);

            result &= VerifyEquippedItems();

            return result;
        }

        public override ModificationTypes Check_Changes()
        {
            CurModificationType = ModificationTypes.None;

            if (_changes == null)
                return ModificationTypes.None;

            if (OFModel.value as GameObject != SavingSystem.GetAsset<GameObject>(_changes.modelPath))
                return ModificationTypes.EditData;

            CurModificationType = UIParts.Check_Changes();

            if (_changes?.equipment == null ^ MClEquipmentElements?.Components == null)
            {
                CurModificationType = ModificationTypes.EditData;
            }
            else if (_changes?.equipment?.Count != MClEquipmentElements?.Components?.Count)
            {
                CurModificationType = ModificationTypes.EditData;
            }
            else
                foreach (var item in _changes.equipment)
                {
                    foreach (var element in MClEquipmentElements.Components)
                    {
                        if (element.Id == item.Key)
                        {
                            if ((ElementType)element.Type != item.Value.type || element.Toggle.value != item.Value.equipped)
                            {
                                CurModificationType = ModificationTypes.EditData;
                                break;
                            }
                        }
                        else
                        {
                            CurModificationType = ModificationTypes.EditData;
                            break;
                        }
                    }
                }

            return CurModificationType;
        }

        public override void Load_Changes()
        {
            var newData = _changes;
            LoadEquipment(newData, SavingSystem.GetAsset<GameObject>(newData.modelPath), _inventory);
        }
    }

    public class TransformNode
    {
        public int id;
        public string name;
        public List<TransformNode> children = new();
        public Transform transform;
    }

    public class EquipmentListElement : ListElementUI<ElementType>
    {
        public EquipmentListElement() : base(new ListElementTypedUI<ElementType, EquipmentType>(syncWithParent: false))
        {

        }
    }
}
