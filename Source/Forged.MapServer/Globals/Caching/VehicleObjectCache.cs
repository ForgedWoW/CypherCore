// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.V;
using Forged.MapServer.Entities;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class VehicleObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly DB6Storage<VehicleSeatRecord> _vehicleSeatRecords;
    private readonly SpellClickInfoCache _spellClickInfoObjectManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly MultiMap<ulong, VehicleAccessory> _vehicleAccessoryStore = new();
    private readonly Dictionary<uint, VehicleSeatAddon> _vehicleSeatAddonStore = new();
    private readonly MultiMap<uint, VehicleAccessory> _vehicleTemplateAccessoryStore = new();
    private readonly Dictionary<uint, VehicleTemplate> _vehicleTemplateStore = new();

    public VehicleObjectCache(WorldDatabase worldDatabase, DB6Storage<VehicleSeatRecord> vehicleSeatRecords, SpellClickInfoCache spellClickInfoObjectManager, GameObjectManager gameObjectManager)
    {
        _worldDatabase = worldDatabase;
        _vehicleSeatRecords = vehicleSeatRecords;
        _spellClickInfoObjectManager = spellClickInfoObjectManager;
        _gameObjectManager = gameObjectManager;
    }

    public List<VehicleAccessory> GetVehicleAccessoryList(Vehicle veh)
    {
        var cre = veh.Base.AsCreature;

        if (cre != null)
            // Give preference to GUID-based accessories
            if (_vehicleAccessoryStore.TryGetValue(cre.SpawnId, out var list))
                return list;

        // Otherwise return entry-based
        return _vehicleTemplateAccessoryStore.LookupByKey(veh.CreatureEntry);
    }

    public VehicleSeatAddon GetVehicleSeatAddon(uint seatId)
    {
        return _vehicleSeatAddonStore.LookupByKey(seatId);
    }

    public VehicleTemplate GetVehicleTemplate(Vehicle veh)
    {
        return _vehicleTemplateStore.LookupByKey(veh.CreatureEntry);
    }

    public void Load()
    {
        LoadVehicleTemplate();
        LoadVehicleAccessories();
        LoadVehicleSeatAddon();
        LoadVehicleTemplateAccessories();
    }

    public void LoadVehicleAccessories()
    {
        var oldMSTime = Time.MSTime;

        _vehicleAccessoryStore.Clear(); // needed for reload case

        uint count = 0;

        //                                          0             1             2          3           4             5
        var result = _worldDatabase.Query("SELECT `guid`, `accessory_entry`, `seat_id`, `minion`, `summontype`, `summontimer` FROM `vehicle_accessory`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Vehicle Accessories in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));

            return;
        }

        do
        {
            var uiGUID = result.Read<uint>(0);
            var uiAccessory = result.Read<uint>(1);
            var uiSeat = result.Read<sbyte>(2);
            var bMinion = result.Read<bool>(3);
            var uiSummonType = result.Read<byte>(4);
            var uiSummonTimer = result.Read<uint>(5);

            if (_gameObjectManager.GetCreatureTemplate(uiAccessory) == null)
            {
                Log.Logger.Error("Table `vehicle_accessory`: Accessory {0} does not exist.", uiAccessory);

                continue;
            }

            _vehicleAccessoryStore.Add(uiGUID, new VehicleAccessory(uiAccessory, uiSeat, bMinion, uiSummonType, uiSummonTimer));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Vehicle Accessories in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadVehicleSeatAddon()
    {
        var oldMSTime = Time.MSTime;

        _vehicleSeatAddonStore.Clear(); // needed for reload case

        //                                          0            1                  2             3             4             5             6
        var result = _worldDatabase.Query("SELECT `SeatEntry`, `SeatOrientation`, `ExitParamX`, `ExitParamY`, `ExitParamZ`, `ExitParamO`, `ExitParamValue` FROM `vehicle_seat_addon`");

        if (result.IsEmpty())
        {
            Log.Logger.Error("Loaded 0 vehicle seat addons. DB table `vehicle_seat_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var seatID = result.Read<uint>(0);
            var orientation = result.Read<float>(1);
            var exitX = result.Read<float>(2);
            var exitY = result.Read<float>(3);
            var exitZ = result.Read<float>(4);
            var exitO = result.Read<float>(5);
            var exitParam = result.Read<byte>(6);

            if (!_vehicleSeatRecords.ContainsKey(seatID))
            {
                Log.Logger.Error($"Table `vehicle_seat_addon`: SeatID: {seatID} does not exist in VehicleSeat.dbc. Skipping entry.");

                continue;
            }

            // Sanitizing values
            if (orientation > MathF.PI * 2)
            {
                Log.Logger.Error($"Table `vehicle_seat_addon`: SeatID: {seatID} is using invalid angle offset value ({orientation}). Set Value to 0.");
                orientation = 0.0f;
            }

            if (exitParam >= (byte)VehicleExitParameters.VehicleExitParamMax)
            {
                Log.Logger.Error($"Table `vehicle_seat_addon`: SeatID: {seatID} is using invalid exit parameter value ({exitParam}). Setting to 0 (none).");

                continue;
            }

            _vehicleSeatAddonStore[seatID] = new VehicleSeatAddon(orientation, exitX, exitY, exitZ, exitO, exitParam);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} Vehicle Seat Addon entries in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadVehicleTemplate()
    {
        var oldMSTime = Time.MSTime;

        _vehicleTemplateStore.Clear();

        //                                         0           1
        var result = _worldDatabase.Query("SELECT creatureId, despawnDelayMs FROM vehicle_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 vehicle template. DB table `vehicle_template` is empty.");

            return;
        }

        do
        {
            var creatureId = result.Read<uint>(0);

            if (_gameObjectManager.GetCreatureTemplate(creatureId) == null)
            {
                Log.Logger.Error($"Table `vehicle_template`: Vehicle {creatureId} does not exist.");

                continue;
            }

            VehicleTemplate vehicleTemplate = new()
            {
                DespawnDelay = TimeSpan.FromMilliseconds(result.Read<int>(1))
            };

            _vehicleTemplateStore[creatureId] = vehicleTemplate;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_vehicleTemplateStore.Count} Vehicle Template entries in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadVehicleTemplateAccessories()
    {
        var oldMSTime = Time.MSTime;

        _vehicleTemplateAccessoryStore.Clear(); // needed for reload case

        uint count = 0;

        //                                          0             1              2          3           4             5
        var result = _worldDatabase.Query("SELECT `entry`, `accessory_entry`, `seat_id`, `minion`, `summontype`, `summontimer` FROM `vehicle_template_accessory`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 vehicle template accessories. DB table `vehicle_template_accessory` is empty.");

            return;
        }

        do
        {
            var entry = result.Read<uint>(0);
            var accessory = result.Read<uint>(1);
            var seatId = result.Read<sbyte>(2);
            var isMinion = result.Read<bool>(3);
            var summonType = result.Read<byte>(4);
            var summonTimer = result.Read<uint>(5);

            if (_gameObjectManager.GetCreatureTemplate(entry) == null)
            {
                Log.Logger.Error("Table `vehicle_template_accessory`: creature template entry {0} does not exist.", entry);

                continue;
            }

            if (_gameObjectManager.GetCreatureTemplate(accessory) == null)
            {
                Log.Logger.Error("Table `vehicle_template_accessory`: Accessory {0} does not exist.", accessory);

                continue;
            }

            if (_spellClickInfoObjectManager.GetSpellClickInfoMapBounds(entry) == null)
            {
                Log.Logger.Error("Table `vehicle_template_accessory`: creature template entry {0} has no data in npc_spellclick_spells", entry);

                continue;
            }

            _vehicleTemplateAccessoryStore.Add(entry, new VehicleAccessory(accessory, seatId, isMinion, summonType, summonTimer));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Vehicle Template Accessories in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}