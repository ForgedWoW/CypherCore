// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildDemoteMember : ClientPacket
{
    public ObjectGuid Demotee;
    public GuildDemoteMember(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Demotee = WorldPacket.ReadPackedGuid();
    }
}