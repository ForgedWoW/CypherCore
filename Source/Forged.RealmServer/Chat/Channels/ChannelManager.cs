// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking.Packets;
using Serilog;
using Forged.RealmServer.World;

namespace Forged.RealmServer.Chat;

public class ChannelManager
{
    private readonly TeamFaction _team;
    private readonly WorldConfig _worldConfig;
    private readonly CharacterDatabase _characterDatabase;
    private readonly WorldManager _worldManager;
    private readonly CliDB _cliDB;

    public FactionChannel AllianceChannel { get; }
    public FactionChannel HordeChannel { get; }

	public ChannelManager(WorldConfig worldConfig, CharacterDatabase characterDatabase, WorldManager worldManager, CliDB cliDB)
    {
        _worldConfig = worldConfig;
        _characterDatabase = characterDatabase;
        _worldManager = worldManager;
        _cliDB = cliDB;

        AllianceChannel = new FactionChannel(TeamFaction.Alliance, _worldConfig, _characterDatabase, _worldManager, _cliDB);
        HordeChannel = new FactionChannel(TeamFaction.Horde, _worldConfig, _characterDatabase, _worldManager, _cliDB);
    }

	public void LoadFromDB()
	{
		if (!_worldConfig.GetBoolValue(WorldCfg.PreserveCustomChannels))
		{
			Log.Logger.Information("Loaded 0 custom chat channels. Custom channel saving is disabled.");

			return;
		}

		var oldMSTime = Time.MSTime;
		var days = _worldConfig.GetUIntValue(WorldCfg.PreserveCustomChannelDuration);

		if (days != 0)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_OLD_CHANNELS);
			stmt.AddValue(0, days * Time.Day);
			_characterDatabase.Execute(stmt);
		}

		var result = _characterDatabase.Query("SELECT name, team, announce, ownership, password, bannedList FROM channels");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 custom chat channels. DB table `channels` is empty.");

			return;
		}

		List<(string name, TeamFaction team)> toDelete = new();
		uint count = 0;

		do
		{
			var dbName = result.Read<string>(0); // may be different - channel names are case insensitive
			var team = (TeamFaction)result.Read<int>(1);
			var dbAnnounce = result.Read<bool>(2);
			var dbOwnership = result.Read<bool>(3);
			var dbPass = result.Read<string>(4);
			var dbBanned = result.Read<string>(5);

            FactionChannel mgr = null;

			if (mgr == null)
			{
				Log.Logger.Error($"Failed to load custom chat channel '{dbName}' from database - invalid team {team}. Deleted.");
				toDelete.Add((dbName, team));

				continue;
			}

			var channel = new Channel(mgr.CreateCustomChannelGuid(), dbName, team, dbBanned);
			channel.SetAnnounce(dbAnnounce);
			channel.SetOwnership(dbOwnership);
			channel.SetPassword(dbPass);
			mgr.CustomChannels.TryAdd(dbName, channel);

			++count;
		} while (result.NextRow());

		foreach (var (name, team) in toDelete)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHANNEL);
			stmt.AddValue(0, name);
			stmt.AddValue(1, (uint)team);
			_characterDatabase.Execute(stmt);
		}

		Log.Logger.Information($"Loaded {count} custom chat channels in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public FactionChannel ForTeam(TeamFaction team)
	{
		if (_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionChannel))
			return AllianceChannel; // cross-faction

		if (team == TeamFaction.Alliance)
			return AllianceChannel;

		if (team == TeamFaction.Horde)
			return HordeChannel;

		return null;
	}

	public void SaveToDB()
	{
		AllianceChannel.SaveToDB();
		HordeChannel.SaveToDB();
	}
}