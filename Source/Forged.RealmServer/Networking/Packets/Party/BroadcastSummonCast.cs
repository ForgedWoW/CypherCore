// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class BroadcastSummonCast : ServerPacket
{
	public ObjectGuid Target;

	public BroadcastSummonCast() : base(ServerOpcodes.BroadcastSummonCast) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
	}
}