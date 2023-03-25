// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

class ChangeRealmTicket : ClientPacket
{
	public uint Token;
	public Array<byte> Secret = new(32);
	public ChangeRealmTicket(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Token = _worldPacket.ReadUInt32();

		for (var i = 0; i < Secret.GetLimit(); ++i)
			Secret[i] = _worldPacket.ReadUInt8();
	}
}