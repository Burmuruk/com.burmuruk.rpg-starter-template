using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class ComponentsList
    {
        public const string CONTAINER_NAME = "infoComponents";
    }

    public class ComponentsList<T> : ComponentsList, IClearable where T : ElementCreationUI, new()
    {
        const string DEFAULT_ELEMENT_PATH = "Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/CharacterEditor/Elements/ElementComponent.uxml";
        List<int> _amounts;

        public Action<int> OnComponentClicked = delegate { };
        /// <summary>
        /// Called after the element is created. Is empty by default.
        /// </summary>
        public Action<T> OnElementCreated = delegate { };
        /// <summary>
        /// Called after the element is added. Is empty by default.
        /// </summary>
        public Action<T> OnElementAdded = delegate { };
        /// <summary>
        /// Called before the element is removed. Is empty by default.
        /// </summary>
        public Action<T> OnElementRemoved = delegate { };
        /// <summary>
        /// Called after the element is obteined but before it's added. Is empty by default.
        /// </summary>
        public Action<T> AddElementExtraData;
        /// <summary>
        /// By default, looks for the first disabled element.
        /// </summary>
        public Func<IList, string, int?> CreationValidator = null;
        /// <summary>
        /// By default, checks if the element is enabled.
        /// </summary>
        public Func<int, bool> DeletionValidator = null;

        public VisualElement Parent { get; private set; }
        public VisualElement Container { get; private set; }
        public List<T> Components { get; private set; }
        public List<int> Amounts { get => _amounts; }
        public int EnabledCount
        {
            get
            {
                int i = 0;
                foreach (var component in Components)
                {
                    if (IsDisabled(component.element))
                        return i;
                    
                    i++;
                }

                return i;
            }
        }
        public List<T> EnabledComponents
        {
            get => Components.Where(c => !IsDisabled(c.element)).ToList();
        }

        public T this[int index]
        {
            get => Components[index];
            set => Components[index] = value;
        }
        private string ElementPath { get; set; }

        public ComponentsList(VisualElement container)
        {
            Parent = container;
            Container = container.Q<VisualElement>("componentsConatiner");
            if (Container == null)
            {
                Container = container.Q<VisualElement>("elementsContainer");
            }

            _amounts = new();
            Components = new();
            ElementPath = DEFAULT_ELEMENT_PATH;
        }

        public ComponentsList(VisualElement container, string elementPath) : this(container)
        {
            ElementPath = elementPath;
        }

        public void IncrementElement(int idx, bool shouldIncrement = true, int value = 1)
        {
            _amounts[idx] += shouldIncrement ? value : -value;
            Components[idx].IFAmount?.SetValueWithoutNotify(_amounts[idx]);
        }

        public bool ChangeAmount(int idx, int amount)
        {
            if (amount < 0)
            {
                Components[idx].IFAmount?.SetValueWithoutNotify(_amounts[idx]);
                return false;
            }

            _amounts[idx] = amount;
            Components[idx].IFAmount?.SetValueWithoutNotify(amount);

            return true;
        }

        public void StartAmount(T element, int idx)
        {
            element.IFAmount?.SetValueWithoutNotify(1);
            _amounts[idx] = 1;
        }

        public void Disable_CharacterComponents() =>
            Components.ForEach(c => EnableContainer(c.element, false));

        public void RestartValues()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                Amounts[i] = 0;
                EnableContainer(Components[i].element, false);
            }
        }

        public bool Contains(string value)
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (!Components[i].element.ClassListContains("Disable") && Components[i].NameButton.text.Contains(value))
                    return true;
            }

            return false;
        }

        public bool AddElement(string name, string type)
        {
            if (!AddNewElement(name, type, out int? componentIdx))
                return false;

            OnElementAdded(Components[componentIdx.Value]);
            return true;
        }

        public bool AddElement(string name)
        {
            if (!AddNewElement(name, name, out int? componentIdx))
                return false;

            OnElementAdded(Components[componentIdx.Value]);
            return true;
        }

        private bool AddNewElement(string name, string type, out int? componentIdx)
        {
            componentIdx = null;
            if (name == "None") return false;

            if (CreationValidator == null)
            {
                componentIdx = DefaultCreationValidator();
            }
            else
            {
                componentIdx = CreationValidator(Components, name);
            }

            if (componentIdx == -1)
            {
                CreateNewComponent(name, type, out int newIdx);
                componentIdx = newIdx;
            }
            else if (!componentIdx.HasValue)
                return false;

            Components[componentIdx.Value].NameButton.text = name;
            Components[componentIdx.Value].SetType(type);
            EnableContainer(Components[componentIdx.Value].element, true);

            AddElementExtraData?.Invoke(Components[componentIdx.Value]);

            return true;
        }

        /// <summary>
        /// Looks for the first disabled element.
        /// </summary>
        /// <returns></returns>
        private int? DefaultCreationValidator()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i].element.ClassListContains("Disable"))
                {
                    return i;
                }
            }

            return -1;
        }

        protected virtual T CreateNewComponent(string value, string type, out int idx)
        {
            idx = Components.Count;

            VisualTreeAsset element = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ElementPath);
            var component = new T();
            component.Initialize(element.Instantiate(), idx);
            component.SetType(type);

            Components.Add(component);
            Container.Add(Components[idx].element);
            Amounts.Add(idx);

            if (component.NameButton != null)
            {
                component.OnNameClicked = _ => OnComponentClicked(component.idx);
                component.NameButton.RegisterCallback<ClickEvent>(component.OnNameClicked);
            }
            StartAmount(component, idx);
            OnElementCreated(component);

            return component;
        }

        public virtual void RemoveComponent(int idx)
        {
            if (DeletionValidator != null)
            {
                if (!DeletionValidator(idx)) return;
            }
            else if (IsDisabled(Components[idx].element))
                return;

            OnElementRemoved?.Invoke(Components[idx]);

            ChangeAmount(idx, 0);
            EnableContainer(Components[idx].element, false);
            DisplaceIds(idx);
        }

        private void DisplaceIds(int startIdx)
        {
            if (startIdx < 0 || startIdx >= Components.Count)
                return;

            // Guarda los elementos a mover
            var component = Components[startIdx];
            var amount = Amounts[startIdx];

            // Elimina de su posición actual
            Components.RemoveAt(startIdx);
            Amounts.RemoveAt(startIdx);

            // Añade al final
            Components.Add(component);
            Amounts.Add(amount);

            // Reasigna índices en orden
            for (int i = 0; i < Components.Count; i++)
                Components[i].idx = i;

            // Actualiza también el orden visual (si aplica)
            Container.Remove(component.element);
            Container.Add(component.element);
        }



        public virtual void Clear()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                EnableContainer(Components[i].element, false);
                ChangeAmount(i, 0);
            }
        }
    }
}
