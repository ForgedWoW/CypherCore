// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IBattlefield;
using Framework.Database;
using Framework.Threading;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.BattleFields;

public class BattleFieldManager
{
    private static readonly uint[] BattlefieldIdToMapId =
    {
        0, 571, 732
    };

    private static readonly uint[] BattlefieldIdToScriptId =
    {
        0, 0, 0
    };

    private static readonly uint[] BattlefieldIdToZoneId =
        {
        0, 4197, 5095
    }; // imitate World_PVP_Area.db2
       // contains all initiated battlefield events
       // used when initing / cleaning up
    private readonly MultiMap<Map, BattleField> _battlefieldsByMap = new();

    // maps the zone ids to an battlefield event
    // used in player event handling
    private readonly Dictionary<(Map map, uint zoneId), BattleField> _battlefieldsByZone = new();

    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _objectManager;
    private readonly ScriptManager _scriptManager;
    private readonly LimitedThreadTaskManager _threadTaskManager;
    private readonly WorldDatabase _worldDatabase;
    // update interval
    private uint _updateTimer;

    public BattleFieldManager(IConfiguration configuration, WorldDatabase worldDatabase, GameObjectManager objectManager, ScriptManager scriptManager)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
        _scriptManager = scriptManager;
        _threadTaskManager = new LimitedThreadTaskManager(_configuration.GetDefaultValue("Map.ParellelUpdateTasks", 20));
    }

    public void AddZone(uint zoneId, BattleField bf)
    {
        _battlefieldsByZone[(bf.GetMap(), zoneId)] = bf;
    }

    public void CreateBattlefieldsForMap(Map map)
    {
        for (uint i = 0; i < (int)BattleFieldTypes.Max; ++i)
        {
            if (BattlefieldIdToScriptId[i] == 0)
                continue;

            if (BattlefieldIdToMapId[i] != map.Id)
                continue;

            var bf = _scriptManager.RunScriptRet<IBattlefieldGetBattlefield, BattleField>(p => p.GetBattlefield(map), BattlefieldIdToScriptId[i]);

            if (bf == null)
                continue;

            if (!bf.SetupBattlefield())
            {
                Log.Logger.Information($"Setting up battlefield with TypeId {(BattleFieldTypes)i} on map {map.Id} instance id {map.InstanceId} failed.");

                continue;
            }

            _battlefieldsByMap.Add(map, bf);
            Log.Logger.Information($"Setting up battlefield with TypeId {(BattleFieldTypes)i} on map {map.Id} instance id {map.InstanceId} succeeded.");
        }
    }

    public void DestroyBattlefieldsForMap(Map map)
    {
        _battlefieldsByMap.Remove(map);
    }

    public BattleField GetBattlefieldByBattleId(Map map, uint battleId)
    {
        var battlefields = _battlefieldsByMap.LookupByKey(map);

        foreach (var battlefield in battlefields)
            if (battlefield.GetBattleId() == battleId)
                return battlefield;

        return null;
    }

    public BattleField GetBattlefieldToZoneId(Map map, uint zoneId)
    {
        var bf = _battlefieldsByZone.LookupByKey((map, zoneId));

        if (bf == null)
            // no handle for this zone, return
            return null;

        if (!bf.IsEnabled())
            return null;

        return bf;
    }

    public void HandlePlayerEnterZone(Player player, uint zoneId)
    {
        var bf = _battlefieldsByZone.LookupByKey((player.Location.Map, zoneId));

        if (bf == null)
            return;

        if (!bf.IsEnabled() || bf.HasPlayer(player))
            return;

        bf.HandlePlayerEnterZone(player, zoneId);
        Log.Logger.Debug("Player {0} entered battlefield id {1}", player.GUID.ToString(), bf.GetTypeId());
    }

    public void HandlePlayerLeaveZone(Player player, uint zoneId)
    {
        var bf = _battlefieldsByZone.LookupByKey((player.Location.Map, zoneId));

        if (bf == null)
            return;

        // teleport: remove once in removefromworld, once in updatezone
        if (!bf.HasPlayer(player))
            return;

        bf.HandlePlayerLeaveZone(player, zoneId);
        Log.Logger.Debug("Player {0} left battlefield id {1}", player.GUID.ToString(), bf.GetTypeId());
    }

    public void InitBattlefield()
    {
        var oldMSTime = Time.MSTime;

        uint count = 0;
        var result = _worldDatabase.Query("SELECT TypeId, ScriptName FROM battlefield_template");

        if (!result.IsEmpty())
            do
            {
                var typeId = (BattleFieldTypes)result.Read<byte>(0);

                if (typeId >= BattleFieldTypes.Max)
                {
                    Log.Logger.Error($"BattlefieldMgr::InitBattlefield: Invalid TypeId value {typeId} in battlefield_template, skipped.");

                    continue;
                }

                BattlefieldIdToScriptId[(int)typeId] = _objectManager.GetScriptId(result.Read<string>(1));
                ++count;
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} battlefields in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
    public bool IsWorldPvpArea(uint zoneId)
    {
        return BattlefieldIdToZoneId.Contains(zoneId);
    }
    public void Update(uint diff)
    {
        _updateTimer += diff;

        if (_updateTimer > 1000)
        {
            foreach (var (map, battlefield) in _battlefieldsByMap.KeyValueList)
                if (battlefield.IsEnabled())
                    _threadTaskManager.Schedule(() => battlefield.Update(_updateTimer));

            _threadTaskManager.Wait();
            _updateTimer = 0;
        }
    }
}