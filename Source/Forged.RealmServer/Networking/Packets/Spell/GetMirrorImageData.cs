// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class GetMirrorImageData : ClientPacket
{
	public ObjectGuid UnitGUID;
	public GetMirrorImageData(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UnitGUID = _worldPacket.ReadPackedGuid();
	}
}