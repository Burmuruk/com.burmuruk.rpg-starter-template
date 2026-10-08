using Burmuruk.RPGStarterTemplate.Saving;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Burmuruk.RPGStarterTemplate.Editor.Utilities.UtilitiesUI;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public partial class TabSystemEditor : BaseLevelEditor
    {
        private readonly List<SavingStageDraft> savingStages = new();
        private readonly List<string> savingBaseline = new();
        private readonly Dictionary<SavingStageDraft, VisualElement> savingRows = new();
        private readonly List<TemplateContainer> savingRowClones = new();
        private readonly List<SavingStageDraft> savingReplacementCandidates = new();
        private bool savingDraftLoaded;
        private bool savingAwaitingCompilation;
        private VisualElement savingRowsContainer;
        private VisualElement savingRemovalPanel;
        private VisualTreeAsset savingRowTemplate;
        private Label savingStatus;
        private Label savingRemovalPrompt;
        private DropdownField savingReplacement;
        private Button savingAddButton;
        private Button savingAcceptButton;
        private Button savingCancelButton;
        private Button savingConfirmRemovalButton;
        private Button savingCancelRemovalButton;
        private SavingStageDraft savingPendingRemoval;
        private SavingStageDraft savingDragged;
        private SavingStageDraft savingDropTarget;
        private bool savingDropAfter;

        public Toggle TglEncrypt { get; private set; }

        private static T RequireSavingElement<T>(VisualElement root, string elementName) where T : VisualElement
        {
            return root.Q<T>(elementName) ?? throw new InvalidOperationException(
                $"Saving UI: missing {typeof(T).Name} named '{elementName}' in UXML.");
        }

        private void InitializeSaving()
        {
            TglEncrypt = RequireSavingElement<Toggle>(container, "TglEncryptSaving");
            savingRowsContainer = RequireSavingElement<VisualElement>(container, "savingRowsContainer");
            savingAddButton = RequireSavingElement<Button>(container, "btnAddSavingStage");
            savingAcceptButton = RequireSavingElement<Button>(container, "btnAcceptSaving");
            savingCancelButton = RequireSavingElement<Button>(container, "btnCancelSaving");
            savingRemovalPanel = RequireSavingElement<VisualElement>(container, "savingRemovalPanel");
            savingRemovalPrompt = RequireSavingElement<Label>(savingRemovalPanel, "lblSavingRemovalPrompt");
            savingReplacement = RequireSavingElement<DropdownField>(savingRemovalPanel, "ddSavingReplacement");
            savingConfirmRemovalButton = RequireSavingElement<Button>(savingRemovalPanel, "btnConfirmSavingRemoval");
            savingCancelRemovalButton = RequireSavingElement<Button>(savingRemovalPanel, "btnCancelSavingRemoval");
            savingStatus = RequireSavingElement<Label>(container, "lblSavingStatus");

            TemplateContainer template = RequireSavingElement<TemplateContainer>(container, "tplSavingStage");
            savingRowTemplate = template.templateSource;
            //TglEncrypt.SetValueWithoutNotify(PlayerPrefs.GetInt(encrypt_pref_key, 0) != 0);
            TglEncrypt.RegisterValueChangedCallback(OnTglEcryptClicked);
            SubscribeTemplate();
            HideSavingRemoval();
        }

        private void SubscribeTemplate()
        {
            savingAddButton.clicked += AddSavingStage;
            savingAcceptButton.clicked += OnAccept_SavingBtn;
            savingCancelButton.clicked += OnCanceled_SavingBtn;
            savingConfirmRemovalButton.clicked += ConfirmSavingRemoval;
            savingCancelRemovalButton.clicked += HideSavingRemoval;
        }

        private void OnTglEcryptClicked(ChangeEvent<bool> evt)
        {
            //PlayerPrefs.SetInt(encrypt_pref_key, evt.newValue ? 1 : 0);
        }

        private void Show_Saving()
        {
            DisableNotification(NotificationType.System);
            ChangeTab(infoSavingName);
            SelectTabBtn(btnSavingName);

            if (!savingDraftLoaded)
                LoadSavingOptions();

            CreateSavingTextFields();
        }
        
        private void LoadSavingOptions()
        {
            savingStages.Clear();
            savingBaseline.Clear();

            foreach (SavingExecution stage in Enum.GetValues(typeof(SavingExecution)))
            {
                string name = stage.ToString();
                savingBaseline.Add(name);
                savingStages.Add(new SavingStageDraft { OriginalName = name, Name = name });
            }

            savingDraftLoaded = true;
        }

        private void CreateSavingTextFields()
        {
            HideSavingRemoval();
            savingDragged = null;
            savingDropTarget = null;

            foreach (TemplateContainer clone in savingRowClones)
                clone.RemoveFromHierarchy();

            savingRowClones.Clear();
            savingRows.Clear();

            foreach (SavingStageDraft stage in savingStages.Where(s => !s.Removed))
            {
                TemplateContainer clone = savingRowTemplate.Instantiate();
                VisualElement row = RequireSavingElement<VisualElement>(clone, "savingStageRow");
                Label handle = RequireSavingElement<Label>(row, "lblSavingStageDrag");
                TextField field = RequireSavingElement<TextField>(row, "txtSavingStageName");
                Button remove = RequireSavingElement<Button>(row, "btnRemoveSavingStage");
                field.SetValueWithoutNotify(stage.Name);
                field.isReadOnly = stage.IsSystem;
                field.RegisterValueChangedCallback(evt =>
                {
                    stage.Name = evt.newValue;
                    HideSavingRemoval();
                    RefreshSavingValidation();
                });
                remove.clicked += () => ShowSavingRemoval(stage);
                bool editable = !stage.IsSystem && !savingAwaitingCompilation;
                remove.SetEnabled(editable);
                handle.SetEnabled(editable);
                field.SetEnabled(editable);
                AttachSavingDrag(handle, stage);
                savingRows[stage] = row;
                savingRowClones.Add(clone);
                savingRowsContainer.Add(clone);
            }

            savingAddButton.SetEnabled(!savingAwaitingCompilation);
            RefreshSavingValidation();
        }

        private void AddSavingStage()
        {
            if (savingAwaitingCompilation)
                return;

            var reserved = new HashSet<string>(savingStages.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
            reserved.UnionWith(savingBaseline);
            reserved.UnionWith(SavingExecutionAliases.GetMappings().Keys);
            string name = "NewStage";
            int suffix = 2;

            while (reserved.Contains(name))
                name = "NewStage" + suffix++;

            var stage = new SavingStageDraft { Name = name };
            savingStages.Add(stage);
            CreateSavingTextFields();
            TextField field = RequireSavingElement<TextField>(savingRows[stage], "txtSavingStageName");
            field.schedule.Execute(() => { field.Focus(); field.SelectAll(); });
        }

        private void ShowSavingRemoval(SavingStageDraft stage)
        {
            if (stage.IsSystem || savingAwaitingCompilation)
                return;

            HideSavingRemoval();

            if (stage.OriginalName == null && !savingStages.Any(s => s.Replacement == stage))
            {
                savingStages.Remove(stage);
                CreateSavingTextFields();
                return;
            }

            savingPendingRemoval = stage;
            savingReplacementCandidates.AddRange(savingStages.Where(s => !s.Removed && s != stage));
            savingReplacement.choices = new List<string> { "No replacement (only if unused in code)" };
            savingReplacement.choices.AddRange(savingReplacementCandidates.Select(s => s.Name));
            savingReplacement.SetValueWithoutNotify(savingReplacement.choices[0]);
            savingRemovalPrompt.text = $"Satage: {stage.Name}\n\n Redirect code references to another stage:";
            EnableContainer(savingRemovalPanel, true);
        }

        private void ConfirmSavingRemoval()
        {
            if (savingPendingRemoval == null || savingAwaitingCompilation)
                return;

            int index = savingReplacement.index;

            if (index < 0 || index > savingReplacementCandidates.Count)
                return;

            savingPendingRemoval.Removed = true;
            savingPendingRemoval.Replacement = index > 0 ? savingReplacementCandidates[index - 1] : null;
            CreateSavingTextFields();
        }

        private void HideSavingRemoval()
        {
            savingPendingRemoval = null;
            savingReplacementCandidates.Clear();

            if (savingRemovalPanel != null)
                EnableContainer(savingRemovalPanel, false);
        }

        private void ClearSavingDropMarkers()
        {
            foreach (VisualElement row in savingRows.Values)
            {
                row.RemoveFromClassList("saving-drop-before");
                row.RemoveFromClassList("saving-drop-after");
            }
        }

        private void AttachSavingDrag(Label handle, SavingStageDraft stage)
        {
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || stage.IsSystem || savingAwaitingCompilation)
                    return;

                savingDragged = stage;
                savingDropTarget = null;
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (savingDragged != stage || !handle.HasPointerCapture(evt.pointerId))
                    return;

                ClearSavingDropMarkers();
                savingDropTarget = null;

                foreach (KeyValuePair<SavingStageDraft, VisualElement> pair in savingRows)
                {
                    if (pair.Key.IsSystem || pair.Key == stage)
                        continue;

                    Rect bounds = pair.Value.worldBound;
                    if (evt.position.y < bounds.yMin || evt.position.y > bounds.yMax)
                        continue;

                    savingDropTarget = pair.Key;
                    savingDropAfter = evt.position.y > bounds.center.y;
                    pair.Value.AddToClassList(savingDropAfter ? "saving-drop-after" : "saving-drop-before");
                    break;
                }
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (savingDragged != stage)
                    return;

                SavingStageDraft target = savingDropTarget;
                bool after = savingDropAfter;
                savingDragged = null;
                savingDropTarget = null;

                if (handle.HasPointerCapture(evt.pointerId))
                    handle.ReleasePointer(evt.pointerId);

                if (target != null)
                {
                    savingStages.Remove(stage);
                    savingStages.Insert(savingStages.IndexOf(target) + (after ? 1 : 0), stage);
                }

                CreateSavingTextFields();
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                savingDragged = null;
                savingDropTarget = null;
                ClearSavingDropMarkers();
            });
        }

        private bool SavingHasChanges()
        {
            var active = savingStages.Where(s => !s.Removed).ToList();

            return !active.Select(s => s.OriginalName).SequenceEqual(savingBaseline) ||
                active.Any(s => s.Name != s.OriginalName);
        }

        private void RefreshSavingHighlights(bool hasChanges)
        {
            var active = savingStages.Where(s => !s.Removed).ToList();

            for (int i = 0; i < active.Count; i++)
            {
                SavingStageDraft stage = active[i];

                if (!savingRows.TryGetValue(stage, out VisualElement row))
                    continue;

                bool added = stage.OriginalName == null;
                bool renamed = added || stage.Name != stage.OriginalName;
                bool moved = !added && savingBaseline.IndexOf(stage.OriginalName) != i;
                Highlight(RequireSavingElement<Label>(row, "lblSavingStageDrag"), moved);
            }
        }

        private void RefreshSavingValidation()
        {
            bool changed = SavingHasChanges();
            string error = null;

            try
            {
                SavingStageEditor.BuildChanges(savingBaseline, savingStages);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            changesInTab = changed;
            RefreshSavingHighlights(changed);
            EnableSavingButtons(changed);
            savingAcceptButton.SetEnabled(changed && error == null && !savingAwaitingCompilation);
            savingCancelButton.SetEnabled(changed && !savingAwaitingCompilation);
            savingStatus.text = savingAwaitingCompilation ? "Changes written. Waiting for Unity to compile." :
                error ?? (changed ? "Pending changes. Apply updates the enum and its script references." : "No pending changes.");
        }

        private void EnableSavingButtons(bool shouldEnable)
        {
            EnableContainer(savingAcceptButton, shouldEnable);
            EnableContainer(savingCancelButton, shouldEnable);

            Highlight(savingAcceptButton, shouldEnable, BorderColour.SpecialChange);
        }

        private void OnAccept_SavingBtn()
        {
            if (!SavingHasChanges() || savingAwaitingCompilation)
                return;

            try
            {
                int count = SavingStageEditor.Apply(savingBaseline, savingStages);
                savingAwaitingCompilation = count > 0;
                CreateSavingTextFields();
                Notify($"Updated {count} scripts.", BorderColour.Success, NotificationType.System);
            }
            catch (Exception ex)
            {
                Notify(ex.Message, BorderColour.Error, NotificationType.System);
            }
        }

        private void OnCanceled_SavingBtn()
        {
            if (savingAwaitingCompilation)
                return;

            LoadSavingOptions();
            CreateSavingTextFields();
            DisableNotification(NotificationType.System);
        }
    }
}
