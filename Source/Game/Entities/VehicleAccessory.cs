namespace Game.Entities;

public struct VehicleAccessory
{
	public VehicleAccessory(uint entry, sbyte seatId, bool isMinion, byte summonType, uint summonTime)
	{
		AccessoryEntry = entry;
		IsMinion = isMinion;
		SummonTime = summonTime;
		SeatId = seatId;
		SummonedType = summonType;
	}

	public uint AccessoryEntry;
	public bool IsMinion;
	public uint SummonTime;
	public sbyte SeatId;
	public byte SummonedType;
}