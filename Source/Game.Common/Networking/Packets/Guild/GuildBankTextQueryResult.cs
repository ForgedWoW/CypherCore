// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Guild;

public class GuildBankTextQueryResult : ServerPacket
{
	public int Tab;
	public string Text;
	public GuildBankTextQueryResult() : base(ServerOpcodes.GuildBankTextQueryResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);

		_worldPacket.WriteBits(Text.GetByteCount(), 14);
		_worldPacket.WriteString(Text);
	}
}
