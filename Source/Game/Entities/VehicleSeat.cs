using Game.DataStorage;

namespace Game.Entities;

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
		return Passenger.Guid.IsEmpty();
	}
}