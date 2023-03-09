// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class DeclineGuildInvites : ClientPacket
{
	public bool Allow;
	public DeclineGuildInvites(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Allow = _worldPacket.HasBit();
	}
}