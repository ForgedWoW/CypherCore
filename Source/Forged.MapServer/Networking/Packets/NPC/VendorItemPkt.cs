// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.NPC;

public class VendorItemPkt
{
    public bool DoNotFilterOnVendor;
    public int Durability;
    public int ExtendedCostID;
    public ItemInstance Item = new();
    public bool Locked;
    public int MuID;
    public int PlayerConditionFailed;
    public ulong Price;
    public int Quantity = -1;
    public bool Refundable;
    public int StackCount;
    public int Type;

    public void Write(WorldPacket data)
    {
        data.WriteUInt64(Price);
        data.WriteInt32(MuID);
        data.WriteInt32(Type);
        data.WriteInt32(Durability);
        data.WriteInt32(StackCount);
        data.WriteInt32(Quantity);
        data.WriteInt32(ExtendedCostID);
        data.WriteInt32(PlayerConditionFailed);
        data.WriteBit(Locked);
        data.WriteBit(DoNotFilterOnVendor);
        data.WriteBit(Refundable);
        data.FlushBits();

        Item.Write(data);
    }
}