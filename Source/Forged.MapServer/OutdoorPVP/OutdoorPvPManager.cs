// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IOutdoorPvP;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.OutdoorPVP;

public class OutdoorPvPManager
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly DisableManager _disableManager;

    private readonly GameObjectManager _objectManager;

    // Holds the outdoor PvP templates
    private readonly uint[] _outdoorMapIds =
    {
        0, 530, 530, 530, 530, 1
    };

    // contains all initiated outdoor pvp events
    // used when initing / cleaning up
    private readonly MultiMap<Map, OutdoorPvP> _outdoorPvPByMap = new();

    private readonly Dictionary<OutdoorPvPTypes, uint> _outdoorPvPDatas = new();

    // maps the zone ids to an outdoor pvp event
    // used in player event handling
    private readonly Dictionary<(Map map, uint zoneId), OutdoorPvP> _outdoorPvPMap = new();

    private readonly ScriptManager _scriptManager;
    private readonly LimitedThreadTaskManager _threadTaskManager;

    private readonly WorldDatabase _worldDatabase;

    // update interval
    private uint _updateTimer;

    public OutdoorPvPManager(IConfiguration configuration, WorldDatabase worldDatabase, DisableManager disableManager, GameObjectManager objectManager,
                             ScriptManager scriptManager, CliDB cliDB, DB2Manager db2Manager)
    {
        _worldDatabase = worldDatabase;
        _disableManager = disableManager;
        _objectManager = objectManager;
        _scriptManager = scriptManager;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        _threadTaskManager = new LimitedThreadTaskManager(configuration.GetDefaultValue("Map:ParellelUpdateTasks", 20));
    }

    public void AddZone(uint zoneid, OutdoorPvP handle)
    {
        _outdoorPvPMap[(handle.Map, zoneid)] = handle;
    }

    public void CreateOutdoorPvPForMap(Map map)
    {
        for (var outdoorPvpType = OutdoorPvPTypes.HellfirePeninsula; outdoorPvpType < OutdoorPvPTypes.Max; ++outdoorPvpType)
        {
            if (map.Id != _outdoorMapIds[(int)outdoorPvpType])
                continue;

            if (!_outdoorPvPDatas.ContainsKey(outdoorPvpType))
            {
                Log.Logger.Error("Could not initialize OutdoorPvP object for type ID {0}; no entry in database.", outdoorPvpType);

                continue;
            }

            var pvp = _scriptManager.RunScriptRet<IOutdoorPvPGetOutdoorPvP, OutdoorPvP>(p => p.GetOutdoorPvP(map), _outdoorPvPDatas[outdoorPvpType]);

            if (pvp == null)
            {
                Log.Logger.Error("Could not initialize OutdoorPvP object for type ID {0}; got NULL pointer from script.", outdoorPvpType);

                continue;
            }

            if (!pvp.SetupOutdoorPvP())
            {
                Log.Logger.Error("Could not initialize OutdoorPvP object for type ID {0}; SetupOutdoorPvP failed.", outdoorPvpType);

                continue;
            }

            _outdoorPvPByMap.Add(map, pvp);
        }
    }

    public void DestroyOutdoorPvPForMap(Map map)
    {
        _outdoorPvPByMap.Remove(map);
    }

    public string GetDefenseMessage(uint zoneId, uint id, Locale locale)
    {
        if (_cliDB.BroadcastTextStorage.TryGetValue(id, out var bct))
            return _db2Manager.GetBroadcastTextValue(bct, locale);

        Log.Logger.Error("Can not find DefenseMessage (Zone: {0}, Id: {1}). BroadcastText (Id: {2}) does not exist.", zoneId, id, id);

        return "";
    }

    public OutdoorPvP GetOutdoorPvPToZoneId(Map map, uint zoneid)
    {
        return _outdoorPvPMap.LookupByKey((map, zoneid));
    }

    public bool HandleCustomSpell(Player player, uint spellId, GameObject go)
    {
        var pvp = player.GetOutdoorPvP();

        if (pvp != null && pvp.HasPlayer(player))
            return pvp.HandleCustomSpell(player, spellId, go);

        return false;
    }

    public void HandleDropFlag(Player player, uint spellId)
    {
        var pvp = player.GetOutdoorPvP();

        if (pvp != null && pvp.HasPlayer(player))
            pvp.HandleDropFlag(player, spellId);
    }

    public bool HandleOpenGo(Player player, GameObject go)
    {
        var pvp = player.GetOutdoorPvP();

        if (pvp != null && pvp.HasPlayer(player))
            return pvp.HandleOpenGo(player, go);

        return false;
    }

    public void HandlePlayerEnterZone(Player player, uint zoneid)
    {
        var outdoor = GetOutdoorPvPToZoneId(player.Location.Map, zoneid);

        if (outdoor == null)
            return;

        if (outdoor.HasPlayer(player))
            return;

        outdoor.HandlePlayerEnterZone(player, zoneid);
        Log.Logger.Debug("Player {0} entered outdoorpvp id {1}", player.GUID.ToString(), outdoor.TypeId);
    }

    public void HandlePlayerLeaveZone(Player player, uint zoneid)
    {
        var outdoor = GetOutdoorPvPToZoneId(player.Location.Map, zoneid);

        if (outdoor == null)
            return;

        // teleport: remove once in removefromworld, once in updatezone
        if (!outdoor.HasPlayer(player))
            return;

        outdoor.HandlePlayerLeaveZone(player, zoneid);
        Log.Logger.Debug("Player {0} left outdoorpvp id {1}", player.GUID.ToString(), outdoor.TypeId);
    }

    public void HandlePlayerResurrects(Player player, uint zoneid)
    {
        var pvp = player.GetOutdoorPvP();

        if (pvp != null && pvp.HasPlayer(player))
            pvp.HandlePlayerResurrects(player, zoneid);
    }

    public void InitOutdoorPvP()
    {
        var oldMSTime = Time.MSTime;

        //                                             0       1
        var result = _worldDatabase.Query("SELECT TypeId, ScriptName FROM outdoorpvp_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 outdoor PvP definitions. DB table `outdoorpvp_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var typeId = (OutdoorPvPTypes)result.Read<byte>(0);

            if (_disableManager.IsDisabledFor(DisableType.OutdoorPVP, (uint)typeId, null))
                continue;

            if (typeId >= OutdoorPvPTypes.Max)
            {
                Log.Logger.Error("Invalid OutdoorPvPTypes value {0} in outdoorpvp_template; skipped.", typeId);

                continue;
            }

            _outdoorPvPDatas[typeId] = _scriptManager.GetScriptId(result.Read<string>(1));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} outdoor PvP definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void Update(uint diff)
    {
        _updateTimer += diff;

        if (_updateTimer > 1000)
        {
            foreach (var (_, outdoor) in _outdoorPvPByMap.KeyValueList)
                _threadTaskManager.Schedule(() => outdoor.Update(_updateTimer));

            _threadTaskManager.Wait();
            _updateTimer = 0;
        }
    }
}