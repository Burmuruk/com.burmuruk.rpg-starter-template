using Burmuruk.RPGStarterTemplate.Editor.Controls;
using Burmuruk.RPGStarterTemplate.Inventory;
using Burmuruk.RPGStarterTemplate.Stats;
using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Editor
{
    public class ItemDataConverter
    {
        public static InventoryItem GetItem(ElementType type, string id)
        {
            var creation = SavingSystem.Data.creations[type][id];

            switch (type)
            {
                case ElementType.Item:
                case ElementType.Armour:
                    var item = (creation as ItemCreationData).Data;
                    return item;

                case ElementType.Weapon:
                case ElementType.Consumable:
                    var buffUserData = creation as BuffUserCreationData;
                    var (buffUser, cArgs) = (buffUserData.Data, buffUserData.Names);
                    Update_BuffsInfo(buffUser as IBuffUser, cArgs);

                    return buffUser;

                default:
                    return null;
            }
        }

        public static void Update_BuffsInfo(IBuffUser buffUser, BuffsNamesDataArgs args)
        {
            List<BuffData> newBuffs = new();
            int idx = 0;

            if (args != null && args.BuffsNames != null)
            {
                foreach (var name in args.BuffsNames)
                {
                    if (name == "")
                    {
                        BuffData newBuff = buffUser.Buffs[idx];
                        newBuff.name = "Custom";
                        newBuffs.Add(newBuff);
                        ++idx;
                    }
                    else
                    {
                        var buffCreation = SavingSystem.Data.creations[ElementType.Buff][name] as BuffCreationData;
                        newBuffs.Add(buffCreation.Data);
                    }
                }
            }

            buffUser.UpdateBuffData(newBuffs.ToArray());
        }
    }
}
