// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Artifact;

internal class ArtifactAddPower : ClientPacket
{
    public ObjectGuid ArtifactGUID;
    public ObjectGuid ForgeGUID;
    public Array<ArtifactPowerChoice> PowerChoices = new(1);
    public ArtifactAddPower(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ArtifactGUID = WorldPacket.ReadPackedGuid();
        ForgeGUID = WorldPacket.ReadPackedGuid();

        var powerCount = WorldPacket.ReadUInt32();

        for (var i = 0; i < powerCount; ++i)
        {
            ArtifactPowerChoice artifactPowerChoice;
            artifactPowerChoice.ArtifactPowerID = WorldPacket.ReadUInt32();
            artifactPowerChoice.Rank = WorldPacket.ReadUInt8();
            PowerChoices[i] = artifactPowerChoice;
        }
    }

    public struct ArtifactPowerChoice
    {
        public uint ArtifactPowerID;
        public byte Rank;
    }
}