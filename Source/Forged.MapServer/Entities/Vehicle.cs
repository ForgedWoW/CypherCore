// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.V;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting.Interfaces.IVehicle;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities;

public class Vehicle : ITransport
{
    public enum Status
    {
        None,
        Installed,
        UnInstalling,
    }

    public Dictionary<sbyte, VehicleSeat> Seats = new();
    public uint UsableSeatNum; //< Number of seats that match VehicleSeatEntry.UsableByPlayer, used for proper display flags

    private readonly Unit _me;
    private readonly VehicleRecord _vehicleInfo; //< DBC data for vehicle

    private readonly uint _creatureEntry; //< Can be different than the entry of _me in case of players

    private readonly List<VehicleJoinEvent> _pendingJoinEvents = new();
    private Status _status; //< Internal variable for sanity checks

    public Vehicle(Unit unit, VehicleRecord vehInfo, uint creatureEntry)
    {
        _me = unit;
        _vehicleInfo = vehInfo;
        _creatureEntry = creatureEntry;
        _status = Status.None;

        for (uint i = 0; i < SharedConst.MaxVehicleSeats; ++i)
        {
            uint seatId = _vehicleInfo.SeatID[i];

            if (seatId != 0)
            {
                var veSeat = CliDB.VehicleSeatStorage.LookupByKey(seatId);

                if (veSeat != null)
                {
                    var addon = Global.ObjectMgr.GetVehicleSeatAddon(seatId);
                    Seats.Add((sbyte)i, new VehicleSeat(veSeat, addon));

                    if (veSeat.CanEnterOrExit())
                        ++UsableSeatNum;
                }
            }
        }

        // Set or remove correct flags based on available seats. Will overwrite db data (if wrong).
        if (UsableSeatNum != 0)
            _me.SetNpcFlag(_me.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);
        else
            _me.RemoveNpcFlag(_me.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);

        InitMovementInfoForBase();
    }

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
                         _me.Entry,
                         _vehicleInfo.Id,
                         _me.GUID.ToString(),
                         seat.Key);

        if (seat.Value.SeatInfo.CanEnterOrExit() && ++UsableSeatNum != 0)
            _me.SetNpcFlag(_me.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);

        // Enable gravity for passenger when he did not have it active before entering the vehicle
        if (seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.DisableGravity) && !seat.Value.Passenger.IsGravityDisabled)
            unit.SetDisableGravity(false);

        // Remove UNIT_FLAG_NOT_SELECTABLE if passenger did not have it before entering vehicle
        if (seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.PassengerNotSelectable) && !seat.Value.Passenger.IsUninteractible)
            unit.RemoveUnitFlag(UnitFlags.Uninteractible);

        seat.Value.Passenger.Reset();

        if (_me.IsTypeId(TypeId.Unit) && unit.IsTypeId(TypeId.Player) && seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
            _me.RemoveCharmedBy(unit);

        if (_me.Location.IsInWorld)
            unit.MovementInfo.ResetTransport();

        // only for flyable vehicles
        if (unit.IsFlying)
            _me.CastSpell(unit, SharedConst.VehicleSpellParachute, true);

        if (_me.IsTypeId(TypeId.Unit) && _me.AsCreature.IsAIEnabled)
            _me.AsCreature.AI.PassengerBoarded(unit, seat.Key, false);

        if (GetBase().IsTypeId(TypeId.Unit))
            Global.ScriptMgr.RunScript<IVehicleOnRemovePassenger>(p => p.OnRemovePassenger(this, unit), GetBase().AsCreature.GetScriptId());

        unit.Vehicle = null;

        return this;
    }

    public ObjectGuid GetTransportGUID()
    {
        return GetBase().GUID;
    }

    public float GetTransportOrientation()
    {
        return GetBase().Location.Orientation;
    }

    public void AddPassenger(WorldObject passenger)
    {
        Log.Logger.Fatal("Vehicle cannot directly gain passengers without auras");
    }

    public void CalculatePassengerPosition(Position pos)
    {
        ITransport.CalculatePassengerPosition(pos,
                                              GetBase().Location.X,
                                              GetBase().Location.Y,
                                              GetBase().Location.Z,
                                              GetBase().Location.Orientation);
    }

    public void CalculatePassengerOffset(Position pos)
    {
        ITransport.CalculatePassengerOffset(pos,
                                            GetBase().Location.X,
                                            GetBase().Location.Y,
                                            GetBase().Location.Z,
                                            GetBase().Location.Orientation);
    }

    public int GetMapIdForSpawning()
    {
        return (int)GetBase().Location.MapId;
    }

    public void Install()
    {
        _status = Status.Installed;

        if (GetBase().IsTypeId(TypeId.Unit))
            Global.ScriptMgr.RunScript<IVehicleOnInstall>(p => p.OnInstall(this), GetBase().AsCreature.GetScriptId());
    }

    public void InstallAllAccessories(bool evading)
    {
        if (GetBase().IsTypeId(TypeId.Player) || !evading)
            RemoveAllPassengers(); // We might have aura's saved in the DB with now invalid casters - remove

        var accessories = Global.ObjectMgr.GetVehicleAccessoryList(this);

        if (accessories == null)
            return;

        foreach (var acc in accessories)
            if (!evading || acc.IsMinion) // only install minions on evade mode
                InstallAccessory(acc.AccessoryEntry, acc.SeatId, acc.IsMinion, acc.SummonedType, acc.SummonTime);
    }

    public void Uninstall()
    {
        // @Prevent recursive uninstall call. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
        if (_status == Status.UnInstalling && !GetBase().HasUnitTypeMask(UnitTypeMask.Minion))
        {
            Log.Logger.Error("Vehicle GuidLow: {0}, Entry: {1} attempts to uninstall, but already has STATUS_UNINSTALLING! " +
                             "Check Uninstall/PassengerBoarded script hooks for errors.",
                             _me.GUID.ToString(),
                             _me.Entry);

            return;
        }

        _status = Status.UnInstalling;
        Log.Logger.Debug("Vehicle.Uninstall Entry: {0}, GuidLow: {1}", _creatureEntry, _me.GUID.ToString());
        RemoveAllPassengers();

        if (GetBase().IsTypeId(TypeId.Unit))
            Global.ScriptMgr.RunScript<IVehicleOnUninstall>(p => p.OnUninstall(this), GetBase().AsCreature.GetScriptId());
    }

    public void Reset(bool evading = false)
    {
        if (!GetBase().IsTypeId(TypeId.Unit))
            return;

        Log.Logger.Debug("Vehicle.Reset (Entry: {0}, GuidLow: {1}, DBGuid: {2})", GetCreatureEntry(), _me.GUID.ToString(), _me.AsCreature.SpawnId);

        ApplyAllImmunities();

        if (GetBase().IsAlive)
            InstallAllAccessories(evading);

        Global.ScriptMgr.RunScript<IVehicleOnReset>(p => p.OnReset(this), GetBase().AsCreature.GetScriptId());
    }

    public void RemoveAllPassengers()
    {
        Log.Logger.Debug("Vehicle.RemoveAllPassengers. Entry: {0}, GuidLow: {1}", _creatureEntry, _me.GUID.ToString());

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
        _me.RemoveAurasByType(AuraType.ControlVehicle);
    }

    public bool HasEmptySeat(sbyte seatId)
    {
        var seat = Seats.LookupByKey(seatId);

        if (seat == null)
            return false;

        return seat.IsEmpty();
    }

    public Unit GetPassenger(sbyte seatId)
    {
        var seat = Seats.LookupByKey(seatId);

        if (seat == null)
            return null;

        return Global.ObjAccessor.GetUnit(GetBase(), seat.Passenger.Guid);
    }

    public VehicleSeat GetNextEmptySeat(sbyte seatId, bool next)
    {
        var seat = Seats.LookupByKey(seatId);

        if (seat == null)
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

    /// <summary>
    ///     Gets the vehicle seat addon data for the seat of a passenger
    /// </summary>
    /// <param name="passenger"> Identifier for the current seat user </param>
    /// <returns> The seat addon data for the currently used seat of a passenger </returns>
    public VehicleSeatAddon GetSeatAddonForSeatOfPassenger(Unit passenger)
    {
        foreach (var pair in Seats)
            if (!pair.Value.IsEmpty() && pair.Value.Passenger.Guid == passenger.GUID)
                return pair.Value.SeatAddon;

        return null;
    }

    public bool AddVehiclePassenger(Unit unit, sbyte seatId)
    {
        // @Prevent adding passengers when vehicle is uninstalling. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
        if (_status == Status.UnInstalling)
        {
            Log.Logger.Error("Passenger GuidLow: {0}, Entry: {1}, attempting to board vehicle GuidLow: {2}, Entry: {3} during uninstall! SeatId: {4}",
                             unit.GUID.ToString(),
                             unit.Entry,
                             _me.GUID.ToString(),
                             _me.Entry,
                             seatId);

            return false;
        }

        Log.Logger.Debug("Unit {0} scheduling enter vehicle (entry: {1}, vehicleId: {2}, guid: {3} (dbguid: {4}) on seat {5}",
                         unit.GetName(),
                         _me.Entry,
                         _vehicleInfo.Id,
                         _me.GUID.ToString(),
                         (_me.IsTypeId(TypeId.Unit) ? _me.AsCreature.SpawnId : 0),
                         seatId);

        // The seat selection code may kick other passengers off the vehicle.
        // While the validity of the following may be arguable, it is possible that when such a passenger
        // exits the vehicle will dismiss. That's why the actual adding the passenger to the vehicle is scheduled
        // asynchronously, so it can be cancelled easily in case the vehicle is uninstalled meanwhile.
        VehicleJoinEvent e = new(this, unit);
        unit.Events.AddEvent(e, unit.Events.CalculateTime(TimeSpan.Zero));

        KeyValuePair<sbyte, VehicleSeat> seat = new();

        if (seatId < 0) // no specific seat requirement
        {
            foreach (var _seat in Seats)
            {
                seat = _seat;

                if (seat.Value.IsEmpty() && !HasPendingEventForSeat(seat.Key) && (_seat.Value.SeatInfo.CanEnterOrExit() || _seat.Value.SeatInfo.IsUsableByOverride()))
                    break;
            }

            if (seat.Value == null) // no available seat
            {
                e.ScheduleAbort();

                return false;
            }

            e.Seat = seat;
            _pendingJoinEvents.Add(e);
        }
        else
        {
            seat = new KeyValuePair<sbyte, VehicleSeat>(seatId, Seats.LookupByKey(seatId));

            if (seat.Value == null)
            {
                e.ScheduleAbort();

                return false;
            }

            e.Seat = seat;
            _pendingJoinEvents.Add(e);

            if (!seat.Value.IsEmpty())
            {
                var passenger = Global.ObjAccessor.GetUnit(GetBase(), seat.Value.Passenger.Guid);
                passenger.ExitVehicle();
            }
        }

        return true;
    }

    public void RelocatePassengers()
    {
        List<Tuple<Unit, Position>> seatRelocation = new();

        // not sure that absolute position calculation is correct, it must depend on vehicle pitch angle
        foreach (var pair in Seats)
        {
            var passenger = Global.ObjAccessor.GetUnit(GetBase(), pair.Value.Passenger.Guid);

            if (passenger != null)
            {
                var pos = passenger.MovementInfo.Transport.Pos.Copy();
                CalculatePassengerPosition(pos);

                seatRelocation.Add(Tuple.Create(passenger, pos));
            }
        }

        foreach (var (passenger, position) in seatRelocation)
            ITransport.UpdatePassengerPosition(this, _me.Location.Map, passenger, position, false);
    }

    public bool IsVehicleInUse()
    {
        foreach (var pair in Seats)
            if (!pair.Value.IsEmpty())
                return true;

        return false;
    }

    public bool IsControllableVehicle()
    {
        foreach (var itr in Seats)
            if (itr.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
                return true;

        return false;
    }

    public VehicleSeatRecord GetSeatForPassenger(Unit passenger)
    {
        foreach (var pair in Seats)
            if (pair.Value.Passenger.Guid == passenger.GUID)
                return pair.Value.SeatInfo;

        return null;
    }

    public byte GetAvailableSeatCount()
    {
        byte ret = 0;

        foreach (var pair in Seats)
            if (pair.Value.IsEmpty() && !HasPendingEventForSeat(pair.Key) && (pair.Value.SeatInfo.CanEnterOrExit() || pair.Value.SeatInfo.IsUsableByOverride()))
                ++ret;

        return ret;
    }

    public void RemovePendingEvent(VehicleJoinEvent e)
    {
        foreach (var Event in _pendingJoinEvents)
            if (Event == e)
            {
                _pendingJoinEvents.Remove(Event);

                break;
            }
    }

    public void RemovePendingEventsForSeat(sbyte seatId)
    {
        for (var i = 0; i < _pendingJoinEvents.Count; ++i)
        {
            var joinEvent = _pendingJoinEvents[i];

            if (joinEvent.Seat.Key == seatId)
            {
                joinEvent.ScheduleAbort();
                _pendingJoinEvents.Remove(joinEvent);
            }
        }
    }

    public void RemovePendingEventsForPassenger(Unit passenger)
    {
        for (var i = 0; i < _pendingJoinEvents.Count; ++i)
        {
            var joinEvent = _pendingJoinEvents[i];

            if (joinEvent.Passenger == passenger)
            {
                joinEvent.ScheduleAbort();
                _pendingJoinEvents.Remove(joinEvent);
            }
        }
    }

    public TimeSpan GetDespawnDelay()
    {
        var vehicleTemplate = Global.ObjectMgr.GetVehicleTemplate(this);

        if (vehicleTemplate != null)
            return vehicleTemplate.DespawnDelay;

        return TimeSpan.FromMilliseconds(1);
    }

    public string GetDebugInfo()
    {
        var str = new StringBuilder("Vehicle seats:\n");

        foreach (var (id, seat) in Seats)
            str.Append($"seat {id}: {(seat.IsEmpty() ? "empty" : seat.Passenger.Guid)}\n");

        str.Append("Vehicle pending events:");

        if (_pendingJoinEvents.Empty())
        {
            str.Append(" none");
        }
        else
        {
            str.Append("\n");

            foreach (var joinEvent in _pendingJoinEvents)
                str.Append($"seat {joinEvent.Seat.Key}: {joinEvent.Passenger.GUID}\n");
        }

        return str.ToString();
    }

    public Unit GetBase()
    {
        return _me;
    }

    public VehicleRecord GetVehicleInfo()
    {
        return _vehicleInfo;
    }

    public uint GetCreatureEntry()
    {
        return _creatureEntry;
    }

    public static implicit operator bool(Vehicle vehicle)
    {
        return vehicle != null;
    }

    private void ApplyAllImmunities()
    {
        // This couldn't be done in DB, because some spells have MECHANIC_NONE

        // Vehicles should be immune on Knockback ...
        _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
        _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBackDest, true);

        // Mechanical units & vehicles ( which are not Bosses, they have own immunities in DB ) should be also immune on healing ( exceptions in switch below )
        if (_me.IsTypeId(TypeId.Unit) && _me.AsCreature.Template.CreatureType == CreatureType.Mechanical && !_me.AsCreature.IsWorldBoss)
        {
            // Heal & dispel ...
            _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.Heal, true);
            _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.HealPct, true);
            _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.Dispel, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.PeriodicHeal, true);

            // ... Shield & Immunity grant spells ...
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.SchoolImmunity, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModUnattackable, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.SchoolAbsorb, true);
            _me.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.Banish, true);
            _me.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.Shield, true);
            _me.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.ImmuneShield, true);

            // ... Resistance, Split damage, Change stats ...
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.DamageShield, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.SplitDamagePct, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModResistance, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModStat, true);
            _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDamagePercentTaken, true);
        }

        // Different immunities for vehicles goes below
        switch (GetVehicleInfo().Id)
        {
            // code below prevents a bug with movable cannons
            case 160: // Strand of the Ancients
            case 244: // Wintergrasp
            case 452: // Isle of Conquest
            case 510: // Isle of Conquest
            case 543: // Isle of Conquest
                _me.SetControlled(true, UnitState.Root);
                // why we need to apply this? we can simple add immunities to slow mechanic in DB
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDecreaseSpeed, true);

                break;
            case 335:                                                                                // Salvaged Chopper
            case 336:                                                                                // Salvaged Siege Engine
            case 338:                                                                                // Salvaged Demolisher
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDamagePercentTaken, false); // Battering Ram

                break;
            default:
                break;
        }
    }

    private void InstallAccessory(uint entry, sbyte seatId, bool minion, byte type, uint summonTime)
    {
        // @Prevent adding accessories when vehicle is uninstalling. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)

        if (_status == Status.UnInstalling)
        {
            Log.Logger.Error("Vehicle ({0}, Entry: {1}) attempts to install accessory (Entry: {2}) on seat {3} with STATUS_UNINSTALLING! " +
                             "Check Uninstall/PassengerBoarded script hooks for errors.",
                             _me.GUID.ToString(),
                             GetCreatureEntry(),
                             entry,
                             seatId);

            return;
        }

        Log.Logger.Debug("Vehicle ({0}, Entry {1}): installing accessory (Entry: {2}) on seat: {3}", _me.GUID.ToString(), GetCreatureEntry(), entry, seatId);

        var accessory = _me.SummonCreature(entry, _me.Location, (TempSummonType)type, TimeSpan.FromMilliseconds(summonTime));

        if (minion)
            accessory.AddUnitTypeMask(UnitTypeMask.Accessory);

        _me.HandleSpellClick(accessory, seatId);

        // If for some reason adding accessory to vehicle fails it will unsummon in
        // @VehicleJoinEvent.Abort
    }

    private void InitMovementInfoForBase()
    {
        var vehicleFlags = (VehicleFlags)GetVehicleInfo().Flags;

        if (vehicleFlags.HasAnyFlag(VehicleFlags.NoStrafe))
            _me.AddUnitMovementFlag2(MovementFlag2.NoStrafe);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.NoJumping))
            _me.AddUnitMovementFlag2(MovementFlag2.NoJumping);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.Fullspeedturning))
            _me.AddUnitMovementFlag2(MovementFlag2.FullSpeedTurning);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.AllowPitching))
            _me.AddUnitMovementFlag2(MovementFlag2.AlwaysAllowPitching);

        if (vehicleFlags.HasAnyFlag(VehicleFlags.Fullspeedpitching))
            _me.AddUnitMovementFlag2(MovementFlag2.FullSpeedPitching);
    }

    private KeyValuePair<sbyte, VehicleSeat> GetSeatKeyValuePairForPassenger(Unit passenger)
    {
        foreach (var pair in Seats)
            if (pair.Value.Passenger.Guid == passenger.GUID)
                return pair;

        return Seats.Last();
    }

    private bool HasPendingEventForSeat(sbyte seatId)
    {
        for (var i = 0; i < _pendingJoinEvents.Count; ++i)
        {
            var joinEvent = _pendingJoinEvents[i];

            if (joinEvent.Seat.Key == seatId)
                return true;
        }

        return false;
    }
}