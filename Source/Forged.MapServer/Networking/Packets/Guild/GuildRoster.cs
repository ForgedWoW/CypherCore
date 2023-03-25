// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildRoster : ServerPacket
{
	public List<GuildRosterMemberData> MemberData;
	public string WelcomeText;
	public string InfoText;
	public uint CreateDate;
	public int NumAccounts;
	public int GuildFlags;

	public GuildRoster() : base(ServerOpcodes.GuildRoster)
	{
		MemberData = new List<GuildRosterMemberData>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(NumAccounts);
		_worldPacket.WritePackedTime(CreateDate);
		_worldPacket.WriteInt32(GuildFlags);
		_worldPacket.WriteInt32(MemberData.Count);
		_worldPacket.WriteBits(WelcomeText.GetByteCount(), 11);
		_worldPacket.WriteBits(InfoText.GetByteCount(), 10);
		_worldPacket.FlushBits();

		MemberData.ForEach(p => p.Write(_worldPacket));

		_worldPacket.WriteString(WelcomeText);
		_worldPacket.WriteString(InfoText);
	}
}