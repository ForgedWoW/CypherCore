// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using System.Collections.Generic;

namespace Forged.RealmServer.DataStorage;

public class WhoListStorageManager
{
    private readonly ObjectAccessor _objectAccessor;
    private readonly GuildManager _guildManager;
    readonly List<WhoListPlayerInfo> _whoListStorage;

	public WhoListStorageManager(ObjectAccessor objectAccessor, GuildManager guildManager)
    {
        _objectAccessor = objectAccessor;
        _guildManager = guildManager;
        _whoListStorage = new List<WhoListPlayerInfo>();
    }

	public void Update()
	{
		// clear current list
		_whoListStorage.Clear();

		var players = _objectAccessor.GetPlayers();

		foreach (var player in players)
		{
			if (player.Map == null || player.Session.PlayerLoading)
				continue;

			var playerName = player.GetName();
			var guildName = _guildManager.GetGuildNameById((uint)player.GuildId);

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