﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.MoveDismissVehicle, Processing = PacketProcessing.ThreadSafe)]
	void HandleMoveDismissVehicle(MoveDismissVehicle packet)
	{
		var vehicleGUID = Player.CharmedGUID;

		if (vehicleGUID.IsEmpty) // something wrong here...
			return;

		Player.ValidateMovementInfo(packet.Status);
		Player.MovementInfo = packet.Status;

		Player.ExitVehicle();
	}

	[WorldPacketHandler(ClientOpcodes.RequestVehiclePrevSeat, Processing = PacketProcessing.Inplace)]
	void HandleRequestVehiclePrevSeat(RequestVehiclePrevSeat packet)
	{
		var vehicle_base = Player.VehicleBase;

		if (!vehicle_base)
			return;

		var seat = Player.Vehicle1.GetSeatForPassenger(Player);

		if (!seat.CanSwitchFromSeat())
		{
			Log.outError(LogFilter.Network,
						"HandleRequestVehiclePrevSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
						Player.GUID.ToString(),
						seat.Flags);

			return;
		}

		Player.ChangeSeat(-1, false);
	}

	[WorldPacketHandler(ClientOpcodes.RequestVehicleNextSeat, Processing = PacketProcessing.Inplace)]
	void HandleRequestVehicleNextSeat(RequestVehicleNextSeat packet)
	{
		var vehicle_base = Player.VehicleBase;

		if (!vehicle_base)
			return;

		var seat = Player.Vehicle1.GetSeatForPassenger(Player);

		if (!seat.CanSwitchFromSeat())
		{
			Log.outError(LogFilter.Network,
						"HandleRequestVehicleNextSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
						Player.GUID.ToString(),
						seat.Flags);

			return;
		}

		Player.ChangeSeat(-1, true);
	}

	[WorldPacketHandler(ClientOpcodes.MoveChangeVehicleSeats, Processing = PacketProcessing.ThreadSafe)]
	void HandleMoveChangeVehicleSeats(MoveChangeVehicleSeats packet)
	{
		var vehicle_base = Player.VehicleBase;

		if (!vehicle_base)
			return;

		var seat = Player.Vehicle1.GetSeatForPassenger(Player);

		if (!seat.CanSwitchFromSeat())
		{
			Log.outError(LogFilter.Network,
						"HandleMoveChangeVehicleSeats, {0} tried to switch seats but current seatflags {1} don't permit that.",
						Player.GUID.ToString(),
						seat.Flags);

			return;
		}

		Player.ValidateMovementInfo(packet.Status);

		if (vehicle_base.GUID != packet.Status.Guid)
			return;

		vehicle_base.MovementInfo = packet.Status;

		if (packet.DstVehicle.IsEmpty)
		{
			Player.ChangeSeat(-1, packet.DstSeatIndex != 255);
		}
		else
		{
			var vehUnit = Global.ObjAccessor.GetUnit(Player, packet.DstVehicle);

			if (vehUnit)
			{
				var vehicle = vehUnit.VehicleKit1;

				if (vehicle)
					if (vehicle.HasEmptySeat((sbyte)packet.DstSeatIndex))
						vehUnit.HandleSpellClick(Player, (sbyte)packet.DstSeatIndex);
			}
		}
	}

	[WorldPacketHandler(ClientOpcodes.RequestVehicleSwitchSeat, Processing = PacketProcessing.Inplace)]
	void HandleRequestVehicleSwitchSeat(RequestVehicleSwitchSeat packet)
	{
		var vehicle_base = Player.VehicleBase;

		if (!vehicle_base)
			return;

		var seat = Player.Vehicle1.GetSeatForPassenger(Player);

		if (!seat.CanSwitchFromSeat())
		{
			Log.outError(LogFilter.Network,
						"HandleRequestVehicleSwitchSeat: {0} tried to switch seats but current seatflags {1} don't permit that.",
						Player.GUID.ToString(),
						seat.Flags);

			return;
		}

		if (vehicle_base.GUID == packet.Vehicle)
		{
			Player.ChangeSeat((sbyte)packet.SeatIndex);
		}
		else
		{
			var vehUnit = Global.ObjAccessor.GetUnit(Player, packet.Vehicle);

			if (vehUnit)
			{
				var vehicle = vehUnit.VehicleKit1;

				if (vehicle)
					if (vehicle.HasEmptySeat((sbyte)packet.SeatIndex))
						vehUnit.HandleSpellClick(Player, (sbyte)packet.SeatIndex);
			}
		}
	}

	[WorldPacketHandler(ClientOpcodes.RideVehicleInteract)]
	void HandleRideVehicleInteract(RideVehicleInteract packet)
	{
		var player = Global.ObjAccessor.GetPlayer(_player, packet.Vehicle);

		if (player)
		{
			if (!player.VehicleKit1)
				return;

			if (!player.IsInRaidWith(Player))
				return;

			if (!player.IsWithinDistInMap(Player, SharedConst.InteractionDistance))
				return;

			// Dont' allow players to enter player vehicle on arena
			if (!_player.Map || _player.Map.IsBattleArena)
				return;

			Player.EnterVehicle(player);
		}
	}

	[WorldPacketHandler(ClientOpcodes.EjectPassenger)]
	void HandleEjectPassenger(EjectPassenger packet)
	{
		var vehicle = Player.VehicleKit1;

		if (!vehicle)
		{
			Log.outError(LogFilter.Network, "HandleEjectPassenger: {0} is not in a vehicle!", Player.GUID.ToString());

			return;
		}

		if (packet.Passenger.IsUnit)
		{
			var unit = Global.ObjAccessor.GetUnit(Player, packet.Passenger);

			if (!unit)
			{
				Log.outError(LogFilter.Network, "{0} tried to eject {1} from vehicle, but the latter was not found in world!", Player.GUID.ToString(), packet.Passenger.ToString());

				return;
			}

			if (!unit.IsOnVehicle(vehicle.GetBase()))
			{
				Log.outError(LogFilter.Network, "{0} tried to eject {1}, but they are not in the same vehicle", Player.GUID.ToString(), packet.Passenger.ToString());

				return;
			}

			var seat = vehicle.GetSeatForPassenger(unit);

			if (seat.IsEjectable())
				unit.ExitVehicle();
			else
				Log.outError(LogFilter.Network, "{0} attempted to eject {1} from non-ejectable seat.", Player.GUID.ToString(), packet.Passenger.ToString());
		}

		else
		{
			Log.outError(LogFilter.Network, "HandleEjectPassenger: {0} tried to eject invalid {1}", Player.GUID.ToString(), packet.Passenger.ToString());
		}
	}

	[WorldPacketHandler(ClientOpcodes.RequestVehicleExit, Processing = PacketProcessing.Inplace)]
	void HandleRequestVehicleExit(RequestVehicleExit packet)
	{
		var vehicle = Player.Vehicle1;

		if (vehicle)
		{
			var seat = vehicle.GetSeatForPassenger(Player);

			if (seat != null)
			{
				if (seat.CanEnterOrExit())
					Player.ExitVehicle();
				else
					Log.outError(LogFilter.Network,
								"{0} tried to exit vehicle, but seatflags {1} (ID: {2}) don't permit that.",
								Player.GUID.ToString(),
								seat.Id,
								seat.Flags);
			}
		}
	}

	[WorldPacketHandler(ClientOpcodes.MoveSetVehicleRecIdAck)]
	void HandleMoveSetVehicleRecAck(MoveSetVehicleRecIdAck setVehicleRecIdAck)
	{
		Player.ValidateMovementInfo(setVehicleRecIdAck.Data.Status);
	}
}