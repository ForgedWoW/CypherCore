﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Chat.Channels;

public class ChannelManager
{
    private readonly Dictionary<ObjectGuid, Channel> _channels = new();
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, Channel> _customChannels = new();
    private readonly ObjectGuidGenerator _guidGenerator;
    private readonly GameObjectManager _objectManager;
    private readonly Realm _realm;
    private readonly TeamFaction _team;

    public ChannelManager(TeamFaction team, IConfiguration configuration, CharacterDatabase characterDatabase, Realm realm, CliDB cliDB, ClassFactory classFactory, GameObjectManager objectManager)
    {
        _team = team;
        _configuration = configuration;
        _characterDatabase = characterDatabase;
        _realm = realm;
        _cliDB = cliDB;
        _classFactory = classFactory;
        _objectManager = objectManager;
        _guidGenerator = classFactory.Resolve<ObjectGuidGenerator>(new PositionalParameter(0, HighGuid.ChatChannel), new PositionalParameter(1, 1));
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

    public Channel GetChannel(uint channelId, string name, Player player, bool notify = true, AreaTableRecord zoneEntry = null)
    {
        Channel result = null;

        if (channelId != 0) // builtin
        {
            if (_channels.TryGetValue(CreateBuiltinChannelGuid(channelId, zoneEntry), out var channel))
                result = channel;
        }
        else // custom
        {
            if (_customChannels.TryGetValue(name.ToLower(), out var channel))
                result = channel;
        }

        if (result != null || !notify)
            return result;

        var channelName = name;
        Channel.GetChannelName(ref channelName, channelId, player.Session.SessionDbcLocale, zoneEntry, _cliDB, _objectManager);

        SendNotOnChannelNotify(player, channelName);

        return result;
    }

    public Channel GetChannelForPlayerByGuid(ObjectGuid channelGuid, Player playerSearcher)
    {
        return playerSearcher.JoinedChannels.FirstOrDefault(channel => channel.GUID == channelGuid);
    }

    public Channel GetChannelForPlayerByNamePart(string namePart, Player playerSearcher)
    {
        foreach (var channel in playerSearcher.JoinedChannels)
        {
            var chanName = channel.GetName(playerSearcher.Session.SessionDbcLocale);

            if (chanName.ToLower().Equals(namePart.ToLower()))
                return channel;
        }

        return null;
    }

    public Channel GetCustomChannel(string name)
    {
        return _customChannels.LookupByKey(name.ToLower());
    }

    public Channel GetSystemChannel(uint channelId, AreaTableRecord zoneEntry = null)
    {
        var channelGuid = CreateBuiltinChannelGuid(channelId, zoneEntry);

        if (_channels.TryGetValue(channelGuid, out var currentChannel))
            return currentChannel;

        var newChannel = _classFactory.Resolve<Channel>(new PositionalParameter(0, channelGuid),
                                                        new PositionalParameter(1, channelId),
                                                        new PositionalParameter(2, _team),
                                                        new PositionalParameter(3, zoneEntry));

        _channels[channelGuid] = newChannel;

        return newChannel;
    }

    public void LeftChannel(uint channelId, AreaTableRecord zoneEntry)
    {
        var guid = CreateBuiltinChannelGuid(channelId, zoneEntry);

        if (!_channels.TryGetValue(guid, out var channel))
            return;

        if (channel.NumPlayers == 0)
            _channels.Remove(guid);
    }

    public void LoadFromDB()
    {
        if (!_configuration.GetDefaultValue("PreserveCustomChannels", false))
        {
            Log.Logger.Information("Loaded 0 custom chat channels. Custom channel saving is disabled.");

            return;
        }

        var oldMSTime = Time.MSTime;
        var days = _configuration.GetDefaultValue("PreserveCustomChannelDuration", 14);

        if (days != 0)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_OLD_CHANNELS);
            stmt.AddValue(0, days * Time.DAY);
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


            var channel = new Channel(CreateCustomChannelGuid(), dbName, team, dbBanned);
            channel.SetAnnounce(dbAnnounce);
            channel.SetOwnership(dbOwnership);
            channel.SetPassword(dbPass);
            _customChannels.Add(dbName, channel);

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

    public void SaveToDB()
    {
        foreach (var pair in _customChannels)
            pair.Value.UpdateChannelInDB();
    }

    public void SendNotOnChannelNotify(Player player, string name)
    {
        ChannelNotify notify = new()
        {
            Type = ChatNotify.NotMemberNotice,
            Channel = name
        };

        player.SendPacket(notify);
    }

    private ObjectGuid CreateBuiltinChannelGuid(uint channelId, AreaTableRecord zoneEntry = null)
    {
        var channelEntry = _cliDB.ChatChannelsStorage.LookupByKey(channelId);
        var zoneId = zoneEntry?.Id ?? 0;

        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global | ChannelDBCFlags.CityOnly))
            zoneId = 0;

        ulong high = 0;
        high |= (ulong)HighGuid.ChatChannel << 58;
        high |= (ulong)_realm.Id.Index << 42;
        high |= 1ul << 25; // built-in

        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly2))
            high |= 1ul << 24; // trade

        high |= (ulong)zoneId << 10;
        high |= (ulong)(_team == TeamFaction.Alliance ? 3 : 5) << 4;

        ObjectGuid channelGuid = new();
        channelGuid.SetRawValue(high, channelId);

        return channelGuid;
    }

    private ObjectGuid CreateCustomChannelGuid()
    {
        ulong high = 0;
        high |= (ulong)HighGuid.ChatChannel << 58;
        high |= (ulong)_realm.Id.Index << 42;
        high |= (ulong)(_team == TeamFaction.Alliance ? 3 : 5) << 4;

        ObjectGuid channelGuid = new();
        channelGuid.SetRawValue(high, _guidGenerator.Generate());

        return channelGuid;
    }
}