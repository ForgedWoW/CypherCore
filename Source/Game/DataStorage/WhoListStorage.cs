// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Guilds;

namespace Game.DataStorage
{
    public class WhoListPlayerInfo
    {
        public WhoListPlayerInfo(ObjectGuid guid, TeamFaction team, AccountTypes security, uint level, Class clss, Race race, uint zoneid, byte gender, bool visible, bool gamemaster, string playerName, string guildName, ObjectGuid guildguid)
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
    }

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

                string playerName = player.GetName();
                string guildName = Global.GuildMgr.GetGuildNameById((uint)player.GuildId);

                Guild guild = player.Guild;
                ObjectGuid guildGuid = ObjectGuid.Empty;

                if (guild)
                    guildGuid = guild.GetGUID();

                _whoListStorage.Add(new WhoListPlayerInfo(player.GUID, player.Team, player.Session.Security, player.Level,
                    player.                    Class, player.Race, player.Zone, (byte)player.NativeGender, player.IsVisible(),
                    player.                    IsGameMaster, playerName, guildName, guildGuid));
            }
        }

        public List<WhoListPlayerInfo> GetWhoList() { return _whoListStorage; }
    }
}
