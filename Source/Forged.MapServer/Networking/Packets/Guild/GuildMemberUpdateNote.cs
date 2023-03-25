// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildMemberUpdateNote : ServerPacket
{
	public ObjectGuid Member;
	public bool IsPublic; // 0 == Officer, 1 == Public
	public string Note;
	public GuildMemberUpdateNote() : base(ServerOpcodes.GuildMemberUpdateNote) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Member);

		_worldPacket.WriteBits(Note.GetByteCount(), 8);
		_worldPacket.WriteBit(IsPublic);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(Note);
	}
}