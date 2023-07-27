// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.V;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IVehicle;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities;

public class Vehicle : ITransport
{
    public Dictionary<sbyte, VehicleSeat> Seats = new();

    public uint UsableSeatNum;

    private readonly GameObjectManager _gameObjectManager;

    private readonly ObjectAccessor _objectAccessor;
    private readonly VehicleObjectCache _vehicleObjectManager;

    private readonly List<VehicleJoinEvent> _pendingJoinEvents = new();

    private readonly ScriptManager _scriptManager;

    //< Can be different than the entry of _me in case of players
    private Status _status;

    //< Number of seats that match VehicleSeatEntry.UsableByPlayer, used for proper display flags
    public Vehicle(Unit unit, VehicleRecord vehInfo, uint creatureEntry, DB6Storage<VehicleSeatRecord> vehicleSeatRecords, 
                   GameObjectManager gameObjectManager, ScriptManager scriptManager, ObjectAccessor objectAccessor, VehicleObjectCache vehicleObjectManager)
    {
        Base = unit;
        VehicleInfo = vehInfo;
        CreatureEntry = creatureEntry;
        _gameObjectManager = gameObjectManager;
        _scriptManager = scriptManager;
        _objectAccessor = objectAccessor;
        _vehicleObjectManager = vehicleObjectManager;
        _status = Status.None;

        for (uint i = 0; i < SharedConst.MaxVehicleSeats; ++i)
        {
            uint seatId = VehicleInfo.SeatID[i];

            if (seatId == 0)
                continue;

            if (!vehicleSeatRecords.TryGetValue(seatId, out var veSeat))
                continue;

            var addon = vehicleObjectManager.GetVehicleSeatAddon(seatId);
            Seats.Add((sbyte)i, new VehicleSeat(veSeat, addon));

            if (veSeat.CanEnterOrExit())
                ++UsableSeatNum;
        }

        // Set or remove correct flags based on available seats. Will overwrite db data (if wrong).
        if (UsableSeatNum != 0)
            Base.SetNpcFlag(Base.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);
        else
            Base.RemoveNpcFlag(Base.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);

        InitMovementInfoForBase();
    }

    public enum Status
    {
        None,
        Installed,
        UnInstalling,
    }

    public int AvailableSeatCount
    {
        get { return Seats.Count(pair => pair.Value.IsEmpty() && !HasPendingEventForSeat(pair.Key) && (pair.Value.SeatInfo.CanEnterOrExit() || pair.Value.SeatInfo.IsUsableByOverride())); }
    }

    public Unit Base { get; }
    public uint CreatureEntry { get; }
    public TimeSpan DespawnDelay => _vehicleObjectManager.GetVehicleTemplate(this)?.DespawnDelay ?? TimeSpan.FromMilliseconds(1);
    public ObjectGuid GUID => Base.GUID;

    public bool IsControllableVehicle
    {
        get { return Seats.Any(itr => itr.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl)); }
    }

    public bool IsVehicleInUse
    {
        get { return Seats.Any(pair => !pair.Value.IsEmpty()); }
    }

    public int MapIdForSpawning => (int)Base.Location.MapId;
    public float TransportOrientation => Base.Location.Orientation;
    public VehicleRecord VehicleInfo { get; }
    //< DBC data for vehicle

    public void AddPassenger(WorldObject passenger)
    {
        Log.Logger.Fatal("Vehicle cannot directly gain passengers without auras");
    }

    public bool AddVehiclePassenger(Unit unit, sbyte seatId)
    {
        // @Prevent adding passengers when vehicle is uninstalling. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
        if (_status == Status.UnInstalling)
        {
            Log.Logger.Error("Passenger GuidLow: {0}, Entry: {1}, attempting to board vehicle GuidLow: {2}, Entry: {3} during uninstall! SeatId: {4}",
                             unit.GUID.ToString(),
                             unit.Entry,
                             Base.GUID.ToString(),
                             Base.Entry,
                             seatId);

            return false;
        }

        Log.Logger.Debug("Unit {0} scheduling enter vehicle (entry: {1}, vehicleId: {2}, guid: {3} (dbguid: {4}) on seat {5}",
                         unit.GetName(),
                         Base.Entry,
                         VehicleInfo.Id,
                         Base.GUID.ToString(),
                         Base.IsTypeId(TypeId.Unit) ? Base.AsCreature.SpawnId : 0,
                         seatId);

        // The seat selection code may kick other passengers off the vehicle.
        // While the validity of the following may be arguable, it is possible that when such a passenger
        // exits the vehicle will dismiss. That's why the actual adding the passenger to the vehicle is scheduled
        // asynchronously, so it can be cancelled easily in case the vehicle is uninstalled meanwhile.
        VehicleJoinEvent e = new(this, unit);
        unit.Events.AddEvent(e, unit.Events.CalculateTime(TimeSpan.Zero));

        KeyValuePair<sbyte, VehicleSeat> seatKvp = new();

        if (seatId < 0) // no specific seat requirement
        {
            foreach (var seat in Seats)
            {
                seatKvp = seat;

                if (seatKvp.Value.IsEmpty() && !HasPendingEventForSeat(seatKvp.Key) && (seat.Value.SeatInfo.CanEnterOrExit() || seat.Value.SeatInfo.IsUsableByOverride()))
                    break;
            }

            if (seatKvp.Value == null) // no available seat
            {
                e.ScheduleAbort();

                return false;
            }

            e.Seat = seatKvp;
            _pendingJoinEvents.Add(e);
        }
        else
        {
            seatKvp = new KeyValuePair<sbyte, VehicleSeat>(seatId, Seats.LookupByKey(seatId));

            if (seatKvp.Value == null)
            {
                e.ScheduleAbort();

                return false;
            }

            e.Seat = seatKvp;
            _pendingJoinEvents.Add(e);

            if (seatKvp.Value.IsEmpty())
                return true;

            var passenger = _objectAccessor.GetUnit(Base, seatKvp.Value.Passenger.Guid);
            passenger.ExitVehicle();
        }

        return true;
    }

    public void CalculatePassengerOffset(Position pos)
    {
        ITransport.CalculatePassengerOffset(pos,
                                            Base.Location.X,
                                            Base.Location.Y,
                                            Base.Location.Z,
                                            Base.Location.Orientation);
    }

    public void CalculatePassengerPosition(Position pos)
    {
        ITransport.CalculatePassengerPosition(pos,
                                              Base.Location.X,
                                              Base.Location.Y,
                                              Base.Location.Z,
                                              Base.Location.Orientation);
    }

    public string GetDebugInfo()
    {
        var str = new StringBuilder("Vehicle seats:\n");

        foreach (var (id, seat) in Seats)
            str.Append($"seat {id}: {(seat.IsEmpty() ? "empty" : seat.Passenger.Guid)}\n");

        str.Append("Vehicle pending events:");

        if (_pendingJoinEvents.Empty())
            str.Append(" none");
        else
        {
            str.Append("\n");

            foreach (var joinEvent in _pendingJoinEvents)
                str.Append($"seat {joinEvent.Seat.Key}: {joinEvent.Passenger.GUID}\n");
        }

        return str.ToString();
    }

    public VehicleSeat GetNextEmptySeat(sbyte seatId, bool next)
    {
        if (!Seats.TryGetValue(seatId, out var seat))
            return null;

        var newSeatId = seatId;

        while (!seat.IsEmpty() || HasPendingEventForSeat(newSeatId) || (!seat.SeatInfo.CanEnterOrExit() && !seat.SeatInfo.IsUsableByOverride()))
        {
            if (next)
            {
                if (!Seats.ContainsKey(++newSeatId))
                    newSeatId = 0;
            }
            else
            {
                if (!Seats.ContainsKey(newSeatId))
                    newSeatId = SharedConst.MaxVehicleSeats;

                --newSeatId;
            }

            // Make sure we don't loop indefinetly
            if (newSeatId == seatId)
                return null;

            seat = Seats[newSeatId];
        }

        return seat;
    }

    public Unit GetPassenger(sbyte seatId)
    {
        return !Seats.TryGetValue(seatId, out var seat) ? null : _objectAccessor.GetUnit(Base, seat.Passenger.Guid);
    }

    /// <summary>
    ///     Gets the vehicle seat addon data for the seat of a passenger
    /// </summary>
    /// <param name="passenger"> Identifier for the current seat user </param>
    /// <returns> The seat addon data for the currently used seat of a passenger </returns>
    public VehicleSeatAddon GetSeatAddonForSeatOfPassenger(Unit passenger)
    {
        return (from pair in Seats where !pair.Value.IsEmpty() && pair.Value.Passenger.Guid == passenger.GUID select pair.Value.SeatAddon).FirstOrDefault();
    }

    public VehicleSeatRecord GetSeatForPassenger(Unit passenger)
    {
        return (from pair in Seats where pair.Value.Passenger.Guid == passenger.GUID select pair.Value.SeatInfo).FirstOrDefault();
    }

    public bool HasEmptySeat(sbyte seatId)
    {
        return Seats.TryGetValue(seatId, out var seat) && seat.IsEmpty();
    }

    public void Install()
    {
        _status = Status.Installed;

        if (Base.IsTypeId(TypeId.Unit))
            _scriptManager.RunScript<IVehicleOnInstall>(p => p.OnInstall(this), Base.AsCreature.GetScriptId());
    }

    public void InstallAllAccessories(bool evading)
    {
        if (Base.IsTypeId(TypeId.Player) || !evading)
            RemoveAllPassengers(); // We might have aura's saved in the DB with now invalid casters - remove

        var accessories = _vehicleObjectManager.GetVehicleAccessoryList(this);

        if (accessories == null)
            return;

        foreach (var acc in accessories.Where(acc => !evading || acc.IsMinion))
            InstallAccessory(acc.AccessoryEntry, acc.SeatId, acc.IsMinion, acc.SummonedType, acc.SummonTime);
    }

    public void RelocatePassengers()
    {
        List<Tuple<Unit, Position>> seatRelocation = new();

        // not sure that absolute position calculation is correct, it must depend on vehicle pitch angle
        foreach (var pair in Seats)
        {
            var passenger = _objectAccessor.GetUnit(Base, pair.Value.Passenger.Guid);

            if (passenger == null)
                continue;

            var pos = passenger.MovementInfo.Transport.Pos.Copy();
            CalculatePassengerPosition(pos);

            seatRelocation.Add(Tuple.Create(passenger, pos));
        }

        foreach (var (passenger, position) in seatRelocation)
            ITransport.UpdatePassengerPosition(this, Base.Location.Map, passenger, position, false);
    }

    public void RemoveAllPassengers()
    {
        Log.Logger.Debug("Vehicle.RemoveAllPassengers. Entry: {0}, GuidLow: {1}", CreatureEntry, Base.GUID.ToString());

        // Setting to_Abort to true will cause @VehicleJoinEvent.Abort to be executed on next @Unit.UpdateEvents call
        // This will properly "reset" the pending join process for the passenger.
        {
            // Update vehicle in every pending join event - Abort may be called after vehicle is deleted
            var eventVehicle = _status != Status.UnInstalling ? this : null;

            while (!_pendingJoinEvents.Empty())
            {
                var e = _pendingJoinEvents.First();
                e.ScheduleAbort();
                e.Target = eventVehicle;
                _pendingJoinEvents.Remove(_pendingJoinEvents.First());
            }
        }

        // Passengers always cast an aura with SPELL_AURA_CONTROL_VEHICLE on the vehicle
        // We just remove the aura and the unapply handler will make the target leave the vehicle.
        // We don't need to iterate over Seats
        Base.RemoveAurasByType(AuraType.ControlVehicle);
    }

    //< Internal variable for sanity checks
    public ITransport RemovePassenger(WorldObject passenger)
    {
        var unit = passenger.AsUnit;

        if (unit == null)
            return null;

        if (unit.Vehicle != this)
            return null;

        var seat = GetSeatKeyValuePairForPassenger(unit);

        Log.Logger.Debug("Unit {0} exit vehicle entry {1} id {2} dbguid {3} seat {4}",
                         unit.GetName(),
                         Base.Entry,
                         VehicleInfo.Id,
                         Base.GUID.ToString(),
                         seat.Key);

        if (seat.Value.SeatInfo.CanEnterOrExit() && ++UsableSeatNum != 0)
            Base.SetNpcFlag(Base.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);

        // Enable gravity for passenger when he did not have it active before entering the vehicle
        if (seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.DisableGravity) && !seat.Value.Passenger.IsGravityDisabled)
            unit.SetDisableGravity(false);

        // Remove UNIT_FLAG_NOT_SELECTABLE if passenger did not have it before entering vehicle
        if (seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.PassengerNotSelectable) && !seat.Value.Passenger.IsUninteractible)
            unit.RemoveUnitFlag(UnitFlags.Uninteractible);

        seat.Value.Passenger.Reset();

        if (Base.IsTypeId(TypeId.Unit) && unit.IsTypeId(TypeId.Player) && seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
            Base.RemoveCharmedBy();

        if (Base.Location.IsInWorld)
            unit.MovementInfo.ResetTransport();

        // only for flyable vehicles
        if (unit.IsFlying)
            Base.SpellFactory.CastSpell(unit, SharedConst.VehicleSpellParachute, true);

        if (Base.IsTypeId(TypeId.Unit) && Base.AsCreature.IsAIEnabled)
            Base.AsCreature.AI.PassengerBoarded(unit, seat.Key, false);

        if (Base.IsTypeId(TypeId.Unit))
            _scriptManager.RunScript<IVehicleOnRemovePassenger>(p => p.OnRemovePassenger(this, unit), Base.AsCreature.GetScriptId());

        unit.Vehicle = null;

        return this;
    }

    public void RemovePendingEvent(VehicleJoinEvent e)
    {
        foreach (var @event in _pendingJoinEvents.Where(@event => @event == e))
        {
            _pendingJoinEvents.Remove(@event);

            break;
        }
    }

    public void RemovePendingEventsForPassenger(Unit passenger)
    {
        for (var i = 0; i < _pendingJoinEvents.Count; ++i)
        {
            var joinEvent = _pendingJoinEvents[i];

            if (joinEvent.Passenger != passenger)
                continue;

            joinEvent.ScheduleAbort();
            _pendingJoinEvents.Remove(joinEvent);
        }
    }

    public void RemovePendingEventsForSeat(sbyte seatId)
    {
        for (var i = 0; i < _pendingJoinEvents.Count; ++i)
        {
            var joinEvent = _pendingJoinEvents[i];

            if (joinEvent.Seat.Key != seatId)
                continue;

            joinEvent.ScheduleAbort();
            _pendingJoinEvents.Remove(joinEvent);
        }
    }

    public void Reset(bool evading = false)
    {
        if (!Base.IsTypeId(TypeId.Unit))
            return;

        Log.Logger.Debug("Vehicle.Reset (Entry: {0}, GuidLow: {1}, DBGuid: {2})", CreatureEntry, Base.GUID.ToString(), Base.AsCreature.SpawnId);

        ApplyAllImmunities();

        if (Base.IsAlive)
            InstallAllAccessories(evading);

        _scriptManager.RunScript<IVehicleOnReset>(p => p.OnReset(this), Base.AsCreature.GetScriptId());
    }

    public void Uninstall()
    {
        // @Prevent recursive uninstall call. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
        if (_status == Status.UnInstalling && !Base.HasUnitTypeMask(UnitTypeMask.Minion))
        {
            Log.Logger.Error("Vehicle GuidLow: {0}, Entry: {1} attempts to uninstall, but already has STATUS_UNINSTALLING! " +
                             "Check Uninstall/PassengerBoarded script hooks for errors.",
                             Base.GUID.ToString(),
                             Base.Entry);

            return;
        }

        _status = Status.UnInstalling;
        Log.Logger.Debug("Vehicle.Uninstall Entry: {0}, GuidLow: {1}", CreatureEntry, Base.GUID.ToString());
        RemoveAllPassengers();

        if (Base.IsTypeId(TypeId.Unit))
            _scriptManager.RunScript<IVehicleOnUninstall>(p => p.OnUninstall(this), Base.AsCreature.GetScriptId());
    }

    private void ApplyAllImmunities()
    {
        // This couldn't be done in DB, because some spells have MECHANIC_NONE

        // Vehicles should be immune on Knockback ...
        Base.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
        Base.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBackDest, true);

        // Mechanical units & vehicles ( which are not Bosses, they have own immunities in DB ) should be also immune on healing ( exceptions in switch below )
        if (Base.IsTypeId(TypeId.Unit) && Base.AsCreature.Template.CreatureType == CreatureType.Mechanical && !Base.AsCreature.IsWorldBoss)
        {
            // Heal & dispel ...
            Base.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.Heal, true);
            Base.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.HealPct, true);
            Base.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.Dispel, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.PeriodicHeal, true);

            // ... Shield & Immunity grant spells ...
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.SchoolImmunity, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModUnattackable, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.SchoolAbsorb, true);
            Base.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.Banish, true);
            Base.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.Shield, true);
            Base.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.ImmuneShield, true);

            // ... Resistance, Split damage, Change stats ...
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.DamageShield, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.SplitDamagePct, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModResistance, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModStat, true);
            Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDamagePercentTaken, true);
        }

        // Different immunities for vehicles goes below
        switch (VehicleInfo.Id)
        {
            // code below prevents a bug with movable cannons
            case 160: // Strand of the Ancients
            case 244: // Wintergrasp
            case 452: // Isle of Conquest
            case 510: // Isle of Conquest
            case 543: // Isle of Conquest
                Base.SetControlled(true, UnitState.Root);
                // why we need to apply this? we can simple add immunities to slow mechanic in DB
                Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDecreaseSpeed, true);

                break;

            case 335:                                                                                 // Salvaged Chopper
            case 336:                                                                                 // Salvaged Siege Engine
            case 338:                                                                                 // Salvaged Demolisher
                Base.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDamagePercentTaken, false); // Battering Ram

                break;
        }
    }

    private KeyValuePair<sbyte, VehicleSeat> GetSeatKeyValuePairForPassenger(Unit passenger)
    {
        foreach (var pair in Seats.Where(pair => pair.Value.Passenger.Guid == passenger.GUID))
            return pair;

        return Seats.Last();
    }

    private bool HasPendingEventForSeat(sbyte seatId)
    {
        return _pendingJoinEvents.Any(joinEvent => joinEvent.Seat.Key == seatId);
    }

    private void InitMovementInfoForBase()
    {
        var vehicleFlags = VehicleInfo.Flags;

        if (vehicleFlags.HasAnyFlag(VehicleFlags.NoStrafe))
            Base.AddUnitMovementFlag2(MovementFlag2.NoStrafe);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.NoJumping))
            Base.AddUnitMovementFlag2(MovementFlag2.NoJumping);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.Fullspeedturning))
            Base.AddUnitMovementFlag2(MovementFlag2.FullSpeedTurning);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.AllowPitching))
            Base.AddUnitMovementFlag2(MovementFlag2.AlwaysAllowPitching);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.Fullspeedpitching))
            Base.AddUnitMovementFlag2(MovementFlag2.FullSpeedPitching);
    }

    private void InstallAccessory(uint entry, sbyte seatId, bool minion, byte type, uint summonTime)
    {
        // @Prevent adding accessories when vehicle is uninstalling. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)

        if (_status == Status.UnInstalling)
        {
            Log.Logger.Error("Vehicle ({0}, Entry: {1}) attempts to install accessory (Entry: {2}) on seat {3} with STATUS_UNINSTALLING! " +
                             "Check Uninstall/PassengerBoarded script hooks for errors.",
                             Base.GUID.ToString(),
                             CreatureEntry,
                             entry,
                             seatId);

            return;
        }

        Log.Logger.Debug("Vehicle ({0}, Entry {1}): installing accessory (Entry: {2}) on seat: {3}", Base.GUID.ToString(), CreatureEntry, entry, seatId);

        var accessory = Base.SummonCreature(entry, Base.Location, (TempSummonType)type, TimeSpan.FromMilliseconds(summonTime));

        if (minion)
            accessory.AddUnitTypeMask(UnitTypeMask.Accessory);

        Base.HandleSpellClick(accessory, seatId);
    }
}