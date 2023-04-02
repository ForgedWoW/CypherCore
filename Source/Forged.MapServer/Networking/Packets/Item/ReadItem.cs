// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

internal class ReadItem : ClientPacket
{
    public byte PackSlot;
    public byte Slot;
    public ReadItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PackSlot = WorldPacket.ReadUInt8();
        Slot = WorldPacket.ReadUInt8();
    }
}