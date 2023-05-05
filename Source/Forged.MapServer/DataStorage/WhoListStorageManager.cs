// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;

namespace Forged.MapServer.DataStorage;

public class WhoListStorageManager
{
    private readonly GuildManager _guildManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly List<WhoListPlayerInfo> _whoListStorage;

    public WhoListStorageManager(ObjectAccessor objectAccessor, GuildManager guildManager)
    {
        _objectAccessor = objectAccessor;
        _guildManager = guildManager;
        _whoListStorage = new List<WhoListPlayerInfo>();
    }

    public List<WhoListPlayerInfo> GetWhoList()
    {
        return _whoListStorage;
    }

    public void Update()
    {
        // clear current list
        _whoListStorage.Clear();

        var players = _objectAccessor.GetPlayers();

        foreach (var player in players)
        {
            if (player.Location.Map == null || player.Session.PlayerLoading)
                continue;

            var playerName = player.GetName();
            var guildName = _guildManager.GetGuildNameById((uint)player.GuildId);

            var guild = player.Guild;
            var guildGuid = ObjectGuid.Empty;

            if (guild != null)
                guildGuid = guild.GetGUID();

            _whoListStorage.Add(new WhoListPlayerInfo(player.GUID,
                                                      player.Team,
                                                      player.Session.Security,
                                                      player.Level,
                                                      player.Class,
                                                      player.Race,
                                                      player.Location.Zone,
                                                      (byte)player.NativeGender,
                                                      player.IsVisible(),
                                                      player.IsGameMaster,
                                                      playerName,
                                                      guildName,
                                                      guildGuid));
        }
    }
}