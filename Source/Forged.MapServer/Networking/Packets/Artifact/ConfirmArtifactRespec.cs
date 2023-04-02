// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Artifact;

internal class ConfirmArtifactRespec : ClientPacket
{
    public ObjectGuid ArtifactGUID;
    public ObjectGuid NpcGUID;
    public ConfirmArtifactRespec(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ArtifactGUID = WorldPacket.ReadPackedGuid();
        NpcGUID = WorldPacket.ReadPackedGuid();
    }
}