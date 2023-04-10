// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Artifact;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Serilog;
using ItemMod = Forged.MapServer.Entities.Objects.Update.ItemMod;
using ItemModList = Forged.MapServer.Entities.Objects.Update.ItemModList;

namespace Forged.MapServer.Entities.Items;

public class Item : WorldObject
{
    public static int[] ItemTransmogrificationSlots =
    {
        -1,                      // INVTYPE_NON_EQUIP
        EquipmentSlot.Head,      // INVTYPE_HEAD
        -1,                      // INVTYPE_NECK
        EquipmentSlot.Shoulders, // INVTYPE_SHOULDERS
        EquipmentSlot.Shirt,     // INVTYPE_BODY
        EquipmentSlot.Chest,     // INVTYPE_CHEST
        EquipmentSlot.Waist,     // INVTYPE_WAIST
        EquipmentSlot.Legs,      // INVTYPE_LEGS
        EquipmentSlot.Feet,      // INVTYPE_FEET
        EquipmentSlot.Wrist,     // INVTYPE_WRISTS
        EquipmentSlot.Hands,     // INVTYPE_HANDS
        -1,                      // INVTYPE_FINGER
        -1,                      // INVTYPE_TRINKET
        -1,                      // INVTYPE_WEAPON
        EquipmentSlot.OffHand,   // INVTYPE_SHIELD
        EquipmentSlot.MainHand,  // INVTYPE_RANGED
        EquipmentSlot.Cloak,     // INVTYPE_CLOAK
        EquipmentSlot.MainHand,  // INVTYPE_2HWEAPON
        -1,                      // INVTYPE_BAG
        EquipmentSlot.Tabard,    // INVTYPE_TABARD
        EquipmentSlot.Chest,     // INVTYPE_ROBE
        EquipmentSlot.MainHand,  // INVTYPE_WEAPONMAINHAND
        EquipmentSlot.MainHand,  // INVTYPE_WEAPONOFFHAND
        EquipmentSlot.OffHand,   // INVTYPE_HOLDABLE
        -1,                      // INVTYPE_AMMO
        -1,                      // INVTYPE_THROWN
        EquipmentSlot.MainHand,  // INVTYPE_RANGEDRIGHT
        -1,                      // INVTYPE_QUIVER
        -1,                      // INVTYPE_RELIC
        -1,                      // INVTYPE_PROFESSION_TOOL
        -1,                      // INVTYPE_PROFESSION_GEAR
        -1,                      // INVTYPE_EQUIPABLE_SPELL_OFFENSIVE
        -1,                      // INVTYPE_EQUIPABLE_SPELL_UTILITY
        -1,                      // INVTYPE_EQUIPABLE_SPELL_DEFENSIVE
        -1                       // INVTYPE_EQUIPABLE_SPELL_MOBILITY
    };

    private readonly Dictionary<uint, ushort> _artifactPowerIdToIndex = new();
    private readonly Array<uint> _gemScalingLevels = new(ItemConst.MaxGemSockets);

    private List<ObjectGuid> _allowedGuiDs = new();
    private long _lastPlayedTimeUpdate;
    public Item() : base(false)
    {
        ObjectTypeMask |= TypeMask.Item;
        ObjectTypeId = TypeId.Item;

        ItemData = new ItemData();

        State = ItemUpdateState.New;
        QueuePos = -1;
        _lastPlayedTimeUpdate = GameTime.CurrentTime;
    }

    public uint AppearanceModId => ItemData.ItemAppearanceModID;
    public AzeriteEmpoweredItem AsAzeriteEmpoweredItem => this as AzeriteEmpoweredItem;
    public AzeriteItem AsAzeriteItem => this as AzeriteItem;
    public Bag AsBag => this as Bag;
    public byte BagSlot => Container?.Slot ?? InventorySlots.Bag0;
    public ItemBondingType Bonding => BonusData.Bonding;
    public BonusData BonusData { get; set; }
    public ObjectGuid ChildItem { get; private set; }
    public ObjectGuid ContainedIn => ItemData.ContainedIn;
    public Bag Container { get; private set; }
    public uint Count => ItemData.StackCount;
    public ObjectGuid Creator => ItemData.Creator;
    public ItemEffectRecord[] Effects => BonusData.Effects[0..BonusData.EffectCount];
    public ObjectGuid GiftCreator => ItemData.GiftCreator;
    public bool IsAzeriteEmpoweredItem => TypeId == TypeId.AzeriteEmpoweredItem;
    public bool IsAzeriteItem => TypeId == TypeId.AzeriteItem;
    public bool IsBag => Template.InventoryType == InventoryType.Bag;
    public bool IsBattlenetAccountBound => Template.HasFlag(ItemFlags2.BnetAccountTradeOk);
    public bool IsBOPTradeable => HasItemFlag(ItemFieldFlags.BopTradeable);
    public bool IsBoundAccountWide => Template.HasFlag(ItemFlags.IsBoundToAccount);
    public bool IsBroken => ItemData.MaxDurability > 0 && ItemData.Durability == 0;
    public bool IsConjuredConsumable => Template.IsConjuredConsumable;
    public bool IsCurrencyToken => Template.IsCurrencyToken;
    public bool IsEquipped => !IsInBag && Slot is < EquipmentSlot.End or >= ProfessionSlots.Start and < ProfessionSlots.End;
    public bool IsInTrade { get; private set; }
    public bool IsInUpdateQueue => QueuePos != -1;
    public bool IsLocked => !HasItemFlag(ItemFieldFlags.Unlocked);
    public bool IsNotEmptyBag
    {
        get
        {
            var bag = AsBag;

            if (bag != null)
                return !bag.IsEmpty();

            return false;
        }
    }

    public bool IsPotion => Template.IsPotion;
    public bool IsRangedWeapon => Template.IsRangedWeapon;
    public bool IsRefundable => HasItemFlag(ItemFieldFlags.Refundable);
    public bool IsRefundExpired => (PlayedTime > 2 * Time.HOUR);
    public bool IsSoulBound => HasItemFlag(ItemFieldFlags.Soulbound);
    public bool IsVellum => Template.IsVellum;
    public bool IsWrapped => HasItemFlag(ItemFieldFlags.Wrapped);
    public ItemData ItemData { get; set; }

    public uint ItemRandomBonusListId { get; private set; }
    public LootManagement.Loot Loot { get; set; }
    public bool LootGenerated { get; set; }
    public uint MaxStackCount => Template.MaxStackSize;
    public override ObjectGuid OwnerGUID => ItemData.Owner;

    public override Player OwnerUnit => Global.ObjAccessor.FindPlayer(OwnerGUID);

    public uint PaidExtendedCost { get; private set; }
    public ulong PaidMoney { get; private set; }
    public uint PlayedTime
    {
        get
        {
            var curtime = GameTime.CurrentTime;
            var elapsed = (uint)(curtime - _lastPlayedTimeUpdate);

            return ItemData.CreatePlayedTime + elapsed;
        }
    }

    public ushort Pos => (ushort)(BagSlot << 8 | Slot);
    public ItemQuality Quality => BonusData.Quality;
    public int QueuePos { get; private set; }
    public ObjectGuid RefundRecipient { get; private set; }
    public float RepairCostMultiplier => BonusData.RepairCostMultiplier;
    public uint ScalingContentTuningId => BonusData.ContentTuningId;
    public uint ScriptId => Template.ScriptId;
    public SkillType Skill
    {
        get
        {
            var proto = Template;

            return proto.GetSkill();
        }
    }

    public byte Slot { get; private set; }
    public ItemUpdateState State { get; private set; }
    public ItemTemplate Template => Global.ObjectMgr.GetItemTemplate(Entry);
    public string Text { get; private set; }
    private bool IsInBag => Container != null;
    public static void AddItemsSetItem(Player player, Item item)
    {
        var proto = item.Template;
        var setid = proto.ItemSet;

        var set = CliDB.ItemSetStorage.LookupByKey(setid);

        if (set == null)
        {
            Log.Logger.Error("Item set {0} for item (id {1}) not found, mods not applied.", setid, proto.Id);

            return;
        }

        if (set.RequiredSkill != 0 && player.GetSkillValue((SkillType)set.RequiredSkill) < set.RequiredSkillRank)
            return;

        if (set.SetFlags.HasAnyFlag(ItemSetFlags.LegacyInactive))
            return;

        // Check player level for heirlooms
        if (Global.DB2Mgr.GetHeirloomByItemId(item.Entry) != null)
            if (item.BonusData.PlayerLevelToItemLevelCurveId != 0)
            {
                var maxLevel = (uint)Global.DB2Mgr.GetCurveXAxisRange(item.BonusData.PlayerLevelToItemLevelCurveId).Item2;

                var contentTuning = Global.DB2Mgr.GetContentTuningData(item.BonusData.ContentTuningId, player.PlayerData.CtrOptions.Value.ContentTuningConditionMask, true);

                if (contentTuning.HasValue)
                    maxLevel = Math.Min(maxLevel, (uint)contentTuning.Value.MaxLevel);

                if (player.Level > maxLevel)
                    return;
            }

        ItemSetEffect eff = null;

        for (var x = 0; x < player.ItemSetEff.Count; ++x)
            if (player.ItemSetEff[x]?.ItemSetId == setid)
            {
                eff = player.ItemSetEff[x];

                break;
            }

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

        var itemSetSpells = Global.DB2Mgr.GetItemSetSpells(setid);

        foreach (var itemSetSpell in itemSetSpells)
        {
            //not enough for  spell
            if (itemSetSpell.Threshold > eff.EquippedItems.Count)
                continue;

            if (eff.SetBonuses.Contains(itemSetSpell))
                continue;

            var spellInfo = Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID, Difficulty.None);

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

    public static bool CanTransmogrifyItemWithItem(Item item, ItemModifiedAppearanceRecord itemModifiedAppearance)
    {
        var source = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID); // source
        var target = item.Template;                                                   // dest

        if (source == null || target == null)
            return false;

        if (itemModifiedAppearance == item.GetItemModifiedAppearance())
            return false;

        if (!item.IsValidTransmogrificationTarget())
            return false;

        if (source.Class != target.Class)
            return false;

        if (source.InventoryType == InventoryType.Bag ||
            source.InventoryType == InventoryType.Relic ||
            source.InventoryType == InventoryType.Finger ||
            source.InventoryType == InventoryType.Trinket ||
            source.InventoryType == InventoryType.Ammo ||
            source.InventoryType == InventoryType.Quiver)
            return false;

        if (source.SubClass != target.SubClass)
            switch (source.Class)
            {
                case ItemClass.Weapon:
                    if (GetTransmogrificationWeaponCategory(source) != GetTransmogrificationWeaponCategory(target))
                        return false;

                    break;
                case ItemClass.Armor:
                    if ((ItemSubClassArmor)source.SubClass != ItemSubClassArmor.Cosmetic)
                        return false;

                    if (source.InventoryType != target.InventoryType)
                        if (ItemTransmogrificationSlots[(int)source.InventoryType] != ItemTransmogrificationSlots[(int)target.InventoryType])
                            return false;

                    break;
                default:
                    return false;
            }

        return true;
    }

    public static Item CreateItem(uint item, uint count, ItemContext context, Player player = null)
    {
        if (count < 1)
            return null; //don't create item at zero count

        var pProto = Global.ObjectMgr.GetItemTemplate(item);

        if (pProto != null)
        {
            if (count > pProto.MaxStackSize)
                count = pProto.MaxStackSize;

            var pItem = NewItemOrBag(pProto);

            if (pItem.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), item, context, player))
            {
                pItem.SetCount(count);

                return pItem;
            }
        }

        return null;
    }

    public static void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
        stmt.AddValue(0, itemGuid);
        DB.Characters.ExecuteOrAppend(trans, stmt);
    }

    public static void DeleteFromInventoryDB(SQLTransaction trans, ulong itemGuid)
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
        stmt.AddValue(0, itemGuid);
        trans.Append(stmt);
    }

    public static ItemDisenchantLootRecord GetDisenchantLoot(ItemTemplate itemTemplate, uint quality, uint itemLevel)
    {
        if (itemTemplate.HasFlag(ItemFlags.Conjured) || itemTemplate.HasFlag(ItemFlags.NoDisenchant) || itemTemplate.Bonding == ItemBondingType.Quest)
            return null;

        if (itemTemplate.GetArea(0) != 0 || itemTemplate.GetArea(1) != 0 || itemTemplate.Map != 0 || itemTemplate.MaxStackSize > 1)
            return null;

        if (GetSellPrice(itemTemplate, quality, itemLevel) == 0 && !Global.DB2Mgr.HasItemCurrencyCost(itemTemplate.Id))
            return null;

        var itemClass = (byte)itemTemplate.Class;
        var itemSubClass = itemTemplate.SubClass;
        var expansion = itemTemplate.RequiredExpansion;

        foreach (var disenchant in CliDB.ItemDisenchantLootStorage.Values)
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

    public static uint GetItemLevel(ItemTemplate itemTemplate, BonusData bonusData, uint level, uint fixedLevel, uint minItemLevel, uint minItemLevelCutoff, uint maxItemLevel, bool pvpBonus, uint azeriteLevel)
    {
        if (itemTemplate == null)
            return 1;

        var itemLevel = itemTemplate.BaseItemLevel;
        var azeriteLevelInfo = CliDB.AzeriteLevelInfoStorage.LookupByKey(azeriteLevel);

        if (azeriteLevelInfo != null)
            itemLevel = azeriteLevelInfo.ItemLevel;

        if (bonusData.PlayerLevelToItemLevelCurveId != 0)
        {
            if (fixedLevel != 0)
            {
                level = fixedLevel;
            }
            else
            {
                var levels = Global.DB2Mgr.GetContentTuningData(bonusData.ContentTuningId, 0, true);

                if (levels.HasValue)
                    level = Math.Min(Math.Max((ushort)level, levels.Value.MinLevel), levels.Value.MaxLevel);
            }

            itemLevel = (uint)Global.DB2Mgr.GetCurveValueAt(bonusData.PlayerLevelToItemLevelCurveId, level);
        }

        itemLevel += (uint)bonusData.ItemLevelBonus;

        for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
            itemLevel += bonusData.GemItemLevelBonus[i];

        var itemLevelBeforeUpgrades = itemLevel;

        if (pvpBonus)
            itemLevel += Global.DB2Mgr.GetPvpItemLevelBonus(itemTemplate.Id);

        if (itemTemplate.InventoryType != InventoryType.NonEquip)
        {
            if (minItemLevel != 0 && (minItemLevelCutoff == 0 || itemLevelBeforeUpgrades >= minItemLevelCutoff) && itemLevel < minItemLevel)
                itemLevel = minItemLevel;

            if (maxItemLevel != 0 && itemLevel > maxItemLevel)
                itemLevel = maxItemLevel;
        }

        return Math.Min(Math.Max(itemLevel, 1), 1300);
    }

    public static uint GetSellPrice(ItemTemplate proto, uint quality, uint itemLevel)
    {
        if (proto.HasFlag(ItemFlags2.OverrideGoldCost))
            return proto.SellPrice;

        var cost = GetBuyPrice(proto, quality, itemLevel, out var standardPrice);

        if (standardPrice)
        {
            var classEntry = Global.DB2Mgr.GetItemClassByOldEnum(proto.Class);

            if (classEntry != null)
            {
                var buyCount = Math.Max(proto.BuyCount, 1u);

                return cost * classEntry.PriceModifier / buyCount;
            }

            return 0;
        }
        else
        {
            return proto.SellPrice;
        }
    }

    //Static
    public static bool ItemCanGoIntoBag(ItemTemplate pProto, ItemTemplate pBagProto)
    {
        if (pProto == null || pBagProto == null)
            return false;

        switch (pBagProto.Class)
        {
            case ItemClass.Container:
                switch ((ItemSubClassContainer)pBagProto.SubClass)
                {
                    case ItemSubClassContainer.Container:
                        return true;
                    case ItemSubClassContainer.SoulContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.SoulShards))
                            return false;

                        return true;
                    case ItemSubClassContainer.HerbContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Herbs))
                            return false;

                        return true;
                    case ItemSubClassContainer.EnchantingContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.EnchantingSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.MiningContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.MiningSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.EngineeringContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.EngineeringSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.GemContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Gems))
                            return false;

                        return true;
                    case ItemSubClassContainer.LeatherworkingContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.LeatherworkingSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.InscriptionContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.InscriptionSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.TackleContainer:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.FishingSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.CookingContainer:
                        if (!pProto.BagFamily.HasAnyFlag(BagFamilyMask.CookingSupp))
                            return false;

                        return true;
                    case ItemSubClassContainer.ReagentContainer:
                        return pProto.IsCraftingReagent;
                    default:
                        return false;
                }
            //can remove?
            case ItemClass.Quiver:
                switch ((ItemSubClassQuiver)pBagProto.SubClass)
                {
                    case ItemSubClassQuiver.Quiver:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Arrows))
                            return false;

                        return true;
                    case ItemSubClassQuiver.AmmoPouch:
                        if (!Convert.ToBoolean(pProto.BagFamily & BagFamilyMask.Bullets))
                            return false;

                        return true;
                    default:
                        return false;
                }
        }

        return false;
    }

    public static uint ItemSubClassToDurabilityMultiplierId(ItemClass itemClass, uint itemSubClass)
    {
        return itemClass switch
        {
            ItemClass.Weapon => itemSubClass,
            ItemClass.Armor  => itemSubClass + 21,
            _                => 0
        };
    }

    public static Item NewItemOrBag(ItemTemplate proto)
    {
        if (proto.InventoryType == InventoryType.Bag)
            return new Bag();

        if (Global.DB2Mgr.IsAzeriteItem(proto.Id))
            return new AzeriteItem();

        if (Global.DB2Mgr.GetAzeriteEmpoweredItem(proto.Id) != null)
            return new AzeriteEmpoweredItem();

        return new Item();
    }

    public static void RemoveItemFromUpdateQueueOf(Item item, Player player)
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

    public static void RemoveItemsSetItem(Player player, Item item)
    {
        var setid = item.Template.ItemSet;

        var set = CliDB.ItemSetStorage.LookupByKey(setid);

        if (set == null)
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

        var itemSetSpells = Global.DB2Mgr.GetItemSetSpells(setid);

        foreach (var itemSetSpell in itemSetSpells)
        {
            // enough for spell
            if (itemSetSpell.Threshold <= eff.EquippedItems.Count)
                continue;

            if (!eff.SetBonuses.Contains(itemSetSpell))
                continue;

            player.ApplyEquipSpell(Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID, Difficulty.None), null, false);
            eff.SetBonuses.Remove(itemSetSpell);
        }

        if (eff.EquippedItems.Empty()) //all items of a set were removed
            player.ItemSetEff[setindex] = null;
    }

    public void AddBonuses(uint bonusListID)
    {
        var bonusListIDs = GetBonusListIDs();

        if (bonusListIDs.Contains(bonusListID))
            return;

        var bonuses = Global.DB2Mgr.GetItemBonusList(bonusListID);

        if (bonuses != null)
        {
            ItemBonusKey itemBonusKey = new()
            {
                ItemID = Entry,
                BonusListIDs = GetBonusListIDs()
            };

            itemBonusKey.BonusListIDs.Add(bonusListID);
            SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemBonusKey), itemBonusKey);

            foreach (var bonus in bonuses)
                BonusData.AddBonus(bonus.BonusType, bonus.Value);

            SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemAppearanceModID), (byte)BonusData.AppearanceModID);
        }
    }

    public override bool AddToObjectUpdate()
    {
        var owner = OwnerUnit;

        if (owner)
        {
            owner.Location.Map.AddUpdateObject(this);

            return true;
        }

        return false;
    }

    public override void BuildUpdate(Dictionary<Player, UpdateData> data)
    {
        var owner = OwnerUnit;

        if (owner != null)
            BuildFieldsUpdate(owner, data);

        ClearUpdateMask(false);
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        ObjectData.WriteCreate(buffer, flags, this, target);
        ItemData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize() + 1);
        data.WriteUInt8((byte)flags);
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Item))
            ItemData.WriteUpdate(buffer, flags, this, target);


        data.WriteUInt32(buffer.GetSize());
        data.WriteUInt32(Values.GetChangedObjectTypeMask());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
    {
        UpdateMask valuesMask = new(14);
        valuesMask.Set((int)TypeId.Item);

        WorldPacket buffer = new();
        UpdateMask mask = new(40);

        buffer.WriteUInt32(valuesMask.GetBlock(0));
        ItemData.AppendAllowedFieldsMaskForFlag(mask, flags);
        ItemData.WriteUpdate(buffer, mask, true, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public ulong CalculateDurabilityRepairCost(float discount)
    {
        uint maxDurability = ItemData.MaxDurability;

        if (maxDurability == 0)
            return 0;

        uint curDurability = ItemData.Durability;

        var lostDurability = maxDurability - curDurability;

        if (lostDurability == 0)
            return 0;

        var itemTemplate = Template;

        var durabilityCost = CliDB.DurabilityCostsStorage.LookupByKey(GetItemLevel(OwnerUnit));

        if (durabilityCost == null)
            return 0;

        var durabilityQualityEntryId = ((uint)Quality + 1) * 2;
        var durabilityQualityEntry = CliDB.DurabilityQualityStorage.LookupByKey(durabilityQualityEntryId);

        if (durabilityQualityEntry == null)
            return 0;

        uint dmultiplier = itemTemplate.Class switch
        {
            ItemClass.Weapon => durabilityCost.WeaponSubClassCost[itemTemplate.SubClass],
            ItemClass.Armor  => durabilityCost.ArmorSubClassCost[itemTemplate.SubClass],
            _                => 0
        };

        var cost = (ulong)Math.Round(lostDurability * dmultiplier * durabilityQualityEntry.Data * RepairCostMultiplier);
        cost = (ulong)(cost * discount * GetDefaultValue("Rate.RepairCost", 1.0f));

        if (cost == 0) // Fix for ITEM_QUALITY_ARTIFACT
            cost = 1;

        return cost;
    }

    public InventoryResult CanBeMergedPartlyWith(ItemTemplate proto)
    {
        // not allow merge looting currently items
        if (LootGenerated)
            return InventoryResult.LootGone;

        // check item type
        if (Entry != proto.Id)
            return InventoryResult.CantStack;

        // check free space (full stacks can't be target of merge
        if (Count >= proto.MaxStackSize)
            return InventoryResult.CantStack;

        return InventoryResult.Ok;
    }

    public bool CanBeTraded(bool mail = false, bool trade = false)
    {
        if (LootGenerated)
            return false;

        if ((!mail || !IsBoundAccountWide) && (IsSoulBound && (!IsBOPTradeable || !trade)))
            return false;

        if (IsBag && (PlayerComputators.IsBagPos(Pos) || !AsBag.IsEmpty()))
            return false;

        var owner = OwnerUnit;

        if (owner != null)
        {
            if (owner.CanUnequipItem(Pos, false) != InventoryResult.Ok)
                return false;

            if (owner.GetLootGUID() == GUID)
                return false;
        }

        if (IsBoundByEnchant())
            return false;

        return true;
    }

    public void CheckArtifactRelicSlotUnlock(Player owner)
    {
        if (!owner)
            return;

        var artifactId = Template.ArtifactID;

        if (artifactId == 0)
            return;

        foreach (var artifactUnlock in CliDB.ArtifactUnlockStorage.Values)
            if (artifactUnlock.ArtifactID == artifactId)
                if (owner.MeetPlayerCondition(artifactUnlock.PlayerConditionID))
                    AddBonuses(artifactUnlock.ItemBonusListID);
    }

    public bool CheckSoulboundTradeExpire()
    {
        // called from owner's update - GetOwner() MUST be valid
        if (ItemData.CreatePlayedTime + 2 * Time.HOUR < OwnerUnit.TotalPlayedTime)
        {
            ClearSoulboundTradeable(OwnerUnit);

            return true; // remove from tradeable list
        }

        return false;
    }

    public void ClearBonuses()
    {
        ItemBonusKey itemBonusKey = new()
        {
            ItemID = Entry
        };

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemBonusKey), itemBonusKey);
        BonusData = new BonusData(Template);
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemAppearanceModID), (byte)BonusData.AppearanceModID);
    }

    public void ClearEnchantment(EnchantmentSlot slot)
    {
        if (GetEnchantmentId(slot) == 0)
            return;

        var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, (int)slot);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), 0u);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), 0u);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), (short)0);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Inactive), (ushort)0);
        SetState(ItemUpdateState.Changed, OwnerUnit);
    }

    public void ClearSoulboundTradeable(Player currentOwner)
    {
        RemoveItemFlag(ItemFieldFlags.BopTradeable);

        if (_allowedGuiDs.Empty())
            return;

        currentOwner.Session.CollectionMgr.AddItemAppearance(this);
        _allowedGuiDs.Clear();
        SetState(ItemUpdateState.Changed, currentOwner);
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_BOP_TRADE);
        stmt.AddValue(0, GUID.Counter);
        DB.Characters.Execute(stmt);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(ItemData);
        base.ClearUpdateMask(remove);
    }

    public Item CloneItem(uint count, Player player = null)
    {
        var newItem = CreateItem(Entry, count, GetContext(), player);

        if (newItem == null)
            return null;

        newItem.SetCreator(Creator);
        newItem.SetGiftCreator(GiftCreator);
        newItem.ReplaceAllItemFlags((ItemFieldFlags)(ItemData.DynamicFlags & ~(uint)(ItemFieldFlags.Refundable | ItemFieldFlags.BopTradeable)));
        newItem.SetExpiration(ItemData.Expiration);

        // player CAN be NULL in which case we must not update random properties because that accesses player's item update queue
        if (player != null)
            newItem.SetItemRandomBonusList(ItemRandomBonusListId);

        return newItem;
    }

    public void CopyArtifactDataFromParent(Item parent)
    {
        Array.Copy(parent.BonusData.GemItemLevelBonus, BonusData.GemItemLevelBonus, BonusData.GemItemLevelBonus.Length);
        SetModifier(ItemModifier.ArtifactAppearanceId, parent.GetModifier(ItemModifier.ArtifactAppearanceId));
        SetAppearanceModId(parent.AppearanceModId);
    }

    public virtual bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
    {
        Create(ObjectGuid.Create(HighGuid.Item, guidlow));

        Entry = itemId;
        ObjectScale = 1.0f;

        if (owner)
        {
            SetOwnerGUID(owner.GUID);
            SetContainedIn(owner.GUID);
        }

        var itemProto = Global.ObjectMgr.GetItemTemplate(itemId);

        if (itemProto == null)
            return false;

        BonusData = new BonusData(itemProto);
        SetCount(1);
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.MaxDurability), itemProto.MaxDurability);
        SetDurability(itemProto.MaxDurability);

        for (var i = 0; i < itemProto.Effects.Count; ++i)
            if (itemProto.Effects[i].LegacySlotIndex < 5)
                SetSpellCharges(itemProto.Effects[i].LegacySlotIndex, itemProto.Effects[i].Charges);

        SetExpiration(itemProto.Duration);
        SetCreatePlayedTime(0);
        SetContext(context);

        if (itemProto.ArtifactID != 0)
        {
            InitArtifactPowers(itemProto.ArtifactID, 0);

            foreach (var artifactAppearance in CliDB.ArtifactAppearanceStorage.Values)
            {
                var artifactAppearanceSet = CliDB.ArtifactAppearanceSetStorage.LookupByKey(artifactAppearance.ArtifactAppearanceSetID);

                if (artifactAppearanceSet != null)
                {
                    if (itemProto.ArtifactID != artifactAppearanceSet.ArtifactID)
                        continue;

                    var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactAppearance.UnlockPlayerConditionID);

                    if (playerCondition != null)
                        if (!owner || !ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                            continue;

                    SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearance.Id);
                    SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);

                    break;
                }
            }

            CheckArtifactRelicSlotUnlock(owner ?? OwnerUnit);
        }

        return true;
    }

    public virtual void DeleteFromDB(SQLTransaction trans)
    {
        DeleteFromDB(trans, GUID.Counter);

        // Delete the items if this is a container
        if (Loot != null && !Loot.IsLooted())
            Global.LootItemStorage.RemoveStoredLootForContainer(GUID.Counter);
    }

    public void DeleteFromInventoryDB(SQLTransaction trans)
    {
        DeleteFromInventoryDB(trans, GUID.Counter);
    }

    public void DeleteRefundDataFromDB(SQLTransaction trans = null)
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
        stmt.AddValue(0, GUID.Counter);

        if (trans != null)
            trans.Append(stmt);
        else
            DB.Characters.Execute(stmt);
    }

    public void FSetState(ItemUpdateState state) // forced
    {
        State = state;
    }

    public bool GemsFitSockets()
    {
        uint gemSlot = 0;

        foreach (var gemData in ItemData.Gems)
        {
            var SocketColor = Template.GetSocketColor(gemSlot);

            if (SocketColor == 0) // no socket slot
                continue;

            SocketColor GemColor = 0;

            var gemProto = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);

            if (gemProto != null)
            {
                var gemProperty = CliDB.GemPropertiesStorage.LookupByKey(gemProto.GemProperties);

                if (gemProperty != null)
                    GemColor = gemProperty.Type;
            }

            if (!GemColor.HasAnyFlag(ItemConst.SocketColorToGemTypeMask[(int)SocketColor])) // bad gem color on this socket
                return false;
        }

        return true;
    }

    public ArtifactPower GetArtifactPower(uint artifactPowerId)
    {
        var index = _artifactPowerIdToIndex.LookupByKey(artifactPowerId);

        if (index != 0)
            return ItemData.ArtifactPowers[index];

        return null;
    }

    public List<uint> GetBonusListIDs()
    {
        return ItemData.ItemBonusKey.Value.BonusListIDs;
    }

    public ItemContext GetContext()
    {
        return (ItemContext)(int)ItemData.Context;
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nOwner: {OwnerGUID} Count: {Count} BagSlot: {BagSlot} Slot: {Slot} Equipped: {IsEquipped}";
    }

    public ItemDisenchantLootRecord GetDisenchantLoot(Player owner)
    {
        if (!BonusData.CanDisenchant)
            return null;

        return GetDisenchantLoot(Template, (uint)Quality, GetItemLevel(owner));
    }

    public uint GetDisplayId(Player owner)
    {
        var itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (itemModifiedAppearanceId == 0)
            itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

        var transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);

        if (transmog != null)
        {
            var itemAppearance = CliDB.ItemAppearanceStorage.LookupByKey(transmog.ItemAppearanceID);

            if (itemAppearance != null)
                return itemAppearance.ItemDisplayInfoID;
        }

        return Global.DB2Mgr.GetItemDisplayId(Entry, AppearanceModId);
    }

    public int GetEnchantmentCharges(EnchantmentSlot slot)
    {
        return ItemData.Enchantment[(int)slot].Charges;
    }

    public uint GetEnchantmentDuration(EnchantmentSlot slot)
    {
        return ItemData.Enchantment[(int)slot].Duration;
    }

    public uint GetEnchantmentId(EnchantmentSlot slot)
    {
        return ItemData.Enchantment[(int)slot].ID;
    }

    public SocketedGem GetGem(ushort slot)
    {
        //ASSERT(slot < MAX_GEM_SOCKETS);
        return slot < ItemData.Gems.Size() ? ItemData.Gems[slot] : null;
    }

    public byte GetGemCountWithID(uint GemID)
    {
        var list = (List<SocketedGem>)ItemData.Gems;

        return (byte)list.Count(gemData => gemData.ItemId == GemID);
    }

    public byte GetGemCountWithLimitCategory(uint limitCategory)
    {
        var list = (List<SocketedGem>)ItemData.Gems;

        return (byte)list.Count(gemData =>
        {
            var gemProto = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);

            if (gemProto == null)
                return false;

            return gemProto.ItemLimitCategory == limitCategory;
        });
    }

    public uint GetItemLevel(Player owner)
    {
        var itemTemplate = Template;
        uint minItemLevel = owner.UnitData.MinItemLevel;
        uint minItemLevelCutoff = owner.UnitData.MinItemLevelCutoff;
        var maxItemLevel = itemTemplate.HasFlag(ItemFlags3.IgnoreItemLevelCapInPvp) ? 0u : owner.UnitData.MaxItemLevel;
        var pvpBonus = owner.IsUsingPvpItemLevels;

        uint azeriteLevel = 0;
        var azeriteItem = AsAzeriteItem;

        if (azeriteItem != null)
            azeriteLevel = azeriteItem.GetEffectiveLevel();

        return GetItemLevel(itemTemplate,
                            BonusData,
                            owner.Level,
                            GetModifier(ItemModifier.TimewalkerLevel),
                            minItemLevel,
                            minItemLevelCutoff,
                            maxItemLevel,
                            pvpBonus,
                            azeriteLevel);
    }

    public ItemModifiedAppearanceRecord GetItemModifiedAppearance()
    {
        return Global.DB2Mgr.GetItemModifiedAppearance(Entry, BonusData.AppearanceModID);
    }

    public int GetItemStatType(uint index)
    {
        return BonusData.ItemStatType[index];
    }

    public float GetItemStatValue(uint index, Player owner)
    {
        switch ((ItemModType)GetItemStatType(index))
        {
            case ItemModType.Corruption:
            case ItemModType.CorruptionResistance:
                return BonusData.StatPercentEditor[index];
            
        }

        var itemLevel = GetItemLevel(owner);
        var randomPropPoints = ItemEnchantmentManager.GetRandomPropertyPoints(itemLevel, Quality, Template.InventoryType, Template.SubClass);

        if (randomPropPoints != 0)
        {
            var statValue = BonusData.StatPercentEditor[index] * randomPropPoints * 0.0001f;
            var gtCost = CliDB.ItemSocketCostPerLevelGameTable.GetRow(itemLevel);

            if (gtCost != null)
                statValue -= BonusData.ItemStatSocketCostMultiplier[index] * gtCost.SocketCost;

            return statValue;
        }

        return 0f;
    }

    public override LootManagement.Loot GetLootForPlayer(Player player)
    {
        return Loot;
    }

    public uint GetModifier(ItemModifier modifier)
    {
        var modifierIndex = ItemData.Modifiers.Value.Values.FindIndexIf(mod => { return mod.Type == (byte)modifier; });

        if (modifierIndex != -1)
            return ItemData.Modifiers.Value.Values[modifierIndex].Value;

        return 0;
    }

    public override string GetName(Locale locale = Locale.enUS)
    {
        var itemTemplate = Template;
        var suffix = CliDB.ItemNameDescriptionStorage.LookupByKey(BonusData.Suffix);

        if (suffix != null)
            return $"{itemTemplate.GetName(locale)} {suffix.Description[locale]}";

        return itemTemplate.GetName(locale);
    }

    public int GetRequiredLevel()
    {
        var fixedLevel = (int)GetModifier(ItemModifier.TimewalkerLevel);

        if (BonusData.RequiredLevelCurve != 0)
            return (int)Global.DB2Mgr.GetCurveValueAt(BonusData.RequiredLevelCurve, fixedLevel);

        if (BonusData.RequiredLevelOverride != 0)
            return BonusData.RequiredLevelOverride;

        if (BonusData.HasFixedLevel && BonusData.PlayerLevelToItemLevelCurveId != 0)
            return fixedLevel;

        return BonusData.RequiredLevel;
    }

    public uint GetSellPrice(Player owner)
    {
        return GetSellPrice(Template, (uint)Quality, GetItemLevel(owner));
    }

    public SocketColor GetSocketColor(uint index)
    {
        return BonusData.socketColor[index];
    }

    public int GetSpellCharges(int index = 0)
    {
        return ItemData.SpellCharges[index];
    }

    public uint GetTotalPurchasedArtifactPowers()
    {
        uint purchasedRanks = 0;

        foreach (var power in ItemData.ArtifactPowers)
            purchasedRanks += power.PurchasedRank;

        return purchasedRanks;
    }

    public uint GetTotalUnlockedArtifactPowers()
    {
        var purchased = GetTotalPurchasedArtifactPowers();
        ulong artifactXp = ItemData.ArtifactXP;
        var currentArtifactTier = GetModifier(ItemModifier.ArtifactTier);
        uint extraUnlocked = 0;

        do
        {
            ulong xpCost = 0;
            var cost = CliDB.ArtifactLevelXPGameTable.GetRow(purchased + extraUnlocked + 1);

            if (cost != null)
                xpCost = (ulong)(currentArtifactTier == PlayerConst.MaxArtifactTier ? cost.XP2 : cost.XP);

            if (artifactXp < xpCost)
                break;

            artifactXp -= xpCost;
            ++extraUnlocked;
        } while (true);

        return purchased + extraUnlocked;
    }

    public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
    {
        if (target.GUID == OwnerGUID)
            return UpdateFieldFlag.Owner;

        return UpdateFieldFlag.None;
    }

    public ushort GetVisibleAppearanceModId(Player owner)
    {
        var itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (itemModifiedAppearanceId == 0)
            itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

        var transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);

        if (transmog != null)
            return (ushort)transmog.ItemAppearanceModifierID;

        return (ushort)AppearanceModId;
    }

    public uint GetVisibleEnchantmentId(Player owner)
    {
        var enchantmentId = GetModifier(ItemConst.IllusionModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (enchantmentId == 0)
            enchantmentId = GetModifier(ItemModifier.EnchantIllusionAllSpecs);

        if (enchantmentId == 0)
            enchantmentId = GetEnchantmentId(EnchantmentSlot.Perm);

        return enchantmentId;
    }

    public uint GetVisibleEntry(Player owner)
    {
        var itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (itemModifiedAppearanceId == 0)
            itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

        var transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);

        if (transmog != null)
            return transmog.ItemID;

        return Entry;
    }

    public ushort GetVisibleItemVisual(Player owner)
    {
        var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetVisibleEnchantmentId(owner));

        if (enchant != null)
            return enchant.ItemVisual;

        return 0;
    }

    public uint GetVisibleSecondaryModifiedAppearanceId(Player owner)
    {
        var itemModifiedAppearanceId = GetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (itemModifiedAppearanceId == 0)
            itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs);

        return itemModifiedAppearanceId;
    }

    public void GiveArtifactXp(ulong amount, Item sourceItem, ArtifactCategory artifactCategoryId)
    {
        var owner = OwnerUnit;

        if (!owner)
            return;

        if (artifactCategoryId != 0)
        {
            uint artifactKnowledgeLevel = 1;

            if (sourceItem != null && sourceItem.GetModifier(ItemModifier.ArtifactKnowledgeLevel) != 0)
                artifactKnowledgeLevel = sourceItem.GetModifier(ItemModifier.ArtifactKnowledgeLevel);

            var artifactKnowledge = CliDB.ArtifactKnowledgeMultiplierGameTable.GetRow(artifactKnowledgeLevel);

            if (artifactKnowledge != null)
                amount = (ulong)(amount * artifactKnowledge.Multiplier);

            amount = amount switch
            {
                >= 5000 => 50 * (amount / 50),
                >= 1000 => 25 * (amount / 25),
                >= 50   => 5 * (amount / 5),
                _       => amount
            };
        }

        SetArtifactXP(ItemData.ArtifactXP + amount);

        ArtifactXpGain artifactXpGain = new()
        {
            ArtifactGUID = GUID,
            Amount = amount
        };

        owner.SendPacket(artifactXpGain);

        SetState(ItemUpdateState.Changed, owner);
    }

    public override bool HasInvolvedQuest(uint quest_id)
    {
        return false;
    }

    public bool HasItemFlag(ItemFieldFlags flag)
    {
        return (ItemData.DynamicFlags & (uint)flag) != 0;
    }

    public bool HasItemFlag2(ItemFieldFlags2 flag)
    {
        return (ItemData.DynamicFlags2 & (uint)flag) != 0;
    }

    public override bool HasQuest(uint quest_id)
    {
        return Template.StartQuest == quest_id;
    }

    public void InitArtifactPowers(byte artifactId, byte artifactTier)
    {
        foreach (var artifactPower in Global.DB2Mgr.GetArtifactPowers(artifactId))
        {
            if (artifactPower.Tier != artifactTier)
                continue;

            if (_artifactPowerIdToIndex.ContainsKey(artifactPower.Id))
                continue;

            ArtifactPowerData powerData = new()
            {
                ArtifactPowerId = artifactPower.Id,
                PurchasedRank = 0,
                CurrentRankWithBonus = (byte)((artifactPower.Flags & ArtifactPowerFlag.First) == ArtifactPowerFlag.First ? 1 : 0)
            };

            AddArtifactPower(powerData);
        }
    }

    public bool IsArtifactDisabled()
    {
        var artifact = CliDB.ArtifactStorage.LookupByKey(Template.ArtifactID);

        if (artifact != null)
            return artifact.ArtifactCategoryID != 2; // fishing artifact

        return true;
    }

    public bool IsBindedNotWith(Player player)
    {
        // not binded item
        if (!IsSoulBound)
            return false;

        // own item
        if (OwnerGUID == player.GUID)
            return false;

        if (IsBOPTradeable)
            if (_allowedGuiDs.Contains(player.GUID))
                return false;

        // BOA item case
        if (IsBoundAccountWide)
            return false;

        return true;
    }

    public bool IsFitToSpellRequirements(SpellInfo spellInfo)
    {
        var proto = Template;

        var isEnchantSpell = spellInfo.HasEffect(SpellEffectName.EnchantItem) || spellInfo.HasEffect(SpellEffectName.EnchantItemTemporary) || spellInfo.HasEffect(SpellEffectName.EnchantItemPrismatic);

        if ((int)spellInfo.EquippedItemClass != -1) // -1 == any item class
        {
            if (isEnchantSpell && proto.HasFlag(ItemFlags3.CanStoreEnchants))
                return true;

            if (spellInfo.EquippedItemClass != proto.Class)
                return false; //  wrong item class

            if (spellInfo.EquippedItemSubClassMask != 0) // 0 == any subclass
                if ((spellInfo.EquippedItemSubClassMask & (1 << (int)proto.SubClass)) == 0)
                    return false; // subclass not present in mask
        }

        if (isEnchantSpell && spellInfo.EquippedItemInventoryTypeMask != 0) // 0 == any inventory type
        {
            // Special case - accept weapon type for main and offhand requirements
            if (proto.InventoryType == InventoryType.Weapon &&
                Convert.ToBoolean(spellInfo.EquippedItemInventoryTypeMask & (1 << (int)InventoryType.WeaponMainhand)) ||
                Convert.ToBoolean(spellInfo.EquippedItemInventoryTypeMask & (1 << (int)InventoryType.WeaponOffhand)))
                return true;
            else if ((spellInfo.EquippedItemInventoryTypeMask & (1 << (int)proto.InventoryType)) == 0)
                return false; // inventory type not present in mask
        }

        return true;
    }

    public bool IsLimitedToAnotherMapOrZone(uint cur_mapId, uint cur_zoneId)
    {
        var proto = Template;

        return proto != null &&
               ((proto.Map != 0 && proto.Map != cur_mapId) ||
                ((proto.GetArea(0) != 0 && proto.GetArea(0) != cur_zoneId) && (proto.GetArea(1) != 0 && proto.GetArea(1) != cur_zoneId)));
    }

    public void LoadArtifactData(Player owner, ulong xp, uint artifactAppearanceId, uint artifactTier, List<ArtifactPowerData> powers)
    {
        for (byte i = 0; i <= artifactTier; ++i)
            InitArtifactPowers(Template.ArtifactID, i);

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactXP), xp);
        SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearanceId);
        SetModifier(ItemModifier.ArtifactTier, artifactTier);

        var artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifactAppearanceId);

        if (artifactAppearance != null)
            SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);

        byte totalPurchasedRanks = 0;

        foreach (var power in powers)
        {
            power.CurrentRankWithBonus += power.PurchasedRank;
            totalPurchasedRanks += power.PurchasedRank;

            var artifactPower = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);

            for (var e = EnchantmentSlot.Sock1; e <= EnchantmentSlot.Sock3; ++e)
            {
                var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetEnchantmentId(e));

                if (enchant != null)
                    for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                        switch (enchant.Effect[i])
                        {
                            case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                                if (artifactPower.Label == enchant.EffectArg[i])
                                    power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];

                                break;
                            case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                                if (artifactPower.Id == enchant.EffectArg[i])
                                    power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];

                                break;
                            case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                                if (BonusData.GemRelicType[e - EnchantmentSlot.Sock1] != -1)
                                {
                                    var artifactPowerPicker = CliDB.ArtifactPowerPickerStorage.LookupByKey(enchant.EffectArg[i]);

                                    if (artifactPowerPicker != null)
                                    {
                                        var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactPowerPicker.PlayerConditionID);

                                        if (playerCondition == null || (owner != null && ConditionManager.IsPlayerMeetingCondition(owner, playerCondition)))
                                            if (artifactPower.Label == BonusData.GemRelicType[e - EnchantmentSlot.Sock1])
                                                power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                    }
                                }

                                break;
                            
                        }
            }

            SetArtifactPower((ushort)power.ArtifactPowerId, power.PurchasedRank, power.CurrentRankWithBonus);
        }

        foreach (var power in powers)
        {
            var scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);

            if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                continue;

            SetArtifactPower((ushort)power.ArtifactPowerId, power.PurchasedRank, (byte)(totalPurchasedRanks + 1));
        }

        CheckArtifactRelicSlotUnlock(owner);
    }

    public virtual bool LoadFromDB(ulong guid, ObjectGuid ownerGuid, SQLFields fields, uint entry)
    {
        // create item before any checks for store correct guid
        // and allow use "FSetState(ITEM_REMOVED); SaveToDB();" for deleting item from DB
        Create(ObjectGuid.Create(HighGuid.Item, guid));

        Entry = entry;
        ObjectScale = 1.0f;

        var proto = Template;

        if (proto == null)
            return false;

        BonusData = new BonusData(proto);

        // set owner (not if item is only loaded for gbank/auction/mail
        if (!ownerGuid.IsEmpty)
            SetOwnerGUID(ownerGuid);

        var itemFlags = fields.Read<uint>(7);
        var need_save = false;
        var creator = fields.Read<ulong>(2);

        if (creator != 0)
        {
            if (!Convert.ToBoolean(itemFlags & (int)ItemFieldFlags.Child))
                SetCreator(ObjectGuid.Create(HighGuid.Player, creator));
            else
                SetCreator(ObjectGuid.Create(HighGuid.Item, creator));
        }

        var giftCreator = fields.Read<ulong>(3);

        if (giftCreator != 0)
            SetGiftCreator(ObjectGuid.Create(HighGuid.Player, giftCreator));

        SetCount(fields.Read<uint>(4));

        var duration = fields.Read<uint>(5);
        SetExpiration(duration);

        // update duration if need, and remove if not need
        if (proto.Duration != duration)
        {
            SetExpiration(proto.Duration);
            need_save = true;
        }

        ReplaceAllItemFlags((ItemFieldFlags)itemFlags);

        var durability = fields.Read<uint>(10);
        SetDurability(durability);
        // update max durability (and durability) if need
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.MaxDurability), proto.MaxDurability);

        // do not overwrite durability for wrapped items
        if (durability > proto.MaxDurability && !IsWrapped)
        {
            SetDurability(proto.MaxDurability);
            need_save = true;
        }

        SetCreatePlayedTime(fields.Read<uint>(11));
        SetText(fields.Read<string>(12));

        SetModifier(ItemModifier.BattlePetSpeciesId, fields.Read<uint>(13));
        SetModifier(ItemModifier.BattlePetBreedData, fields.Read<uint>(14));
        SetModifier(ItemModifier.BattlePetLevel, fields.Read<ushort>(14));
        SetModifier(ItemModifier.BattlePetDisplayId, fields.Read<uint>(16));

        SetContext((ItemContext)fields.Read<byte>(17));

        var bonusListString = new StringArray(fields.Read<string>(18), ' ');
        List<uint> bonusListIDs = new();

        for (var i = 0; i < bonusListString.Length; ++i)
            if (uint.TryParse(bonusListString[i], out var bonusListID))
                bonusListIDs.Add(bonusListID);

        SetBonuses(bonusListIDs);

        // load charges after bonuses, they can add more item effects
        var tokens = new StringArray(fields.Read<string>(6), ' ');

        for (byte i = 0; i < ItemData.SpellCharges.GetSize() && i < BonusData.EffectCount && i < tokens.Length; ++i)
            if (int.TryParse(tokens[i], out var value))
                SetSpellCharges(i, value);

        SetModifier(ItemModifier.TransmogAppearanceAllSpecs, fields.Read<uint>(19));
        SetModifier(ItemModifier.TransmogAppearanceSpec1, fields.Read<uint>(20));
        SetModifier(ItemModifier.TransmogAppearanceSpec2, fields.Read<uint>(21));
        SetModifier(ItemModifier.TransmogAppearanceSpec3, fields.Read<uint>(22));
        SetModifier(ItemModifier.TransmogAppearanceSpec4, fields.Read<uint>(23));
        SetModifier(ItemModifier.TransmogAppearanceSpec5, fields.Read<uint>(24));

        SetModifier(ItemModifier.EnchantIllusionAllSpecs, fields.Read<uint>(25));
        SetModifier(ItemModifier.EnchantIllusionSpec1, fields.Read<uint>(26));
        SetModifier(ItemModifier.EnchantIllusionSpec2, fields.Read<uint>(27));
        SetModifier(ItemModifier.EnchantIllusionSpec3, fields.Read<uint>(28));
        SetModifier(ItemModifier.EnchantIllusionSpec4, fields.Read<uint>(29));
        SetModifier(ItemModifier.EnchantIllusionSpec4, fields.Read<uint>(30));

        SetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs, fields.Read<uint>(31));
        SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, fields.Read<uint>(32));
        SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, fields.Read<uint>(33));
        SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, fields.Read<uint>(34));
        SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, fields.Read<uint>(35));
        SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec5, fields.Read<uint>(36));

        var gemFields = 4;
        var gemData = new ItemDynamicFieldGems[ItemConst.MaxGemSockets];

        for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
        {
            gemData[i] = new ItemDynamicFieldGems
            {
                ItemId = fields.Read<uint>(37 + i * gemFields)
            };

            var gemBonusListIDs = new StringArray(fields.Read<string>(38 + i * gemFields), ' ');

            if (!gemBonusListIDs.IsEmpty())
            {
                uint b = 0;

                foreach (string token in gemBonusListIDs)
                    if (uint.TryParse(token, out var bonusListID) && bonusListID != 0)
                        gemData[i].BonusListIDs[b++] = (ushort)bonusListID;
            }

            gemData[i].Context = fields.Read<byte>(39 + i * gemFields);

            if (gemData[i].ItemId != 0)
                SetGem((ushort)i, gemData[i], fields.Read<uint>(40 + i * gemFields));
        }

        SetModifier(ItemModifier.TimewalkerLevel, fields.Read<uint>(49));
        SetModifier(ItemModifier.ArtifactKnowledgeLevel, fields.Read<uint>(50));

        // Enchants must be loaded after all other bonus/scaling data
        var enchantmentTokens = new StringArray(fields.Read<string>(8), ' ');

        if (enchantmentTokens.Length == (int)EnchantmentSlot.Max * 3)
            for (var i = 0; i < (int)EnchantmentSlot.Max; ++i)
            {
                var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, i);
                SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), uint.Parse(enchantmentTokens[i * 3 + 0]));
                SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), uint.Parse(enchantmentTokens[i * 3 + 1]));
                SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), short.Parse(enchantmentTokens[i * 3 + 2]));
            }

        ItemRandomBonusListId = fields.Read<uint>(9);

        // Remove bind Id for items vs NO_BIND set
        if (IsSoulBound && Bonding == ItemBondingType.None)
        {
            RemoveItemFlag(ItemFieldFlags.Soulbound);
            need_save = true;
        }

        if (need_save) // normal item changed state set not work at loading
        {
            byte index = 0;
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_ON_LOAD);
            stmt.AddValue(index++, (uint)ItemData.Expiration);
            stmt.AddValue(index++, (uint)ItemData.DynamicFlags);
            stmt.AddValue(index++, (uint)ItemData.Durability);
            stmt.AddValue(index++, guid);
            DB.Characters.Execute(stmt);
        }

        return true;
    }

    public override void RemoveFromObjectUpdate()
    {
        var owner = OwnerUnit;

        if (owner)
            owner.Location.Map.RemoveUpdateObject(this);
    }

    public void RemoveItemFlag(ItemFieldFlags flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.DynamicFlags), (uint)flags);
    }

    public void RemoveItemFlag2(ItemFieldFlags2 flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.DynamicFlags2), (uint)flags);
    }

    public void ReplaceAllItemFlags(ItemFieldFlags flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.DynamicFlags), (uint)flags);
    }

    public void ReplaceAllItemFlags2(ItemFieldFlags2 flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.DynamicFlags2), (uint)flags);
    }

    public void SaveRefundDataToDB()
    {
        DeleteRefundDataFromDB();

        var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_REFUND_INSTANCE);
        stmt.AddValue(0, GUID.Counter);
        stmt.AddValue(1, RefundRecipient.Counter);
        stmt.AddValue(2, PaidMoney);
        stmt.AddValue(3, (ushort)PaidExtendedCost);
        DB.Characters.Execute(stmt);
    }

    public virtual void SaveToDB(SQLTransaction trans)
    {
        PreparedStatement stmt;

        switch (State)
        {
            case ItemUpdateState.New:
            case ItemUpdateState.Changed:
            {
                byte index = 0;
                stmt = DB.Characters.GetPreparedStatement(State == ItemUpdateState.New ? CharStatements.REP_ITEM_INSTANCE : CharStatements.UPD_ITEM_INSTANCE);
                stmt.AddValue(index, Entry);
                stmt.AddValue(++index, OwnerGUID.Counter);
                stmt.AddValue(++index, Creator.Counter);
                stmt.AddValue(++index, GiftCreator.Counter);
                stmt.AddValue(++index, Count);
                stmt.AddValue(++index, (uint)ItemData.Expiration);

                StringBuilder ss = new();

                for (byte i = 0; i < ItemData.SpellCharges.GetSize() && i < BonusData.EffectCount; ++i)
                    ss.Append($"{GetSpellCharges(i)} ");

                stmt.AddValue(++index, ss.ToString());
                stmt.AddValue(++index, (uint)ItemData.DynamicFlags);

                ss.Clear();

                for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                {
                    var enchantment = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetEnchantmentId(slot));

                    if (enchantment != null && !enchantment.GetFlags().HasFlag(SpellItemEnchantmentFlags.DoNotSaveToDB))
                        ss.Append($"{GetEnchantmentId(slot)} {GetEnchantmentDuration(slot)} {GetEnchantmentCharges(slot)} ");
                    else
                        ss.Append("0 0 0 ");
                }

                stmt.AddValue(++index, ss.ToString());
                stmt.AddValue(++index, ItemRandomBonusListId);
                stmt.AddValue(++index, (uint)ItemData.Durability);
                stmt.AddValue(++index, (uint)ItemData.CreatePlayedTime);
                stmt.AddValue(++index, Text);
                stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetSpeciesId));
                stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetBreedData));
                stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetLevel));
                stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetDisplayId));
                stmt.AddValue(++index, (byte)ItemData.Context);

                ss.Clear();

                foreach (int bonusListID in GetBonusListIDs())
                    ss.Append($"{bonusListID} ");

                stmt.AddValue(++index, ss.ToString());
                stmt.AddValue(++index, GUID.Counter);

                DB.Characters.Execute(stmt);

                if ((State == ItemUpdateState.Changed) && IsWrapped)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GIFT_OWNER);
                    stmt.AddValue(0, OwnerGUID.Counter);
                    stmt.AddValue(1, GUID.Counter);
                    DB.Characters.Execute(stmt);
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (ItemData.Gems.Size() != 0)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_GEMS);
                    stmt.AddValue(0, GUID.Counter);
                    var i = 0;
                    var gemFields = 4;

                    foreach (var gemData in ItemData.Gems)
                    {
                        if (gemData.ItemId != 0)
                        {
                            stmt.AddValue(1 + i * gemFields, (uint)gemData.ItemId);
                            StringBuilder gemBonusListIDs = new();

                            foreach (var bonusListID in gemData.BonusListIDs)
                                if (bonusListID != 0)
                                    gemBonusListIDs.Append($"{bonusListID} ");

                            stmt.AddValue(2 + i * gemFields, gemBonusListIDs.ToString());
                            stmt.AddValue(3 + i * gemFields, (byte)gemData.Context);
                            stmt.AddValue(4 + i * gemFields, _gemScalingLevels[i]);
                        }
                        else
                        {
                            stmt.AddValue(1 + i * gemFields, 0);
                            stmt.AddValue(2 + i * gemFields, "");
                            stmt.AddValue(3 + i * gemFields, 0);
                            stmt.AddValue(4 + i * gemFields, 0);
                        }

                        ++i;
                    }

                    for (; i < ItemConst.MaxGemSockets; ++i)
                    {
                        stmt.AddValue(1 + i * gemFields, 0);
                        stmt.AddValue(2 + i * gemFields, "");
                        stmt.AddValue(3 + i * gemFields, 0);
                        stmt.AddValue(4 + i * gemFields, 0);
                    }

                    trans.Append(stmt);
                }

                ItemModifier[] transmogMods =
                {
                    ItemModifier.TransmogAppearanceAllSpecs, ItemModifier.TransmogAppearanceSpec1, ItemModifier.TransmogAppearanceSpec2, ItemModifier.TransmogAppearanceSpec3, ItemModifier.TransmogAppearanceSpec4, ItemModifier.TransmogAppearanceSpec5, ItemModifier.EnchantIllusionAllSpecs, ItemModifier.EnchantIllusionSpec1, ItemModifier.EnchantIllusionSpec2, ItemModifier.EnchantIllusionSpec3, ItemModifier.EnchantIllusionSpec4, ItemModifier.EnchantIllusionSpec5, ItemModifier.TransmogSecondaryAppearanceAllSpecs, ItemModifier.TransmogSecondaryAppearanceSpec1, ItemModifier.TransmogSecondaryAppearanceSpec2, ItemModifier.TransmogSecondaryAppearanceSpec3, ItemModifier.TransmogSecondaryAppearanceSpec4, ItemModifier.TransmogSecondaryAppearanceSpec5
                };

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (transmogMods.Any(modifier => GetModifier(modifier) != 0))
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_TRANSMOG);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                    stmt.AddValue(2, GetModifier(ItemModifier.TransmogAppearanceSpec1));
                    stmt.AddValue(3, GetModifier(ItemModifier.TransmogAppearanceSpec2));
                    stmt.AddValue(4, GetModifier(ItemModifier.TransmogAppearanceSpec3));
                    stmt.AddValue(5, GetModifier(ItemModifier.TransmogAppearanceSpec4));
                    stmt.AddValue(6, GetModifier(ItemModifier.TransmogAppearanceSpec5));
                    stmt.AddValue(7, GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                    stmt.AddValue(8, GetModifier(ItemModifier.EnchantIllusionSpec1));
                    stmt.AddValue(9, GetModifier(ItemModifier.EnchantIllusionSpec2));
                    stmt.AddValue(10, GetModifier(ItemModifier.EnchantIllusionSpec3));
                    stmt.AddValue(11, GetModifier(ItemModifier.EnchantIllusionSpec4));
                    stmt.AddValue(12, GetModifier(ItemModifier.EnchantIllusionSpec5));
                    stmt.AddValue(13, GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                    stmt.AddValue(14, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1));
                    stmt.AddValue(15, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2));
                    stmt.AddValue(16, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3));
                    stmt.AddValue(17, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4));
                    stmt.AddValue(18, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec5));
                    trans.Append(stmt);
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (Template.ArtifactID != 0)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, (ulong)ItemData.ArtifactXP);
                    stmt.AddValue(2, GetModifier(ItemModifier.ArtifactAppearanceId));
                    stmt.AddValue(3, GetModifier(ItemModifier.ArtifactTier));
                    trans.Append(stmt);

                    foreach (var artifactPower in ItemData.ArtifactPowers)
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT_POWERS);
                        stmt.AddValue(0, GUID.Counter);
                        stmt.AddValue(1, artifactPower.ArtifactPowerId);
                        stmt.AddValue(2, artifactPower.PurchasedRank);
                        trans.Append(stmt);
                    }
                }

                ItemModifier[] modifiersTable =
                {
                    ItemModifier.TimewalkerLevel, ItemModifier.ArtifactKnowledgeLevel
                };

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (modifiersTable.Any(modifier => GetModifier(modifier) != 0))
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_MODIFIERS);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, GetModifier(ItemModifier.TimewalkerLevel));
                    stmt.AddValue(2, GetModifier(ItemModifier.ArtifactKnowledgeLevel));
                    trans.Append(stmt);
                }

                break;
            }
            case ItemUpdateState.Removed:
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (IsWrapped)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
                    stmt.AddValue(0, GUID.Counter);
                    trans.Append(stmt);
                }

                // Delete the items if this is a container
                if (Loot != null && !Loot.IsLooted())
                    Global.LootItemStorage.RemoveStoredLootForContainer(GUID.Counter);

                Dispose();

                return;
            }
            case ItemUpdateState.Unchanged:
                break;
        }

        SetState(ItemUpdateState.Unchanged);
    }

    public void SendTimeUpdate(Player owner)
    {
        uint duration = ItemData.Expiration;

        if (duration == 0)
            return;

        ItemTimeUpdate itemTimeUpdate = new()
        {
            ItemGuid = GUID,
            DurationLeft = duration
        };

        owner.SendPacket(itemTimeUpdate);
    }

    public void SendUpdateSockets()
    {
        SocketGemsSuccess socketGems = new()
        {
            Item = GUID
        };

        OwnerUnit.SendPacket(socketGems);
    }

    public void SetAppearanceModId(uint appearanceModId)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemAppearanceModID), (byte)appearanceModId);
    }

    public void SetArtifactPower(ushort artifactPowerId, byte purchasedRank, byte currentRankWithBonus)
    {
        var foundIndex = _artifactPowerIdToIndex.LookupByKey(artifactPowerId);

        if (foundIndex != 0)
        {
            ArtifactPower artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, foundIndex);
            SetUpdateFieldValue(ref artifactPower.PurchasedRank, purchasedRank);
            SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, currentRankWithBonus);
        }
    }

    public void SetArtifactXP(ulong xp)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactXP), xp);
    }

    public void SetBinding(bool val)
    {
        if (val)
            SetItemFlag(ItemFieldFlags.Soulbound);
        else
            RemoveItemFlag(ItemFieldFlags.Soulbound);
    }

    public void SetBonuses(List<uint> bonusListIDs)
    {
        if (bonusListIDs == null)
            bonusListIDs = new List<uint>();

        ItemBonusKey itemBonusKey = new()
        {
            ItemID = Entry,
            BonusListIDs = bonusListIDs
        };

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemBonusKey), itemBonusKey);

        foreach (var bonusListID in GetBonusListIDs())
            BonusData.AddBonusList(bonusListID);

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemAppearanceModID), (byte)BonusData.AppearanceModID);
    }

    public void SetChildItem(ObjectGuid childItem)
    {
        ChildItem = childItem;
    }

    public void SetContainedIn(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ContainedIn), guid);
    }

    public void SetContainer(Bag container)
    {
        Container = container;
    }

    public void SetContext(ItemContext context)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Context), (int)context);
    }

    public void SetCount(uint value)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.StackCount), value);

        var player = OwnerUnit;

        if (player)
        {
            var tradeData = player.GetTradeData();

            if (tradeData != null)
            {
                var slot = tradeData.GetTradeSlotForItem(GUID);

                if (slot != TradeSlots.Invalid)
                    tradeData.SetItem(slot, this, true);
            }
        }
    }

    public void SetCreatePlayedTime(uint createPlayedTime)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.CreatePlayedTime), createPlayedTime);
    }

    public void SetCreator(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Creator), guid);
    }

    public void SetDurability(uint durability)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Durability), durability);
    }

    public void SetEnchantment(EnchantmentSlot slot, uint id, uint duration, uint charges, ObjectGuid caster = default)
    {
        // Better lost small time at check in comparison lost time at item save to DB.
        if ((GetEnchantmentId(slot) == id) && (GetEnchantmentDuration(slot) == duration) && (GetEnchantmentCharges(slot) == charges))
            return;

        var owner = OwnerUnit;

        if (slot < EnchantmentSlot.MaxInspected)
        {
            var oldEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetEnchantmentId(slot));

            if (oldEnchant != null && !oldEnchant.GetFlags().HasFlag(SpellItemEnchantmentFlags.DoNotLog))
                owner.Session.SendEnchantmentLog(OwnerGUID, ObjectGuid.Empty, GUID, Entry, oldEnchant.Id, (uint)slot);

            var newEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(id);

            if (newEnchant != null && !newEnchant.GetFlags().HasFlag(SpellItemEnchantmentFlags.DoNotLog))
                owner.Session.SendEnchantmentLog(OwnerGUID, caster, GUID, Entry, id, (uint)slot);
        }

        ApplyArtifactPowerEnchantmentBonuses(slot, GetEnchantmentId(slot), false, owner);
        ApplyArtifactPowerEnchantmentBonuses(slot, id, true, owner);

        var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, (int)slot);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), id);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), duration);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), (short)charges);
        SetState(ItemUpdateState.Changed, owner);
    }

    public void SetEnchantmentCharges(EnchantmentSlot slot, uint charges)
    {
        if (GetEnchantmentCharges(slot) == charges)
            return;

        var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, (int)slot);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), (short)charges);
        SetState(ItemUpdateState.Changed, OwnerUnit);
    }

    public void SetEnchantmentDuration(EnchantmentSlot slot, uint duration, Player owner)
    {
        if (GetEnchantmentDuration(slot) == duration)
            return;

        var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, (int)slot);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), duration);
        SetState(ItemUpdateState.Changed, owner);
        // Cannot use GetOwner() here, has to be passed as an argument to avoid freeze due to hashtable locking
    }

    public void SetFixedLevel(uint level)
    {
        if (!BonusData.HasFixedLevel || GetModifier(ItemModifier.TimewalkerLevel) != 0)
            return;

        if (BonusData.PlayerLevelToItemLevelCurveId != 0)
        {
            var levels = Global.DB2Mgr.GetContentTuningData(BonusData.ContentTuningId, 0, true);

            if (levels.HasValue)
                level = (uint)Math.Min(Math.Max((short)level, levels.Value.MinLevel), levels.Value.MaxLevel);

            SetModifier(ItemModifier.TimewalkerLevel, level);
        }
    }

    public void SetGem(ushort slot, ItemDynamicFieldGems gem, uint gemScalingLevel)
    {
        //ASSERT(slot < MAX_GEM_SOCKETS);
        _gemScalingLevels[slot] = gemScalingLevel;
        BonusData.GemItemLevelBonus[slot] = 0;
        var gemTemplate = Global.ObjectMgr.GetItemTemplate(gem.ItemId);

        if (gemTemplate != null)
        {
            var gemProperties = CliDB.GemPropertiesStorage.LookupByKey(gemTemplate.GemProperties);

            if (gemProperties != null)
            {
                var gemEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(gemProperties.EnchantId);

                if (gemEnchant != null)
                {
                    BonusData gemBonus = new(gemTemplate);

                    foreach (var bonusListId in gem.BonusListIDs)
                        gemBonus.AddBonusList(bonusListId);

                    var gemBaseItemLevel = gemTemplate.BaseItemLevel;

                    if (gemBonus.PlayerLevelToItemLevelCurveId != 0)
                    {
                        var scaledIlvl = (uint)Global.DB2Mgr.GetCurveValueAt(gemBonus.PlayerLevelToItemLevelCurveId, gemScalingLevel);

                        if (scaledIlvl != 0)
                            gemBaseItemLevel = scaledIlvl;
                    }

                    BonusData.GemRelicType[slot] = gemBonus.RelicType;

                    for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                        switch (gemEnchant.Effect[i])
                        {
                            case ItemEnchantmentType.BonusListID:
                            {
                                var bonusesEffect = Global.DB2Mgr.GetItemBonusList(gemEnchant.EffectArg[i]);

                                if (bonusesEffect != null)
                                    foreach (var itemBonus in bonusesEffect)
                                        if (itemBonus.BonusType == ItemBonusType.ItemLevel)

                                            BonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];

                                break;
                            }
                            case ItemEnchantmentType.BonusListCurve:
                            {
                                var artifactrBonusListId = Global.DB2Mgr.GetItemBonusListForItemLevelDelta((short)Global.DB2Mgr.GetCurveValueAt((uint)Curves.ArtifactRelicItemLevelBonus, gemBaseItemLevel + gemBonus.ItemLevelBonus));

                                if (artifactrBonusListId != 0)
                                {
                                    var bonusesEffect = Global.DB2Mgr.GetItemBonusList(artifactrBonusListId);

                                    if (bonusesEffect != null)
                                        foreach (var itemBonus in bonusesEffect)
                                            if (itemBonus.BonusType == ItemBonusType.ItemLevel)
                                                BonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];
                                }

                                break;
                            }
                            
                        }
                }
            }
        }

        SocketedGem gemField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Gems, slot);
        SetUpdateFieldValue(gemField.ModifyValue(gemField.ItemId), gem.ItemId);
        SetUpdateFieldValue(gemField.ModifyValue(gemField.Context), gem.Context);

        for (var i = 0; i < 16; ++i)
            SetUpdateFieldValue(ref gemField.ModifyValue(gemField.BonusListIDs, i), gem.BonusListIDs[i]);
    }

    public void SetGiftCreator(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.GiftCreator), guid);
    }

    public void SetInTrade(bool b = true)
    {
        IsInTrade = b;
    }

    public void SetItemFlag(ItemFieldFlags flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.DynamicFlags), (uint)flags);
    }

    public void SetItemFlag2(ItemFieldFlags2 flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.DynamicFlags2), (uint)flags);
    }

    public void SetItemRandomBonusList(uint bonusListId)
    {
        if (bonusListId == 0)
            return;

        AddBonuses(bonusListId);
    }

    public void SetMaxDurability(uint maxDurability)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.MaxDurability), maxDurability);
    }

    public void SetModifier(ItemModifier modifier, uint value)
    {
        var modifierIndex = ItemData.Modifiers.Value.Values.FindIndexIf(mod => { return mod.Type == (byte)modifier; });

        if (value != 0)
        {
            if (modifierIndex == -1)
            {
                ItemMod mod = new()
                {
                    Value = value,
                    Type = (byte)modifier
                };

                AddDynamicUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Modifiers).Value.ModifyValue(ItemData.Modifiers.Value.Values), mod);
            }
            else
            {
                ItemModList itemModList = Values.ModifyValue(ItemData).ModifyValue(ItemData.Modifiers);
                itemModList.ModifyValue(itemModList.Values, modifierIndex);
                SetUpdateFieldValue(ref itemModList.ModifyValue(itemModList.Values, modifierIndex).Value.Value, value);
            }
        }
        else
        {
            if (modifierIndex == -1)
                return;

            RemoveDynamicUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Modifiers).Value.ModifyValue(ItemData.Modifiers.Value.Values), modifierIndex);
        }
    }

    public void SetNotRefundable(Player owner, bool changestate = true, SQLTransaction trans = null, bool addToCollection = true)
    {
        if (!IsRefundable)
            return;

        ItemExpirePurchaseRefund itemExpirePurchaseRefund = new()
        {
            ItemGUID = GUID
        };

        owner.SendPacket(itemExpirePurchaseRefund);

        RemoveItemFlag(ItemFieldFlags.Refundable);

        // Following is not applicable in the trading procedure
        if (changestate)
            SetState(ItemUpdateState.Changed, owner);

        SetRefundRecipient(ObjectGuid.Empty);
        SetPaidMoney(0);
        SetPaidExtendedCost(0);
        DeleteRefundDataFromDB(trans);

        owner.DeleteRefundReference(GUID);

        if (addToCollection)
            owner.Session.CollectionMgr.AddItemAppearance(this);
    }

    public void SetOwnerGUID(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Owner), guid);
    }

    public void SetPaidExtendedCost(uint iece)
    {
        PaidExtendedCost = iece;
    }

    public void SetPaidMoney(ulong money)
    {
        PaidMoney = money;
    }

    public void SetPetitionId(uint petitionId)
    {
        var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, 0);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), petitionId);
    }

    public void SetPetitionNumSignatures(uint signatures)
    {
        var enchantmentField = Values.ModifyValue(ItemData).ModifyValue(ItemData.Enchantment, 0);
        SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), signatures);
    }

    public void SetRefundRecipient(ObjectGuid guid)
    {
        RefundRecipient = guid;
    }

    public void SetSlot(byte slot)
    {
        Slot = slot;
    }

    public void SetSoulboundTradeable(List<ObjectGuid> allowedLooters)
    {
        SetItemFlag(ItemFieldFlags.BopTradeable);
        _allowedGuiDs = allowedLooters;
    }

    public void SetSpellCharges(int index, int value)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(ItemData).ModifyValue(ItemData.SpellCharges, index), value);
    }

    public void SetState(ItemUpdateState state, Player forplayer = null)
    {
        if (State == ItemUpdateState.New && state == ItemUpdateState.Removed)
        {
            // pretend the item never existed
            if (forplayer)
            {
                RemoveItemFromUpdateQueueOf(this, forplayer);
                forplayer.DeleteRefundReference(GUID);
            }

            return;
        }

        if (state != ItemUpdateState.Unchanged)
        {
            // new items must stay in new state until saved
            if (State != ItemUpdateState.New)
                State = state;

            if (forplayer)
                AddItemToUpdateQueueOf(this, forplayer);
        }
        else
        {
            // unset in queue
            // the item must be removed from the queue manually
            QueuePos = -1;
            State = ItemUpdateState.Unchanged;
        }
    }

    public void SetText(string text)
    {
        Text = text;
    }

    public void UpdateDuration(Player owner, uint diff)
    {
        uint duration = ItemData.Expiration;

        if (duration == 0)
            return;

        Log.Logger.Debug("Item.UpdateDuration Item (Entry: {0} Duration {1} Diff {2})", Entry, duration, diff);

        if (duration <= diff)
        {
            var itemTemplate = Template;
            ScriptManager.RunScriptRet<IItemOnExpire>(p => p.OnExpire(owner, itemTemplate), itemTemplate.ScriptId);

            owner.DestroyItem(BagSlot, Slot, true);

            return;
        }

        SetExpiration(duration - diff);
        SetState(ItemUpdateState.Changed, owner); // save new time in database
    }
    public void UpdatePlayedTime(Player owner)
    {
        // Get current played time
        uint current_playtime = ItemData.CreatePlayedTime;
        // Calculate time elapsed since last played time update
        var curtime = GameTime.CurrentTime;
        var elapsed = (uint)(curtime - _lastPlayedTimeUpdate);
        var new_playtime = current_playtime + elapsed;

        // Check if the refund timer has expired yet
        if (new_playtime <= 2 * Time.HOUR)
        {
            // No? Proceed.
            // Update the data field
            SetCreatePlayedTime(new_playtime);
            // Flag as changed to get saved to DB
            SetState(ItemUpdateState.Changed, owner);
            // Speaks for itself
            _lastPlayedTimeUpdate = curtime;

            return;
        }

        // Yes
        SetNotRefundable(owner);
    }
    private static void AddItemToUpdateQueueOf(Item item, Player player)
    {
        if (item.IsInUpdateQueue)
            return;

        if (player.GUID != item.OwnerGUID)
        {
            Log.Logger.Error("Item.AddToUpdateQueueOf - Owner's guid ({0}) and player's guid ({1}) don't match!", item.OwnerGUID, player.GUID.ToString());

            return;
        }

        if (player.ItemUpdateQueueBlocked)
            return;

        player.ItemUpdateQueue.Add(item);
        item.QueuePos = player.ItemUpdateQueue.Count - 1;
    }

    private static uint GetBuyPrice(ItemTemplate proto, uint quality, uint itemLevel, out bool standardPrice)
    {
        standardPrice = true;

        if (proto.HasFlag(ItemFlags2.OverrideGoldCost))
            return proto.BuyPrice;

        var qualityPrice = CliDB.ImportPriceQualityStorage.LookupByKey(quality + 1);

        if (qualityPrice == null)
            return 0;

        var basePrice = CliDB.ItemPriceBaseStorage.LookupByKey(proto.BaseItemLevel);

        if (basePrice == null)
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
                var armorPrice = CliDB.ImportPriceArmorStorage.LookupByKey(inventoryType);

                if (armorPrice == null)
                    return 0;

                typeFactor = (ItemSubClassArmor)proto.SubClass switch
                {
                    ItemSubClassArmor.Miscellaneous => armorPrice.ClothModifier,
                    ItemSubClassArmor.Cloth         => armorPrice.ClothModifier,
                    ItemSubClassArmor.Leather       => armorPrice.LeatherModifier,
                    ItemSubClassArmor.Mail          => armorPrice.ChainModifier,
                    ItemSubClassArmor.Plate         => armorPrice.PlateModifier,
                    _                               => 1.0f
                };

                break;
            }
            case InventoryType.Shield:
            {
                var shieldPrice = CliDB.ImportPriceShieldStorage.LookupByKey(2); // it only has two rows, it's unclear which is the one used

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
            var weaponPrice = CliDB.ImportPriceWeaponStorage.LookupByKey(weapType + 1);

            if (weaponPrice == null)
                return 0;

            typeFactor = weaponPrice.Data;
        }

        standardPrice = false;

        return (uint)(proto.PriceVariance * typeFactor * baseFactor * qualityFactor * proto.PriceRandomValue);
    }

    private static ItemTransmogrificationWeaponCategory GetTransmogrificationWeaponCategory(ItemTemplate proto)
    {
        if (proto.Class == ItemClass.Weapon)
            switch ((ItemSubClassWeapon)proto.SubClass)
            {
                case ItemSubClassWeapon.Axe2:
                case ItemSubClassWeapon.Mace2:
                case ItemSubClassWeapon.Sword2:
                case ItemSubClassWeapon.Staff:
                case ItemSubClassWeapon.Polearm:
                    return ItemTransmogrificationWeaponCategory.Melee2H;
                case ItemSubClassWeapon.Bow:
                case ItemSubClassWeapon.Gun:
                case ItemSubClassWeapon.Crossbow:
                    return ItemTransmogrificationWeaponCategory.Ranged;
                case ItemSubClassWeapon.Axe:
                case ItemSubClassWeapon.Mace:
                case ItemSubClassWeapon.Sword:
                case ItemSubClassWeapon.Warglaives:
                    return ItemTransmogrificationWeaponCategory.AxeMaceSword1H;
                case ItemSubClassWeapon.Dagger:
                    return ItemTransmogrificationWeaponCategory.Dagger;
                case ItemSubClassWeapon.Fist:
                    return ItemTransmogrificationWeaponCategory.Fist;
                
            }

        return ItemTransmogrificationWeaponCategory.Invalid;
    }

    private static bool HasStats(ItemInstance itemInstance, BonusData bonus)
    {
        for (byte i = 0; i < ItemConst.MaxStats; ++i)
            if (bonus.StatPercentEditor[i] != 0)
                return true;

        return false;
    }

    private void AddArtifactPower(ArtifactPowerData artifactPower)
    {
        var index = _artifactPowerIdToIndex.Count;
        _artifactPowerIdToIndex[artifactPower.ArtifactPowerId] = (ushort)index;

        ArtifactPower powerField = new()
        {
            ArtifactPowerId = (ushort)artifactPower.ArtifactPowerId,
            PurchasedRank = artifactPower.PurchasedRank,
            CurrentRankWithBonus = artifactPower.CurrentRankWithBonus
        };

        AddDynamicUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers), powerField);
    }

    private void ApplyArtifactPowerEnchantmentBonuses(EnchantmentSlot slot, uint enchantId, bool apply, Player owner)
    {
        var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);

        if (enchant != null)
            for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                switch (enchant.Effect[i])
                {
                    case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                    {
                        for (var artifactPowerIndex = 0; artifactPowerIndex < ItemData.ArtifactPowers.Size(); ++artifactPowerIndex)
                        {
                            var artifactPower = ItemData.ArtifactPowers[artifactPowerIndex];

                            if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label == enchant.EffectArg[i])
                            {
                                var newRank = artifactPower.CurrentRankWithBonus;

                                if (apply)
                                    newRank += (byte)enchant.EffectPointsMin[i];
                                else
                                    newRank -= (byte)enchant.EffectPointsMin[i];

                                artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, artifactPowerIndex);
                                SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                                if (IsEquipped)
                                {
                                    var artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));

                                    if (artifactPowerRank != null)
                                        owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                                }
                            }
                        }
                    }

                    break;
                    case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                    {
                        var artifactPowerIndex = _artifactPowerIdToIndex.LookupByKey(enchant.EffectArg[i]);

                        if (artifactPowerIndex != 0)
                        {
                            var newRank = ItemData.ArtifactPowers[artifactPowerIndex].CurrentRankWithBonus;

                            if (apply)
                                newRank += (byte)enchant.EffectPointsMin[i];
                            else
                                newRank -= (byte)enchant.EffectPointsMin[i];

                            ArtifactPower artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, artifactPowerIndex);
                            SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                            if (IsEquipped)
                            {
                                var artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(ItemData.ArtifactPowers[artifactPowerIndex].ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));

                                if (artifactPowerRank != null)
                                    owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                            }
                        }
                    }

                    break;
                    case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                        if (slot is >= EnchantmentSlot.Sock1 and <= EnchantmentSlot.Sock3 && BonusData.GemRelicType[slot - EnchantmentSlot.Sock1] != -1)
                        {
                            var artifactPowerPicker = CliDB.ArtifactPowerPickerStorage.LookupByKey(enchant.EffectArg[i]);

                            if (artifactPowerPicker != null)
                            {
                                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactPowerPicker.PlayerConditionID);

                                if (playerCondition == null || ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                                    for (var artifactPowerIndex = 0; artifactPowerIndex < ItemData.ArtifactPowers.Size(); ++artifactPowerIndex)
                                    {
                                        var artifactPower = ItemData.ArtifactPowers[artifactPowerIndex];

                                        if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label == BonusData.GemRelicType[slot - EnchantmentSlot.Sock1])
                                        {
                                            var newRank = artifactPower.CurrentRankWithBonus;

                                            if (apply)
                                                newRank += (byte)enchant.EffectPointsMin[i];
                                            else
                                                newRank -= (byte)enchant.EffectPointsMin[i];

                                            artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, artifactPowerIndex);
                                            SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                                            if (IsEquipped)
                                            {
                                                var artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));

                                                if (artifactPowerRank != null)
                                                    owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                                            }
                                        }
                                    }
                            }
                        }

                        break;
                    
                }
    }

    private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        UpdateMask valuesMask = new((int)TypeId.Max);

        if (requestedObjectMask.IsAnySet())
            valuesMask.Set((int)TypeId.Object);

        ItemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);

        if (requestedItemMask.IsAnySet())
            valuesMask.Set((int)TypeId.Item);

        WorldPacket buffer = new();
        buffer.WriteUInt32(valuesMask.GetBlock(0));

        if (valuesMask[(int)TypeId.Object])
            ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

        if (valuesMask[(int)TypeId.Item])
            ItemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

        WorldPacket buffer1 = new();
        buffer1.WriteUInt8((byte)UpdateType.Values);
        buffer1.WritePackedGuid(GUID);
        buffer1.WriteUInt32(buffer.GetSize());
        buffer1.WriteBytes(buffer.GetData());

        data.AddUpdateBlock(buffer1);
    }

    private uint GetBuyPrice(Player owner, out bool standardPrice)
    {
        return GetBuyPrice(Template, (uint)Quality, GetItemLevel(owner), out standardPrice);
    }

    private uint GetEnchantRequiredLevel()
    {
        uint level = 0;

        // Check all enchants for required level
        for (var enchant_slot = EnchantmentSlot.Perm; enchant_slot < EnchantmentSlot.Max; ++enchant_slot)
        {
            var enchant_id = GetEnchantmentId(enchant_slot);

            if (enchant_id != 0)
            {
                var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

                if (enchantEntry != null)
                    if (enchantEntry.MinLevel > level)
                        level = enchantEntry.MinLevel;
            }
        }

        return level;
    }

    private bool HasEnchantRequiredSkill(Player player)
    {
        // Check all enchants for required skill
        for (var enchant_slot = EnchantmentSlot.Perm; enchant_slot < EnchantmentSlot.Max; ++enchant_slot)
        {
            var enchant_id = GetEnchantmentId(enchant_slot);

            if (enchant_id != 0)
            {
                var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

                if (enchantEntry != null)
                    if (enchantEntry.RequiredSkillID != 0 && player.GetSkillValue((SkillType)enchantEntry.RequiredSkillID) < enchantEntry.RequiredSkillRank)
                        return false;
            }
        }

        return true;
    }
    private bool HasStats()
    {
        var proto = Template;
        var owner = OwnerUnit;

        for (byte i = 0; i < ItemConst.MaxStats; ++i)
            if ((owner ? GetItemStatValue(i, owner) : proto.GetStatPercentEditor(i)) != 0)
                return true;

        return false;
    }

    private bool IsBoundByEnchant()
    {
        // Check all enchants for soulbound
        for (var enchant_slot = EnchantmentSlot.Perm; enchant_slot < EnchantmentSlot.Max; ++enchant_slot)
        {
            var enchant_id = GetEnchantmentId(enchant_slot);

            if (enchant_id != 0)
            {
                var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

                if (enchantEntry != null)
                    if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
                        return true;
            }
        }

        return false;
    }
    private bool IsValidTransmogrificationTarget()
    {
        var proto = Template;

        if (proto == null)
            return false;

        if (proto.Class != ItemClass.Armor &&
            proto.Class != ItemClass.Weapon)
            return false;

        if (proto.Class == ItemClass.Weapon && proto.SubClass == (uint)ItemSubClassWeapon.FishingPole)
            return false;

        if (proto.HasFlag(ItemFlags2.NoAlterItemVisual))
            return false;

        if (!HasStats())
            return false;

        return true;
    }
    private void SetExpiration(uint expiration)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Expiration), expiration);
    }

    private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
    {
        private readonly ItemData _itemMask = new();
        private readonly ObjectFieldData _objectMask = new();
        private readonly Item _owner;
        public ValuesUpdateForPlayerWithMaskSender(Item owner)
        {
            _owner = owner;
        }

        public void Invoke(Player player)
        {
            UpdateData udata = new(_owner.Location.MapId);

            _owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _itemMask.GetUpdateMask(), player);

            udata.BuildPacket(out var packet);
            player.SendPacket(packet);
        }
    }
}