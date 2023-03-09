// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildEventTabModified : ServerPacket
{
	public string Icon;
	public string Name;
	public int Tab;
	public GuildEventTabModified() : base(ServerOpcodes.GuildEventTabModified) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);

		_worldPacket.WriteBits(Name.GetByteCount(), 7);
		_worldPacket.WriteBits(Icon.GetByteCount(), 9);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(Name);
		_worldPacket.WriteString(Icon);
	}
}