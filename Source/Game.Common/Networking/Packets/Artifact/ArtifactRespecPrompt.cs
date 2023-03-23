// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Artifact;

public class ArtifactRespecPrompt : ServerPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid NpcGUID;
	public ArtifactRespecPrompt() : base(ServerOpcodes.ArtifactRespecPrompt) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArtifactGUID);
		_worldPacket.WritePackedGuid(NpcGUID);
	}
}
