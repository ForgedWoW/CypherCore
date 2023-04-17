// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.V;

namespace Forged.MapServer.Entities;

public class VehicleSeat
{
    public PassengerInfo Passenger;
    public VehicleSeatAddon SeatAddon;
    public VehicleSeatRecord SeatInfo;

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