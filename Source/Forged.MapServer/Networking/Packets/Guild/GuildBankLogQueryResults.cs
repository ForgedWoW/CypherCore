// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankLogQueryResults : ServerPacket
{
    public List<GuildBankLogEntry> Entry;
    public int Tab;
    public ulong? WeeklyBonusMoney;

    public GuildBankLogQueryResults() : base(ServerOpcodes.GuildBankLogQueryResults)
    {
        Entry = new List<GuildBankLogEntry>();
    }

    public override void Write()
    {
        WorldPacket.WriteInt32(Tab);
        WorldPacket.WriteInt32(Entry.Count);
        WorldPacket.WriteBit(WeeklyBonusMoney.HasValue);
        WorldPacket.FlushBits();

        foreach (var logEntry in Entry)
        {
            WorldPacket.WritePackedGuid(logEntry.PlayerGUID);
            WorldPacket.WriteUInt32(logEntry.TimeOffset);
            WorldPacket.WriteInt8(logEntry.EntryType);

            WorldPacket.WriteBit(logEntry.Money.HasValue);
            WorldPacket.WriteBit(logEntry.ItemID.HasValue);
            WorldPacket.WriteBit(logEntry.Count.HasValue);
            WorldPacket.WriteBit(logEntry.OtherTab.HasValue);
            WorldPacket.FlushBits();

            if (logEntry.Money.HasValue)
                WorldPacket.WriteUInt64(logEntry.Money.Value);

            if (logEntry.ItemID.HasValue)
                WorldPacket.WriteInt32(logEntry.ItemID.Value);

            if (logEntry.Count.HasValue)
                WorldPacket.WriteInt32(logEntry.Count.Value);

            if (logEntry.OtherTab.HasValue)
                WorldPacket.WriteInt8(logEntry.OtherTab.Value);
        }

        if (WeeklyBonusMoney.HasValue)
            WorldPacket.WriteUInt64(WeeklyBonusMoney.Value);
    }
}