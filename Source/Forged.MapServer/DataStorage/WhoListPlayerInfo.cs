﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DataStorage;

public class WhoListPlayerInfo
{
    public WhoListPlayerInfo(ObjectGuid guid, TeamFaction team, AccountTypes security, uint level, PlayerClass clss, Race race, uint zoneid, byte gender, bool visible, bool gamemaster, string playerName, string guildName, ObjectGuid guildguid)
    {
        Guid = guid;
        Team = team;
        Security = security;
        Level = level;
        Class = (byte)clss;
        Race = (byte)race;
        ZoneId = zoneid;
        Gender = gender;
        IsVisible = visible;
        IsGamemaster = gamemaster;
        PlayerName = playerName;
        GuildName = guildName;
        GuildGuid = guildguid;
    }

    public byte Class { get; }
    public byte Gender { get; }
    public ObjectGuid Guid { get; }
    public ObjectGuid GuildGuid { get; }
    public string GuildName { get; }
    public bool IsGamemaster { get; }
    public bool IsVisible { get; }
    public uint Level { get; }
    public string PlayerName { get; }
    public byte Race { get; }
    public AccountTypes Security { get; }
    public TeamFaction Team { get; }
    public uint ZoneId { get; }
}