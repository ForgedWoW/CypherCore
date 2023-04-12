// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Guilds;

public class GuildLogHolder<T> where T : GuildLogEntry
{
    private readonly List<T> _log = new();
    private readonly uint _maxRecords;
    private uint _nextGUID;

    public GuildLogHolder(IConfiguration configuration)
    {
        _maxRecords = configuration.GetDefaultValue(typeof(T) == typeof(GuildBankEventLogEntry) ? "Guild.BankEventLogRecordsCount" : "Guild.EventLogRecordsCount", 20u);
        _nextGUID = GuildConst.EventLogGuidUndefined;
    }

    public T AddEvent(SQLTransaction trans, T entry)
    {
        // Check max records limit
        if (!CanInsert())
            _log.RemoveAt(0);

        // Add event to list
        _log.Add(entry);

        // Save to DB
        entry.SaveToDB(trans);

        return entry;
    }

    // Checks if new log entry can be added to holder
    public bool CanInsert()
    {
        return _log.Count < _maxRecords;
    }

    public List<T> GetGuildLog()
    {
        return _log;
    }

    public uint GetNextGUID()
    {
        if (_nextGUID == GuildConst.EventLogGuidUndefined)
            _nextGUID = 0;
        else
            _nextGUID = (_nextGUID + 1) % _maxRecords;

        return _nextGUID;
    }

    public byte GetSize()
    {
        return (byte)_log.Count;
    }

    public void LoadEvent(T entry)
    {
        if (_nextGUID == GuildConst.EventLogGuidUndefined)
            _nextGUID = entry.GUID;

        _log.Insert(0, entry);
    }
}