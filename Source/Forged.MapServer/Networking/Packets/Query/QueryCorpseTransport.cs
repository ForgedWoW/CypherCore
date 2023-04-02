// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryCorpseTransport : ClientPacket
{
    public ObjectGuid Player;
    public ObjectGuid Transport;
    public QueryCorpseTransport(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Player = WorldPacket.ReadPackedGuid();
        Transport = WorldPacket.ReadPackedGuid();
    }
}