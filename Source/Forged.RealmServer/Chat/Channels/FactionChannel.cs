// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Serilog;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.World;
using System.Collections.Concurrent;

namespace Forged.RealmServer.Chat;

public class FactionChannel
{
    public ConcurrentDictionary<string, Channel> CustomChannels { get; } = new();

    readonly Dictionary<ObjectGuid, Channel> _channels = new();
	readonly TeamFaction _team;
    private readonly WorldConfig _worldConfig;
    private readonly CharacterDatabase _characterDatabase;
    private readonly WorldManager _worldManager;
    private readonly CliDB _cliDB;
    readonly ObjectGuidGenerator _guidGenerator;

	public FactionChannel(TeamFaction team, WorldConfig worldConfig, CharacterDatabase characterDatabase, 
		WorldManager worldManager, CliDB cliDB)
	{
		_team = team;
        _worldConfig = worldConfig;
        _characterDatabase = characterDatabase;
        _worldManager = worldManager;
        _cliDB = cliDB;
        _guidGenerator = new ObjectGuidGenerator(HighGuid.ChatChannel);
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
		if (CustomChannels.ContainsKey(name.ToLower()))
			return null;

		Channel newChannel = new(CreateCustomChannelGuid(), name, _team);
		newChannel.SetDirty();

		CustomChannels[name.ToLower()] = newChannel;

		return newChannel;
	}

	public Channel GetCustomChannel(string name)
	{
		return CustomChannels.LookupByKey(name.ToLower());
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
			var channel = CustomChannels.LookupByKey(name.ToLower());

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

    internal void SaveToDB()
    {
        foreach (var pair in CustomChannels)
            pair.Value.UpdateChannelInDB();
    }

    internal ObjectGuid CreateCustomChannelGuid()
	{
		ulong high = 0;
		high |= (ulong)HighGuid.ChatChannel << 58;
		high |= (ulong)_worldManager.RealmId.Index << 42;
		high |= (ulong)(_team == TeamFaction.Alliance ? 3 : 5) << 4;

		ObjectGuid channelGuid = new();
		channelGuid.SetRawValue(high, _guidGenerator.Generate());

		return channelGuid;
	}

    internal ObjectGuid CreateBuiltinChannelGuid(uint channelId, AreaTableRecord zoneEntry = null)
	{
		var channelEntry = _cliDB.ChatChannelsStorage.LookupByKey(channelId);
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