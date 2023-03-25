// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Networking.Packets;

public class WhoIsResponse : ServerPacket
{
	public string AccountName;
	public WhoIsResponse() : base(ServerOpcodes.WhoIs) { }

	public override void Write()
	{
		_worldPacket.WriteBits(AccountName.GetByteCount(), 11);
		_worldPacket.WriteString(AccountName);
	}
}