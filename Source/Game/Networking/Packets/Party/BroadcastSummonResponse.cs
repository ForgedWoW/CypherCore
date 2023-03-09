// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class BroadcastSummonResponse : ServerPacket
{
	public ObjectGuid Target;
	public bool Accepted;

	public BroadcastSummonResponse() : base(ServerOpcodes.BroadcastSummonResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteBit(Accepted);
		_worldPacket.FlushBits();
	}
}