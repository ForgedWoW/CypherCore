// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities.Items;

public class ItemFactory
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly ClassFactory _classFactory;
    private readonly ObjectGuidGeneratorFactory _objectGuidGeneratorFactory;
    private readonly SpellManager _spellManager;

    public ItemFactory(CliDB cliDB, DB2Manager db2Manager, SpellManager spellManager, CharacterDatabase characterDatabase, ItemTemplateCache itemTemplateCache, 
                       ClassFactory classFactory, ObjectGuidGeneratorFactory objectGuidGeneratorFactory)
    {
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        _spellManager = spellManager;
        _characterDatabase = characterDatabase;
        _itemTemplateCache = itemTemplateCache;
        _classFactory = classFactory;
        _objectGuidGeneratorFactory = objectGuidGeneratorFactory;
    }

    public void AddItemsSetItem(Player player, Item item)
    {
        var proto = item.Template;
        var setid = proto.ItemSet;

        if (!_cliDB.ItemSetStorage.TryGetValue(setid, out var set))
        {
            Log.Logger.Error("Item set {0} for item (id {1}) not found, mods not applied.", setid, proto.Id);

            return;
        }

        if (set.RequiredSkill != 0 && player.GetSkillValue((SkillType)set.RequiredSkill) < set.RequiredSkillRank)
            return;

        if (set.SetFlags.HasAnyFlag(ItemSetFlags.LegacyInactive))
            return;

        // Check player level for heirlooms
        if (_db2Manager.GetHeirloomByItemId(item.Entry) != null)
            if (item.BonusData.PlayerLevelToItemLevelCurveId != 0)
            {
                var maxLevel = (uint)_db2Manager.GetCurveXAxisRange(item.BonusData.PlayerLevelToItemLevelCurveId).Item2;

                var contentTuning = _db2Manager.GetContentTuningData(item.BonusData.ContentTuningId, player.PlayerData.CtrOptions.Value.ContentTuningConditionMask, true);

                if (contentTuning.HasValue)
                    maxLevel = Math.Min(maxLevel, (uint)contentTuning.Value.MaxLevel);

                if (player.Level > maxLevel)
                    return;
            }

        var eff = player.ItemSetEff.FirstOrDefault(t => t?.ItemSetId == setid);

        if (eff == null)
        {
            eff = new ItemSetEffect
            {
                ItemSetId = setid
            };

            var x = 0;

            for (; x < player.ItemSetEff.Count; ++x)
                if (player.ItemSetEff[x] == null)
                    break;

            if (x < player.ItemSetEff.Count)
                player.ItemSetEff[x] = eff;
            else
                player.ItemSetEff.Add(eff);
        }

        eff.EquippedItems.Add(item);

        var itemSetSpells = _db2Manager.GetItemSetSpells(setid);

        foreach (var itemSetSpell in itemSetSpells)
        {
            //not enough for  spell
            if (itemSetSpell.Threshold > eff.EquippedItems.Count)
                continue;

            if (eff.SetBonuses.Contains(itemSetSpell))
                continue;

            var spellInfo = _spellManager.GetSpellInfo(itemSetSpell.SpellID);

            if (spellInfo == null)
            {
                Log.Logger.Error("WORLD: unknown spell id {0} in items set {1} effects", itemSetSpell.SpellID, setid);

                continue;
            }

            eff.SetBonuses.Add(itemSetSpell);

            // spell cast only if fit form requirement, in other case will cast at form change
            if (itemSetSpell.ChrSpecID == 0 || itemSetSpell.ChrSpecID == player.GetPrimarySpecialization())
                player.ApplyEquipSpell(spellInfo, null, true);
        }
    }

    public bool CanTransmogrifyItemWithItem(Item item, ItemModifiedAppearanceRecord itemModifiedAppearance)
    {
        var source = _itemTemplateCache.GetItemTemplate(itemModifiedAppearance.ItemID); // source
        var target = item.Template;                                                     // dest

        if (source == null || target == null)
            return false;

        if (itemModifiedAppearance == item.GetItemModifiedAppearance())
            return false;

        if (!item.IsValidTransmogrificationTarget())
            return false;

        if (source.Class != target.Class)
            return false;

        if (source.InventoryType is InventoryType.Bag or InventoryType.Relic or InventoryType.Finger or InventoryType.Trinket or InventoryType.Ammo or InventoryType.Quiver)
            return false;

        if (source.SubClass == target.SubClass)
            return true;

        switch (source.Class)
        {
            case ItemClass.Weapon:
                if (item.GetTransmogrificationWeaponCategory(source) != item.GetTransmogrificationWeaponCategory(target))
                    return false;

                break;

            case ItemClass.Armor:
                if ((ItemSubClassArmor)source.SubClass != ItemSubClassArmor.Cosmetic)
                    return false;

                if (source.InventoryType != target.InventoryType)
                    if (Item.ItemTransmogrificationSlots[(int)source.InventoryType] != Item.ItemTransmogrificationSlots[(int)target.InventoryType])
                        return false;

                break;

            default:
                return false;
        }

        return true;
    }

    public Item CreateItem(uint item, uint count, ItemContext context, Player player = null)
    {
        if (count < 1)
            return null; //don't create item at zero count

        var pProto = _itemTemplateCache.GetItemTemplate(item);

        if (pProto == null)
            return null;

        if (count > pProto.MaxStackSize)
            count = pProto.MaxStackSize;

        var pItem = NewItemOrBag(pProto);

        if (!pItem.Create(_objectGuidGeneratorFactory.GetGenerator(HighGuid.Item).Generate(), item, context, player))
            return null;

        pItem.SetCount(count);

        return pItem;

    }

    public void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public void DeleteFromInventoryDB(SQLTransaction trans, ulong itemGuid)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
        stmt.AddValue(0, itemGuid);
        trans.Append(stmt);
    }

    public uint GetBuyPrice(ItemTemplate proto, uint quality, uint itemLevel, out bool standardPrice)
    {
        standardPrice = true;

        if (proto.HasFlag(ItemFlags2.OverrideGoldCost))
            return proto.BuyPrice;

        if (!_cliDB.ImportPriceQualityStorage.TryGetValue(quality + 1, out var qualityPrice))
            return 0;

        if (!_cliDB.ItemPriceBaseStorage.TryGetValue(proto.BaseItemLevel, out var basePrice))
            return 0;

        var qualityFactor = qualityPrice.Data;
        float baseFactor;

        var inventoryType = proto.InventoryType;

        if (inventoryType is InventoryType.Weapon or InventoryType.Weapon2Hand or InventoryType.WeaponMainhand or InventoryType.WeaponOffhand or InventoryType.Ranged or InventoryType.Thrown or InventoryType.RangedRight)
            baseFactor = basePrice.Weapon;
        else
            baseFactor = basePrice.Armor;

        if (inventoryType == InventoryType.Robe)
            inventoryType = InventoryType.Chest;

        if (proto.Class == ItemClass.Gem && (ItemSubClassGem)proto.SubClass == ItemSubClassGem.ArtifactRelic)
        {
            inventoryType = InventoryType.Weapon;
            baseFactor = basePrice.Weapon / 3.0f;
        }

        var typeFactor = 0.0f;
        sbyte weapType = -1;

        switch (inventoryType)
        {
            case InventoryType.Head:
            case InventoryType.Neck:
            case InventoryType.Shoulders:
            case InventoryType.Chest:
            case InventoryType.Waist:
            case InventoryType.Legs:
            case InventoryType.Feet:
            case InventoryType.Wrists:
            case InventoryType.Hands:
            case InventoryType.Finger:
            case InventoryType.Trinket:
            case InventoryType.Cloak:
            case InventoryType.Holdable:
            {
                if (!_cliDB.ImportPriceArmorStorage.TryGetValue((uint)inventoryType, out var armorPrice))
                    return 0;

                typeFactor = (ItemSubClassArmor)proto.SubClass switch
                {
                    ItemSubClassArmor.Miscellaneous => armorPrice.ClothModifier,
                    ItemSubClassArmor.Cloth => armorPrice.ClothModifier,
                    ItemSubClassArmor.Leather => armorPrice.LeatherModifier,
                    ItemSubClassArmor.Mail => armorPrice.ChainModifier,
                    ItemSubClassArmor.Plate => armorPrice.PlateModifier,
                    _ => 1.0f
                };

                break;
            }
            case InventoryType.Shield:
            {
                var shieldPrice = _cliDB.ImportPriceShieldStorage.LookupByKey(2); // it only has two rows, it's unclear which is the one used

                if (shieldPrice == null)
                    return 0;

                typeFactor = shieldPrice.Data;

                break;
            }
            case InventoryType.WeaponMainhand:
                weapType = 0;

                break;

            case InventoryType.WeaponOffhand:
                weapType = 1;

                break;

            case InventoryType.Weapon:
                weapType = 2;

                break;

            case InventoryType.Weapon2Hand:
                weapType = 3;

                break;

            case InventoryType.Ranged:
            case InventoryType.RangedRight:
            case InventoryType.Relic:
                weapType = 4;

                break;

            default:
                return proto.BuyPrice;
        }

        if (weapType != -1)
        {
            if (!_cliDB.ImportPriceWeaponStorage.TryGetValue((uint)(weapType + 1), out var weaponPrice))
                return 0;

            typeFactor = weaponPrice.Data;
        }

        standardPrice = false;

        return (uint)(proto.PriceVariance * typeFactor * baseFactor * qualityFactor * proto.ExtendedData.PriceRandomValue);
    }

    public ItemDisenchantLootRecord GetDisenchantLoot(ItemTemplate itemTemplate, uint quality, uint itemLevel)
    {
        if (itemTemplate.HasFlag(ItemFlags.Conjured) || itemTemplate.HasFlag(ItemFlags.NoDisenchant) || itemTemplate.Bonding == ItemBondingType.Quest)
            return null;

        if (itemTemplate.GetArea(0) != 0 || itemTemplate.GetArea(1) != 0 || itemTemplate.Map != 0 || itemTemplate.MaxStackSize > 1)
            return null;

        if (GetSellPrice(itemTemplate, quality, itemLevel) == 0 && !_db2Manager.HasItemCurrencyCost(itemTemplate.Id))
            return null;

        var itemClass = (byte)itemTemplate.Class;
        var itemSubClass = itemTemplate.SubClass;
        var expansion = itemTemplate.RequiredExpansion;

        foreach (var disenchant in _cliDB.ItemDisenchantLootStorage.Values)
        {
            if (disenchant.Class != itemClass)
                continue;

            if (disenchant.Subclass >= 0 && itemSubClass != 0)
                continue;

            if (disenchant.Quality != quality)
                continue;

            if (disenchant.MinLevel > itemLevel || disenchant.MaxLevel < itemLevel)
                continue;

            if (disenchant.ExpansionID != -2 && disenchant.ExpansionID != expansion)
                continue;

            return disenchant;
        }

        return null;
    }

    public uint GetItemLevel(ItemTemplate itemTemplate, BonusData bonusData, uint level, uint fixedLevel, uint minItemLevel, uint minItemLevelCutoff, uint maxItemLevel, bool pvpBonus, uint azeriteLevel)
    {
        if (itemTemplate == null)
            return 1;

        var itemLevel = itemTemplate.BaseItemLevel;

        if (_cliDB.AzeriteLevelInfoStorage.TryGetValue(azeriteLevel, out var azeriteLevelInfo))
            itemLevel = azeriteLevelInfo.ItemLevel;

        if (bonusData.PlayerLevelToItemLevelCurveId != 0)
        {
            if (fixedLevel != 0)
                level = fixedLevel;
            else
            {
                var levels = _db2Manager.GetContentTuningData(bonusData.ContentTuningId, 0, true);

                if (levels.HasValue)
                    level = (uint)Math.Min(Math.Max((ushort)level, levels.Value.MinLevel), levels.Value.MaxLevel);
            }

            itemLevel = (uint)_db2Manager.GetCurveValueAt(bonusData.PlayerLevelToItemLevelCurveId, level);
        }

        itemLevel += (uint)bonusData.ItemLevelBonus;

        for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
            itemLevel += bonusData.GemItemLevelBonus[i];

        var itemLevelBeforeUpgrades = itemLevel;

        if (pvpBonus)
            itemLevel += _db2Manager.GetPvpItemLevelBonus(itemTemplate.Id);

        if (itemTemplate.InventoryType == InventoryType.NonEquip)
            return Math.Min(Math.Max(itemLevel, 1), 1300);

        if (minItemLevel != 0 && (minItemLevelCutoff == 0 || itemLevelBeforeUpgrades >= minItemLevelCutoff) && itemLevel < minItemLevel)
            itemLevel = minItemLevel;

        if (maxItemLevel != 0 && itemLevel > maxItemLevel)
            itemLevel = maxItemLevel;

        return Math.Min(Math.Max(itemLevel, 1), 1300);
    }

    public uint GetSellPrice(ItemTemplate proto, uint quality, uint itemLevel)
    {
        if (proto.HasFlag(ItemFlags2.OverrideGoldCost))
            return proto.SellPrice;

        var cost = GetBuyPrice(proto, quality, itemLevel, out var standardPrice);

        if (!standardPrice)
            return proto.SellPrice;

        var classEntry = _db2Manager.GetItemClassByOldEnum(proto.Class);

        if (classEntry == null)
            return 0;

        var buyCount = Math.Max(proto.BuyCount, 1u);

        return (uint)(cost * classEntry.PriceModifier / buyCount);
    }

    public bool ItemCanGoIntoBag(ItemTemplate pProto, ItemTemplate pBagProto)
    {
        if (pProto == null || pBagProto == null)
            return false;

        switch (pBagProto.Class)
        {
            case ItemClass.Container:
                return (ItemSubClassContainer)pBagProto.SubClass switch
                {
                    ItemSubClassContainer.Container => true,
                    ItemSubClassContainer.SoulContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.SoulShards),
                    ItemSubClassContainer.HerbContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Herbs),
                    ItemSubClassContainer.EnchantingContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.EnchantingSupp),
                    ItemSubClassContainer.MiningContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.MiningSupp),
                    ItemSubClassContainer.EngineeringContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.EngineeringSupp),
                    ItemSubClassContainer.GemContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Gems),
                    ItemSubClassContainer.LeatherworkingContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.LeatherworkingSupp),
                    ItemSubClassContainer.InscriptionContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.InscriptionSupp),
                    ItemSubClassContainer.TackleContainer => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.FishingSupp),
                    ItemSubClassContainer.CookingContainer => pProto.BagFamily.HasAnyFlag(BagFamilyMask.CookingSupp),
                    ItemSubClassContainer.ReagentContainer => pProto.IsCraftingReagent,
                    _ => false
                };
            //can remove?
            case ItemClass.Quiver:
                return (ItemSubClassQuiver)pBagProto.SubClass switch
                {
                    ItemSubClassQuiver.Quiver => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Arrows),
                    ItemSubClassQuiver.AmmoPouch => Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Bullets),
                    _ => false
                };
        }

        return false;
    }

    public uint ItemSubClassToDurabilityMultiplierId(ItemClass itemClass, uint itemSubClass)
    {
        return itemClass switch
        {
            ItemClass.Weapon => itemSubClass,
            ItemClass.Armor => itemSubClass + 21,
            _ => 0
        };
    }

    public Item NewItemOrBag(ItemTemplate proto)
    {
        if (proto.InventoryType == InventoryType.Bag)
            return _classFactory.Resolve<Bag>();

        if (_db2Manager.IsAzeriteItem(proto.Id))
            return _classFactory.Resolve<AzeriteItem>();

        return _db2Manager.GetAzeriteEmpoweredItem(proto.Id) != null ? _classFactory.Resolve<AzeriteEmpoweredItem>() : _classFactory.Resolve<Item>();
    }

    public void RemoveItemFromUpdateQueueOf(Item item, Player player)
    {
        if (!item.IsInUpdateQueue)
            return;

        if (player.GUID != item.OwnerGUID)
        {
            Log.Logger.Error("Item.RemoveFromUpdateQueueOf - Owner's guid ({0}) and player's guid ({1}) don't match!", item.OwnerGUID.ToString(), player.GUID.ToString());

            return;
        }

        if (player.ItemUpdateQueueBlocked)
            return;

        player.ItemUpdateQueue[item.QueuePos] = null;
        item.QueuePos = -1;
    }

    public void RemoveItemsSetItem(Player player, Item item)
    {
        var setid = item.Template.ItemSet;

        if (!_cliDB.ItemSetStorage.ContainsKey(setid))
        {
            Log.Logger.Error($"Item set {setid} for item {item.Entry} not found, mods not removed.");

            return;
        }

        ItemSetEffect eff = null;
        var setindex = 0;

        for (; setindex < player.ItemSetEff.Count; setindex++)
            if (player.ItemSetEff[setindex] != null && player.ItemSetEff[setindex].ItemSetId == setid)
            {
                eff = player.ItemSetEff[setindex];

                break;
            }

        // can be in case now enough skill requirement for set appling but set has been appliend when skill requirement not enough
        if (eff == null)
            return;

        eff.EquippedItems.Remove(item);

        var itemSetSpells = _db2Manager.GetItemSetSpells(setid);

        foreach (var itemSetSpell in itemSetSpells)
        {
            // enough for spell
            if (itemSetSpell.Threshold <= eff.EquippedItems.Count)
                continue;

            if (!eff.SetBonuses.Contains(itemSetSpell))
                continue;

            player.ApplyEquipSpell(_spellManager.GetSpellInfo(itemSetSpell.SpellID), null, false);
            eff.SetBonuses.Remove(itemSetSpell);
        }

        if (eff.EquippedItems.Empty()) //all items of a set were removed
            player.ItemSetEff[setindex] = null;
    }
}