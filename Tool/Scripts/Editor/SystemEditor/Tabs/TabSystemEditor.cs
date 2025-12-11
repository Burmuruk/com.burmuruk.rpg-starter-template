using Burmuruk.RPGStarterTemplate.Editor.Controls;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public partial class TabSystemEditor : BaseLevelEditor
    {
        const string btnNavName = "btnNavigation";
        const string btnInteractionName = "btnInteractions";
        const string btnMissionName = "btnMissions";
        const string btnSavingName = "btnSaving";

        const string infoNavName = "navContainer";
        const string infoInteractionName = "interactionContainer";
        const string infoMissionsName = "missionsContainer";
        const string infoSavingName = "savingContainer";

        const string defaultSaveFile = "miGuardado-";
        NavGenerator navGenerator;

        class NavvGeneratorVisualizer : ScriptableObject
        {
            [SerializeField] public Controls.NavGenerator navGenerator;
        }

        [MenuItem("RPGTemplate/System")]
        public static void ShowWindow()
        {
            TabSystemEditor window = GetWindow<TabSystemEditor>();
            window.titleContent = new GUIContent("System settings");
            window.minSize = new Vector2(400, 300);
        }

        public void CreateGUI()
        {
            container = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/com.burmuruk.rpg-starter-template/Tool/UIToolkit/SystemEditor/SystemTab.uxml");
            container.Add(visualTree.Instantiate());
            BaseStyleSheets.ForEach(styleSheet => { container.styleSheets.Add(styleSheet); });

            GetTabButtons();
            GetInfoContainers();
            GetNotificationSection();
            (notifications ??= new()).TryAdd(
                NotificationType.System,
                new NotificationData(ntf, ntfLbl));
            InitializeSaving();

            EditorSceneManager.sceneOpened += OnSceneChanged;
            Show_NavMesh();
        }

        private void OnDestroy()
        {
            EditorSceneManager.sceneOpened -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene scene, OpenSceneMode mode)
        {
            if (infoContainers[infoNavName].element.ClassListContains("Disable"))
                return;

            navGenerator.Clear();
            navGenerator.LoadInfo();
        }

        protected override void GetInfoContainers()
        {
            infoContainers.Add(infoNavName, (container.Q<VisualElement>(infoNavName), default));
            //infoContainers.Add(infoInteractionName, Parent.Q<VisualElement>(infoInteractionName));
            //infoContainers.Add(infoMissionsName, Parent.Q<VisualElement>(infoMissionsName));
            infoContainers.Add(infoSavingName, (container.Q<VisualElement>(infoSavingName), default));

            foreach (var container in infoContainers.Values)
            {
                container.element.AddToClassList("Disable");
            }
        }

        protected override void GetTabButtons()
        {
            tabButtons.Add(btnNavName, container.Q<Button>(btnNavName));
            tabButtons[btnNavName].clicked += Show_NavMesh;

            tabButtons.Add(btnInteractionName, container.Q<Button>(btnInteractionName));
            tabButtons[btnInteractionName].clicked += Show_Interactions;

            tabButtons.Add(btnMissionName, container.Q<Button>(btnMissionName));
            tabButtons[btnMissionName].clicked += Show_Missions;

            tabButtons.Add(btnSavingName, container.Q<Button>(btnSavingName));
            tabButtons[btnSavingName].clicked += Show_Saving;
        }

        private void Show_Missions()
        {
            //if (changesInTab) ;
            //Display warning

            DisableNotification(NotificationType.System);
            ChangeTab(btnMissionName);
        }

        private void Show_Interactions()
        {
            //if (changesInTab) ;
            //Display warning

            DisableNotification(NotificationType.System);
            ChangeTab(btnInteractionName);
        }

        #region Navigation
        private void Show_NavMesh()
        {
            ChangeTab(infoContainers[infoNavName].element);
            SelectTabBtn(btnNavName);
            VisualElement navInfo = container.Q<VisualElement>("navInfoContainer");

            if (navInfo.childCount == 0)
            {
                navGenerator = new NavGenerator();
                navGenerator.Initialize(navInfo, container.Q<VisualElement>("navContainer"));
                //var nodesInsta = ScriptableObject.CreateInstance<Controls.NavGenerator>();
                //var nodesEditor = NodeListEditor.CreateEditor(nodesInsta, typeof(NodesList));

                //navInfo.Add(new InspectorElement(navGenerator));
            }

            if (navGenerator.LoadInfo())
                Notify("Navigation data found.", BorderColour.Success, NotificationType.System);
            else
                Notify("The Navigation data wasn't found.", BorderColour.Error, NotificationType.System);
        }
        #endregion
    }

    public enum NotificationType
    {
        None,
        System,
        Creation
    }
}
