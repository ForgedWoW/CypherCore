// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class AutoStoreBagItem : ClientPacket
{
    public byte ContainerSlotA;
    public byte ContainerSlotB;
    public InvUpdate Inv;
    public byte SlotA;
    public AutoStoreBagItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(WorldPacket);
        ContainerSlotB = WorldPacket.ReadUInt8();
        ContainerSlotA = WorldPacket.ReadUInt8();
        SlotA = WorldPacket.ReadUInt8();
    }
}