// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class GuildNameChanged : ServerPacket
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