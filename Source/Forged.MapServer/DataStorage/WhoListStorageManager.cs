// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.DataStorage;

public class WhoListStorageManager : Singleton<WhoListStorageManager>
{
	readonly List<WhoListPlayerInfo> _whoListStorage;

	WhoListStorageManager()
	{
		_whoListStorage = new List<WhoListPlayerInfo>();
	}

	public void Update()
	{
		// clear current list
		_whoListStorage.Clear();

		var players = Global.ObjAccessor.GetPlayers();

		foreach (var player in players)
		{
			if (player.Map == null || player.Session.PlayerLoading)
				continue;

			var playerName = player.GetName();
			var guildName = Global.GuildMgr.GetGuildNameById((uint)player.GuildId);

			var guild = player.Guild;
			var guildGuid = ObjectGuid.Empty;

			if (guild)
				guildGuid = guild.GetGUID();

			_whoListStorage.Add(new WhoListPlayerInfo(player.GUID,
													player.Team,
													player.Session.Security,
													player.Level,
													player.Class,
													player.Race,
													player.Zone,
													(byte)player.NativeGender,
													player.IsVisible(),
													player.IsGameMaster,
													playerName,
													guildName,
													guildGuid));
		}
	}

	public List<WhoListPlayerInfo> GetWhoList()
	{
		return _whoListStorage;
	}
}