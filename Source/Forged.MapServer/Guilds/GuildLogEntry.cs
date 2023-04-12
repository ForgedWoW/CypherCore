// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public class GuildLogEntry
{
    public GuildLogEntry(ulong guildId, uint guid)
    {
        GuildId = guildId;
        GUID = guid;
        Timestamp = GameTime.CurrentTime;
    }

    public GuildLogEntry(ulong guildId, uint guid, long timestamp)
    {
        GuildId = guildId;
        GUID = guid;
        Timestamp = timestamp;
    }

    public uint GUID { get; set; }
    public ulong GuildId { get; set; }
    public long Timestamp { get; set; }
    public virtual void SaveToDB(SQLTransaction trans) { }
}