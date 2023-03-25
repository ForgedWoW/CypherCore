// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class ArtifactSetAppearance : ClientPacket
{
	public ObjectGuid ArtifactGUID;
	public ObjectGuid ForgeGUID;
	public int ArtifactAppearanceID;
	public ArtifactSetAppearance(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArtifactGUID = _worldPacket.ReadPackedGuid();
		ForgeGUID = _worldPacket.ReadPackedGuid();
		ArtifactAppearanceID = _worldPacket.ReadInt32();
	}
}