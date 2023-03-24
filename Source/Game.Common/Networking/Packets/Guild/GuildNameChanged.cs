// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Guild;

public class GuildNameChanged : ServerPacket
{
	public ObjectGuid GuildGUID;
	public string GuildName;
	public GuildNameChanged() : base(ServerOpcodes.GuildNameChanged) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteBits(GuildName.GetByteCount(), 7);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(GuildName);
	}
}
