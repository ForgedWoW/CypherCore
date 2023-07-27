// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class ItemTemplateCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly ClassFactory _classFactory;
    private readonly ScriptManager _scriptManager;
    private readonly DB2Manager _db2Manager;
    private readonly DB6Storage<ItemSparseRecord> _itemSparseRecords;
    private readonly DB6Storage<ItemRecord> _itemRecords;
    private readonly DB6Storage<ChrSpecializationRecord> _chrSpecializationRecords;
    private readonly DB6Storage<ItemSpecRecord> _itemSpecRecords;
    private readonly DB6Storage<ItemXItemEffectRecord> _itemXItemEffectRecords;
    private readonly DB6Storage<ItemEffectRecord> _itemEffectRecords;

    private readonly float[] _armorMultipliers = {
        0.00f, // INVTYPE_NON_EQUIP
        0.60f, // INVTYPE_HEAD
        0.00f, // INVTYPE_NECK
        0.60f, // INVTYPE_SHOULDERS
        0.00f, // INVTYPE_BODY
        1.00f, // INVTYPE_CHEST
        0.33f, // INVTYPE_WAIST
        0.72f, // INVTYPE_LEGS
        0.48f, // INVTYPE_FEET
        0.33f, // INVTYPE_WRISTS
        0.33f, // INVTYPE_HANDS
        0.00f, // INVTYPE_FINGER
        0.00f, // INVTYPE_TRINKET
        0.00f, // INVTYPE_WEAPON
        0.72f, // INVTYPE_SHIELD
        0.00f, // INVTYPE_RANGED
        0.00f, // INVTYPE_CLOAK
        0.00f, // INVTYPE_2HWEAPON
        0.00f, // INVTYPE_BAG
        0.00f, // INVTYPE_TABARD
        1.00f, // INVTYPE_ROBE
        0.00f, // INVTYPE_WEAPONMAINHAND
        0.00f, // INVTYPE_WEAPONOFFHAND
        0.00f, // INVTYPE_HOLDABLE
        0.00f, // INVTYPE_AMMO
        0.00f, // INVTYPE_THROWN
        0.00f, // INVTYPE_RANGEDRIGHT
        0.00f, // INVTYPE_QUIVER
        0.00f, // INVTYPE_RELIC
        0.00f, // INVTYPE_PROFESSION_TOOL
        0.00f, // INVTYPE_PROFESSION_GEAR
        0.00f, // INVTYPE_EQUIPABLE_SPELL_OFFENSIVE
        0.00f, // INVTYPE_EQUIPABLE_SPELL_UTILITY
        0.00f, // INVTYPE_EQUIPABLE_SPELL_DEFENSIVE
        0.00f, // INVTYPE_EQUIPABLE_SPELL_MOBILITY
    };

    private readonly float[] _qualityMultipliers = {
        0.92f, 0.92f, 0.92f, 1.11f, 1.32f, 1.61f, 0.0f, 0.0f
    };

    private readonly float[] _weaponMultipliers = {
        0.91f, // ITEM_SUBCLASS_WEAPON_AXE
        1.00f, // ITEM_SUBCLASS_WEAPON_AXE2
        1.00f, // ITEM_SUBCLASS_WEAPON_BOW
        1.00f, // ITEM_SUBCLASS_WEAPON_GUN
        0.91f, // ITEM_SUBCLASS_WEAPON_MACE
        1.00f, // ITEM_SUBCLASS_WEAPON_MACE2
        1.00f, // ITEM_SUBCLASS_WEAPON_POLEARM
        0.91f, // ITEM_SUBCLASS_WEAPON_SWORD
        1.00f, // ITEM_SUBCLASS_WEAPON_SWORD2
        1.00f, // ITEM_SUBCLASS_WEAPON_WARGLAIVES
        1.00f, // ITEM_SUBCLASS_WEAPON_STAFF
        0.00f, // ITEM_SUBCLASS_WEAPON_EXOTIC
        0.00f, // ITEM_SUBCLASS_WEAPON_EXOTIC2
        0.66f, // ITEM_SUBCLASS_WEAPON_FIST_WEAPON
        0.00f, // ITEM_SUBCLASS_WEAPON_MISCELLANEOUS
        0.66f, // ITEM_SUBCLASS_WEAPON_DAGGER
        0.00f, // ITEM_SUBCLASS_WEAPON_THROWN
        0.00f, // ITEM_SUBCLASS_WEAPON_SPEAR
        1.00f, // ITEM_SUBCLASS_WEAPON_CROSSBOW
        0.66f, // ITEM_SUBCLASS_WEAPON_WAND
        0.66f, // ITEM_SUBCLASS_WEAPON_FISHING_POLE
    };

    public ItemTemplateCache(WorldDatabase worldDatabase, ClassFactory classFactory, ScriptManager scriptManager, DB2Manager db2Manager, DB6Storage<ItemSparseRecord> itemSparseRecords,
                             DB6Storage<ItemRecord> itemRecords, DB6Storage<ChrSpecializationRecord> chrSpecializationRecords, DB6Storage<ItemSpecRecord> itemSpecRecords,
                             DB6Storage<ItemXItemEffectRecord> itemXItemEffectRecords, DB6Storage<ItemEffectRecord> itemEffectRecords)
    {
        _worldDatabase = worldDatabase;
        _classFactory = classFactory;
        _scriptManager = scriptManager;
        _db2Manager = db2Manager;
        _itemSparseRecords = itemSparseRecords;
        _itemRecords = itemRecords;
        _chrSpecializationRecords = chrSpecializationRecords;
        _itemSpecRecords = itemSpecRecords;
        _itemXItemEffectRecords = itemXItemEffectRecords;
        _itemEffectRecords = itemEffectRecords;
    }

    public Dictionary<uint, ItemTemplate> ItemTemplates { get; } = new();

    public ItemTemplate GetItemTemplate(uint itemId)
    {
        return ItemTemplates.LookupByKey(itemId);
    }

    public void LoadItemScriptNames()
    {
        var oldMSTime = Time.MSTime;
        uint count = 0;

        var result = _worldDatabase.Query("SELECT Id, ScriptName FROM item_script_names");

        if (!result.IsEmpty())
            do
            {
                var itemId = result.Read<uint>(0);

                if (GetItemTemplate(itemId) == null)
                {
                    Log.Logger.Error("Item {0} specified in `item_script_names` does not exist, skipped.", itemId);

                    continue;
                }

                ItemTemplates[itemId].ScriptId = _scriptManager.GetScriptId(result.Read<string>(1));
                ++count;
            } while (result.NextRow());

        Log.Logger.Information("Loaded {0} item script names in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void Load()
    {
        LoadItemTemplates();
        LoadItemTemplateAddon();
    }

    public void LoadItemTemplateAddon()
    {
        var time = Time.MSTime;

        uint count = 0;
        var result = _worldDatabase.Query("SELECT Id, FlagsCu, FoodType, MinMoneyLoot, MaxMoneyLoot, SpellPPMChance, RandomBonusListTemplateId FROM item_template_addon");

        if (!result.IsEmpty())
            do
            {
                var itemId = result.Read<uint>(0);
                var itemTemplate = GetItemTemplate(itemId);

                if (itemTemplate == null)
                {
                    Log.Logger.Error("Item {0} specified in `itemtemplateaddon` does not exist, skipped.", itemId);

                    continue;
                }

                var minMoneyLoot = result.Read<uint>(3);
                var maxMoneyLoot = result.Read<uint>(4);

                if (minMoneyLoot > maxMoneyLoot)
                {
                    Log.Logger.Error("Minimum money loot specified in `itemtemplateaddon` for item {0} was greater than maximum amount, swapping.", itemId);
                    (minMoneyLoot, maxMoneyLoot) = (maxMoneyLoot, minMoneyLoot);
                }

                itemTemplate.FlagsCu = (ItemFlagsCustom)result.Read<uint>(1);
                itemTemplate.FoodType = result.Read<uint>(2);
                itemTemplate.MinMoneyLoot = minMoneyLoot;
                itemTemplate.MaxMoneyLoot = maxMoneyLoot;
                itemTemplate.SpellPPMRate = result.Read<float>(5);
                itemTemplate.RandomBonusListTemplateId = result.Read<uint>(6);
                ++count;
            } while (result.NextRow());

        Log.Logger.Information("Loaded {0} item addon templates in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadItemTemplates()
    {
        var oldMSTime = Time.MSTime;
        uint sparseCount = 0;

        foreach (var sparse in _itemSparseRecords.Values)
        {
            if (!_itemRecords.TryGetValue(sparse.Id, out var db2Data))
                continue;

            var itemTemplate = _classFactory.ResolveWithPositionalParameters<ItemTemplate>(db2Data, sparse);
            itemTemplate.MaxDurability = FillMaxDurability(db2Data.ClassID, db2Data.SubclassID, sparse.inventoryType, (ItemQuality)sparse.OverallQualityID, sparse.ItemLevel);

            var itemSpecOverrides = _db2Manager.GetItemSpecOverrides(sparse.Id);

            if (itemSpecOverrides != null)
            {
                foreach (var itemSpecOverride in itemSpecOverrides)
                    if (_chrSpecializationRecords.TryGetValue(itemSpecOverride.SpecID, out var specialization))
                    {
                        itemTemplate.ItemSpecClassMask |= 1u << specialization.ClassID - 1;
                        itemTemplate.Specializations[0].Set(ItemTemplate.CalculateItemSpecBit(specialization), true);

                        itemTemplate.Specializations[1] = itemTemplate.Specializations[1].Or(itemTemplate.Specializations[0]);
                        itemTemplate.Specializations[2] = itemTemplate.Specializations[2].Or(itemTemplate.Specializations[0]);
                    }
            }
            else
            {
                var itemSpecStats = _classFactory.ResolveWithPositionalParameters<ItemSpecStats>(db2Data, sparse);

                foreach (var itemSpec in _itemSpecRecords.Values)
                {
                    if (itemSpecStats.ItemType != itemSpec.ItemType)
                        continue;

                    var hasPrimary = itemSpec.PrimaryStat == ItemSpecStat.None;
                    var hasSecondary = itemSpec.SecondaryStat == ItemSpecStat.None;

                    for (uint i = 0; i < itemSpecStats.ItemSpecStatCount; ++i)
                    {
                        if (itemSpecStats.ItemSpecStatTypes[i] == itemSpec.PrimaryStat)
                            hasPrimary = true;

                        if (itemSpecStats.ItemSpecStatTypes[i] == itemSpec.SecondaryStat)
                            hasSecondary = true;
                    }

                    if (!hasPrimary || !hasSecondary)
                        continue;

                    if (!_chrSpecializationRecords.TryGetValue(itemSpec.SpecializationID, out var specialization) || !Convert.ToBoolean(1 << specialization.ClassID - 1 & sparse.AllowableClass))
                        continue;

                    itemTemplate.ItemSpecClassMask |= 1u << specialization.ClassID - 1;
                    var specBit = ItemTemplate.CalculateItemSpecBit(specialization);
                    itemTemplate.Specializations[0].Set(specBit, true);

                    if (itemSpec.MaxLevel > 40)
                        itemTemplate.Specializations[1].Set(specBit, true);

                    if (itemSpec.MaxLevel >= 110)
                        itemTemplate.Specializations[2].Set(specBit, true);
                }
            }

            // Items that have no specializations set can be used by everyone
            foreach (var specs in itemTemplate.Specializations)
                if (specs.Count == 0)
                    specs.SetAll(true);

            ++sparseCount;
            ItemTemplates.Add(sparse.Id, itemTemplate);
        }

        // Load item effects (spells)
        foreach (var effectEntry in _itemXItemEffectRecords.Values)
            if (ItemTemplates.TryGetValue(effectEntry.ItemID, out var item))
                if (_itemEffectRecords.TryGetValue((uint)effectEntry.ItemEffectID, out var effect))
                    item.Effects.Add(effect);

        Log.Logger.Information("Loaded {0} item templates in {1} ms", sparseCount, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private uint FillMaxDurability(ItemClass itemClass, uint itemSubClass, InventoryType inventoryType, ItemQuality quality, uint itemLevel)
    {
        if (itemClass != ItemClass.Armor && itemClass != ItemClass.Weapon)
            return 0;

        var levelPenalty = 1.0f;

        if (itemLevel <= 28)
            levelPenalty = 0.966f - (28u - itemLevel) / 54.0f;

        if (itemClass != ItemClass.Armor)
            return 5 * (uint)Math.Round(18.0f * _qualityMultipliers[(int)quality] * _weaponMultipliers[itemSubClass] * levelPenalty);

        if (inventoryType > InventoryType.Robe)
            return 0;

        return 5 * (uint)Math.Round(25.0f * _qualityMultipliers[(int)quality] * _armorMultipliers[(int)inventoryType] * levelPenalty);

    }
}