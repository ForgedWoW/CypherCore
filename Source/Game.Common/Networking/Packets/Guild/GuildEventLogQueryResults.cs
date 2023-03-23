// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Guild;

namespace Game.Common.Networking.Packets.Guild;

public class GuildEventLogQueryResults : ServerPacket
{
	public List<GuildEventEntry> Entry;

	public GuildEventLogQueryResults() : base(ServerOpcodes.GuildEventLogQueryResults)
	{
		Entry = new List<GuildEventEntry>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(Entry.Count);

		foreach (var entry in Entry)
		{
			_worldPacket.WritePackedGuid(entry.PlayerGUID);
			_worldPacket.WritePackedGuid(entry.OtherGUID);
			_worldPacket.WriteUInt8(entry.TransactionType);
			_worldPacket.WriteUInt8(entry.RankID);
			_worldPacket.WriteUInt32(entry.TransactionDate);
		}
	}
}
