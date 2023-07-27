// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class VendorItemCache : IObjectCache
{
    private readonly Dictionary<uint, VendorItemData> _cacheVendorItemStorage = new();
    private readonly IConfiguration _configuration;
    private readonly CreatureTemplateCache _creatureTemplateCache;
    private readonly DB6Storage<CurrencyTypesRecord> _currencyTypesRecords;
    private readonly DB2Manager _db2Manager;
    private readonly DB6Storage<ItemExtendedCostRecord> _itemExtendedCostRecords;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly DB6Storage<PlayerConditionRecord> _playerConditionRecords;
    private readonly WorldDatabase _worldDatabase;

    public VendorItemCache(WorldDatabase worldDatabase, IConfiguration configuration, ItemTemplateCache itemTemplateCache, DB2Manager db2Manager, CreatureTemplateCache creatureTemplateCache,
                           DB6Storage<CurrencyTypesRecord> currencyTypesRecords, DB6Storage<PlayerConditionRecord> playerConditionRecords, DB6Storage<ItemExtendedCostRecord> itemExtendedCostRecords)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _itemTemplateCache = itemTemplateCache;
        _db2Manager = db2Manager;
        _creatureTemplateCache = creatureTemplateCache;
        _currencyTypesRecords = currencyTypesRecords;
        _playerConditionRecords = playerConditionRecords;
        _itemExtendedCostRecords = itemExtendedCostRecords;
    }

    public void AddVendorItem(uint entry, VendorItem vItem, bool persist = true)
    {
        var vList = _cacheVendorItemStorage[entry];
        vList.AddItem(vItem);

        if (!persist)
            return;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_NPC_VENDOR);

        stmt.AddValue(0, entry);
        stmt.AddValue(1, vItem.Item);
        stmt.AddValue(2, vItem.Maxcount);
        stmt.AddValue(3, vItem.Incrtime);
        stmt.AddValue(4, vItem.ExtendedCost);
        stmt.AddValue(5, (byte)vItem.Type);

        _worldDatabase.Execute(stmt);
    }

    public VendorItemData GetNpcVendorItemList(uint entry)
    {
        return _cacheVendorItemStorage.LookupByKey(entry);
    }

    public bool IsVendorItemValid(uint vendorentry, VendorItem vItem, Player player = null, List<uint> skipvendors = null, ulong oRnpcflag = 0)
    {
        var cInfo = _creatureTemplateCache.GetCreatureTemplate(vendorentry);

        if (cInfo == null)
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.CommandVendorselection);
            else if (_configuration.GetDefaultValue("load:autoclean", false))
                _worldDatabase.Execute($"DELETE FROM npc_vendor WHERE entry = {vendorentry}");
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` have data for not existed creature template (Entry: {0}), ignore", vendorentry);

            return false;
        }

        if (!Convert.ToBoolean((cInfo.Npcflag | oRnpcflag) & (ulong)NPCFlags.Vendor))
        {
            if (skipvendors != null && skipvendors.Count != 0)
                return false;

            if (player != null)
                player.SendSysMessage(CypherStrings.CommandVendorselection);
            else if (_configuration.GetDefaultValue("load:autoclean", false))
                _worldDatabase.Execute($"DELETE FROM npc_vendor WHERE entry = {vendorentry}");
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` have data for not creature template (Entry: {0}) without vendor Id, ignore", vendorentry);

            skipvendors?.Add(vendorentry);

            return false;
        }

        if (vItem.Type == ItemVendorType.Item && _itemTemplateCache.GetItemTemplate(vItem.Item) == null ||
            vItem.Type == ItemVendorType.Currency && _currencyTypesRecords.LookupByKey(vItem.Item) == null)
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.ItemNotFound, vItem.Item, vItem.Type);
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` for Vendor (Entry: {0}) have in item list non-existed item ({1}, type {2}), ignore", vendorentry, vItem.Item, vItem.Type);

            return false;
        }

        if (vItem.PlayerConditionId != 0 && !_playerConditionRecords.ContainsKey(vItem.PlayerConditionId))
        {
            Log.Logger.Error("Table `(game_event_)npc_vendor` has Item (Entry: {0}) with invalid PlayerConditionId ({1}) for vendor ({2}), ignore", vItem.Item, vItem.PlayerConditionId, vendorentry);

            return false;
        }

        if (vItem.ExtendedCost != 0 && !_itemExtendedCostRecords.ContainsKey(vItem.ExtendedCost))
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.ExtendedCostNotExist, vItem.ExtendedCost);
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` have Item (Entry: {0}) with wrong ExtendedCost ({1}) for vendor ({2}), ignore", vItem.Item, vItem.ExtendedCost, vendorentry);

            return false;
        }

        if (vItem.Type == ItemVendorType.Item) // not applicable to currencies
        {
            switch (vItem.Maxcount)
            {
                case > 0 when vItem.Incrtime == 0:
                {
                    if (player != null)
                        player.SendSysMessage("MaxCount != 0 ({0}) but IncrTime == 0", vItem.Maxcount);
                    else
                        Log.Logger.Error("Table `(gameevent)npcvendor` has `maxcount` ({0}) for item {1} of vendor (Entry: {2}) but `incrtime`=0, ignore", vItem.Maxcount, vItem.Item, vendorentry);

                    return false;
                }
                case 0 when vItem.Incrtime > 0:
                {
                    if (player != null)
                        player.SendSysMessage("MaxCount == 0 but IncrTime<>= 0");
                    else
                        Log.Logger.Error("Table `(gameevent)npcvendor` has `maxcount`=0 for item {0} of vendor (Entry: {1}) but `incrtime`<>0, ignore", vItem.Item, vendorentry);

                    return false;
                }
            }

            foreach (var bonusList in vItem.BonusListIDs.Where(bonusList => _db2Manager.GetItemBonusList(bonusList) == null))
            {
                Log.Logger.Error("Table `(game_event_)npc_vendor` have Item (Entry: {0}) with invalid bonus {1} for vendor ({2}), ignore", vItem.Item, bonusList, vendorentry);

                return false;
            }
        }

        var vItems = GetNpcVendorItemList(vendorentry);

        if (vItems == null)
            return true; // later checks for non-empty lists

        if (vItems.FindItemCostPair(vItem.Item, vItem.ExtendedCost, vItem.Type) != null)
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.ItemAlreadyInList, vItem.Item, vItem.ExtendedCost, vItem.Type);
            else
                Log.Logger.Error("Table `npcvendor` has duplicate items {0} (with extended cost {1}, type {2}) for vendor (Entry: {3}), ignoring", vItem.Item, vItem.ExtendedCost, vItem.Type, vendorentry);

            return false;
        }

        if (vItem.Type != ItemVendorType.Currency || vItem.Maxcount != 0)
            return true;

        Log.Logger.Error("Table `(game_event_)npc_vendor` have Item (Entry: {0}, type: {1}) with missing maxcount for vendor ({2}), ignore", vItem.Item, vItem.Type, vendorentry);

        return false;
    }

    public void Load()
    {
        var time = Time.MSTime;
        // For reload case
        _cacheVendorItemStorage.Clear();

        List<uint> skipvendors = new();

        var result = _worldDatabase.Query("SELECT entry, item, maxcount, incrtime, ExtendedCost, type, BonusListIDs, PlayerConditionID, IgnoreFiltering FROM npc_vendor ORDER BY entry, slot ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Vendors. DB table `npc_vendor` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var itemid = result.Read<int>(1);

            // if item is a negative, its a reference
            if (itemid < 0)
                count += LoadReferenceVendor((int)entry, -itemid, skipvendors);
            else
            {
                VendorItem vItem = new()
                {
                    Item = (uint)itemid,
                    Maxcount = result.Read<uint>(2),
                    Incrtime = result.Read<uint>(3),
                    ExtendedCost = result.Read<uint>(4),
                    Type = (ItemVendorType)result.Read<byte>(5),
                    PlayerConditionId = result.Read<uint>(7),
                    IgnoreFiltering = result.Read<bool>(8)
                };

                var bonusListIDsTok = new StringArray(result.Read<string>(6), ' ');

                if (!bonusListIDsTok.IsEmpty())
                    foreach (string token in bonusListIDsTok)
                        if (uint.TryParse(token, out var id))
                            vItem.BonusListIDs.Add(id);

                if (!IsVendorItemValid(entry, vItem, null, skipvendors))
                    continue;

                if (_cacheVendorItemStorage.LookupByKey(entry) == null)
                    _cacheVendorItemStorage.Add(entry, new VendorItemData());

                _cacheVendorItemStorage[entry].AddItem(vItem);
                ++count;
            }
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Vendors in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public bool RemoveVendorItem(uint entry, uint item, ItemVendorType type, bool persist = true)
    {
        if (!_cacheVendorItemStorage.TryGetValue(entry, out var iter))
            return false;

        if (!iter.RemoveItem(item, type))
            return false;

        if (!persist)
            return true;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_NPC_VENDOR);

        stmt.AddValue(0, entry);
        stmt.AddValue(1, item);
        stmt.AddValue(2, (byte)type);

        _worldDatabase.Execute(stmt);

        return true;
    }

    private uint LoadReferenceVendor(int vendor, int item, List<uint> skipVendors)
    {
        // find all items from the reference vendor
        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_NPC_VENDOR_REF);
        stmt.AddValue(0, item);
        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
            return 0;

        uint count = 0;

        do
        {
            var itemID = result.Read<int>(0);

            // if item is a negative, its a reference
            if (itemID < 0)
                count += LoadReferenceVendor(vendor, -itemID, skipVendors);
            else
            {
                VendorItem vItem = new()
                {
                    Item = (uint)itemID,
                    Maxcount = result.Read<uint>(1),
                    Incrtime = result.Read<uint>(2),
                    ExtendedCost = result.Read<uint>(3),
                    Type = (ItemVendorType)result.Read<byte>(4),
                    PlayerConditionId = result.Read<uint>(6),
                    IgnoreFiltering = result.Read<bool>(7)
                };

                var bonusListIDsTok = new StringArray(result.Read<string>(5), ' ');

                if (!bonusListIDsTok.IsEmpty())
                    foreach (string token in bonusListIDsTok)
                        if (uint.TryParse(token, out var id))
                            vItem.BonusListIDs.Add(id);

                if (!IsVendorItemValid((uint)vendor, vItem, null, skipVendors))
                    continue;

                if (!_cacheVendorItemStorage.TryGetValue((uint)vendor, out var vList))
                    continue;

                vList.AddItem(vItem);
                ++count;
            }
        } while (result.NextRow());

        return count;
    }
}