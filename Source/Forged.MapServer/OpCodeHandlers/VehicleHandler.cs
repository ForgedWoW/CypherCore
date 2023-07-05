// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Vehicle;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class VehicleHandler : IWorldSessionHandler
{
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;

    public VehicleHandler(WorldSession session, ObjectAccessor objectAccessor)
    {
        _session = session;
        _objectAccessor = objectAccessor;
    }

    [WorldPacketHandler(ClientOpcodes.EjectPassenger)]
    private void HandleEjectPassenger(EjectPassenger packet)
    {
        var vehicle = _session.Player.VehicleKit;

        if (vehicle == null)
        {
            Log.Logger.Error("HandleEjectPassenger: {0} is not in a vehicle!", _session.Player.GUID.ToString());

            return;
        }

        if (packet.Passenger.IsUnit)
        {
            var unit = _objectAccessor.GetUnit(_session.Player, packet.Passenger);

            if (unit == null)
            {
                Log.Logger.Error("{0} tried to eject {1} from vehicle, but the latter was not found in world!", _session.Player.GUID.ToString(), packet.Passenger.ToString());

                return;
            }

            if (!unit.IsOnVehicle(vehicle.Base))
            {
                Log.Logger.Error("{0} tried to eject {1}, but they are not in the same vehicle", _session.Player.GUID.ToString(), packet.Passenger.ToString());

                return;
            }

            var seat = vehicle.GetSeatForPassenger(unit);

            if (seat.IsEjectable())
                unit.ExitVehicle();
            else
                Log.Logger.Error("{0} attempted to eject {1} from non-ejectable seat.", _session.Player.GUID.ToString(), packet.Passenger.ToString());
        }
        else
            Log.Logger.Error("HandleEjectPassenger: {0} tried to eject invalid {1}", _session.Player.GUID.ToString(), packet.Passenger.ToString());
    }

    [WorldPacketHandler(ClientOpcodes.MoveChangeVehicleSeats, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveChangeVehicleSeats(MoveChangeVehicleSeats packet)
    {
        var vehicleBase = _session.Player.VehicleBase;

        if (vehicleBase == null)
            return;

        var seat = _session.Player.Vehicle.GetSeatForPassenger(_session.Player);

        if (!seat.CanSwitchFromSeat())
        {
            Log.Logger.Error("HandleMoveChangeVehicleSeats, {0} tried to switch seats but current seatflags {1} don't permit that.",
                             _session.Player.GUID.ToString(),
                             seat.Flags);

            return;
        }

        _session.Player.ValidateMovementInfo(packet.Status);

        if (vehicleBase.GUID != packet.Status.Guid)
            return;

        vehicleBase.MovementInfo = packet.Status;

        if (packet.DstVehicle.IsEmpty)
            _session.Player.ChangeSeat(-1, packet.DstSeatIndex != 255);
        else
        {
            var vehUnit = _objectAccessor.GetUnit(_session.Player, packet.DstVehicle);

            var vehicle = vehUnit?.VehicleKit;

            if (vehicle != null && vehicle.HasEmptySeat((sbyte)packet.DstSeatIndex))
                vehUnit.HandleSpellClick(_session.Player, (sbyte)packet.DstSeatIndex);
        }
    }

    [WorldPacketHandler(ClientOpcodes.MoveDismissVehicle, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveDismissVehicle(MoveDismissVehicle packet)
    {
        var vehicleGUID = _session.Player.CharmedGUID;

        if (vehicleGUID.IsEmpty) // something wrong here...
            return;

        _session.Player.ValidateMovementInfo(packet.Status);
        _session.Player.MovementInfo = packet.Status;

        _session.Player.ExitVehicle();
    }

    [WorldPacketHandler(ClientOpcodes.MoveSetVehicleRecIdAck)]
    private void HandleMoveSetVehicleRecAck(MoveSetVehicleRecIdAck setVehicleRecIdAck)
    {
        _session.Player.ValidateMovementInfo(setVehicleRecIdAck.Data.Status);
    }

    [WorldPacketHandler(ClientOpcodes.RequestVehicleExit, Processing = PacketProcessing.Inplace)]
    private void HandleRequestVehicleExit(RequestVehicleExit packet)
    {
        if (packet == null) return;

        var vehicle = _session.Player.Vehicle;

        var seat = vehicle?.GetSeatForPassenger(_session.Player);

        if (seat == null)
            return;

        if (seat.CanEnterOrExit())
            _session.Player.ExitVehicle();
        else
            Log.Logger.Error("{0} tried to exit vehicle, but seatflags {1} (ID: {2}) don't permit that.",
                             _session.Player.GUID.ToString(),
                             seat.Id,
                             seat.Flags);
    }

    [WorldPacketHandler(ClientOpcodes.RequestVehicleNextSeat, Processing = PacketProcessing.Inplace)]
    private void HandleRequestVehicleNextSeat(RequestVehicleNextSeat packet)
    {
        if (packet == null) return;

        var vehicleBase = _session.Player.VehicleBase;

        if (vehicleBase == null)
            return;

        var seat = _session.Player.Vehicle.GetSeatForPassenger(_session.Player);

        if (!seat.CanSwitchFromSeat())
        {
            Log.Logger.Error("HandleRequestVehicleNextSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
                             _session.Player.GUID.ToString(),
                             seat.Flags);

            return;
        }

        _session.Player.ChangeSeat(-1);
    }

    [WorldPacketHandler(ClientOpcodes.RequestVehiclePrevSeat, Processing = PacketProcessing.Inplace)]
    private void HandleRequestVehiclePrevSeat(RequestVehiclePrevSeat packet)
    {
        if (packet == null) return;

        var vehicleBase = _session.Player.VehicleBase;

        if (vehicleBase == null)
            return;

        var seat = _session.Player.Vehicle.GetSeatForPassenger(_session.Player);

        if (!seat.CanSwitchFromSeat())
        {
            Log.Logger.Error("HandleRequestVehiclePrevSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
                             _session.Player.GUID.ToString(),
                             seat.Flags);

            return;
        }

        _session.Player.ChangeSeat(-1, false);
    }

    [WorldPacketHandler(ClientOpcodes.RequestVehicleSwitchSeat, Processing = PacketProcessing.Inplace)]
    private void HandleRequestVehicleSwitchSeat(RequestVehicleSwitchSeat packet)
    {
        var vehicleBase = _session.Player.VehicleBase;

        if (vehicleBase == null)
            return;

        var seat = _session.Player.Vehicle.GetSeatForPassenger(_session.Player);

        if (!seat.CanSwitchFromSeat())
        {
            Log.Logger.Error("HandleRequestVehicleSwitchSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
                             _session.Player.GUID.ToString(),
                             seat.Flags);

            return;
        }

        if (vehicleBase.GUID == packet.Vehicle)
            _session.Player.ChangeSeat((sbyte)packet.SeatIndex);
        else
        {
            var vehUnit = _objectAccessor.GetUnit(_session.Player, packet.Vehicle);

            if (vehUnit == null)
                return;

            var vehicle = vehUnit.VehicleKit;

            if (vehicle != null && vehicle.HasEmptySeat((sbyte)packet.SeatIndex))
                vehUnit.HandleSpellClick(_session.Player, (sbyte)packet.SeatIndex);
        }
    }

    [WorldPacketHandler(ClientOpcodes.RideVehicleInteract)]
    private void HandleRideVehicleInteract(RideVehicleInteract packet)
    {
        var player = _objectAccessor.GetPlayer(_session.Player, packet.Vehicle);

        if (player?.VehicleKit == null)
            return;

        if (!player.IsInRaidWith(_session.Player))
            return;

        if (!player.Location.IsWithinDistInMap(_session.Player, SharedConst.InteractionDistance))
            return;

        // Dont' allow players to enter player vehicle on arena
        if (_session.Player.Location.Map == null || _session.Player.Location.Map.IsBattleArena)
            return;

        _session.Player.EnterVehicle(player);
    }
}