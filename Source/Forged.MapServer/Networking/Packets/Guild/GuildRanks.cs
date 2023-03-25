// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRanks : ServerPacket
{
	public List<GuildRankData> Ranks;

	public GuildRanks() : base(ServerOpcodes.GuildRanks)
	{
		Ranks = new List<GuildRankData>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(Ranks.Count);

		Ranks.ForEach(p => p.Write(_worldPacket));
	}
}