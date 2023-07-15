// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class ChannelHandler : IWorldSessionHandler
{
    private readonly DB6Storage<AreaTableRecord> _areaTableRecords;
    private readonly ChannelManagerFactory _channelManagerFactory;
    private readonly DB6Storage<ChatChannelsRecord> _chatChannelsRecords;
    private readonly GameObjectManager _gameObjectManager;
    private readonly WorldSession _session;

    public ChannelHandler(WorldSession session, DB6Storage<AreaTableRecord> areaTableRecords, DB6Storage<ChatChannelsRecord> chatChannelsRecords,
                          ChannelManagerFactory channelManagerFactory, GameObjectManager gameObjectManager)
    {
        _session = session;
        _areaTableRecords = areaTableRecords;
        _chatChannelsRecords = chatChannelsRecords;
        _channelManagerFactory = channelManagerFactory;
        _gameObjectManager = gameObjectManager;
    }

    [WorldPacketHandler(ClientOpcodes.ChatChannelAnnouncements)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelDeclineInvite)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelDisplayList)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelList)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelOwner)]
    private void HandleChannelCommand(ChannelCommand packet)
    {
        var cMgr = _channelManagerFactory.ForTeam(_session.Player.Team);
        var channel = cMgr?.GetChannelForPlayerByNamePart(packet.ChannelName, _session.Player);

        if (channel == null)
            return;

        switch (packet.GetOpcode())
        {
            case ClientOpcodes.ChatChannelAnnouncements:
                channel.Announce(_session.Player);

                break;

            case ClientOpcodes.ChatChannelDeclineInvite:
                channel.DeclineInvite(_session.Player);

                break;

            case ClientOpcodes.ChatChannelDisplayList:
            case ClientOpcodes.ChatChannelList:
                channel.List(_session.Player);

                break;

            case ClientOpcodes.ChatChannelOwner:
                channel.SendWhoOwner(_session.Player);

                break;
        }
    }

    [WorldPacketHandler(ClientOpcodes.ChatChannelPassword)]
    private void HandleChannelPassword(ChannelPassword packet)
    {
        if (packet.Password.Length > 31)
        {
            Log.Logger.Debug("{0} {1} ChannelName: {2}, Password: {3}, Password too long.",
                             packet.GetOpcode(),
                             _session.GetPlayerInfo(),
                             packet.ChannelName,
                             packet.Password);

            return;
        }

        Log.Logger.Debug("{0} {1} ChannelName: {2}, Password: {3}", packet.GetOpcode(), _session.GetPlayerInfo(), packet.ChannelName, packet.Password);

        var cMgr = _channelManagerFactory.ForTeam(_session.Player.Team);
        var channel = cMgr?.GetChannelForPlayerByNamePart(packet.ChannelName, _session.Player);
        channel?.Password(_session.Player, packet.Password);
    }

    [WorldPacketHandler(ClientOpcodes.ChatChannelBan)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelInvite)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelKick)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelModerator)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelSetOwner)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelSilenceAll)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelUnban)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelUnmoderator)]
    [WorldPacketHandler(ClientOpcodes.ChatChannelUnsilenceAll)]
    private void HandleChannelPlayerCommand(ChannelPlayerCommand packet)
    {
        if (packet.Name.Length >= 49)
        {
            Log.Logger.Debug("{0} {1} ChannelName: {2}, Name: {3}, Name too long.", packet.GetOpcode(), _session.GetPlayerInfo(), packet.ChannelName, packet.Name);

            return;
        }

        if (!_gameObjectManager.NormalizePlayerName(ref packet.Name))
            return;

        var cMgr = _channelManagerFactory.ForTeam(_session.Player.Team);
        var channel = cMgr?.GetChannelForPlayerByNamePart(packet.ChannelName, _session.Player);

        if (channel == null)
            return;

        switch (packet.GetOpcode())
        {
            case ClientOpcodes.ChatChannelBan:
                channel.Ban(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelInvite:
                channel.Invite(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelKick:
                channel.Kick(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelModerator:
                channel.SetModerator(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelSetOwner:
                channel.SetOwner(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelSilenceAll:
                channel.SilenceAll(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelUnban:
                channel.UnBan(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelUnmoderator:
                channel.UnsetModerator(_session.Player, packet.Name);

                break;

            case ClientOpcodes.ChatChannelUnsilenceAll:
                channel.UnsilenceAll(_session.Player, packet.Name);

                break;
        }
    }

    [WorldPacketHandler(ClientOpcodes.ChatJoinChannel)]
    private void HandleJoinChannel(JoinChannel packet)
    {
        var zone = _areaTableRecords.LookupByKey(_session.Player.Location.Zone);

        if (packet.ChatChannelId != 0)
        {
            var channel = _chatChannelsRecords.LookupByKey(packet.ChatChannelId);

            if (channel == null)
                return;

            if (zone == null || !_session.Player.CanJoinConstantChannelInZone(channel, zone))
                return;
        }

        var cMgr = _channelManagerFactory.ForTeam(_session.Player.Team);

        if (cMgr == null)
            return;

        if (packet.ChatChannelId != 0)
        {
            // system channel
            var channel = cMgr.GetSystemChannel((uint)packet.ChatChannelId, zone);

            if (channel != null)
                channel.JoinChannel(_session.Player);
        }
        else
        {
            // custom channel
            if (packet.ChannelName.IsEmpty() || char.IsDigit(packet.ChannelName[0]))
            {
                ChannelNotify channelNotify = new();
                channelNotify.Type = ChatNotify.InvalidNameNotice;
                channelNotify.Channel = packet.ChannelName;
                _session.SendPacket(channelNotify);

                return;
            }

            if (packet.Password.Length > 127)
            {
                Log.Logger.Error($"_session.Player {_session.Player.GUID} tried to create a channel with a password more than {127} characters long - blocked");

                return;
            }

            if (!_session.DisallowHyperlinksAndMaybeKick(packet.ChannelName))
                return;

            var channel = cMgr.GetCustomChannel(packet.ChannelName);

            if (channel != null)
            {
                channel.JoinChannel(_session.Player, packet.Password);
            }
            else
            {
                channel = cMgr.CreateCustomChannel(packet.ChannelName);

                if (channel != null)
                {
                    channel.SetPassword(packet.Password);
                    channel.JoinChannel(_session.Player, packet.Password);
                }
            }
        }
    }

    [WorldPacketHandler(ClientOpcodes.ChatLeaveChannel)]
    private void HandleLeaveChannel(LeaveChannel packet)
    {
        if (string.IsNullOrEmpty(packet.ChannelName) && packet.ZoneChannelID == 0)
            return;

        var zone = _areaTableRecords.LookupByKey(_session.Player.Location.Zone);

        if (packet.ZoneChannelID != 0)
        {
            var channel = _chatChannelsRecords.LookupByKey(packet.ZoneChannelID);

            if (channel == null)
                return;

            if (zone == null || !_session.Player.CanJoinConstantChannelInZone(channel, zone))
                return;
        }

        var cMgr = _channelManagerFactory.ForTeam(_session.Player.Team);

        if (cMgr == null)
            return;

        {
            var channel = cMgr.GetChannel((uint)packet.ZoneChannelID, packet.ChannelName, _session.Player, true, zone);

            channel?.LeaveChannel(_session.Player);

            if (packet.ZoneChannelID != 0)
                cMgr.LeftChannel((uint)packet.ZoneChannelID, zone);
        }
    }
}