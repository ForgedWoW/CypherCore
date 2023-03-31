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
    public List<LootItem> Items = new();
    public uint Gold;
    public byte UnlootedCount;
    public ObjectGuid RoundRobinPlayer; // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
    public LootType LootType;           // required for achievement system
    private readonly ConditionManager _conditionManager;
    private readonly GameObjectManager _objectManager;
    private readonly DB2Manager _db2Manager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly LootStoreBox _lootStorage;
    private readonly IConfiguration _configuration;
    private readonly LootFactory _lootFactory;

    private readonly List<ObjectGuid> _playersLooting = new();
    private readonly MultiMap<ObjectGuid, NotNormalLootItem> _playerFFAItems = new();
    private readonly LootMethod _lootMethod;
    private readonly Dictionary<uint, LootRoll> _rolls = new(); // used if an item is under rolling
    private readonly List<ObjectGuid> _allowedLooters = new();

    // Loot GUID
    private readonly ObjectGuid _guid;
    private readonly ObjectGuid _owner; // The WorldObject that holds this loot
    private readonly ObjectGuid _lootMaster;
    private ItemContext _itemContext;
    private bool _wasOpened; // true if at least one player received the loot content
    private uint _dungeonEncounterId;

    public Loot(Map map, ObjectGuid owner, LootType type, PlayerGroup group, ConditionManager conditionManager, GameObjectManager objectManager,
                DB2Manager db2Manager, ObjectAccessor objectAccessor, LootStoreBox lootStorage, IConfiguration configuration, LootFactory lootFactory)
    {
        LootType = type;
        _conditionManager = conditionManager;
        _objectManager = objectManager;
        _db2Manager = db2Manager;
        _objectAccessor = objectAccessor;
        _lootStorage = lootStorage;
        _configuration = configuration;
        _lootFactory = lootFactory;
        _guid = map ? ObjectGuid.Create(HighGuid.LootObject, map.Id, 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
        _owner = owner;
        _itemContext = ItemContext.None;
        _lootMethod = group != null ? group.LootMethod : LootMethod.FreeForAll;
        _lootMaster = group != null ? group.MasterLooterGuid : ObjectGuid.Empty;
    }

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
            LootItem generatedLoot = new(_objectManager, _conditionManager, item)
            {
                Context = _itemContext,
                Count = (byte)Math.Min(count, proto.MaxStackSize),
                LootListId = (uint)Items.Count
            };

            if (_itemContext != 0)
            {
                var bonusListIDs = _db2Manager.GetDefaultItemBonusTree(generatedLoot.Itemid, _itemContext);
                generatedLoot.BonusListIDs.AddRange(bonusListIDs);
            }

            Items.Add(generatedLoot);
            count -= proto.MaxStackSize;
        }
    }

    public bool AutoStore(Player player, byte bag, byte slot, bool broadcast = false, bool createdByPlayer = false)
    {
        var allLooted = true;

        for (uint i = 0; i < Items.Count; ++i)
        {
            var lootItem = LootItemInSlot(i, player, out var ffaitem);

            if (lootItem == null || lootItem.IsLooted)
                continue;

            if (!lootItem.HasAllowedLooter(GetGuid()))
                continue;

            if (lootItem.IsBlocked)
                continue;

            // dont allow protected item to be looted by someone else
            if (!lootItem.RollWinnerGuid.IsEmpty && lootItem.RollWinnerGuid != GetGuid())
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

    public LootItem GetItemInSlot(uint lootListId)
    {
        if (lootListId < Items.Count)
            return Items[(int)lootListId];

        return null;
    }

    // Calls processor of corresponding LootTemplate (which handles everything including references)
    public bool FillLoot(uint lootId, LootStorageType storageType, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        // Must be provided
        if (lootOwner == null)
            return false;

        var store = GetLootStorage(storageType);
        var tab = store.GetLootFor(lootId);

        if (tab == null)
        {
            if (!noEmptyError)
                Log.Logger.Error("Table '{0}' loot id #{1} used but it doesn't have records.", store.GetName(), lootId);

            return false;
        }

        _itemContext = context;

        tab.Process(this, store.IsRatesAllowed(), (byte)lootMode, 0); // Processing is done there, callback via Loot.AddItem()

        // Setting access rights for group loot case
        var group = lootOwner.Group;

        if (!personal && group != null)
        {
            if (LootType == LootType.Corpse)
                RoundRobinPlayer = lootOwner.GUID;

            for (var refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                var player = refe.Source;

                if (player) // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
                    if (player.IsAtGroupRewardDistance(lootOwner))
                        FillNotNormalLootFor(player);
            }

            foreach (var item in Items)
            {
                if (!item.FollowLootRules || item.Freeforall)
                    continue;

                var proto = _objectManager.GetItemTemplate(item.Itemid);

                if (proto != null)
                {
                    if (proto.Quality < group.LootThreshold)
                        item.IsUnderthreshold = true;
                    else
                        switch (_lootMethod)
                        {
                            case LootMethod.MasterLoot:
                            case LootMethod.GroupLoot:
                            case LootMethod.NeedBeforeGreed:
                            {
                                item.IsBlocked = true;

                                break;
                            }
                        }
                }
            }
        }
        // ... for personal loot
        else
        {
            FillNotNormalLootFor(lootOwner);
        }

        return true;
    }

    public void Update()
    {
        foreach (var pair in _rolls.ToList())
            if (pair.Value.UpdateRoll())
                _rolls.Remove(pair.Key);
    }

    public void FillNotNormalLootFor(Player player)
    {
        var plguid = player.GUID;
        _allowedLooters.Add(plguid);

        List<NotNormalLootItem> ffaItems = new();

        foreach (var item in Items)
        {
            if (!item.AllowedForPlayer(player, this))
                continue;

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
            _playerFFAItems[player.GUID] = ffaItems;
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

            if (player)
                player.SendNotifyLootItemRemoved(GetGuid(), GetOwnerGuid(), lootListId);
            else
                _playersLooting.RemoveAt(i);
        }
    }

    public void NotifyMoneyRemoved(Map map)
    {
        // notify all players that are looting this that the money was removed
        for (var i = 0; i < _playersLooting.Count; ++i)
        {
            var player = _objectAccessor.GetPlayer(map, _playersLooting[i]);

            if (player != null)
                player.SendNotifyLootMoneyRemoved(GetGuid());
            else
                _playersLooting.RemoveAt(i);
        }
    }

    public void OnLootOpened(Map map, ObjectGuid looter)
    {
        AddLooter(looter);

        if (!_wasOpened)
        {
            _wasOpened = true;

            if (_lootMethod == LootMethod.GroupLoot || _lootMethod == LootMethod.NeedBeforeGreed)
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
            }
            else if (_lootMethod == LootMethod.MasterLoot)
            {
                if (looter == _lootMaster)
                {
                    var lootMaster = _objectAccessor.GetPlayer(map, looter);

                    if (lootMaster != null)
                    {
                        MasterLootCandidateList masterLootCandidateList = new()
                        {
                            LootObj = GetGuid(),
                            Players = _allowedLooters
                        };

                        lootMaster.SendPacket(masterLootCandidateList);
                    }
                }
            }
        }
    }

    public bool HasAllowedLooter(ObjectGuid looter)
    {
        return _allowedLooters.Contains(looter);
    }

    public void GenerateMoneyLoot(uint minAmount, uint maxAmount)
    {
        if (maxAmount > 0)
        {
            if (maxAmount <= minAmount)
                Gold = (uint)(maxAmount * _configuration.GetDefaultValue("Rate.Drop.Money", 1.0f));
            else if ((maxAmount - minAmount) < 32700)
                Gold = (uint)(RandomHelper.URand(minAmount, maxAmount) * _configuration.GetDefaultValue("Rate.Drop.Money", 1.0f));
            else
                Gold = (uint)(RandomHelper.URand(minAmount >> 8, maxAmount >> 8) * _configuration.GetDefaultValue("Rate.Drop.Money", 1.0f)) << 8;
        }
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

        if (item.Freeforall)
        {
            var itemList = _playerFFAItems.LookupByKey(player.GUID);

            if (itemList != null)
                foreach (var notNormalLootItem in itemList)
                    if (notNormalLootItem.LootListId == lootListId)
                    {
                        isLooted = notNormalLootItem.IsLooted;
                        ffaItem = notNormalLootItem;

                        break;
                    }
        }

        if (isLooted)
            return null;

        return item;
    }

    // return true if there is any item that is lootable for any player (not quest item, FFA or conditional)
    public bool HasItemForAll()
    {
        // Gold is always lootable
        if (Gold != 0)
            return true;

        foreach (var item in Items)
            if (!item.IsLooted && item.FollowLootRules && !item.Freeforall && item.Conditions.Empty())
                return true;

        return false;
    }

    // return true if there is any FFA, quest or conditional item for the player.
    public bool HasItemFor(Player player)
    {
        // quest items
        foreach (var lootItem in Items)
            if (!lootItem.IsLooted && !lootItem.FollowLootRules && lootItem.GetAllowedLooters().Contains(player.GUID))
                return true;

        var ffaItems = GetPlayerFFAItems().LookupByKey(player.GUID);

        if (ffaItems != null)
        {
            var hasFfaItem = ffaItems.Any(ffaItem => !ffaItem.IsLooted);

            if (hasFfaItem)
                return true;
        }

        return false;
    }

    // return true if there is any item over the group threshold (i.e. not underthreshold).
    public bool HasOverThresholdItem()
    {
        for (byte i = 0; i < Items.Count; ++i)
            if (!Items[i].IsLooted && !Items[i].IsUnderthreshold && !Items[i].Freeforall)
                return true;

        return false;
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

    public void NotifyLootList(Map map)
    {
        LootList lootList = new()
        {
            Owner = GetOwnerGuid(),
            LootObj = GetGuid()
        };

        if (GetLootMethod() == LootMethod.MasterLoot && HasOverThresholdItem())
            lootList.Master = GetLootMasterGuid();

        if (!RoundRobinPlayer.IsEmpty)
            lootList.RoundRobinWinner = RoundRobinPlayer;

        lootList.Write();

        foreach (var allowedLooterGuid in _allowedLooters)
        {
            var allowedLooter = _objectAccessor.GetPlayer(map, allowedLooterGuid);

            if (allowedLooter != null)
                allowedLooter.SendPacket(lootList);
        }
    }

    public bool IsLooted()
    {
        return Gold == 0 && UnlootedCount == 0;
    }

    public void AddLooter(ObjectGuid guid)
    {
        _playersLooting.Add(guid);
    }

    public void RemoveLooter(ObjectGuid guid)
    {
        _playersLooting.Remove(guid);
    }

    public ObjectGuid GetGuid()
    {
        return _guid;
    }

    public ObjectGuid GetOwnerGuid()
    {
        return _owner;
    }

    public ItemContext GetItemContext()
    {
        return _itemContext;
    }

    public void SetItemContext(ItemContext context)
    {
        _itemContext = context;
    }

    public LootMethod GetLootMethod()
    {
        return _lootMethod;
    }

    public ObjectGuid GetLootMasterGuid()
    {
        return _lootMaster;
    }

    public uint GetDungeonEncounterId()
    {
        return _dungeonEncounterId;
    }

    public void SetDungeonEncounterId(uint dungeonEncounterId)
    {
        _dungeonEncounterId = dungeonEncounterId;
    }

    public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerFFAItems()
    {
        return _playerFFAItems;
    }


    private LootStore GetLootStorage(LootStorageType storageType)
    {
        switch (storageType)
        {
            case LootStorageType.Creature:
                return _lootStorage.Creature;
            case LootStorageType.Gameobject:
                return _lootStorage.Gameobject;
            case LootStorageType.Disenchant:
                return _lootStorage.Disenchant;
            case LootStorageType.Fishing:
                return _lootStorage.Fishing;
            case LootStorageType.Items:
                return _lootStorage.Items;
            case LootStorageType.Mail:
                return _lootStorage.Mail;
            case LootStorageType.Milling:
                return _lootStorage.Milling;
            case LootStorageType.Pickpocketing:
                return _lootStorage.Pickpocketing;
            case LootStorageType.Prospecting:
                return _lootStorage.Prospecting;
            case LootStorageType.Reference:
                return _lootStorage.Reference;
            case LootStorageType.Skinning:
                return _lootStorage.Skinning;
            case LootStorageType.Spell:
                return _lootStorage.Spell;
        }

        return _lootStorage.Reference; // it will never hit this. Shutup compiler.
    }
}