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
        Equipment? _changes = null;
        ComponentsListUI<ElementCreation> _inventory;
        Queue<string> _itemsIds = new();
        bool _isLoading = false;

        public Button BTNBackEquipmentSettings { get; private set; }
        public ComponentsListUI<ElementCreation> MClEquipmentElements { get; private set; }
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

            BTNBackEquipmentSettings = _container.Q<Button>();
            BTNBackEquipmentSettings.clicked += () => GoBack?.Invoke();

            EMBodyPart = new EnumModifierUI<EquipmentType>(_instance.Q<VisualElement>(EnumModifierUI<EquipmentType>.ContainerName));
            EMBodyPart.Name.text = "Body Part";

            InfoBodyPlacement = _instance.Q<VisualElement>("infoBodySplit");
            CreateSplitViewEquipment(InfoBodyPlacement);

            MClEquipmentElements = new(_instance.Q<VisualElement>(ComponentsList.CONTAINER_NAME));
            UIParts.OnChoicesChanged += VerifyEquippedItems;
            Setup_ComponentsList();
            RegisterToChanges();
        }

        private void VerifyEquippedItems(List<string> list)
        {
            if (_isLoading) return;

            foreach (var item in MClEquipmentElements.Components)
            {
                if (IsDisabled(item.element)) continue;

                var equipable = ItemDataConverter.GetItem((ElementType)item.Type, item.Id) as EquipeableItem;

                try
                {
                    var requiredPlace = (EquipmentType)equipable.GetEquipLocation();

                    if (requiredPlace != EquipmentType.None && list.Contains(requiredPlace.ToString()))
                    {
                        Set_Tooltip(item.element, _highlighted, "There's no spawn point for: " + requiredPlace.ToString(), true);
                        Notify(item.element.tooltip, BorderColour.HighlightBorder);
                        continue;
                    }

                    Set_Tooltip(item.element, _highlighted, highlight: false);
                }
                catch (NullReferenceException)
                {
                }
            }
        }

        private void Setup_ComponentsList()
        {
            MClEquipmentElements.OnElementCreated += Setup_ElementComponent;
            MClEquipmentElements.AddElementExtraData += Set_Id;
            MClEquipmentElements.OnElementAdded += Setup_EquipmentElementButton;
            MClEquipmentElements.OnElementRemoved += ClearElement;
        }

        private void ClearElement(ElementCreation creation)
        {
            creation.EnumField.SetEnabled(true);
            creation.Toggle.value = false;

            Set_Tooltip(creation.element, _highlighted, highlight: false);
        }

        private void Set_Id(ElementCreation creation)
        {
            if (_itemsIds.Count <= 0) return;

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

        private void Setup_ElementComponent(ElementCreation element)
        {
            //EnableContainer(MClEquipmentElements[componentIdx].IFAmount, false);
            var elementRef = element;
            EnableContainer(element.element.Q<Button>("btnPin"), false);
            EnableContainer(element.RemoveButton, false);
            element.Toggle.RegisterValueChangedCallback((evt) => OnValueChanged_TglEquipment(evt.newValue, elementRef));
            element.EnumField.RegisterValueChangedCallback((evt) => OnValueChanged_EFEquipment(evt.newValue, elementRef));
            element.EnumField.Init(EquipmentType.None);
            element.NameButton.SetEnabled(false);
            element.NameButton.style.marginRight = 15;
            element.EnumField.style.marginLeft= 15;
        }

        private void Setup_EquipmentElementButton(ElementCreation element)
        {
            var type = (ElementType)element.Type;
            var item = ItemDataConverter.GetItem(type, element.Id);
            bool isEquipable = item is EquipeableItem;
            EnableContainer(element.Toggle, isEquipable);
            EnableContainer(element.EnumField, isEquipable); //displays the element

            if (item is EquipeableItem equipable)
            {
                try
                {
                    var place = (EquipmentType)equipable.GetEquipLocation();
                    element.EnumField.SetEnabled(place == EquipmentType.None && element.Toggle.value); //disables functionallity

                    if (place != EquipmentType.None && (EquipmentType)element.EnumField.value != place)
                        element.EnumField.SetValueWithoutNotify(place);
                    else
                        Verify_Placement(element, (EquipmentType)element.EnumField.value);
                }
                catch (NullReferenceException) { }
            }
        }

        private void OnValueChanged_EFEquipment(Enum newValue, ElementCreation element)
        {
            if (Verify_Placement(element, (EquipmentType)newValue) && 
                (EquipmentType)element.EnumField.value == EquipmentType.None)
            {
                element.Toggle.SetValueWithoutNotify(false);
                element.EnumField.SetEnabled(false);
            }
        }

        private void OnValueChanged_TglEquipment(bool newValue, ElementCreation element)
        {
            if (!SavingSystem.Data.TryGetCreation(element.Id, out var data, out var type))
            {
                Notify("The item was not found", BorderColour.Error);
                return;
            }

            var item = ItemDataConverter.GetItem(type, element.Id) as EquipeableItem;
            var place = (EquipmentType)item.GetEquipLocation();

            Set_Tooltip(element.element, _highlighted, highlight: false);

            element.EnumField.SetEnabled(newValue && Verify_Placement(element, place));
        }

        private bool Verify_Placement(ElementCreation element, EquipmentType place)
        {
            Set_Tooltip(element.element, _highlighted, highlight: false);

            if (place == EquipmentType.None)
            {
                return true;
            }

            var points = UIParts.GetInfo();

            foreach (var point in points)
            {
                if (point.type == place)
                {
                    return true;
                }
            }

            Set_Tooltip(element.element, _highlighted, "There's no spawn point for: " + place.ToString());
            if (!_isLoading)
                Notify(element.element.tooltip, BorderColour.HighlightBorder);

            return false;
        }

        public void Load_EquipmentFromList(ComponentsListUI<ElementCreation> inventory)
        {
            MClEquipmentElements.Components.ForEach(c => EnableContainer(c.element, false));

            foreach (var component in inventory.Components)
            {
                if (IsDisabled(component.element)) continue;

                _itemsIds.Enqueue(component.Id);
                if (!MClEquipmentElements.AddElement(component.NameButton.text, component.Type.ToString()))
                    _itemsIds.Dequeue();
            }
        }

        private void CreateSplitViewEquipment(VisualElement container)
        {
            TwoPaneSplitView splitView = new TwoPaneSplitView();
            splitView.orientation = TwoPaneSplitViewOrientation.Horizontal;
            splitView.fixedPaneInitialDimension = 215;
            splitView.AddToClassList("SplitViewStyle");

            var bodVis = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/BodyVisualizer.uxml");
            var leftSide = bodVis.Instantiate();
            var spawnFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/BodySpawnPoint.uxml");
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
            if (model == OFModel.value) return;

            OFModel.value = model;
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
            if (label == null || evt.button != 0) return;

            var data = label.userData as TransformNode;
            var go = data.transform?.gameObject;

            if (go == null || DragAndDrop.objectReferences.Length > 0) return;
            
            evt.StopPropagation();
                    
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { go };
            DragAndDrop.StartDrag($"Dragging {go.name}");
        }

        private void ShowBodyTree(UnityEngine.Object evt)
        {
            GameObject selected = evt as GameObject;
            if (selected == null) return;

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
                if (IsDisabled(MClEquipmentElements[i].element)) continue;

                EquipmentType place = EquipmentType.None;
                bool equipped = false;

                if (IsDisabled(MClEquipmentElements[i].EnumField))
                    continue;

                place = Enum.Parse<EquipmentType>(MClEquipmentElements[i].EnumField.value.ToString());
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

            if (!OFModel.value) return null;

            if (t.gameObject == model)
                return "";

            int i = 0;
            string path = t.name;
            var cur = t.parent;

            while (cur.gameObject != null && cur.gameObject != model && i < 10)
            {
                path = cur.name + "/" + path;
                cur = cur.parent;
                ++i;
            }

            //if (cur.gameObject != model)
            //    path = null;

            return path;
        }

        public void LoadEquipment(in Equipment equipment, in GameObject model, ComponentsListUI<ElementCreation> inventory)
        {
            if (equipment == null) return;
            
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

        public void UpdateUIData<T, U, R>(T equipment, U arg2, R inventory) where T : Equipment where R : ComponentsListUI<ElementCreation>
        {
            if (equipment == null) return;

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

            if (equipment.spawnPoints != null)
            {
                var transform = model.transform;
                UIParts.UpdateUIData(equipment.spawnPoints.Select(p => (FindChildByPath(transform, p.path), p.type)).ToList());
            }
        }

        private void Add_Equipment(Equipment equipment, ComponentsListUI<ElementCreation> inventory, bool save)
        {
            _itemsIds.Clear();
            MClEquipmentElements.RestartValues();
            if (inventory == null) return;
            equipment.equipment ??= new();

            Dictionary<string, EquipData> newItems = new();

            foreach (var item in inventory.Components)
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

                Action<ElementCreation> EditData = (e) =>
                {
                    if (equipFound.HasValue)
                    {
                        e.EnumField.SetValueWithoutNotify(equipFound.Value.place);
                        e.Toggle.SetValueWithoutNotify(equipFound.Value.equipped);
                    }
                };

                MClEquipmentElements.AddElementExtraData += EditData;

                _itemsIds.Enqueue(item.Id);
                if (!MClEquipmentElements.AddElement(item.NameButton.text, item.Type.ToString()))
                    _itemsIds.Dequeue();
                else if (save)
                {
                    var newItem = MClEquipmentElements.EnabledComponents.Last();
                    newItems.Add(item.Id, new EquipData()
                    {
                        equipped = newItem.Toggle.value,
                        place = (EquipmentType)newItem.EnumField.value,
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
            OFModel.value = null;
            UIParts.Clear();
            EMBodyPart.Clear();
            TVBodyParts.Clear();

            foreach (var item in MClEquipmentElements.Components)
            {
                if (!IsDisabled(item.Toggle))
                {
                    item.Toggle.value = false;
                    item.EnumField.value = EquipmentType.None;
                }
            }

            _changes = null;
            _inventory = null;
            foreach (var item in _highlighted)
                Set_Tooltip(item.Key, item.Value, false);
            _highlighted.Clear();
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

            return result;
        }

        public override ModificationTypes Check_Changes()
        {
            CurModificationType = ModificationTypes.None;

            if (_changes == null) return ModificationTypes.None;

            if (OFModel.value as GameObject != SavingSystem.GetAsset<GameObject>(_changes.modelPath)) 
                return ModificationTypes.EditData;

            CurModificationType = UIParts.Check_Changes();

            if (_changes?.equipment == null ^ MClEquipmentElements?.Components == null)
                CurModificationType = ModificationTypes.EditData;
            else if (_changes?.equipment?.Count != MClEquipmentElements?.Components?.Count)
                CurModificationType = ModificationTypes.EditData;
            else
                foreach (var item in _changes.equipment)
                {
                    foreach (var element in MClEquipmentElements.Components)
                    {
                        if (element.Id == item.Key)
                        {
                            if ((ElementType)element.Type != item.Value.type ||
                                element.Toggle.value != item.Value.equipped)
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
}
