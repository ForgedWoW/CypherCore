// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public class OpenItem : ClientPacket
{
    public byte PackSlot;
    public byte Slot;
    public OpenItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Slot = WorldPacket.ReadUInt8();
        PackSlot = WorldPacket.ReadUInt8();
    }
}