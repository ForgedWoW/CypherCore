// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Loot;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class Loot
{
    public uint Gold;
    public List<LootItem> Items = new();
    public LootType LootType;
    public ObjectGuid RoundRobinPlayer;
    public byte UnlootedCount;
    private readonly List<ObjectGuid> _allowedLooters = new();

    // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
    // required for achievement system
    private readonly ConditionManager _conditionManager;
    private readonly IConfiguration _configuration;

    private readonly DB2Manager _db2Manager;
    // Loot GUID

    private readonly ItemEnchantmentManager _itemEnchantmentManager;
    private readonly LootFactory _lootFactory;
    private readonly LootStoreBox _lootStorage;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly List<ObjectGuid> _playersLooting = new();
    private readonly Dictionary<uint, LootRoll> _rolls = new(); // used if an item is under rolling

    // The WorldObject that holds this loot
    private bool _wasOpened; // true if at least one player received the loot content

    public Loot(Map map, ObjectGuid owner, LootType type, PlayerGroup group, ConditionManager conditionManager, GameObjectManager objectManager,
                DB2Manager db2Manager, ObjectAccessor objectAccessor, LootStoreBox lootStorage, IConfiguration configuration, LootFactory lootFactory, ItemEnchantmentManager itemEnchantmentManager)
    {
        LootType = type;
        _conditionManager = conditionManager;
        _objectManager = objectManager;
        _db2Manager = db2Manager;
        _objectAccessor = objectAccessor;
        _lootStorage = lootStorage;
        _configuration = configuration;
        _lootFactory = lootFactory;
        _itemEnchantmentManager = itemEnchantmentManager;
        Guid = map != null ? ObjectGuid.Create(HighGuid.LootObject, map.Id, 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
        OwnerGuid = owner;
        ItemContext = ItemContext.None;
        LootMethod = group?.LootMethod ?? LootMethod.FreeForAll;
        LootMasterGuid = group?.MasterLooterGuid ?? ObjectGuid.Empty;
    }

    public uint DungeonEncounterId { get; set; }

    public ObjectGuid Guid { get; set; }

    public ItemContext ItemContext { get; set; }

    public ObjectGuid LootMasterGuid { get; set; }

    public LootMethod LootMethod { get; set; }

    public ObjectGuid OwnerGuid { get; set; }

    public MultiMap<ObjectGuid, NotNormalLootItem> PlayerFFAItems { get; } = new();
    // Inserts the item into the loot (called by LootTemplate processors)
    public void AddItem(LootStoreItem item)
    {
        var proto = _objectManager.GetItemTemplate(item.Itemid);

        if (proto == null)
            return;

        var count = RandomHelper.URand(item.Mincount, item.Maxcount);
        var stacks = (uint)(count / proto.MaxStackSize + (Convert.ToBoolean(count % proto.MaxStackSize) ? 1 : 0));

        for (uint i = 0; i < stacks && Items.Count < SharedConst.MaxNRLootItems; ++i)
        {
            LootItem generatedLoot = new(_objectManager, _conditionManager, item, _itemEnchantmentManager)
            {
                Context = ItemContext,
                Count = (byte)Math.Min(count, proto.MaxStackSize),
                LootListId = (uint)Items.Count
            };

            if (ItemContext != 0)
            {
                var bonusListIDs = _db2Manager.GetDefaultItemBonusTree(generatedLoot.Itemid, ItemContext);
                generatedLoot.BonusListIDs.AddRange(bonusListIDs);
            }

            Items.Add(generatedLoot);
            count -= proto.MaxStackSize;
        }
    }

    public void AddLooter(ObjectGuid guid)
    {
        _playersLooting.Add(guid);
    }

    public bool AutoStore(Player player, byte bag, byte slot, bool broadcast = false, bool createdByPlayer = false)
    {
        var allLooted = true;

        for (uint i = 0; i < Items.Count; ++i)
        {
            var lootItem = LootItemInSlot(i, player, out var ffaitem);

            if (lootItem == null || lootItem.IsLooted)
                continue;

            if (!lootItem.HasAllowedLooter(Guid))
                continue;

            if (lootItem.IsBlocked)
                continue;

            // dont allow protected item to be looted by someone else
            if (!lootItem.RollWinnerGuid.IsEmpty && lootItem.RollWinnerGuid != Guid)
                continue;

            List<ItemPosCount> dest = new();
            var msg = player.CanStoreNewItem(bag, slot, dest, lootItem.Itemid, lootItem.Count);

            if (msg != InventoryResult.Ok && slot != ItemConst.NullSlot)
                msg = player.CanStoreNewItem(bag, ItemConst.NullSlot, dest, lootItem.Itemid, lootItem.Count);

            if (msg != InventoryResult.Ok && bag != ItemConst.NullBag)
                msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, lootItem.Itemid, lootItem.Count);

            if (msg != InventoryResult.Ok)
            {
                player.SendEquipError(msg, null, null, lootItem.Itemid);
                allLooted = false;

                continue;
            }

            if (ffaitem != null)
                ffaitem.IsLooted = true;

            if (!lootItem.Freeforall)
                lootItem.IsLooted = true;

            --UnlootedCount;

            var pItem = player.StoreNewItem(dest, lootItem.Itemid, true, lootItem.RandomBonusListId, null, lootItem.Context, lootItem.BonusListIDs);
            player.SendNewItem(pItem, lootItem.Count, false, createdByPlayer, broadcast);
            player.ApplyItemLootedSpell(pItem, true);
        }

        return allLooted;
    }

    public void BuildLootResponse(LootResponse packet, Player viewer)
    {
        packet.Coins = Gold;

        foreach (var item in Items)
        {
            var uiType = item.GetUiTypeForPlayer(viewer, this);

            if (!uiType.HasValue)
                continue;

            LootItemData lootItem = new()
            {
                LootListID = (byte)item.LootListId,
                UIType = uiType.Value,
                Quantity = item.Count,
                Loot = new ItemInstance(item)
            };

            packet.Items.Add(lootItem);
        }
    }

    public bool FillLoot(uint lootId, LootStorageType storageType, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var store = GetLootStorage(storageType);

        return FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);
    }

    // Calls processor of corresponding LootTemplate (which handles everything including references)
    public bool FillLoot(uint lootId, LootStore store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        // Must be provided
        if (lootOwner == null)
            return false;


        var tab = store.GetLootFor(lootId);

        if (tab == null)
        {
            if (!noEmptyError)
                Log.Logger.Error("Table '{0}' loot id #{1} used but it doesn't have records.", store.Name, lootId);

            return false;
        }

        ItemContext = context;

        tab.Process(this, store.IsRatesAllowed, (byte)lootMode, 0); // Processing is done there, callback via Loot.AddItem()

        // Setting access rights for group loot case
        var group = lootOwner.Group;

        if (!personal && group != null)
        {
            if (LootType == LootType.Corpse)
                RoundRobinPlayer = lootOwner.GUID;

            for (var refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                var player = refe.Source;

                if (player == null) // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
                    continue;

                if (player.IsAtGroupRewardDistance(lootOwner))
                    FillNotNormalLootFor(player);
            }

            foreach (var item in Items)
            {
                if (!item.FollowLootRules || item.Freeforall)
                    continue;

                var proto = _objectManager.GetItemTemplate(item.Itemid);

                if (proto == null)
                    continue;

                if (proto.Quality < group.LootThreshold)
                    item.IsUnderthreshold = true;
                else
                    item.IsBlocked = LootMethod switch
                    {
                        LootMethod.MasterLoot      => true,
                        LootMethod.GroupLoot       => true,
                        LootMethod.NeedBeforeGreed => true,
                        _                          => item.IsBlocked
                    };
            }
        }
        // ... for personal loot
        else
        {
            FillNotNormalLootFor(lootOwner);
        }

        return true;
    }

    public void FillNotNormalLootFor(Player player)
    {
        var plguid = player.GUID;
        _allowedLooters.Add(plguid);

        List<NotNormalLootItem> ffaItems = new();

        foreach (var item in Items.Where(item => item.AllowedForPlayer(player, this)))
        {
            item.AddAllowedLooter(player);

            if (item.Freeforall)
            {
                ffaItems.Add(new NotNormalLootItem((byte)item.LootListId));
                ++UnlootedCount;
            }

            else if (!item.IsCounted)
            {
                item.IsCounted = true;
                ++UnlootedCount;
            }
        }

        if (!ffaItems.Empty())
            PlayerFFAItems[player.GUID] = ffaItems;
    }

    public void GenerateMoneyLoot(uint minAmount, uint maxAmount)
    {
        if (maxAmount <= 0)
            return;

        if (maxAmount <= minAmount)
            Gold = (uint)(maxAmount * _configuration.GetDefaultValue("Rate:Drop:Money", 1.0f));
        else if (maxAmount - minAmount < 32700)
            Gold = (uint)(RandomHelper.URand(minAmount, maxAmount) * _configuration.GetDefaultValue("Rate:Drop:Money", 1.0f));
        else
            Gold = (uint)(RandomHelper.URand(minAmount >> 8, maxAmount >> 8) * _configuration.GetDefaultValue("Rate:Drop:Money", 1.0f)) << 8;
    }

    public LootItem GetItemInSlot(uint lootListId)
    {
        return lootListId < Items.Count ? Items[(int)lootListId] : null;
    }

    public bool HasAllowedLooter(ObjectGuid looter)
    {
        return _allowedLooters.Contains(looter);
    }

    // return true if there is any FFA, quest or conditional item for the player.
    public bool HasItemFor(Player player)
    {
        // quest items
        if (Items.Any(lootItem => !lootItem.IsLooted && !lootItem.FollowLootRules && lootItem.GetAllowedLooters().Contains(player.GUID)))
            return true;

        if (!PlayerFFAItems.TryGetValue(player.GUID, out var ffaItems))
            return false;

        var hasFfaItem = ffaItems.Any(ffaItem => !ffaItem.IsLooted);

        return hasFfaItem;
    }

    // return true if there is any item that is lootable for any player (not quest item, FFA or conditional)
    public bool HasItemForAll()
    {
        // Gold is always lootable
        return Gold != 0 || Items.Any(item => !item.IsLooted && item.FollowLootRules && !item.Freeforall && item.Conditions.Empty());
    }

    // return true if there is any item over the group threshold (i.e. not underthreshold).
    public bool HasOverThresholdItem()
    {
        for (byte i = 0; i < Items.Count; ++i)
            if (!Items[i].IsLooted && !Items[i].IsUnderthreshold && !Items[i].Freeforall)
                return true;

        return false;
    }

    public bool IsLooted()
    {
        return Gold == 0 && UnlootedCount == 0;
    }

    public LootItem LootItemInSlot(uint lootSlot, Player player)
    {
        return LootItemInSlot(lootSlot, player, out _);
    }

    public LootItem LootItemInSlot(uint lootListId, Player player, out NotNormalLootItem ffaItem)
    {
        ffaItem = null;

        if (lootListId >= Items.Count)
            return null;

        var item = Items[(int)lootListId];
        var isLooted = item.IsLooted;

        if (!item.Freeforall)
            return isLooted ? null : item;

        if (!PlayerFFAItems.TryGetValue(player.GUID, out var itemList))
            return isLooted ? null : item;

        foreach (var notNormalLootItem in itemList.Where(notNormalLootItem => notNormalLootItem.LootListId == lootListId))
        {
            isLooted = notNormalLootItem.IsLooted;
            ffaItem = notNormalLootItem;

            break;
        }

        return isLooted ? null : item;
    }

    public void NotifyItemRemoved(byte lootListId, Map map)
    {
        // notify all players that are looting this that the item was removed
        // convert the index to the slot the player sees
        for (var i = 0; i < _playersLooting.Count; ++i)
        {
            var item = Items[lootListId];

            if (!item.GetAllowedLooters().Contains(_playersLooting[i]))
                continue;

            var player = _objectAccessor.GetPlayer(map, _playersLooting[i]);

            if (player != null)
                player.SendNotifyLootItemRemoved(Guid, OwnerGuid, lootListId);
            else
                _playersLooting.RemoveAt(i);
        }
    }

    public void NotifyLootList(Map map)
    {
        LootList lootList = new()
        {
            Owner = OwnerGuid,
            LootObj = Guid
        };

        if (LootMethod == LootMethod.MasterLoot && HasOverThresholdItem())
            lootList.Master = LootMasterGuid;

        if (!RoundRobinPlayer.IsEmpty)
            lootList.RoundRobinWinner = RoundRobinPlayer;

        lootList.Write();

        foreach (var allowedLooter in _allowedLooters.Select(allowedLooterGuid => _objectAccessor.GetPlayer(map, allowedLooterGuid)))
            allowedLooter?.SendPacket(lootList);
    }

    public void NotifyMoneyRemoved(Map map)
    {
        // notify all players that are looting this that the money was removed
        for (var i = 0; i < _playersLooting.Count; ++i)
        {
            var player = _objectAccessor.GetPlayer(map, _playersLooting[i]);

            if (player != null)
                player.SendNotifyLootMoneyRemoved(Guid);
            else
                _playersLooting.RemoveAt(i);
        }
    }

    public void OnLootOpened(Map map, ObjectGuid looter)
    {
        AddLooter(looter);

        if (_wasOpened)
            return;

        _wasOpened = true;

        switch (LootMethod)
        {
            case LootMethod.GroupLoot or LootMethod.NeedBeforeGreed:
            {
                ushort maxEnchantingSkill = 0;

                foreach (var allowedLooterGuid in _allowedLooters)
                {
                    var allowedLooter = _objectAccessor.GetPlayer(map, allowedLooterGuid);

                    if (allowedLooter != null)
                        maxEnchantingSkill = Math.Max(maxEnchantingSkill, allowedLooter.GetSkillValue(SkillType.Enchanting));
                }

                for (uint lootListId = 0; lootListId < Items.Count; ++lootListId)
                {
                    var item = Items[(int)lootListId];

                    if (!item.IsBlocked)
                        continue;

                    LootRoll lootRoll = new(_objectManager, _objectAccessor, _lootFactory);

                    _rolls.TryAdd(lootListId, lootRoll);

                    if (!lootRoll.TryToStart(map, this, lootListId, maxEnchantingSkill))
                        _rolls.Remove(lootListId);
                }

                break;
            }
            case LootMethod.MasterLoot when looter != LootMasterGuid:
                return;
            case LootMethod.MasterLoot:
            {
                var lootMaster = _objectAccessor.GetPlayer(map, looter);

                if (lootMaster == null)
                    return;

                MasterLootCandidateList masterLootCandidateList = new()
                {
                    LootObj = Guid,
                    Players = _allowedLooters
                };

                lootMaster.SendPacket(masterLootCandidateList);

                break;
            }
        }
    }

    public void RemoveLooter(ObjectGuid guid)
    {
        _playersLooting.Remove(guid);
    }

    public void Update()
    {
        foreach (var pair in _rolls.ToList().Where(pair => pair.Value.UpdateRoll()))
            _rolls.Remove(pair.Key);
    }

    private LootStore GetLootStorage(LootStorageType storageType)
    {
        return storageType switch
        {
            LootStorageType.Creature      => _lootStorage.Creature,
            LootStorageType.Gameobject    => _lootStorage.Gameobject,
            LootStorageType.Disenchant    => _lootStorage.Disenchant,
            LootStorageType.Fishing       => _lootStorage.Fishing,
            LootStorageType.Items         => _lootStorage.Items,
            LootStorageType.Mail          => _lootStorage.Mail,
            LootStorageType.Milling       => _lootStorage.Milling,
            LootStorageType.Pickpocketing => _lootStorage.Pickpocketing,
            LootStorageType.Prospecting   => _lootStorage.Prospecting,
            LootStorageType.Reference     => _lootStorage.Reference,
            LootStorageType.Skinning      => _lootStorage.Skinning,
            LootStorageType.Spell         => _lootStorage.Spell,
            _                             => _lootStorage.Reference // it will never hit this. Shutup compiler.
        };
    }
}