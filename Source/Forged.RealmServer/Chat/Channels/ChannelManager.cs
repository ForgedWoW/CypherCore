// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Entities.Players;
using Forged.RealmServer.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

public class ChannelManager
{
	static readonly ChannelManager allianceChannelMgr = new(TeamFaction.Alliance);
	static readonly ChannelManager hordeChannelMgr = new(TeamFaction.Horde);

	readonly Dictionary<string, Channel> _customChannels = new();
	readonly Dictionary<ObjectGuid, Channel> _channels = new();
	readonly TeamFaction _team;
	readonly ObjectGuidGenerator _guidGenerator;

	public ChannelManager(TeamFaction team)
	{
		_team = team;
		_guidGenerator = new ObjectGuidGenerator(HighGuid.ChatChannel);
	}

	public static void LoadFromDB()
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

			var mgr = ForTeam(team);

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
			mgr._customChannels.Add(dbName, channel);

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

	public static ChannelManager ForTeam(TeamFaction team)
	{
		if (_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionChannel))
			return allianceChannelMgr; // cross-faction

		if (team == TeamFaction.Alliance)
			return allianceChannelMgr;

		if (team == TeamFaction.Horde)
			return hordeChannelMgr;

		return null;
	}

	public static Channel GetChannelForPlayerByNamePart(string namePart, Player playerSearcher)
	{
		foreach (var channel in playerSearcher.JoinedChannels)
		{
			var chanName = channel.GetName(playerSearcher.Session.SessionDbcLocale);

			if (chanName.ToLower().Equals(namePart.ToLower()))
				return channel;
		}

		return null;
	}

	public void SaveToDB()
	{
		foreach (var pair in _customChannels)
			pair.Value.UpdateChannelInDB();
	}

	public static Channel GetChannelForPlayerByGuid(ObjectGuid channelGuid, Player playerSearcher)
	{
		foreach (var channel in playerSearcher.JoinedChannels)
			if (channel.GetGUID() == channelGuid)
				return channel;

		return null;
	}

	public Channel GetSystemChannel(uint channelId, AreaTableRecord zoneEntry = null)
	{
		var channelGuid = CreateBuiltinChannelGuid(channelId, zoneEntry);
		var currentChannel = _channels.LookupByKey(channelGuid);

		if (currentChannel != null)
			return currentChannel;

		var newChannel = new Channel(channelGuid, channelId, _team, zoneEntry);
		_channels[channelGuid] = newChannel;

		return newChannel;
	}

	public Channel CreateCustomChannel(string name)
	{
		if (_customChannels.ContainsKey(name.ToLower()))
			return null;

		Channel newChannel = new(CreateCustomChannelGuid(), name, _team);
		newChannel.SetDirty();

		_customChannels[name.ToLower()] = newChannel;

		return newChannel;
	}

	public Channel GetCustomChannel(string name)
	{
		return _customChannels.LookupByKey(name.ToLower());
	}

	public Channel GetChannel(uint channelId, string name, Player player, bool notify = true, AreaTableRecord zoneEntry = null)
	{
		Channel result = null;

		if (channelId != 0) // builtin
		{
			var channel = _channels.LookupByKey(CreateBuiltinChannelGuid(channelId, zoneEntry));

			if (channel != null)
				result = channel;
		}
		else // custom
		{
			var channel = _customChannels.LookupByKey(name.ToLower());

			if (channel != null)
				result = channel;
		}

		if (result == null && notify)
		{
			var channelName = name;
			Channel.GetChannelName(ref channelName, channelId, player.Session.SessionDbcLocale, zoneEntry);

			SendNotOnChannelNotify(player, channelName);
		}

		return result;
	}

	public void LeftChannel(uint channelId, AreaTableRecord zoneEntry)
	{
		var guid = CreateBuiltinChannelGuid(channelId, zoneEntry);
		var channel = _channels.LookupByKey(guid);

		if (channel == null)
			return;

		if (channel.GetNumPlayers() == 0)
			_channels.Remove(guid);
	}

	public static void SendNotOnChannelNotify(Player player, string name)
	{
		ChannelNotify notify = new();
		notify.Type = ChatNotify.NotMemberNotice;
		notify.Channel = name;
		player.SendPacket(notify);
	}

	ObjectGuid CreateCustomChannelGuid()
	{
		ulong high = 0;
		high |= (ulong)HighGuid.ChatChannel << 58;
		high |= (ulong)_worldManager.RealmId.Index << 42;
		high |= (ulong)(_team == TeamFaction.Alliance ? 3 : 5) << 4;

		ObjectGuid channelGuid = new();
		channelGuid.SetRawValue(high, _guidGenerator.Generate());

		return channelGuid;
	}

	ObjectGuid CreateBuiltinChannelGuid(uint channelId, AreaTableRecord zoneEntry = null)
	{
		var channelEntry = CliDB.ChatChannelsStorage.LookupByKey(channelId);
		var zoneId = zoneEntry != null ? zoneEntry.Id : 0;

		if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global | ChannelDBCFlags.CityOnly))
			zoneId = 0;

		ulong high = 0;
		high |= (ulong)HighGuid.ChatChannel << 58;
		high |= (ulong)_worldManager.RealmId.Index << 42;
		high |= 1ul << 25; // built-in

		if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly2))
			high |= 1ul << 24; // trade

		high |= (ulong)(zoneId) << 10;
		high |= (ulong)(_team == TeamFaction.Alliance ? 3 : 5) << 4;

		ObjectGuid channelGuid = new();
		channelGuid.SetRawValue(high, channelId);

		return channelGuid;
	}
}