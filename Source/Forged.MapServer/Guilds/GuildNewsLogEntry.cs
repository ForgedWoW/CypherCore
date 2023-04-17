// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Guild;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public class GuildNewsLogEntry : GuildLogEntry
{
    private readonly CharacterDatabase _characterDatabase;

    public GuildNewsLogEntry(ulong guildId, uint guid, GuildNews type, ObjectGuid playerGuid, uint flags, uint value, CharacterDatabase characterDatabase)
        : base(guildId, guid)
    {
        NewsType = type;
        PlayerGuid = playerGuid;
        Flags = (int)flags;
        Value = value;
        _characterDatabase = characterDatabase;
    }

    public GuildNewsLogEntry(ulong guildId, uint guid, long timestamp, GuildNews type, ObjectGuid playerGuid, uint flags, uint value, CharacterDatabase characterDatabase)
        : base(guildId, guid, timestamp)
    {
        NewsType = type;
        PlayerGuid = playerGuid;
        Flags = (int)flags;
        Value = value;
        _characterDatabase = characterDatabase;
    }

    public int Flags { get; set; }

    public GuildNews NewsType { get; }

    public ObjectGuid PlayerGuid { get; }

    public uint Value { get; }

    public override void SaveToDB(SQLTransaction trans)
    {
        byte index = 0;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_NEWS);
        stmt.AddValue(index, GuildId);
        stmt.AddValue(++index, GUID);
        stmt.AddValue(++index, (byte)NewsType);
        stmt.AddValue(++index, PlayerGuid.Counter);
        stmt.AddValue(++index, Flags);
        stmt.AddValue(++index, Value);
        stmt.AddValue(++index, Timestamp);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public void SetSticky(bool sticky)
    {
        if (sticky)
            Flags |= 1;
        else
            Flags &= ~1;
    }

    public void WritePacket(GuildNewsPkt newsPacket)
    {
        GuildNewsEvent newsEvent = new()
        {
            Id = (int)GUID,
            MemberGuid = PlayerGuid,
            CompletedDate = (uint)Timestamp,
            Flags = Flags,
            Type = (int)NewsType
        };

        if (NewsType is GuildNews.ItemLooted or GuildNews.ItemCrafted or GuildNews.ItemPurchased)
        {
            ItemInstance itemInstance = new()
            {
                ItemID = Value
            };

            newsEvent.Item = itemInstance;
        }

        newsPacket.NewsEvents.Add(newsEvent);
    }
}