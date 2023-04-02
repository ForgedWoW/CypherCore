// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class SpiritHealerActivate : ClientPacket
{
    public ObjectGuid Healer;
    public SpiritHealerActivate(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Healer = WorldPacket.ReadPackedGuid();
    }
}