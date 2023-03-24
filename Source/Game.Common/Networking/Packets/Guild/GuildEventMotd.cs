// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Guild;

public class GuildEventMotd : ServerPacket
{
	public string MotdText;
	public GuildEventMotd() : base(ServerOpcodes.GuildEventMotd) { }

	public override void Write()
	{
		_worldPacket.WriteBits(MotdText.GetByteCount(), 11);
		_worldPacket.WriteString(MotdText);
	}
}
