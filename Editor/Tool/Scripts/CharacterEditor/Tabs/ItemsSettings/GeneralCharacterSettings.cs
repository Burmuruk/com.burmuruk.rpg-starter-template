using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor.Controls
{
    public class GeneralCharacterSettings : SubWindow, ISaveable
    {
        public event Action OnCustomColoursEnabled;
        public event Action OnTypeColoursEnabled;
        private const string DEFAULT_CREATION_PATH = "Assets/RPGResults";
        Dictionary<ElementType, ISaveable> _creationControls;
        CreationSaver _creationSaver;
        Regex rgxFileName = new Regex(@"(?m)(?x)^[A-Za-z0-9_]+(?:[\\/][A-Za-z0-9_ ]+)*$", RegexOptions.Compiled);

        public TextField TxtLocation { get; private set; }
        public Button BtnRest { get; private set; }
        public Button BtnGenerate { get; private set; }
        public Toggle TglTypeColour { get; private set; }
        public Toggle TglCustomColour { get; private set; }
        public string CreationPath
        {
            get
            {
                string path = PlayerPrefs.GetString("CreationPath");

                return string.IsNullOrEmpty(path) ? DEFAULT_CREATION_PATH : path;
            }
            set
            {
                if (!value.Contains("Assets/") && !value.Contains("Assets\\"))
                    value = "Assets/" + value;

                string[] splited = value.Split('/', '\\');
                var trimmed = splited.Select(t => t.Trim()).ToArray();
                string newPath = Path.Combine(trimmed);
                PlayerPrefs.SetString("CreationPath", newPath);
            }
        }

        public void Initialize(VisualElement container, Dictionary<ElementType, ISaveable> CreationControls)
        {
            _creationControls = CreationControls;
            _container = container;
            TxtLocation = container.Q<TextField>("txtLocation");
            BtnRest = container.Q<Button>("btnReset");
            BtnGenerate = container.Q<Button>("btnGenerateElements");
            TglTypeColour = container.Q<Toggle>("tglShowTypeColour");
            TglCustomColour = container.Q<Toggle>("ShowCustomColours");

            TxtLocation.RegisterValueChangedCallback(OnTxtLocation_Changed);
            TxtLocation.tooltip = "Root is always Assets/ event if it's not specified.";
            TxtLocation.SetValueWithoutNotify(PlayerPrefs.GetString("CreationPath", DEFAULT_CREATION_PATH));
            BtnRest.clicked += ResetPath;
            BtnGenerate.clicked += ApplyChanges;
            _creationSaver = new CreationSaver();
        }

        private void ApplyChanges()
        {
            if (!CanCreate())
                return;

            var registry = SavingSystem.LoadEnumRegistry();

            if (!registry.HasChanges)
            {
                CreatePrefabs();
                return;
            }

            //Save();
            registry.waitingForCompilation = true;
            SavingSystem.SaveEnumRegistry(registry);
            registry.ApplyEnums();
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
        }

        private void ResetPath()
        {
            TxtLocation.SetValueWithoutNotify(DEFAULT_CREATION_PATH);
            VerifyPath(TxtLocation.text);
        }

        private void OnTxtLocation_Changed(ChangeEvent<string> evt)
        {
            VerifyPath(evt.newValue);
        }

        private bool VerifyPath(string path)
        {
            if (path != DEFAULT_CREATION_PATH && !rgxFileName.IsMatch(path))
            {
                Notify("Not valid directory", BorderColour.Error);
                Set_Tooltip(TxtLocation, "Value can't be null nor contains special characters.", true, BorderColour.Error);
                return false;
            }

            Set_Tooltip(TxtLocation, "Root is always Assets/ event if it's not specified.", false, BorderColour.Error);
            DisableNotification(NotificationType.Creation);

            CreationPath = path;
            return true;
        }

        public override ModificationTypes Check_Changes()
        {
            return ModificationTypes.None;
        }

        public override void Clear() { }

        public override void Load_Changes() { }

        private void CreatePrefabs()
        {
            if (!CanCreate())
                return;

            bool elementCreated = CreationManager.CreateEverything();

            if (elementCreated)
            {
                Notify("Elements created", BorderColour.Success);
            }
            else
            {
                Notify("There were no elements to create", BorderColour.HighlightBorder);
            }
        }

        private bool CanCreate()
        {
            if (string.IsNullOrEmpty(TxtLocation.value))
            {
                Highlight(TxtLocation, true, BorderColour.Error);
                Notify("Must enter a location first", BorderColour.Error);
                return false;
            }
            else if (!VerifyPath(TxtLocation.value))
                return false;

            Highlight(TxtLocation, false);

            if (SavingSystem.Data.creations.Count <= 0 ||
                (SavingSystem.Data.creations.Count == 1 && SavingSystem.Data.creations.ContainsKey(ElementType.Buff)))
            {
                Notify("There's no elelemts to create", BorderColour.Error);
                return false;
            }

            return true;
        }

        private ElementType[] GetTypesInOrder()
        {
            LinkedList<ElementType> types = new();
            bool hasCharacter = false;

            foreach (var type in SavingSystem.Data.creations.Keys)
            {
                if (type == ElementType.Item)
                {
                    types.AddFirst(type);
                }
                else if (type == ElementType.Character)
                    hasCharacter = true;
                else
                    types.AddLast(type);
            }

            if (hasCharacter)
                types.AddLast(ElementType.Character);

            return types.ToArray();
        }

        public override void Enable(bool enabled)
        {
            base.Enable(enabled);
            if (enabled)
            {
                DisableNotification(NotificationType.Creation);
                Highlight(TxtLocation, false);
            }
        }

        public override bool VerifyData(out List<string> errors)
        {
            errors = new();
            return true;
        }

        public bool Save()
        {
            return true;
        }

        public CreationData Load(ElementType type, string id)
        {
            return default;
        }

        public CreationData Load(string id)
        {
            return default;
        }

        public CreationData GetInfo()
        {
            return null;
        }

        public void UpdateInfo(CreationData cd) { }

        public static T CloneFakeScriptable<T>(T source) where T : ScriptableObject
        {
            if (ReferenceEquals(source, null))
                return null;

            // Clonamos los campos del objeto (aunque Unity lo trate como null)
            T clone = (T)ScriptableObject.CreateInstance(source.GetType().FullName);
            Type curType = source.GetType();

            while (curType != typeof(ScriptableObject))
            {
                var fields = curType.GetFields(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                );

                foreach (var f in fields) 
                {
                    f.SetValue(clone, f.GetValue(source));
                }
                curType = curType.BaseType;
            }

            return clone;
        }

    }
}

