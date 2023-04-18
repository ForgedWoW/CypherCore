// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Artifact;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
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

    public Item(ClassFactory classFactory, ItemFactory itemFactory, DB2Manager db2Manager, PlayerComputators playerComputators, CharacterDatabase characterDatabase, LootItemStorage lootItemStorage,
                ItemEnchantmentManager itemEnchantmentManager) : base(false, classFactory)
    {
        DB2Manager = db2Manager;
        PlayerComputators = playerComputators;
        ObjectTypeMask |= TypeMask.Item;
        ObjectTypeId = TypeId.Item;

        ItemData = new ItemData();

        State = ItemUpdateState.New;
        QueuePos = -1;
        _lastPlayedTimeUpdate = GameTime.CurrentTime;
        ItemFactory = itemFactory;
        CharacterDatabase = characterDatabase;
        LootItemStorage = lootItemStorage;
        ItemEnchantmentManager = itemEnchantmentManager;
    }

    public uint AppearanceModId => ItemData.ItemAppearanceModID;
    public AzeriteEmpoweredItem AsAzeriteEmpoweredItem => this as AzeriteEmpoweredItem;
    public AzeriteItem AsAzeriteItem => this as AzeriteItem;
    public Bag AsBag => this as Bag;
    public byte BagSlot => Container?.Slot ?? InventorySlots.Bag0;
    public ItemBondingType Bonding => BonusData.Bonding;
    public BonusData BonusData { get; set; }
    public List<uint> BonusListIDs => ItemData.ItemBonusKey.Value.BonusListIDs;
    public CharacterDatabase CharacterDatabase { get; }
    public ObjectGuid ChildItem { get; private set; }
    public ObjectGuid ContainedIn => ItemData.ContainedIn;
    public Bag Container { get; private set; }
    public ItemContext Context => (ItemContext)(int)ItemData.Context;
    public uint Count => ItemData.StackCount;
    public ObjectGuid Creator => ItemData.Creator;
    public DB2Manager DB2Manager { get; }
    public ItemEffectRecord[] Effects => BonusData.Effects[..BonusData.EffectCount];
    public ObjectGuid GiftCreator => ItemData.GiftCreator;
    public bool IsAzeriteEmpoweredItem => TypeId == TypeId.AzeriteEmpoweredItem;
    public bool IsAzeriteItem => TypeId == TypeId.AzeriteItem;
    public bool IsBag => Template.InventoryType == InventoryType.Bag;
    public bool IsBattlenetAccountBound => Template.HasFlag(ItemFlags2.BnetAccountTradeOk);
    public bool IsBopTradeable => HasItemFlag(ItemFieldFlags.BopTradeable);
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
    public bool IsRefundExpired => PlayedTime > 2 * Time.HOUR;
    public bool IsSoulBound => HasItemFlag(ItemFieldFlags.Soulbound);
    public bool IsVellum => Template.IsVellum;
    public bool IsWrapped => HasItemFlag(ItemFieldFlags.Wrapped);
    public ItemData ItemData { get; set; }
    public ItemEnchantmentManager ItemEnchantmentManager { get; }
    public ItemFactory ItemFactory { get; }
    public uint ItemRandomBonusListId { get; private set; }
    public Loot Loot { get; set; }
    public bool LootGenerated { get; set; }
    public LootItemStorage LootItemStorage { get; }
    public uint MaxStackCount => Template.MaxStackSize;
    public override ObjectGuid OwnerGUID => ItemData.Owner;
    public override Player OwnerUnit => ObjectAccessor.FindPlayer(OwnerGUID);
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

    public PlayerComputators PlayerComputators { get; }
    public ushort Pos => (ushort)(BagSlot << 8 | Slot);
    public ItemQuality Quality => BonusData.Quality;
    public int QueuePos { get; set; }
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
    public ItemTemplate Template => GameObjectManager.GetItemTemplate(Entry);
    public string Text { get; private set; }
    private bool IsInBag => Container != null;
    //Static

    public void AddBonuses(uint bonusListID)
    {
        var bonusListIDs = BonusListIDs;

        if (bonusListIDs.Contains(bonusListID))
            return;

        var bonuses = DB2Manager.GetItemBonusList(bonusListID);

        if (bonuses == null)
            return;

        ItemBonusKey itemBonusKey = new()
        {
            ItemID = Entry,
            BonusListIDs = BonusListIDs
        };

        itemBonusKey.BonusListIDs.Add(bonusListID);
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemBonusKey), itemBonusKey);

        foreach (var bonus in bonuses)
            BonusData.AddBonus(bonus.BonusType, bonus.Value);

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemAppearanceModID), (byte)BonusData.AppearanceModID);
    }

    public override bool AddToObjectUpdate()
    {
        var owner = OwnerUnit;

        if (owner == null)
            return false;

        owner.Location.Map.AddUpdateObject(this);

        return true;
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

        if (!CliDB.DurabilityCostsStorage.TryGetValue(GetItemLevel(OwnerUnit), out var durabilityCost))
            return 0;

        var durabilityQualityEntryId = ((uint)Quality + 1) * 2;

        if (!CliDB.DurabilityQualityStorage.TryGetValue(durabilityQualityEntryId, out var durabilityQualityEntry))
            return 0;

        uint dmultiplier = itemTemplate.Class switch
        {
            ItemClass.Weapon => durabilityCost.WeaponSubClassCost[itemTemplate.SubClass],
            ItemClass.Armor => durabilityCost.ArmorSubClassCost[itemTemplate.SubClass],
            _ => 0
        };

        var cost = (ulong)Math.Round(lostDurability * dmultiplier * durabilityQualityEntry.Data * RepairCostMultiplier);
        cost = (ulong)(cost * discount * Configuration.GetDefaultValue("Rate:RepairCost", 1.0f));

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
        return Count >= proto.MaxStackSize ? InventoryResult.CantStack : InventoryResult.Ok;
    }

    public bool CanBeTraded(bool mail = false, bool trade = false)
    {
        if (LootGenerated)
            return false;

        if ((!mail || !IsBoundAccountWide) && IsSoulBound && (!IsBopTradeable || !trade))
            return false;

        if (IsBag && (PlayerComputators.IsBagPos(Pos) || !AsBag.IsEmpty()))
            return false;

        var owner = OwnerUnit;

        if (owner == null)
            return !IsBoundByEnchant();

        if (owner.CanUnequipItem(Pos, false) != InventoryResult.Ok)
            return false;

        if (owner.GetLootGUID() == GUID)
            return false;

        return !IsBoundByEnchant();
    }

    public void CheckArtifactRelicSlotUnlock(Player owner)
    {
        if (owner == null)
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
        if (ItemData.CreatePlayedTime + 2 * Time.HOUR >= OwnerUnit.TotalPlayedTime)
            return false;

        ClearSoulboundTradeable(OwnerUnit);

        return true; // remove from tradeable list
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
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_BOP_TRADE);
        stmt.AddValue(0, GUID.Counter);
        CharacterDatabase.Execute(stmt);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(ItemData);
        base.ClearUpdateMask(remove);
    }

    public Item CloneItem(uint count, Player player = null)
    {
        var newItem = ItemFactory.CreateItem(Entry, count, Context, player);

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

        if (owner != null)
        {
            SetOwnerGUID(owner.GUID);
            SetContainedIn(owner.GUID);
        }

        var itemProto = GameObjectManager.GetItemTemplate(itemId);

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

        if (itemProto.ArtifactID == 0)
            return true;

        InitArtifactPowers(itemProto.ArtifactID, 0);

        foreach (var artifactAppearance in CliDB.ArtifactAppearanceStorage.Values)
            if (CliDB.ArtifactAppearanceSetStorage.TryGetValue(artifactAppearance.ArtifactAppearanceSetID, out var artifactAppearanceSet))
            {
                if (itemProto.ArtifactID != artifactAppearanceSet.ArtifactID)
                    continue;

                if (CliDB.PlayerConditionStorage.TryGetValue(artifactAppearance.UnlockPlayerConditionID, out var playerCondition))
                    if (owner == null || !ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                        continue;

                SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearance.Id);
                SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);

                break;
            }

        CheckArtifactRelicSlotUnlock(owner ?? OwnerUnit);

        return true;
    }

    public virtual void DeleteFromDB(SQLTransaction trans)
    {
        ItemFactory.DeleteFromDB(trans, GUID.Counter);

        // Delete the items if this is a container
        if (Loot != null && !Loot.IsLooted())
            LootItemStorage.RemoveStoredLootForContainer(GUID.Counter);
    }

    public void DeleteFromInventoryDB(SQLTransaction trans)
    {
        ItemFactory.DeleteFromInventoryDB(trans, GUID.Counter);
    }

    public void DeleteRefundDataFromDB(SQLTransaction trans = null)
    {
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
        stmt.AddValue(0, GUID.Counter);

        if (trans != null)
            trans.Append(stmt);
        else
            CharacterDatabase.Execute(stmt);
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
            var socketColor = Template.GetSocketColor(gemSlot);

            if (socketColor == 0) // no socket slot
                continue;

            SocketColor gemColor = 0;

            var gemProto = GameObjectManager.GetItemTemplate(gemData.ItemId);

            if (gemProto != null)
                if (CliDB.GemPropertiesStorage.TryGetValue(gemProto.GemProperties, out var gemProperty))
                    gemColor = gemProperty.Type;

            if (!gemColor.HasAnyFlag(ItemConst.SocketColorToGemTypeMask[(int)socketColor])) // bad gem color on this socket
                return false;
        }

        return true;
    }

    public ArtifactPower GetArtifactPower(uint artifactPowerId)
    {
        return _artifactPowerIdToIndex.TryGetValue(artifactPowerId, out var index) ? ItemData.ArtifactPowers[index] : null;
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nOwner: {OwnerGUID} Count: {Count} BagSlot: {BagSlot} Slot: {Slot} Equipped: {IsEquipped}";
    }

    public ItemDisenchantLootRecord GetDisenchantLoot(Player owner)
    {
        return !BonusData.CanDisenchant ? null : ItemFactory.GetDisenchantLoot(Template, (uint)Quality, GetItemLevel(owner));
    }

    public uint GetDisplayId(Player owner)
    {
        var itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (itemModifiedAppearanceId == 0)
            itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

        if (!CliDB.ItemModifiedAppearanceStorage.TryGetValue(itemModifiedAppearanceId, out var transmog))
            return DB2Manager.GetItemDisplayId(Entry, AppearanceModId);

        if (CliDB.ItemAppearanceStorage.TryGetValue((uint)transmog.ItemAppearanceID, out var itemAppearance))
            return itemAppearance.ItemDisplayInfoID;

        return DB2Manager.GetItemDisplayId(Entry, AppearanceModId);
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

    public byte GetGemCountWithID(uint gemID)
    {
        var list = (List<SocketedGem>)ItemData.Gems;

        return (byte)list.Count(gemData => gemData.ItemId == gemID);
    }

    public byte GetGemCountWithLimitCategory(uint limitCategory)
    {
        var list = (List<SocketedGem>)ItemData.Gems;

        return (byte)list.Count(gemData =>
        {
            var gemProto = GameObjectManager.GetItemTemplate(gemData.ItemId);

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

        return ItemFactory.GetItemLevel(itemTemplate,
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
        return DB2Manager.GetItemModifiedAppearance(Entry, BonusData.AppearanceModID);
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

    public override Loot GetLootForPlayer(Player player)
    {
        return Loot;
    }

    public uint GetModifier(ItemModifier modifier)
    {
        var modifierIndex = ItemData.Modifiers.Value.Values.FindIndexIf(mod => mod.Type == (byte)modifier);

        return modifierIndex != -1 ? ItemData.Modifiers.Value.Values[modifierIndex].Value : 0;
    }

    public override string GetName(Locale locale = Locale.enUS)
    {
        var itemTemplate = Template;

        return CliDB.ItemNameDescriptionStorage.TryGetValue(BonusData.Suffix, out var suffix) ? $"{itemTemplate.GetName(locale)} {suffix.Description[locale]}" : itemTemplate.GetName(locale);
    }

    public int GetRequiredLevel()
    {
        var fixedLevel = (int)GetModifier(ItemModifier.TimewalkerLevel);

        if (BonusData.RequiredLevelCurve != 0)
            return (int)DB2Manager.GetCurveValueAt(BonusData.RequiredLevelCurve, fixedLevel);

        if (BonusData.RequiredLevelOverride != 0)
            return BonusData.RequiredLevelOverride;

        if (BonusData.HasFixedLevel && BonusData.PlayerLevelToItemLevelCurveId != 0)
            return fixedLevel;

        return BonusData.RequiredLevel;
    }

    public uint GetSellPrice(Player owner)
    {
        return ItemFactory.GetSellPrice(Template, (uint)Quality, GetItemLevel(owner));
    }

    public SocketColor GetSocketColor(uint index)
    {
        return BonusData.SocketColor[index];
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

    public ItemTransmogrificationWeaponCategory GetTransmogrificationWeaponCategory(ItemTemplate proto)
    {
        if (proto.Class != ItemClass.Weapon)
            return ItemTransmogrificationWeaponCategory.Invalid;

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

    public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
    {
        return target.GUID == OwnerGUID ? UpdateFieldFlag.Owner : UpdateFieldFlag.None;
    }

    public ushort GetVisibleAppearanceModId(Player owner)
    {
        var itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);

        if (itemModifiedAppearanceId == 0)
            itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

        if (CliDB.ItemModifiedAppearanceStorage.TryGetValue(itemModifiedAppearanceId, out var transmog))
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

        return CliDB.ItemModifiedAppearanceStorage.TryGetValue(itemModifiedAppearanceId, out var transmog) ? transmog.ItemID : Entry;
    }

    public ushort GetVisibleItemVisual(Player owner)
    {
        return CliDB.SpellItemEnchantmentStorage.TryGetValue(GetVisibleEnchantmentId(owner), out var enchant) ? enchant.ItemVisual : (ushort)0;
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

        if (owner == null)
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
                >= 50 => 5 * (amount / 5),
                _ => amount
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

    public override bool HasInvolvedQuest(uint questID)
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

    public override bool HasQuest(uint questID)
    {
        return Template.StartQuest == questID;
    }

    public void InitArtifactPowers(byte artifactId, byte artifactTier)
    {
        foreach (var artifactPower in DB2Manager.GetArtifactPowers(artifactId))
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
        if (CliDB.ArtifactStorage.TryGetValue(Template.ArtifactID, out var artifact))
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

        if (IsBopTradeable)
            if (_allowedGuiDs.Contains(player.GUID))
                return false;

        // BOA item case
        return !IsBoundAccountWide;
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

        if (!isEnchantSpell || spellInfo.EquippedItemInventoryTypeMask == 0) // 0 == any inventory type
            return true;

        // Special case - accept weapon type for main and offhand requirements
        if (proto.InventoryType == InventoryType.Weapon &&
            Convert.ToBoolean(spellInfo.EquippedItemInventoryTypeMask & (1 << (int)InventoryType.WeaponMainhand)) ||
            Convert.ToBoolean(spellInfo.EquippedItemInventoryTypeMask & (1 << (int)InventoryType.WeaponOffhand)))
            return true;

        return (spellInfo.EquippedItemInventoryTypeMask & (1 << (int)proto.InventoryType)) != 0;
        // inventory type not present in mask
    }

    public bool IsLimitedToAnotherMapOrZone(uint curMapId, uint curZoneId)
    {
        var proto = Template;

        return proto != null &&
               ((proto.Map != 0 && proto.Map != curMapId) ||
                (proto.GetArea(0) != 0 && proto.GetArea(0) != curZoneId && proto.GetArea(1) != 0 && proto.GetArea(1) != curZoneId));
    }

    public bool IsValidTransmogrificationTarget()
    {
        var proto = Template;

        if (proto == null)
            return false;

        if (proto.Class != ItemClass.Armor &&
            proto.Class != ItemClass.Weapon)
            return false;

        if (proto.Class == ItemClass.Weapon && proto.SubClass == (uint)ItemSubClassWeapon.FishingPole)
            return false;

        return !proto.HasFlag(ItemFlags2.NoAlterItemVisual) && HasStats();
    }

    public void LoadArtifactData(Player owner, ulong xp, uint artifactAppearanceId, uint artifactTier, List<ArtifactPowerData> powers)
    {
        for (byte i = 0; i <= artifactTier; ++i)
            InitArtifactPowers(Template.ArtifactID, i);

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactXP), xp);
        SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearanceId);
        SetModifier(ItemModifier.ArtifactTier, artifactTier);

        if (CliDB.ArtifactAppearanceStorage.TryGetValue(artifactAppearanceId, out var artifactAppearance))
            SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);

        byte totalPurchasedRanks = 0;

        foreach (var power in powers)
        {
            power.CurrentRankWithBonus += power.PurchasedRank;
            totalPurchasedRanks += power.PurchasedRank;

            var artifactPower = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);

            for (var e = EnchantmentSlot.Sock1; e <= EnchantmentSlot.Sock3; ++e)
                if (CliDB.SpellItemEnchantmentStorage.TryGetValue(GetEnchantmentId(e), out var enchant))
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
                                    if (CliDB.ArtifactPowerPickerStorage.TryGetValue(enchant.EffectArg[i], out var artifactPowerPicker))
                                    {
                                        var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactPowerPicker.PlayerConditionID);

                                        if (playerCondition == null || (owner != null && ConditionManager.IsPlayerMeetingCondition(owner, playerCondition)))
                                            if (artifactPower.Label == BonusData.GemRelicType[e - EnchantmentSlot.Sock1])
                                                power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                    }

                                break;
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
        var needSave = false;
        var creator = fields.Read<ulong>(2);

        if (creator != 0)
            SetCreator(!Convert.ToBoolean(itemFlags & (int)ItemFieldFlags.Child) ? ObjectGuid.Create(HighGuid.Player, creator) : ObjectGuid.Create(HighGuid.Item, creator));

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
            needSave = true;
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
            needSave = true;
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
            needSave = true;
        }

        if (!needSave) // normal item changed state set not work at loading
            return true;

        byte index = 0;
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_ON_LOAD);
        stmt.AddValue(index++, (uint)ItemData.Expiration);
        stmt.AddValue(index++, (uint)ItemData.DynamicFlags);
        stmt.AddValue(index++, (uint)ItemData.Durability);
        stmt.AddValue(index++, guid);
        CharacterDatabase.Execute(stmt);

        return true;
    }

    public override void RemoveFromObjectUpdate()
    {
        OwnerUnit?.Location.Map.RemoveUpdateObject(this);
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

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_REFUND_INSTANCE);
        stmt.AddValue(0, GUID.Counter);
        stmt.AddValue(1, RefundRecipient.Counter);
        stmt.AddValue(2, PaidMoney);
        stmt.AddValue(3, (ushort)PaidExtendedCost);
        CharacterDatabase.Execute(stmt);
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
                stmt = CharacterDatabase.GetPreparedStatement(State == ItemUpdateState.New ? CharStatements.REP_ITEM_INSTANCE : CharStatements.UPD_ITEM_INSTANCE);
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

                foreach (int bonusListID in BonusListIDs)
                    ss.Append($"{bonusListID} ");

                stmt.AddValue(++index, ss.ToString());
                stmt.AddValue(++index, GUID.Counter);

                CharacterDatabase.Execute(stmt);

                if (State == ItemUpdateState.Changed && IsWrapped)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GIFT_OWNER);
                    stmt.AddValue(0, OwnerGUID.Counter);
                    stmt.AddValue(1, GUID.Counter);
                    CharacterDatabase.Execute(stmt);
                }

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (ItemData.Gems.Size() != 0)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_GEMS);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (transmogMods.Any(modifier => GetModifier(modifier) != 0))
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_TRANSMOG);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (Template.ArtifactID != 0)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, (ulong)ItemData.ArtifactXP);
                    stmt.AddValue(2, GetModifier(ItemModifier.ArtifactAppearanceId));
                    stmt.AddValue(3, GetModifier(ItemModifier.ArtifactTier));
                    trans.Append(stmt);

                    foreach (var artifactPower in ItemData.ArtifactPowers)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT_POWERS);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (modifiersTable.Any(modifier => GetModifier(modifier) != 0))
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_MODIFIERS);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, GetModifier(ItemModifier.TimewalkerLevel));
                    stmt.AddValue(2, GetModifier(ItemModifier.ArtifactKnowledgeLevel));
                    trans.Append(stmt);
                }

                break;
            }
            case ItemUpdateState.Removed:
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                stmt.AddValue(0, GUID.Counter);
                trans.Append(stmt);

                if (IsWrapped)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
                    stmt.AddValue(0, GUID.Counter);
                    trans.Append(stmt);
                }

                // Delete the items if this is a container
                if (Loot != null && !Loot.IsLooted())
                    LootItemStorage.RemoveStoredLootForContainer(GUID.Counter);

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
        if (!_artifactPowerIdToIndex.TryGetValue(artifactPowerId, out var foundIndex))
            return;

        ArtifactPower artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, foundIndex);
        SetUpdateFieldValue(ref artifactPower.PurchasedRank, purchasedRank);
        SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, currentRankWithBonus);
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
        bonusListIDs ??= new List<uint>();

        ItemBonusKey itemBonusKey = new()
        {
            ItemID = Entry,
            BonusListIDs = bonusListIDs
        };

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.ItemBonusKey), itemBonusKey);

        foreach (var bonusListID in BonusListIDs)
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

        var tradeData = OwnerUnit?.TradeData;

        if (tradeData == null)
            return;

        var slot = tradeData.GetTradeSlotForItem(GUID);

        if (slot != TradeSlots.Invalid)
            tradeData.SetItem(slot, this, true);
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
        if (GetEnchantmentId(slot) == id && GetEnchantmentDuration(slot) == duration && GetEnchantmentCharges(slot) == charges)
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

        if (BonusData.PlayerLevelToItemLevelCurveId == 0)
            return;

        var levels = DB2Manager.GetContentTuningData(BonusData.ContentTuningId, 0, true);

        if (levels.HasValue)
            level = (uint)Math.Min(Math.Max((short)level, levels.Value.MinLevel), levels.Value.MaxLevel);

        SetModifier(ItemModifier.TimewalkerLevel, level);
    }

    public void SetGem(ushort slot, ItemDynamicFieldGems gem, uint gemScalingLevel)
    {
        //ASSERT(slot < MAX_GEM_SOCKETS);
        _gemScalingLevels[slot] = gemScalingLevel;
        BonusData.GemItemLevelBonus[slot] = 0;
        var gemTemplate = GameObjectManager.GetItemTemplate(gem.ItemId);

        if (gemTemplate != null)
            if (CliDB.GemPropertiesStorage.TryGetValue(gemTemplate.GemProperties, out var gemProperties))
                if (CliDB.SpellItemEnchantmentStorage.TryGetValue(gemProperties.EnchantId, out var gemEnchant))
                {
                    BonusData gemBonus = new(gemTemplate);

                    foreach (var bonusListId in gem.BonusListIDs)
                        gemBonus.AddBonusList(bonusListId);

                    var gemBaseItemLevel = gemTemplate.BaseItemLevel;

                    if (gemBonus.PlayerLevelToItemLevelCurveId != 0)
                    {
                        var scaledIlvl = (uint)DB2Manager.GetCurveValueAt(gemBonus.PlayerLevelToItemLevelCurveId, gemScalingLevel);

                        if (scaledIlvl != 0)
                            gemBaseItemLevel = scaledIlvl;
                    }

                    BonusData.GemRelicType[slot] = gemBonus.RelicType;

                    for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                        switch (gemEnchant.Effect[i])
                        {
                            case ItemEnchantmentType.BonusListID:
                            {
                                var bonusesEffect = DB2Manager.GetItemBonusList(gemEnchant.EffectArg[i]);

                                if (bonusesEffect != null)
                                    foreach (var itemBonus in bonusesEffect.Where(itemBonus => itemBonus.BonusType == ItemBonusType.ItemLevel))
                                        BonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];

                                break;
                            }
                            case ItemEnchantmentType.BonusListCurve:
                            {
                                var artifactrBonusListId = DB2Manager.GetItemBonusListForItemLevelDelta((short)DB2Manager.GetCurveValueAt((uint)Curves.ArtifactRelicItemLevelBonus, gemBaseItemLevel + gemBonus.ItemLevelBonus));

                                if (artifactrBonusListId != 0)
                                {
                                    var bonusesEffect = DB2Manager.GetItemBonusList(artifactrBonusListId);

                                    if (bonusesEffect != null)
                                        foreach (var itemBonus in bonusesEffect.Where(itemBonus => itemBonus.BonusType == ItemBonusType.ItemLevel))
                                            BonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];
                                }

                                break;
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
            if (forplayer == null)
                return;

            ItemFactory.RemoveItemFromUpdateQueueOf(this, forplayer);
            forplayer.DeleteRefundReference(GUID);

            return;
        }

        if (state != ItemUpdateState.Unchanged)
        {
            // new items must stay in new state until saved
            if (State != ItemUpdateState.New)
                State = state;

            if (forplayer != null)
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
        uint currentPlaytime = ItemData.CreatePlayedTime;
        // Calculate time elapsed since last played time update
        var curtime = GameTime.CurrentTime;
        var elapsed = (uint)(curtime - _lastPlayedTimeUpdate);
        var newPlaytime = currentPlaytime + elapsed;

        // Check if the refund timer has expired yet
        if (newPlaytime <= 2 * Time.HOUR)
        {
            // No? Proceed.
            // Update the data field
            SetCreatePlayedTime(newPlaytime);
            // Flag as changed to get saved to DB
            SetState(ItemUpdateState.Changed, owner);
            // Speaks for itself
            _lastPlayedTimeUpdate = curtime;

            return;
        }

        // Yes
        SetNotRefundable(owner);
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

    private void AddItemToUpdateQueueOf(Item item, Player player)
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

    private void ApplyArtifactPowerEnchantmentBonuses(EnchantmentSlot slot, uint enchantId, bool apply, Player owner)
    {
        if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(enchantId, out var enchant))
            return;

        for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
            switch (enchant.Effect[i])
            {
                case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                {
                    for (var artifactPowerIndex = 0; artifactPowerIndex < ItemData.ArtifactPowers.Size(); ++artifactPowerIndex)
                    {
                        var artifactPower = ItemData.ArtifactPowers[artifactPowerIndex];

                        if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label != enchant.EffectArg[i])
                            continue;

                        var newRank = artifactPower.CurrentRankWithBonus;

                        if (apply)
                            newRank += (byte)enchant.EffectPointsMin[i];
                        else
                            newRank -= (byte)enchant.EffectPointsMin[i];

                        artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, artifactPowerIndex);
                        SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                        if (!IsEquipped)
                            continue;

                        var artifactPowerRank = DB2Manager.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));

                        if (artifactPowerRank != null)
                            owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                    }
                }

                break;

                case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                {
                    if (_artifactPowerIdToIndex.TryGetValue(enchant.EffectArg[i], out var artifactPowerIndex))
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
                            var artifactPowerRank = DB2Manager.GetArtifactPowerRank(ItemData.ArtifactPowers[artifactPowerIndex].ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));

                            if (artifactPowerRank != null)
                                owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                        }
                    }
                }

                break;

                case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                    if (slot is >= EnchantmentSlot.Sock1 and <= EnchantmentSlot.Sock3 && BonusData.GemRelicType[slot - EnchantmentSlot.Sock1] != -1)
                        if (CliDB.ArtifactPowerPickerStorage.TryGetValue(enchant.EffectArg[i], out var artifactPowerPicker))
                        {
                            var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactPowerPicker.PlayerConditionID);

                            if (playerCondition == null || ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                                for (var artifactPowerIndex = 0; artifactPowerIndex < ItemData.ArtifactPowers.Size(); ++artifactPowerIndex)
                                {
                                    var artifactPower = ItemData.ArtifactPowers[artifactPowerIndex];

                                    if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label != BonusData.GemRelicType[slot - EnchantmentSlot.Sock1])
                                        continue;

                                    var newRank = artifactPower.CurrentRankWithBonus;

                                    if (apply)
                                        newRank += (byte)enchant.EffectPointsMin[i];
                                    else
                                        newRank -= (byte)enchant.EffectPointsMin[i];

                                    artifactPower = Values.ModifyValue(ItemData).ModifyValue(ItemData.ArtifactPowers, artifactPowerIndex);
                                    SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                                    if (!IsEquipped)
                                        continue;

                                    var artifactPowerRank = DB2Manager.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));

                                    if (artifactPowerRank != null)
                                        owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                                }
                        }

                    break;
            }
    }

    private bool HasStats()
    {
        var proto = Template;
        var owner = OwnerUnit;

        for (byte i = 0; i < ItemConst.MaxStats; ++i)
            if ((owner != null ? GetItemStatValue(i, owner) : proto.GetStatPercentEditor(i)) != 0)
                return true;

        return false;
    }

    private bool IsBoundByEnchant()
    {
        // Check all enchants for soulbound
        for (var enchantSlot = EnchantmentSlot.Perm; enchantSlot < EnchantmentSlot.Max; ++enchantSlot)
        {
            var enchantID = GetEnchantmentId(enchantSlot);

            if (enchantID != 0)
                if (CliDB.SpellItemEnchantmentStorage.TryGetValue(enchantID, out var enchantEntry))
                    if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
                        return true;
        }

        return false;
    }

    private void SetExpiration(uint expiration)
    {
        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.Expiration), expiration);
    }
}