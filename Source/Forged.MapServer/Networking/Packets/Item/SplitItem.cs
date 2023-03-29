// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class SplitItem : ClientPacket
{
    public byte ToSlot;
    public byte ToPackSlot;
    public byte FromPackSlot;
    public int Quantity;
    public InvUpdate Inv;
    public byte FromSlot;
    public SplitItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(_worldPacket);
        FromPackSlot = _worldPacket.ReadUInt8();
        FromSlot = _worldPacket.ReadUInt8();
        ToPackSlot = _worldPacket.ReadUInt8();
        ToSlot = _worldPacket.ReadUInt8();
        Quantity = _worldPacket.ReadInt32();
    }
}