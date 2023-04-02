// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Totem;

internal class TotemDestroyed : ClientPacket
{
    public byte Slot;
    public ObjectGuid TotemGUID;
    public TotemDestroyed(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Slot = WorldPacket.ReadUInt8();
        TotemGUID = WorldPacket.ReadPackedGuid();
    }
}