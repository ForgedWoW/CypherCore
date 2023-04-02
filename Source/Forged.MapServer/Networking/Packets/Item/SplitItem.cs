// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class SplitItem : ClientPacket
{
    public byte FromPackSlot;
    public byte FromSlot;
    public InvUpdate Inv;
    public int Quantity;
    public byte ToPackSlot;
    public byte ToSlot;
    public SplitItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(WorldPacket);
        FromPackSlot = WorldPacket.ReadUInt8();
        FromSlot = WorldPacket.ReadUInt8();
        ToPackSlot = WorldPacket.ReadUInt8();
        ToSlot = WorldPacket.ReadUInt8();
        Quantity = WorldPacket.ReadInt32();
    }
}