// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRosterUpdate : ServerPacket
{
	public List<GuildRosterMemberData> MemberData;

	public GuildRosterUpdate() : base(ServerOpcodes.GuildRosterUpdate)
	{
		MemberData = new List<GuildRosterMemberData>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(MemberData.Count);

		MemberData.ForEach(p => p.Write(_worldPacket));
	}
}