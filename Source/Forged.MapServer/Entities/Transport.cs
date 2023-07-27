// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Pools;
using Forged.MapServer.Scripting.Interfaces.ITransport;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities;

public class Transport : GameObject, ITransport
{
    private readonly TimeTracker _positionChangeTimer = new();
    private readonly HashSet<WorldObject> _staticPassengers = new();

    private int _currentPathLeg;
    private bool _delayedAddModel;
    private BitArray _eventsToTrigger;
    private TransportMovementState _movementState;
    private uint? _requestStopTimestamp;
    private TransportTemplate _transportInfo;

    public Transport(LootFactory lootFactory, ClassFactory classFactory, LootStoreBox lootStoreBox, PoolManager poolManager, DB2Manager db2Manager, WorldDatabase worldDatabase,
                     LootManager lootManager, OutdoorPvPManager outdoorPvPManager, TransportManager transportManager, CreatureFactory creatureFactory)
        : base(lootFactory, classFactory, lootStoreBox, poolManager, db2Manager, worldDatabase, lootManager, outdoorPvPManager)
    {
        TransportManager = transportManager;
        CreatureFactory = creatureFactory;
        UpdateFlag.ServerTime = true;
        UpdateFlag.Stationary = true;
        UpdateFlag.Rotation = true;
    }

    public CreatureFactory CreatureFactory { get; }
    public uint ExpectedMapId => _transportInfo.PathLegs[_currentPathLeg].MapId;
    public int MapIdForSpawning => Template.MoTransport.SpawnMap;

    public HashSet<WorldObject> Passengers { get; } = new();
    public uint Timer { get; private set; }
    public TransportManager TransportManager { get; }
    public float TransportOrientation => Location.Orientation;
    public uint TransportPeriod => GameObjectFieldData.Level;

    public void AddPassenger(WorldObject passenger)
    {
        if (!Location.IsInWorld)
            return;

        lock (_staticPassengers)
            if (Passengers.Add(passenger))
            {
                passenger.Transport = this;
                passenger.MovementInfo.Transport.Guid = GUID;

                var player = passenger.AsPlayer;

                if (player != null)
                    ScriptManager.RunScript<ITransportOnAddPassenger>(p => p.OnAddPassenger(this, player), ScriptId);
            }
    }

    public override void BuildUpdate(Dictionary<Player, UpdateData> dataMap)
    {
        var players = Location.Map.Players;

        if (players.Empty())
            return;

        foreach (var playerReference in players.Where(playerReference => playerReference.Location.InSamePhase(this)))
            BuildFieldsUpdate(playerReference, dataMap);

        ClearUpdateMask(true);
    }

    public void CalculatePassengerOffset(Position pos)
    {
        ITransport.CalculatePassengerOffset(pos, Location.X, Location.Y, Location.Z, TransportOrientation);
    }

    public void CalculatePassengerPosition(Position pos)
    {
        ITransport.CalculatePassengerPosition(pos, Location.X, Location.Y, Location.Z, TransportOrientation);
    }

    public override void CleanupsBeforeDelete(bool finalCleanup = true)
    {
        UnloadStaticPassengers();

        while (!Passengers.Empty())
        {
            var obj = Passengers.FirstOrDefault();
            RemovePassenger(obj);
        }

        base.CleanupsBeforeDelete(finalCleanup);
    }

    public bool Create(ulong guidlow, uint entry, float x, float y, float z, float ang)
    {
        Location.Relocate(x, y, z, ang);

        if (!GridDefines.IsValidMapCoord(Location))
        {
            Log.Logger.Error($"Transport (GUID: {guidlow}) not created. Suggested coordinates isn't valid (X: {x} Y: {y})");

            return false;
        }

        Create(ObjectGuid.Create(HighGuid.Transport, guidlow));

        var goinfo = GameObjectManager.GameObjectTemplateCache.GetGameObjectTemplate(entry);

        if (goinfo == null)
        {
            Log.Logger.Error($"Transport not created: entry in `gameobject_template` not found, entry: {entry}");

            return false;
        }

        GoInfoProtected = goinfo;
        GoTemplateAddonProtected = GameObjectManager.GetGameObjectTemplateAddon(entry);

        var tInfo = TransportManager.GetTransportTemplate(entry);

        if (tInfo == null)
        {
            Log.Logger.Error("Transport {0} (name: {1}) will not be created, missing `transport_template` entry.", entry, goinfo.name);

            return false;
        }

        _transportInfo = tInfo;
        _eventsToTrigger = new BitArray(tInfo.Events.Count, true);

        var goOverride = GameObjectOverride;

        if (goOverride != null)
        {
            Faction = goOverride.Faction;
            ReplaceAllFlags(goOverride.Flags);
        }

        Timer = goinfo.MoTransport.allowstopping == 0 ? Time.MSTime /*might be called before world update loop begins, don't use GameTime*/ % tInfo.TotalPathTime : 0;
        SetPathProgressForClient(Timer / (float)tInfo.TotalPathTime);
        ObjectScale = goinfo.size;
        SetPeriod(tInfo.TotalPathTime);
        Entry = goinfo.entry;
        DisplayId = goinfo.displayId;
        SetGoState(goinfo.MoTransport.allowstopping == 0 ? GameObjectState.Ready : GameObjectState.Active);
        GoType = GameObjectTypes.MapObjTransport;
        SetGoAnimProgress(255);
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.SpawnTrackingStateAnimID), DB2Manager.GetEmptyAnimStateID());
        SetName(goinfo.name);
        SetLocalRotation(0.0f, 0.0f, 0.0f, 1.0f);
        SetParentRotation(Quaternion.Identity);

        var position = _transportInfo.ComputePosition(Timer, out _, out var legIndex);

        if (position != null)
        {
            Location.Relocate(position.X, position.Y, position.Z, position.Orientation);
            _currentPathLeg = legIndex;
        }

        CreateModel();

        return true;
    }

    public Creature CreateNPCPassenger(ulong guid, CreatureData data)
    {
        var map = Location.Map;

        if (map.GetCreatureRespawnTime(guid) != 0)
            return null;

        var creature = CreatureFactory.CreateCreatureFromDB(guid, map, false, true);

        if (creature == null)
            return null;

        var spawn = data.SpawnPoint.Copy();

        creature.Transport = this;
        creature.MovementInfo.Transport.Guid = GUID;
        creature.MovementInfo.Transport.Pos.Relocate(spawn);
        creature.MovementInfo.Transport.Seat = -1;
        CalculatePassengerPosition(spawn);
        creature.Location.Relocate(spawn);
        creature.SetHomePosition(creature.Location.X, creature.Location.Y, creature.Location.Z, creature.Location.Orientation);
        creature.TransportHomePosition = creature.MovementInfo.Transport.Pos;

        // @HACK - transport models are not added to map's dynamic LoS calculations
        //         because the current GameObjectModel cannot be moved without recreating
        creature.AddUnitState(UnitState.IgnorePathfinding);

        if (!GridDefines.IsValidMapCoord(creature.Location))
        {
            Log.Logger.Error("Creature (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", creature.GUID.ToString(), creature.Entry, creature.Location.X, creature.Location.Y);

            return null;
        }

        PhasingHandler.InitDbPhaseShift(creature.Location.PhaseShift, data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
        PhasingHandler.InitDbVisibleMapId(creature.Location.PhaseShift, data.TerrainSwapMap);

        if (!map.AddToMap(creature))
            return null;

        lock (_staticPassengers)
            _staticPassengers.Add(creature);

        ScriptManager.RunScript<ITransportOnAddCreaturePassenger>(p => p.OnAddCreaturePassenger(this, creature), ScriptId);

        return creature;
    }

    public override void Dispose()
    {
        UnloadStaticPassengers();
        base.Dispose();
    }

    public void EnableMovement(bool enabled)
    {
        if (Template.MoTransport.allowstopping == 0)
            return;

        if (!enabled)
            _requestStopTimestamp = Timer / TransportPeriod * TransportPeriod + _transportInfo.GetNextPauseWaypointTimestamp(Timer);
        else
        {
            _requestStopTimestamp = null;
            SetGoState(GameObjectState.Active);
            RemoveDynamicFlag(GameObjectDynamicLowFlags.Stopped);
        }
    }

    public ITransport RemovePassenger(WorldObject passenger)
    {
        lock (_staticPassengers)
            if (Passengers.Remove(passenger) || _staticPassengers.Remove(passenger)) // static passenger can remove itself in case of grid unload
            {
                passenger.Transport = null;
                passenger.MovementInfo.Transport.Reset();
                Log.Logger.Debug("Object {0} removed from transport {1}.", passenger.GetName(), GetName());

                var plr = passenger.AsPlayer;

                if (plr == null)
                    return this;

                ScriptManager.RunScript<ITransportOnRemovePassenger>(p => p.OnRemovePassenger(this, plr), ScriptId);
                plr.SetFallInformation(0, plr.Location.Z);
            }

        return this;
    }

    public void SetDelayedAddModelToMap()
    {
        _delayedAddModel = true;
    }

    public void SetPeriod(uint period)
    {
        SetLevel(period);
    }

    public TempSummon SummonPassenger(uint entry, Position pos, TempSummonType summonType, SummonPropertiesRecord properties = null, uint duration = 0, Unit summoner = null, uint spellId = 0, uint vehId = 0)
    {
        var map = Location.Map;

        if (map == null)
            return null;

        var mask = UnitTypeMask.Summon;

        if (properties != null)
            switch (properties.Control)
            {
                case SummonCategory.Pet:
                    mask = UnitTypeMask.Guardian;

                    break;

                case SummonCategory.Puppet:
                    mask = UnitTypeMask.Puppet;

                    break;

                case SummonCategory.Vehicle:
                    mask = UnitTypeMask.Minion;

                    break;

                case SummonCategory.Wild:
                case SummonCategory.Ally:
                case SummonCategory.Unk:
                {
                    switch (properties.Title)
                    {
                        case SummonTitle.Minion:
                        case SummonTitle.Guardian:
                        case SummonTitle.Runeblade:
                            mask = UnitTypeMask.Guardian;

                            break;

                        case SummonTitle.Totem:
                        case SummonTitle.LightWell:
                            mask = UnitTypeMask.Totem;

                            break;

                        case SummonTitle.Vehicle:
                        case SummonTitle.Mount:
                            mask = UnitTypeMask.Summon;

                            break;

                        case SummonTitle.Companion:
                            mask = UnitTypeMask.Minion;

                            break;

                        default:
                            if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup)) // Mirror Image, Summon Gargoyle
                                mask = UnitTypeMask.Guardian;

                            break;
                    }

                    break;
                }
                default:
                    return null;
            }

        var summon = mask switch
        {
            UnitTypeMask.Summon => ClassFactory.ResolveWithPositionalParameters<TempSummon>(properties, summoner, false),
            UnitTypeMask.Guardian => ClassFactory.ResolveWithPositionalParameters<Guardian>(properties, summoner, false),
            UnitTypeMask.Puppet => ClassFactory.ResolveWithPositionalParameters<Puppet>(properties, summoner),
            UnitTypeMask.Totem => ClassFactory.ResolveWithPositionalParameters<Totem>(properties, summoner),
            UnitTypeMask.Minion => ClassFactory.ResolveWithPositionalParameters<Minion>(properties, summoner, false),
            // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
            _ => ClassFactory.ResolveWithPositionalParameters<TempSummon>(properties, summoner, false)
        };

        var newPos = pos.Copy();
        CalculatePassengerPosition(newPos);

        if (!summon.Create(map.GenerateLowGuid(HighGuid.Creature), map, entry, newPos, null, vehId))
            return null;

        WorldObject phaseShiftOwner = this;

        if (summoner != null && !(properties != null && properties.GetFlags().HasFlag(SummonPropertiesFlags.IgnoreSummonerPhase)))
            phaseShiftOwner = summoner;

        PhasingHandler.InheritPhaseShift(summon, phaseShiftOwner);

        summon.SetCreatedBySpell(spellId);

        summon.Transport = this;
        summon.MovementInfo.Transport.Guid = GUID;
        summon.MovementInfo.Transport.Pos.Relocate(pos);
        summon.Location.Relocate(newPos);
        summon.HomePosition = newPos;
        summon.TransportHomePosition = pos;

        // @HACK - transport models are not added to map's dynamic LoS calculations
        //         because the current GameObjectModel cannot be moved without recreating
        summon.AddUnitState(UnitState.IgnorePathfinding);

        summon.InitStats(duration);

        if (!map.AddToMap(summon))
            return null;

        lock (_staticPassengers)
            _staticPassengers.Add(summon);

        summon.InitSummon();
        summon.SetTempSummonType(summonType);

        return summon;
    }

    public override void Update(uint diff)
    {
        var positionUpdateDelay = TimeSpan.FromMilliseconds(200);

        if (AI != null)
            AI.UpdateAI(diff);
        else if (!AIM_Initialize())
            Log.Logger.Error("Could not initialize GameObjectAI for Transport");

        ScriptManager.RunScript<ITransportOnUpdate>(p => p.OnUpdate(this, diff), ScriptId);

        _positionChangeTimer.Update(diff);

        var cycleId = Timer / TransportPeriod;

        if (Template.MoTransport.allowstopping == 0)
            Timer = GameTime.CurrentTimeMS;
        else if (!_requestStopTimestamp.HasValue || _requestStopTimestamp > Timer + diff)
            Timer += diff;
        else
            Timer = _requestStopTimestamp.Value;

        if (Timer / TransportPeriod != cycleId)
            // reset cycle
            _eventsToTrigger.SetAll(true);

        SetPathProgressForClient(Timer / (float)TransportPeriod);

        var timer = Timer % TransportPeriod;

        var eventToTriggerIndex = -1;

        for (var i = 0; i < _eventsToTrigger.Count; i++)
            if (_eventsToTrigger.Get(i))
            {
                eventToTriggerIndex = i;

                break;
            }

        if (eventToTriggerIndex != -1)
            while (eventToTriggerIndex < _transportInfo.Events.Count && _transportInfo.Events[eventToTriggerIndex].Timestamp < timer)
            {
                var leg = _transportInfo.GetLegForTime(_transportInfo.Events[eventToTriggerIndex].Timestamp);

                if (leg != null)
                    if (leg.MapId == Location.MapId)
                        GameEvents.Trigger(_transportInfo.Events[eventToTriggerIndex].EventId, this, this);

                _eventsToTrigger.Set(eventToTriggerIndex, false);
                ++eventToTriggerIndex;
            }

        var newPosition = _transportInfo.ComputePosition(timer, out var moveState, out var legIndex);

        if (newPosition != null)
        {
            var justStopped = _movementState == TransportMovementState.Moving && moveState != TransportMovementState.Moving;
            _movementState = moveState;

            if (justStopped)
                if (_requestStopTimestamp != 0 && GoState != GameObjectState.Ready)
                {
                    SetGoState(GameObjectState.Ready);
                    SetDynamicFlag(GameObjectDynamicLowFlags.Stopped);
                }

            if (legIndex != _currentPathLeg)
            {
                var oldMapId = _transportInfo.PathLegs[_currentPathLeg].MapId;
                _currentPathLeg = legIndex;
                TeleportTransport(oldMapId, _transportInfo.PathLegs[legIndex].MapId, newPosition.X, newPosition.Y, newPosition.Z, newPosition.Orientation);

                return;
            }

            // set position
            if (_positionChangeTimer.Passed && ExpectedMapId == Location.MapId)
            {
                _positionChangeTimer.Reset(positionUpdateDelay);

                if (_movementState == TransportMovementState.Moving || justStopped)
                    UpdatePosition(newPosition.X, newPosition.Y, newPosition.Z, newPosition.Orientation);
                else
                {
                    /* There are four possible scenarios that trigger loading/unloading passengers:
                      1. transport moves from inactive to active grid
                      2. the grid that transport is currently in becomes active
                      3. transport moves from active to inactive grid
                      4. the grid that transport is currently in unloads
                    */
                    var gridActive = Location.Map.IsGridLoaded(Location.X, Location.Y);

                    lock (_staticPassengers)
                        if (_staticPassengers.Empty() && gridActive) // 2.
                            LoadStaticPassengers();
                        else if (!_staticPassengers.Empty() && !gridActive)
                            // 4. - if transports stopped on grid edge, some passengers can remain in active grids
                            //      unload all static passengers otherwise passengers won't load correctly when the grid that transport is currently in becomes active
                            UnloadStaticPassengers();
                }
            }
        }

        // Add model to map after we are fully done with moving maps
        if (!_delayedAddModel)
            return;

        _delayedAddModel = false;

        if (Model != null)
            Location.Map.InsertGameObjectModel(Model);
    }

    public void UpdatePosition(float x, float y, float z, float o)
    {
        ScriptManager.RunScript<ITransportOnRelocate>(p => p.OnRelocate(this, Location.MapId, x, y, z), ScriptId);

        var newActive = Location.Map.IsGridLoaded(x, y);
        var oldCell = ClassFactory.ResolveWithPositionalParameters<Cell>(Location.X, Location.Y);

        Location.Relocate(x, y, z, o);
        StationaryPosition.Orientation = o;
        UpdateModelPosition();

        UpdatePassengerPositions(Passengers);

        /* There are four possible scenarios that trigger loading/unloading passengers:
        1. transport moves from inactive to active grid
        2. the grid that transport is currently in becomes active
        3. transport moves from active to inactive grid
        4. the grid that transport is currently in unloads
        */
        lock (_staticPassengers)
            if (_staticPassengers.Empty() && newActive) // 1. and 2.
                LoadStaticPassengers();
            else if (!_staticPassengers.Empty() && !newActive && oldCell.DiffGrid(ClassFactory.ResolveWithPositionalParameters<Cell>(Location.X, Location.Y))) // 3.
                UnloadStaticPassengers();
            else
                UpdatePassengerPositions(_staticPassengers);
        // 4. is handed by grid unload
    }

    private void CreateGOPassenger(ulong guid, GameObjectData data)
    {
        var map = Location.Map;

        if (map.GetGORespawnTime(guid) != 0)
            return;

        var go = GameObjectFactory.CreateGameObjectFromDb(guid, map, false);

        if (go == null)
            return;

        var spawn = data.SpawnPoint.Copy();

        go.Transport = this;
        go.MovementInfo.Transport.Guid = GUID;
        go.MovementInfo.Transport.Pos.Relocate(spawn);
        go.MovementInfo.Transport.Seat = -1;
        CalculatePassengerPosition(spawn);
        go.Location.Relocate(spawn);
        go.RelocateStationaryPosition(spawn);

        if (!GridDefines.IsValidMapCoord(go.Location))
        {
            Log.Logger.Error("GameObject (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", go.GUID.ToString(), go.Entry, go.Location.X, go.Location.Y);

            return;
        }

        PhasingHandler.InitDbPhaseShift(go.Location.PhaseShift, data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
        PhasingHandler.InitDbVisibleMapId(go.Location.PhaseShift, data.TerrainSwapMap);

        if (!map.AddToMap(go))
            return;

        lock (_staticPassengers)
            _staticPassengers.Add(go);
    }

    private void LoadStaticPassengers()
    {
        var mapId = (uint)Template.MoTransport.SpawnMap;
        var cells = GameObjectManager.GetMapObjectGuids(mapId, Location.Map.DifficultyID);

        if (cells == null)
            return;

        foreach (var cell in cells)
        {
            // Creatures on transport
            foreach (var npc in cell.Value.Creatures)
                CreateNPCPassenger(npc, GameObjectManager.GetCreatureData(npc));

            // GameObjects on transport
            foreach (var go in cell.Value.Gameobjects)
                CreateGOPassenger(go, GameObjectManager.GameObjectCache.GetGameObjectData(go));
        }
    }

    private void TeleportPassengersAndHideTransport(uint newMapid, float x, float y, float z, float o)
    {
        if (newMapid == Location.MapId)
        {
            AddToWorld();

            foreach (var player in Location.Map.Players)
                if (player.Transport != this && player.Location.InSamePhase(this))
                {
                    UpdateData data = new(Location.Map.Id);
                    BuildCreateUpdateBlockForPlayer(data, player);
                    player.VisibleTransports.Add(GUID);
                    data.BuildPacket(out var packet);
                    player.SendPacket(packet);
                }
        }
        else
        {
            UpdateData data = new(Location.Map.Id);
            BuildOutOfRangeUpdateBlock(data);

            data.BuildPacket(out var packet);

            foreach (var player in Location.Map.Players.Where(player => player.Transport != this && player.VisibleTransports.Contains(GUID)))
            {
                player.SendPacket(packet);
                player.VisibleTransports.Remove(GUID);
            }

            RemoveFromWorld();
        }

        List<WorldObject> passengersToTeleport = new(Passengers);

        foreach (var obj in passengersToTeleport)
        {
            var newPos = obj.MovementInfo.Transport.Pos.Copy();
            ITransport.CalculatePassengerPosition(newPos, x, y, z, o);

            switch (obj.TypeId)
            {
                case TypeId.Player:
                    if (!obj.AsPlayer.TeleportTo(newMapid, newPos, TeleportToOptions.NotLeaveTransport))
                        RemovePassenger(obj);

                    break;

                case TypeId.DynamicObject:
                case TypeId.AreaTrigger:
                    obj.Location.AddObjectToRemoveList();

                    break;

                default:
                    RemovePassenger(obj);

                    break;
            }
        }
    }

    private void TeleportTransport(uint oldMapId, uint newMapId, float x, float y, float z, float o)
    {
        if (oldMapId != newMapId)
        {
            UnloadStaticPassengers();
            TeleportPassengersAndHideTransport(newMapId, x, y, z, o);

            return;
        }

        UpdatePosition(x, y, z, o);

        // Teleport players, they need to know it
        foreach (var obj in Passengers)
            if (obj.IsTypeId(TypeId.Player))
            {
                // will be relocated in UpdatePosition of the vehicle
                var veh = obj.AsUnit.VehicleBase;

                if (veh != null)
                    if (veh.Transport == this)
                        continue;

                var pos = obj.MovementInfo.Transport.Pos.Copy();
                ITransport.CalculatePassengerPosition(pos, x, y, z, o);

                obj.AsUnit.NearTeleportTo(pos);
            }
    }

    private void UnloadStaticPassengers()
    {
        lock (_staticPassengers)
            while (!_staticPassengers.Empty())
            {
                var obj = _staticPassengers.First();
                obj.Location.AddObjectToRemoveList(); // also removes from _staticPassengers
            }
    }

    private void UpdatePassengerPositions(HashSet<WorldObject> passengers)
    {
        foreach (var passenger in passengers)
        {
            var pos = passenger.MovementInfo.Transport.Pos.Copy();
            CalculatePassengerPosition(pos);
            ITransport.UpdatePassengerPosition(this, Location.Map, passenger, pos, true);
        }
    }
}