// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankLogQueryResults : ServerPacket
{
	public int Tab;
	public List<GuildBankLogEntry> Entry;
	public ulong? WeeklyBonusMoney;

	public GuildBankLogQueryResults() : base(ServerOpcodes.GuildBankLogQueryResults)
	{
		Entry = new List<GuildBankLogEntry>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);
		_worldPacket.WriteInt32(Entry.Count);
		_worldPacket.WriteBit(WeeklyBonusMoney.HasValue);
		_worldPacket.FlushBits();

		foreach (var logEntry in Entry)
		{
			_worldPacket.WritePackedGuid(logEntry.PlayerGUID);
			_worldPacket.WriteUInt32(logEntry.TimeOffset);
			_worldPacket.WriteInt8(logEntry.EntryType);

			_worldPacket.WriteBit(logEntry.Money.HasValue);
			_worldPacket.WriteBit(logEntry.ItemID.HasValue);
			_worldPacket.WriteBit(logEntry.Count.HasValue);
			_worldPacket.WriteBit(logEntry.OtherTab.HasValue);
			_worldPacket.FlushBits();

			if (logEntry.Money.HasValue)
				_worldPacket.WriteUInt64(logEntry.Money.Value);

			if (logEntry.ItemID.HasValue)
				_worldPacket.WriteInt32(logEntry.ItemID.Value);

			if (logEntry.Count.HasValue)
				_worldPacket.WriteInt32(logEntry.Count.Value);

			if (logEntry.OtherTab.HasValue)
				_worldPacket.WriteInt8(logEntry.OtherTab.Value);
		}

		if (WeeklyBonusMoney.HasValue)
			_worldPacket.WriteUInt64(WeeklyBonusMoney.Value);
	}
}