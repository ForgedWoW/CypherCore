// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.I;
using Framework.Constants;

namespace Forged.MapServer.Globals;

internal class ItemSpecStats
{
    public uint ItemSpecStatCount;
    public ItemSpecStat[] ItemSpecStatTypes = new ItemSpecStat[ItemConst.MaxStats];
    public uint ItemType;
    public ItemSpecStats(ItemRecord item, ItemSparseRecord sparse)
    {
        if (item.ClassID == ItemClass.Weapon)
        {
            ItemType = 5;

            switch ((ItemSubClassWeapon)item.SubclassID)
            {
                case ItemSubClassWeapon.Axe:
                    AddStat(ItemSpecStat.OneHandedAxe);

                    break;
                case ItemSubClassWeapon.Axe2:
                    AddStat(ItemSpecStat.TwoHandedAxe);

                    break;
                case ItemSubClassWeapon.Bow:
                    AddStat(ItemSpecStat.Bow);

                    break;
                case ItemSubClassWeapon.Gun:
                    AddStat(ItemSpecStat.Gun);

                    break;
                case ItemSubClassWeapon.Mace:
                    AddStat(ItemSpecStat.OneHandedMace);

                    break;
                case ItemSubClassWeapon.Mace2:
                    AddStat(ItemSpecStat.TwoHandedMace);

                    break;
                case ItemSubClassWeapon.Polearm:
                    AddStat(ItemSpecStat.Polearm);

                    break;
                case ItemSubClassWeapon.Sword:
                    AddStat(ItemSpecStat.OneHandedSword);

                    break;
                case ItemSubClassWeapon.Sword2:
                    AddStat(ItemSpecStat.TwoHandedSword);

                    break;
                case ItemSubClassWeapon.Warglaives:
                    AddStat(ItemSpecStat.Warglaives);

                    break;
                case ItemSubClassWeapon.Staff:
                    AddStat(ItemSpecStat.Staff);

                    break;
                case ItemSubClassWeapon.Fist:
                    AddStat(ItemSpecStat.FistWeapon);

                    break;
                case ItemSubClassWeapon.Dagger:
                    AddStat(ItemSpecStat.Dagger);

                    break;
                case ItemSubClassWeapon.Thrown:
                    AddStat(ItemSpecStat.Thrown);

                    break;
                case ItemSubClassWeapon.Crossbow:
                    AddStat(ItemSpecStat.Crossbow);

                    break;
                case ItemSubClassWeapon.Wand:
                    AddStat(ItemSpecStat.Wand);

                    break;
                default:
                    break;
            }
        }
        else if (item.ClassID == ItemClass.Armor)
        {
            switch ((ItemSubClassArmor)item.SubclassID)
            {
                case ItemSubClassArmor.Cloth:
                    if (sparse.inventoryType != InventoryType.Cloak)
                    {
                        ItemType = 1;

                        break;
                    }

                    ItemType = 0;
                    AddStat(ItemSpecStat.Cloak);

                    break;
                case ItemSubClassArmor.Leather:
                    ItemType = 2;

                    break;
                case ItemSubClassArmor.Mail:
                    ItemType = 3;

                    break;
                case ItemSubClassArmor.Plate:
                    ItemType = 4;

                    break;
                default:
                    if (item.SubclassID == (int)ItemSubClassArmor.Shield)
                    {
                        ItemType = 6;
                        AddStat(ItemSpecStat.Shield);
                    }
                    else if (item.SubclassID is > (int)ItemSubClassArmor.Shield and <= (int)ItemSubClassArmor.Relic)
                    {
                        ItemType = 6;
                        AddStat(ItemSpecStat.Relic);
                    }
                    else
                    {
                        ItemType = 0;
                    }

                    break;
            }
        }
        else if (item.ClassID == ItemClass.Gem)
        {
            ItemType = 7;
            var gem = CliDB.GemPropertiesStorage.LookupByKey(sparse.GemProperties);

            if (gem != null)
            {
                if (gem.Type.HasAnyFlag(SocketColor.RelicIron))
                    AddStat(ItemSpecStat.RelicIron);

                if (gem.Type.HasAnyFlag(SocketColor.RelicBlood))
                    AddStat(ItemSpecStat.RelicBlood);

                if (gem.Type.HasAnyFlag(SocketColor.RelicShadow))
                    AddStat(ItemSpecStat.RelicShadow);

                if (gem.Type.HasAnyFlag(SocketColor.RelicFel))
                    AddStat(ItemSpecStat.RelicFel);

                if (gem.Type.HasAnyFlag(SocketColor.RelicArcane))
                    AddStat(ItemSpecStat.RelicArcane);

                if (gem.Type.HasAnyFlag(SocketColor.RelicFrost))
                    AddStat(ItemSpecStat.RelicFrost);

                if (gem.Type.HasAnyFlag(SocketColor.RelicFire))
                    AddStat(ItemSpecStat.RelicFire);

                if (gem.Type.HasAnyFlag(SocketColor.RelicWater))
                    AddStat(ItemSpecStat.RelicWater);

                if (gem.Type.HasAnyFlag(SocketColor.RelicLife))
                    AddStat(ItemSpecStat.RelicLife);

                if (gem.Type.HasAnyFlag(SocketColor.RelicWind))
                    AddStat(ItemSpecStat.RelicWind);

                if (gem.Type.HasAnyFlag(SocketColor.RelicHoly))
                    AddStat(ItemSpecStat.RelicHoly);
            }
        }
        else
        {
            ItemType = 0;
        }

        for (uint i = 0; i < ItemConst.MaxStats; ++i)
            if (sparse.StatModifierBonusStat[i] != -1)
                AddModStat(sparse.StatModifierBonusStat[i]);
    }

    private void AddModStat(int itemStatType)
    {
        switch ((ItemModType)itemStatType)
        {
            case ItemModType.Agility:
                AddStat(ItemSpecStat.Agility);

                break;
            case ItemModType.Strength:
                AddStat(ItemSpecStat.Strength);

                break;
            case ItemModType.Intellect:
                AddStat(ItemSpecStat.Intellect);

                break;
            case ItemModType.DodgeRating:
                AddStat(ItemSpecStat.Dodge);

                break;
            case ItemModType.ParryRating:
                AddStat(ItemSpecStat.Parry);

                break;
            case ItemModType.CritMeleeRating:
            case ItemModType.CritRangedRating:
            case ItemModType.CritSpellRating:
            case ItemModType.CritRating:
                AddStat(ItemSpecStat.Crit);

                break;
            case ItemModType.HasteRating:
                AddStat(ItemSpecStat.Haste);

                break;
            case ItemModType.HitRating:
                AddStat(ItemSpecStat.Hit);

                break;
            case ItemModType.ExtraArmor:
                AddStat(ItemSpecStat.BonusArmor);

                break;
            case ItemModType.AgiStrInt:
                AddStat(ItemSpecStat.Agility);
                AddStat(ItemSpecStat.Strength);
                AddStat(ItemSpecStat.Intellect);

                break;
            case ItemModType.AgiStr:
                AddStat(ItemSpecStat.Agility);
                AddStat(ItemSpecStat.Strength);

                break;
            case ItemModType.AgiInt:
                AddStat(ItemSpecStat.Agility);
                AddStat(ItemSpecStat.Intellect);

                break;
            case ItemModType.StrInt:
                AddStat(ItemSpecStat.Strength);
                AddStat(ItemSpecStat.Intellect);

                break;
        }
    }

    private void AddStat(ItemSpecStat statType)
    {
        if (ItemSpecStatCount >= ItemConst.MaxStats)
            return;

        for (uint i = 0; i < ItemConst.MaxStats; ++i)
            if (ItemSpecStatTypes[i] == statType)
                return;

        ItemSpecStatTypes[ItemSpecStatCount++] = statType;
    }
}