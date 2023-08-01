// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.G;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class GossipMenuItemsCache : IObjectCache
{
    private readonly DB6Storage<BroadcastTextRecord> _broadcastTextRecords;
    private readonly IConfiguration _configuration;
    private readonly MultiMap<uint, GossipMenuItems> _gossipMenuItemsStorage = new();
    private readonly DB6Storage<GossipNPCOptionRecord> _gossipNPCOptionRecords;
    private readonly DB6Storage<LanguagesRecord> _languagesRecords;
    private readonly PointOfInterestCache _pointOfInterestCache;
    private readonly SpellManager _spellManager;
    private readonly WorldDatabase _worldDatabase;

    public GossipMenuItemsCache(WorldDatabase worldDatabase, IConfiguration configuration, DB6Storage<GossipNPCOptionRecord> gossipNPCOptionRecords,
                                DB6Storage<BroadcastTextRecord> broadcastTextRecords, DB6Storage<LanguagesRecord> languagesRecords,
                                SpellManager spellManager, PointOfInterestCache pointOfInterestCache)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _gossipNPCOptionRecords = gossipNPCOptionRecords;
        _broadcastTextRecords = broadcastTextRecords;
        _languagesRecords = languagesRecords;
        _spellManager = spellManager;
        _pointOfInterestCache = pointOfInterestCache;
    }

    public List<GossipMenuItems> GetGossipMenuItemsMapBounds(uint uiMenuId)
    {
        return _gossipMenuItemsStorage.LookupByKey(uiMenuId);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _gossipMenuItemsStorage.Clear();

        //                                         0       1               2         3          4           5                      6         7      8             9            10
        var result = _worldDatabase.Query("SELECT MenuID, GossipOptionID, OptionID, OptionNpc, OptionText, OptionBroadcastTextID, Language, Flags, ActionMenuID, ActionPoiID, GossipNpcOptionID, " +
                                          //11        12        13       14                  15       16
                                          "BoxCoded, BoxMoney, BoxText, BoxBroadcastTextID, SpellID, OverrideIconID FROM gossip_menu_option ORDER BY MenuID, OptionID");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gossip_menu_option Ids. DB table `gossip_menu_option` is empty!");

            return;
        }

        Dictionary<int, uint> optionToNpcOption = new();

        foreach (var (_, npcOption) in _gossipNPCOptionRecords)
            optionToNpcOption[npcOption.GossipOptionID] = npcOption.Id;

        do
        {
            GossipMenuItems gMenuItem = new()
            {
                MenuId = result.Read<uint>(0),
                GossipOptionId = result.Read<int>(1),
                OrderIndex = result.Read<uint>(2),
                OptionNpc = (GossipOptionNpc)result.Read<byte>(3),
                OptionText = result.Read<string>(4),
                OptionBroadcastTextId = result.Read<uint>(5),
                Language = result.Read<uint>(6),
                Flags = (GossipOptionFlags)result.Read<int>(7),
                ActionMenuId = result.Read<uint>(8),
                ActionPoiId = result.Read<uint>(9)
            };

            if (!result.IsNull(10))
                gMenuItem.GossipNpcOptionId = result.Read<int>(10);

            gMenuItem.BoxCoded = result.Read<bool>(11);
            gMenuItem.BoxMoney = result.Read<uint>(12);
            gMenuItem.BoxText = result.Read<string>(13);
            gMenuItem.BoxBroadcastTextId = result.Read<uint>(14);

            if (!result.IsNull(15))
                gMenuItem.SpellId = result.Read<int>(15);

            if (!result.IsNull(16))
                gMenuItem.OverrideIconId = result.Read<int>(16);

            if (gMenuItem.OptionNpc >= GossipOptionNpc.Max)
            {
                Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} has unknown NPC option id {gMenuItem.OptionNpc}. Replacing with GossipOptionNpc.None");
                gMenuItem.OptionNpc = GossipOptionNpc.None;
            }

            if (gMenuItem.OptionBroadcastTextId != 0)
                if (!_broadcastTextRecords.ContainsKey(gMenuItem.OptionBroadcastTextId))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET OptionBroadcastTextID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for MenuId {gMenuItem.MenuId}, OptionIndex {gMenuItem.OrderIndex} has non-existing or incompatible OptionBroadcastTextId {gMenuItem.OptionBroadcastTextId}, ignoring.");

                    gMenuItem.OptionBroadcastTextId = 0;
                }

            if (gMenuItem.Language != 0 && !_languagesRecords.ContainsKey(gMenuItem.Language))
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"UPDATE gossip_menu_option SET OptionID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                else
                    Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing Language {gMenuItem.Language}, ignoring");

                gMenuItem.Language = 0;
            }

            if (gMenuItem.ActionMenuId != 0 && gMenuItem.OptionNpc != GossipOptionNpc.None)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionMenuID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                else
                    Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} can not use ActionMenuID for GossipOptionNpc different from GossipOptionNpc.None, ignoring");

                gMenuItem.ActionMenuId = 0;
            }

            if (gMenuItem.ActionPoiId != 0)
            {
                if (gMenuItem.OptionNpc != GossipOptionNpc.None)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionPoiID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} can not use ActionPoiID for GossipOptionNpc different from GossipOptionNpc.None, ignoring");

                    gMenuItem.ActionPoiId = 0;
                }
                else if (_pointOfInterestCache.GetPointOfInterest(gMenuItem.ActionPoiId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionPoiID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing ActionPoiID {gMenuItem.ActionPoiId}, ignoring");

                    gMenuItem.ActionPoiId = 0;
                }
            }

            if (gMenuItem.GossipNpcOptionId.HasValue)
            {
                if (!_gossipNPCOptionRecords.ContainsKey(gMenuItem.GossipNpcOptionId.Value))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET GossipNpcOptionID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing GossipNPCOption {gMenuItem.GossipNpcOptionId}, ignoring");

                    gMenuItem.GossipNpcOptionId = null;
                }
            }
            else
            {
                if (optionToNpcOption.TryGetValue(gMenuItem.GossipOptionId, out var npcOptionId))
                    gMenuItem.GossipNpcOptionId = (int)npcOptionId;
            }

            if (gMenuItem.BoxBroadcastTextId != 0)
                if (!_broadcastTextRecords.ContainsKey(gMenuItem.BoxBroadcastTextId))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET BoxBroadcastTextID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for MenuId {gMenuItem.MenuId}, OptionIndex {gMenuItem.OrderIndex} has non-existing or incompatible BoxBroadcastTextId {gMenuItem.BoxBroadcastTextId}, ignoring.");

                    gMenuItem.BoxBroadcastTextId = 0;
                }

            if (gMenuItem.SpellId.HasValue)
                if (!_spellManager.HasSpellInfo((uint)gMenuItem.SpellId.Value))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET SpellID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing Spell {gMenuItem.SpellId}, ignoring");

                    gMenuItem.SpellId = null;
                }

            _gossipMenuItemsStorage.Add(gMenuItem.MenuId, gMenuItem);
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_gossipMenuItemsStorage.Count} gossip_menu_option entries in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}