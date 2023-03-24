// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Artifact;

public class ArtifactXpGain : ServerPacket
{
	public ObjectGuid ArtifactGUID;
	public ulong Amount;
	public ArtifactXpGain() : base(ServerOpcodes.ArtifactXpGain) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArtifactGUID);
		_worldPacket.WriteUInt64(Amount);
	}
}
