// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class GraveyardCache : IObjectCache
{
    private readonly DB6Storage<AreaTableRecord> _areaTableRecords;
    private readonly ConditionManager _conditionManager;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly PhasingHandler _phasingHandler;
    private readonly TerrainManager _terrainManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldSafeLocationsCache _worldSafeLocationsCache;

    public GraveyardCache(WorldDatabase worldDatabase, TerrainManager terrainManager, PhasingHandler phasingHandler,
                          DB6Storage<MapRecord> mapRecords, WorldSafeLocationsCache worldSafeLocationsCache, ConditionManager conditionManager,
                          DB6Storage<AreaTableRecord> areaTableRecords)
    {
        _worldDatabase = worldDatabase;
        _terrainManager = terrainManager;
        _phasingHandler = phasingHandler;
        _mapRecords = mapRecords;
        _worldSafeLocationsCache = worldSafeLocationsCache;
        _conditionManager = conditionManager;
        _areaTableRecords = areaTableRecords;
    }

    public MultiMap<uint, GraveYardData> GraveYardStorage { get; set; } = new();

    public bool AddGraveYardLink(uint id, uint zoneId, TeamFaction team, bool persist = true)
    {
        if (FindGraveYardData(id, zoneId) != null)
            return false;

        // add link to loaded data
        GraveYardData data = new()
        {
            SafeLocId = id,
            Team = (uint)team
        };

        GraveYardStorage.Add(zoneId, data);

        // add link to DB
        if (!persist)
            return true;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_GRAVEYARD_ZONE);

        stmt.AddValue(0, id);
        stmt.AddValue(1, zoneId);
        stmt.AddValue(2, (uint)team);

        _worldDatabase.Execute(stmt);

        return true;
    }

    public GraveYardData FindGraveYardData(uint id, uint zoneId)
    {
        var range = GraveYardStorage.LookupByKey(zoneId);

        return range.FirstOrDefault(data => data.SafeLocId == id);
    }

    public WorldSafeLocsEntry GetClosestGraveYard(WorldLocation location, TeamFaction team, WorldObject conditionObject)
    {
        var mapId = location.MapId;

        // search for zone associated closest graveyard
        var zoneId = _terrainManager.GetZoneId(conditionObject != null ? conditionObject.Location.PhaseShift : _phasingHandler.EmptyPhaseShift, mapId, location);

        if (zoneId == 0)
            if (location.Z > -500)
            {
                Log.Logger.Error("ZoneId not found for map {0} coords ({1}, {2}, {3})", mapId, location.X, location.Y, location.Z);

                return GetDefaultGraveYard(team);
            }

        // Simulate std. algorithm:
        //   found some graveyard associated to (ghost_zone, ghost_map)
        //
        //   if mapId == graveyard.mapId (ghost in plain zone or city or Battleground) and search graveyard at same map
        //     then check faction
        //   if mapId != graveyard.mapId (ghost in instance) and search any graveyard associated
        //     then check faction
        var range = GraveYardStorage.LookupByKey(zoneId);
        var mapEntry = _mapRecords.LookupByKey(mapId);

        ConditionSourceInfo conditionSource = new(conditionObject);

        // not need to check validity of map object; MapId _MUST_ be valid here
        if (range.Empty() && !mapEntry.IsBattlegroundOrArena())
        {
            if (zoneId != 0) // zone == 0 can't be fixed, used by bliz for bugged zones
                Log.Logger.Error("Table `game_graveyard_zone` incomplete: Zone {0} Team {1} does not have a linked graveyard.", zoneId, team);

            return GetDefaultGraveYard(team);
        }

        // at corpse map
        var foundNear = false;
        float distNear = 10000;
        WorldSafeLocsEntry entryNear = null;

        // at entrance map for corpse map
        var foundEntr = false;
        float distEntr = 10000;
        WorldSafeLocsEntry entryEntr = null;

        // some where other
        WorldSafeLocsEntry entryFar = null;

        foreach (var data in range)
        {
            var entry = _worldSafeLocationsCache.GetWorldSafeLoc(data.SafeLocId);

            if (entry == null)
            {
                Log.Logger.Error("Table `game_graveyard_zone` has record for not existing graveyard (WorldSafeLocs.dbc id) {0}, skipped.", data.SafeLocId);

                continue;
            }

            // skip enemy faction graveyard
            // team == 0 case can be at call from .neargrave
            if (data.Team != 0 && team != 0 && data.Team != (uint)team)
                continue;

            if (conditionObject != null)
            {
                if (!_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Graveyard, data.SafeLocId, conditionSource))
                    continue;

                if (entry.Location.MapId == mapEntry.ParentMapID && !conditionObject.Location.PhaseShift.HasVisibleMapId(entry.Location.MapId))
                    continue;
            }

            // find now nearest graveyard at other map
            if (mapId != entry.Location.MapId && mapEntry != null && entry.Location.MapId != mapEntry.ParentMapID)
            {
                // if find graveyard at different map from where entrance placed (or no entrance data), use any first
                if (mapEntry.CorpseMapID < 0 || mapEntry.CorpseMapID != entry.Location.MapId || mapEntry.Corpse is { X: 0, Y: 0 })
                {
                    // not have any corrdinates for check distance anyway
                    entryFar = entry;

                    continue;
                }

                // at entrance map calculate distance (2D);
                var dist2 = (entry.Location.X - mapEntry.Corpse.X) * (entry.Location.X - mapEntry.Corpse.X) + (entry.Location.Y - mapEntry.Corpse.Y) * (entry.Location.Y - mapEntry.Corpse.Y);

                if (foundEntr)
                {
                    if (!(dist2 < distEntr))
                        continue;

                    distEntr = dist2;
                    entryEntr = entry;
                }
                else
                {
                    foundEntr = true;
                    distEntr = dist2;
                    entryEntr = entry;
                }
            }
            // find now nearest graveyard at same map
            else
            {
                var dist2 = (entry.Location.X - location.X) * (entry.Location.X - location.X) + (entry.Location.Y - location.Y) * (entry.Location.Y - location.Y) + (entry.Location.Z - location.Z) * (entry.Location.Z - location.Z);

                if (foundNear)
                {
                    if (!(dist2 < distNear))
                        continue;

                    distNear = dist2;
                    entryNear = entry;
                }
                else
                {
                    foundNear = true;
                    distNear = dist2;
                    entryNear = entry;
                }
            }
        }

        if (entryNear != null)
            return entryNear;

        return entryEntr ?? entryFar;
    }

    public WorldSafeLocsEntry GetDefaultGraveYard(TeamFaction team)
    {
        return team switch
        {
            TeamFaction.Horde => _worldSafeLocationsCache.GetWorldSafeLoc(10),
            TeamFaction.Alliance => _worldSafeLocationsCache.GetWorldSafeLoc(4),
            _ => null
        };
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        GraveYardStorage.Clear(); // need for reload case

        //                                         0       1         2
        var result = _worldDatabase.Query("SELECT ID, GhostZone, faction FROM graveyard_zone");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 graveyard-zone links. DB table `graveyard_zone` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            ++count;
            var safeLocId = result.Read<uint>(0);
            var zoneId = result.Read<uint>(1);
            var team = (TeamFaction)result.Read<uint>(2);

            var entry = _worldSafeLocationsCache.GetWorldSafeLoc(safeLocId);

            if (entry == null)
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for not existing graveyard (WorldSafeLocs.dbc id) {0}, skipped.", safeLocId);

                continue;
            }

            if (!_areaTableRecords.TryGetValue(zoneId, out _))
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for not existing zone id ({0}), skipped.", zoneId);

                continue;
            }

            if (team != 0 && team != TeamFaction.Horde && team != TeamFaction.Alliance)
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for non player faction ({0}), skipped.", team);

                continue;
            }

            if (!AddGraveYardLink(safeLocId, zoneId, team, false))
                Log.Logger.Error("Table `graveyard_zone` has a duplicate record for Graveyard (ID: {0}) and Zone (ID: {1}), skipped.", safeLocId, zoneId);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} graveyard-zone links in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void RemoveGraveYardLink(uint id, uint zoneId, TeamFaction team, bool persist = false)
    {
        if (GraveYardStorage.TryGetValue(zoneId, out var range))
        {
            Log.Logger.Error("Table `game_graveyard_zone` incomplete: Zone {0} Team {1} does not have a linked graveyard.", zoneId, team);

            return;
        }

        var found = false;

        foreach (var data in range)
        {
            // skip not matching safezone id
            if (data.SafeLocId != id)
                continue;

            // skip enemy faction graveyard at same map (normal area, city, or Battleground)
            // team == 0 case can be at call from .neargrave
            if (data.Team != 0 && team != 0 && data.Team != (uint)team)
                continue;

            found = true;

            break;
        }

        // no match, return
        if (!found)
            return;

        // remove from links
        GraveYardStorage.Remove(zoneId);

        // remove link from DB
        if (!persist)
            return;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GRAVEYARD_ZONE);

        stmt.AddValue(0, id);
        stmt.AddValue(1, zoneId);
        stmt.AddValue(2, (uint)team);

        _worldDatabase.Execute(stmt);
    }
}