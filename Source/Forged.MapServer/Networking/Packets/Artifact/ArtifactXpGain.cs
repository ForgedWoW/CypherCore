// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Artifact;

internal class ArtifactXpGain : ServerPacket
{
    public ulong Amount;
    public ObjectGuid ArtifactGUID;
    public ArtifactXpGain() : base(ServerOpcodes.ArtifactXpGain) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ArtifactGUID);
        WorldPacket.WriteUInt64(Amount);
    }
}