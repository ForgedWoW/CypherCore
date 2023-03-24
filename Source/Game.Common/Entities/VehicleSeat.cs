// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.Structs.V;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Entities;

public class VehicleSeat
{
	public VehicleSeatRecord SeatInfo;
	public VehicleSeatAddon SeatAddon;
	public PassengerInfo Passenger;

	public VehicleSeat(VehicleSeatRecord seatInfo, VehicleSeatAddon seatAddon)
	{
		SeatInfo = seatInfo;
		SeatAddon = seatAddon;
		Passenger.Reset();
	}

	public bool IsEmpty()
	{
		return Passenger.Guid.IsEmpty;
	}
}
