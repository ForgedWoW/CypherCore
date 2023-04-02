// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Misc;

public class ReclaimCorpse : ClientPacket
{
    public ObjectGuid CorpseGUID;
    public ReclaimCorpse(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        CorpseGUID = WorldPacket.ReadPackedGuid();
    }
}