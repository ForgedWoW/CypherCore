// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class ObjectUpdateFailed : ClientPacket
{
	public ObjectGuid ObjectGUID;
	public ObjectUpdateFailed(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ObjectGUID = _worldPacket.ReadPackedGuid();
	}
}