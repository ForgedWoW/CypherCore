// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Chat;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.ChatJoinChannel)]
        void HandleJoinChannel(JoinChannel packet)
        {
            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(Player.Zone);
            if (packet.ChatChannelId != 0)
            {
                ChatChannelsRecord channel = CliDB.ChatChannelsStorage.LookupByKey(packet.ChatChannelId);
                if (channel == null)
                    return;

                if (zone == null || !Player.CanJoinConstantChannelInZone(channel, zone))
                    return;
            }

            ChannelManager cMgr = ChannelManager.ForTeam(Player.Team);
            if (cMgr == null)
                return;

            if (packet.ChatChannelId != 0)
            { // system channel
                Channel channel = cMgr.GetSystemChannel((uint)packet.ChatChannelId, zone);
                if (channel != null)
                    channel.JoinChannel(Player);
            }
            else
            { // custom channel
                if (packet.ChannelName.IsEmpty() || Char.IsDigit(packet.ChannelName[0]))
                {
                    ChannelNotify channelNotify = new();
                    channelNotify.Type = ChatNotify.InvalidNameNotice;
                    channelNotify.Channel = packet.ChannelName;
                    SendPacket(channelNotify);
                    return;
                }

                if (packet.Password.Length > 127)
                {
                    Log.outError(LogFilter.Network, $"Player {Player.GUID} tried to create a channel with a password more than {127} characters long - blocked");
                    return;
                }
                if (!DisallowHyperlinksAndMaybeKick(packet.ChannelName))
                    return;

                Channel channel = cMgr.GetCustomChannel(packet.ChannelName);
                if (channel != null)
                    channel.JoinChannel(Player, packet.Password);
                else
                {
                    channel = cMgr.CreateCustomChannel(packet.ChannelName);
                    if (channel != null)
                    {
                        channel.SetPassword(packet.Password);
                        channel.JoinChannel(Player, packet.Password);
                    }
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatLeaveChannel)]
        void HandleLeaveChannel(LeaveChannel packet)
        {
            if (string.IsNullOrEmpty(packet.ChannelName) && packet.ZoneChannelID == 0)
                return;

            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(Player.Zone);
            if (packet.ZoneChannelID != 0)
            {
                ChatChannelsRecord channel = CliDB.ChatChannelsStorage.LookupByKey(packet.ZoneChannelID);
                if (channel == null)
                    return;

                if (zone == null || !Player.CanJoinConstantChannelInZone(channel, zone))
                    return;
            }

            ChannelManager cMgr = ChannelManager.ForTeam(Player.Team);
            if (cMgr != null)
            {
                Channel channel = cMgr.GetChannel((uint)packet.ZoneChannelID, packet.ChannelName, Player, true, zone);
                if (channel != null)
                    channel.LeaveChannel(Player, true);

                if (packet.ZoneChannelID != 0)
                    cMgr.LeftChannel((uint)packet.ZoneChannelID, zone);
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatChannelAnnouncements)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelDeclineInvite)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelDisplayList)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelList)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelOwner)]
        void HandleChannelCommand(ChannelCommand packet)
        {
            Channel channel = ChannelManager.GetChannelForPlayerByNamePart(packet.ChannelName, Player);
            if (channel == null)
                return;

            switch (packet.GetOpcode())
            {
                case ClientOpcodes.ChatChannelAnnouncements:
                    channel.Announce(Player);
                    break;
                case ClientOpcodes.ChatChannelDeclineInvite:
                    channel.DeclineInvite(Player);
                    break;
                case ClientOpcodes.ChatChannelDisplayList:
                case ClientOpcodes.ChatChannelList:
                    channel.List(Player);
                    break;
                case ClientOpcodes.ChatChannelOwner:
                    channel.SendWhoOwner(Player);
                    break;
            }
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
        void HandleChannelPlayerCommand(ChannelPlayerCommand packet)
        {
            if (packet.Name.Length >= 49)
            {
                Log.outDebug(LogFilter.ChatSystem, "{0} {1} ChannelName: {2}, Name: {3}, Name too long.", packet.GetOpcode(), GetPlayerInfo(), packet.ChannelName, packet.Name);
                return;
            }            

            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            Channel channel = ChannelManager.GetChannelForPlayerByNamePart(packet.ChannelName, Player);
            if (channel == null)
                return;

            switch (packet.GetOpcode())
            {
                case ClientOpcodes.ChatChannelBan:
                    channel.Ban(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelInvite:
                    channel.Invite(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelKick:
                    channel.Kick(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelModerator:
                    channel.SetModerator(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelSetOwner:
                    channel.SetOwner(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelSilenceAll:
                    channel.SilenceAll(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelUnban:
                    channel.UnBan(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelUnmoderator:
                    channel.UnsetModerator(Player, packet.Name);
                    break;
                case ClientOpcodes.ChatChannelUnsilenceAll:
                    channel.UnsilenceAll(Player, packet.Name);
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatChannelPassword)]
        void HandleChannelPassword(ChannelPassword packet)
        {
            if (packet.Password.Length > 31)
            {
                Log.outDebug(LogFilter.ChatSystem, "{0} {1} ChannelName: {2}, Password: {3}, Password too long.",
                packet.GetOpcode(), GetPlayerInfo(), packet.ChannelName, packet.Password);
                return;
            }

            Log.outDebug(LogFilter.ChatSystem, "{0} {1} ChannelName: {2}, Password: {3}", packet.GetOpcode(), GetPlayerInfo(), packet.ChannelName, packet.Password);

            Channel channel = ChannelManager.GetChannelForPlayerByNamePart(packet.ChannelName, Player);
            if (channel != null)
                channel.Password(Player, packet.Password);
        }
    }
}
