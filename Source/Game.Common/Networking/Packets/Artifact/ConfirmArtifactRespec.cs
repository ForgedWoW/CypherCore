// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Artifact;

public class ConfirmArtifactRespec : ClientPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid NpcGUID;
	public ConfirmArtifactRespec(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArtifactGUID = _worldPacket.ReadPackedGuid();
		NpcGUID = _worldPacket.ReadPackedGuid();
	}
}
