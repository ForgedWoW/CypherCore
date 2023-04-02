// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventLogQueryResults : ServerPacket
{
    public List<GuildEventEntry> Entry;

    public GuildEventLogQueryResults() : base(ServerOpcodes.GuildEventLogQueryResults)
    {
        Entry = new List<GuildEventEntry>();
    }

    public override void Write()
    {
        WorldPacket.WriteInt32(Entry.Count);

        foreach (var entry in Entry)
        {
            WorldPacket.WritePackedGuid(entry.PlayerGUID);
            WorldPacket.WritePackedGuid(entry.OtherGUID);
            WorldPacket.WriteUInt8(entry.TransactionType);
            WorldPacket.WriteUInt8(entry.RankID);
            WorldPacket.WriteUInt32(entry.TransactionDate);
        }
    }
}