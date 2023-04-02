// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

public class UseItem : ClientPacket
{
    public SpellCastRequest Cast;
    public ObjectGuid CastItem;
    public byte PackSlot;
    public byte Slot;
    public UseItem(WorldPacket packet) : base(packet)
    {
        Cast = new SpellCastRequest();
    }

    public override void Read()
    {
        PackSlot = WorldPacket.ReadUInt8();
        Slot = WorldPacket.ReadUInt8();
        CastItem = WorldPacket.ReadPackedGuid();
        Cast.Read(WorldPacket);
    }
}