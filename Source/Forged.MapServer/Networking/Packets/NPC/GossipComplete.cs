// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class GossipComplete : ServerPacket
{
	public bool SuppressSound;

	public GossipComplete() : base(ServerOpcodes.GossipComplete) { }

	public override void Write()
	{
		_worldPacket.WriteBit(SuppressSound);
		_worldPacket.FlushBits();
	}
}