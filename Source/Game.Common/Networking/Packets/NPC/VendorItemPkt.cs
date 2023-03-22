// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class VendorItemPkt
{
	public int MuID;
	public int Type;
	public ItemInstance Item = new();
	public int Quantity = -1;
	public ulong Price;
	public int Durability;
	public int StackCount;
	public int ExtendedCostID;
	public int PlayerConditionFailed;
	public bool Locked;
	public bool DoNotFilterOnVendor;
	public bool Refundable;

	public void Write(WorldPacket data)
	{
		data.WriteUInt64(Price);
		data.WriteInt32(MuID);
		data.WriteInt32(Durability);
		data.WriteInt32(StackCount);
		data.WriteInt32(Quantity);
		data.WriteInt32(ExtendedCostID);
		data.WriteInt32(PlayerConditionFailed);
		data.WriteBits(Type, 3);
		data.WriteBit(Locked);
		data.WriteBit(DoNotFilterOnVendor);
		data.WriteBit(Refundable);
		data.FlushBits();

		Item.Write(data);
	}
}