// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.DataStorage;

public class WhoListPlayerInfo
{
	public ObjectGuid Guid { get; }
	public TeamFaction Team { get; }
	public AccountTypes Security { get; }
	public uint Level { get; }
	public byte Class { get; }
	public byte Race { get; }
	public uint ZoneId { get; }
	public byte Gender { get; }
	public bool IsVisible { get; }
	public bool IsGamemaster { get; }
	public string PlayerName { get; }
	public string GuildName { get; }
	public ObjectGuid GuildGuid { get; }

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
}