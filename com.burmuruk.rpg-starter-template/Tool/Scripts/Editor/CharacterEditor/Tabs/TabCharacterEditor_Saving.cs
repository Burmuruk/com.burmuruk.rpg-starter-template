using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Editor.Saving;
using Burmuruk.RPGStarterTemplate.Editor.Utilities;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public partial class TabCharacterEditor : BaseLevelEditor
    {
        private bool Save_Creation()
        {
            UtilitiesUI.DisableNotification(NotificationType.Creation);
            try
            {
                var type = currentSettingTag.type;
                return CreationControls[type].Save();
            }
            catch (InvalidDataExeption e)
            {
                throw e;
            }
        }

        private void Load_CreationData(ElementCreationPinnable element, ElementType type)
        {
            UtilitiesUI.DisableNotification(NotificationType.Creation);
            ChangeTab(type switch
            {
                ElementType.Character => INFO_CHARACTER_NAME,
                ElementType.Armour => INFO_ARMOUR_SETTINGS_NAME,
                ElementType.Buff => INFO_BUFF_SETTINGS_NAME,
                ElementType.Weapon => INFO_WEAPON_SETTINGS_NAME,
                ElementType.Consumable => INFO_CONSUMABLE_SETTINGS_NAME,
                _ => INFO_ITEM_SETTINGS_NAME,
            });

            CreationControls[type].Load(type, element.Id);
        }

        private void Save_CurrentState()
        {
            var data = new CreationTabUIData(curTab)
            {
                //scrollPosition = scrollView.scrollOffset
                scrollPos = infoRight.Q<ScrollView>("infoContainer").scrollOffset.y,
                elements = infoContainers.Select(x => new TabUIData()
                {
                    type = x.Value.type,
                    data = CreationControls[x.Value.type].GetInfo()
                }).ToList()
            };

            JObject json = new ();

            if (JsonWritter.ReadJson(out json))
            {
                json["unsavedChanges"] = data.GetJson();
            }
            else
            {
                json = new JObject
                {
                    ["unsavedChanges"] = data.GetJson(),
                };
            }

            JsonWritter.WriteJson(json);
        }

        private void Load_UnsavedChanges()
        {
            if (!JsonWritter.ReadJson(out var json)) return;

            CreationTabUIData data = new(null);
            data.RestoreFromJson((JObject)json["unsavedChanges"]);

            foreach (var tabData in data.elements)
            {
                CreationControls[tabData.type].UpdateInfo(tabData.data);

                //if (string.IsNullOrEmpty(data.Id))
                //    (CreationControls[tabData.type] as SubWindow)?.Remove_Changes();
            }

            btnsRight_Tag.ForEach(t => UtilitiesUI.Highlight(t.element, false));

            if (!string.IsNullOrEmpty(data.Id))
            {
                if (data.Id == INFO_GENERAL_SETTINGS_CHARACTER_NAME ||
                    !infoContainers.ContainsKey(data.Id))
                {
                    ChangeTab(INFO_GENERAL_SETTINGS_CHARACTER_NAME);
                    UtilitiesUI.EnableContainer(infoSetup, false);
                }
                else
                {
                    var tabType = infoContainers[data.Id].type;

                    ChangeTab(data.Id);
                    UtilitiesUI.EnableContainer(infoSetup, true);

                    foreach (var tag in btnsRight_Tag)
                    {
                        if (tag.type == tabType)
                        {
                            UtilitiesUI.Highlight(btnsRight_Tag[tag.idx].element, true);
                            currentSettingTag = (tag.type, tag.idx);
                            break;
                        }
                    }
                }
            }

            infoRight.schedule.Execute(() =>
            {
                infoRight.Q<ScrollView>("infoContainer").scrollOffset = new(0, data.scrollPos);

            }).ExecuteLater(100);
            searchBar.CurrentFilter = data.searchFilter;

            json["unsavedChanges"] = null;
            JsonWritter.WriteJson(json);
        }

        private void Load_CreatedAssets()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/RPG-Results");
            bool noAssets = true;

            //foreach (var asset in assets)
            //{
            //    switch (asset)
            //    {
            //        case Tesis.Combat.Weapon weapon:
            //            noAssets &= !SavingSystem.SaveCreation(ElementType.Weapon, null, new CreationData(weapon.Name, weapon), ModificationTypes.Add);
            //            break;

            //        case Tesis.Stats.ConsumableItem consumable:
            //            noAssets &= !SavingSystem.SaveCreation(ElementType.Consumable, null, new CreationData(consumable.Name, consumable), ModificationTypes.Add);
            //            break;

            //        case Tesis.Inventory.ArmourElement armour:
            //            noAssets &= !SavingSystem.SaveCreation(ElementType.Armour, null, new ItemCreationData(armour.Name, armour), ModificationTypes.Add);
            //            break;

            //        case Tesis.Inventory.InventoryItem item:
            //            noAssets &= !SavingSystem.SaveCreation(ElementType.Item, null, new ItemCreationData(item.Name, item), ModificationTypes.Add);
            //            break;

            //        case Tesis.Control.Character character:
            //            noAssets &= !SavingSystem.SaveCreation(ElementType.Character, null, new CharacterCreationData(asset.name, character), ModificationTypes.Add);
            //            break;

            //        default: break;
            //    }
            //}

            if (!noAssets)
            {
                UtilitiesUI.Notify("Assets loaded successfully", BorderColour.Success);
            }
            else
            {
                UtilitiesUI.Notify("No assets were found", BorderColour.Success);
            }
        }
    }
}
