// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Networking.Packets;

class ChatPlayerNotfound : ServerPacket
{
	readonly string Name;

	public ChatPlayerNotfound(string name) : base(ServerOpcodes.ChatPlayerNotfound)
	{
		Name = name;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteString(Name);
	}
}