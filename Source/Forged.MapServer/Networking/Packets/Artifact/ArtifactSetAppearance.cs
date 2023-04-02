// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Artifact;

internal class ArtifactSetAppearance : ClientPacket
{
    public int ArtifactAppearanceID;
    public ObjectGuid ArtifactGUID;
    public ObjectGuid ForgeGUID;
    public ArtifactSetAppearance(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ArtifactGUID = WorldPacket.ReadPackedGuid();
        ForgeGUID = WorldPacket.ReadPackedGuid();
        ArtifactAppearanceID = WorldPacket.ReadInt32();
    }
}