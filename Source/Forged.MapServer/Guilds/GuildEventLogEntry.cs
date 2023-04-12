// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Guild;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public class GuildEventLogEntry : GuildLogEntry
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly GuildEventLogTypes _eventType;
    private readonly byte _newRank;
    private readonly ulong _playerGuid1;
    private readonly ulong _playerGuid2;

    public GuildEventLogEntry(ulong guildId, uint guid, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank, CharacterDatabase characterDatabase)
        : base(guildId, guid)
    {
        _eventType = eventType;
        _playerGuid1 = playerGuid1;
        _playerGuid2 = playerGuid2;
        _newRank = newRank;
        _characterDatabase = characterDatabase;
    }

    public GuildEventLogEntry(ulong guildId, uint guid, long timestamp, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank, CharacterDatabase characterDatabase)
        : base(guildId, guid, timestamp)
    {
        _eventType = eventType;
        _playerGuid1 = playerGuid1;
        _playerGuid2 = playerGuid2;
        _newRank = newRank;
        _characterDatabase = characterDatabase;
    }

    public override void SaveToDB(SQLTransaction trans)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG);
        stmt.AddValue(0, GuildId);
        stmt.AddValue(1, GUID);
        trans.Append(stmt);

        byte index = 0;
        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_EVENTLOG);
        stmt.AddValue(index, GuildId);
        stmt.AddValue(++index, GUID);
        stmt.AddValue(++index, (byte)_eventType);
        stmt.AddValue(++index, _playerGuid1);
        stmt.AddValue(++index, _playerGuid2);
        stmt.AddValue(++index, _newRank);
        stmt.AddValue(++index, Timestamp);
        trans.Append(stmt);
    }

    public void WritePacket(GuildEventLogQueryResults packet)
    {
        var playerGUID = ObjectGuid.Create(HighGuid.Player, _playerGuid1);
        var otherGUID = ObjectGuid.Create(HighGuid.Player, _playerGuid2);

        GuildEventEntry eventEntry = new()
        {
            PlayerGUID = playerGUID,
            OtherGUID = otherGUID,
            TransactionType = (byte)_eventType,
            TransactionDate = (uint)(GameTime.CurrentTime - Timestamp),
            RankID = _newRank
        };

        packet.Entry.Add(eventEntry);
    }
}