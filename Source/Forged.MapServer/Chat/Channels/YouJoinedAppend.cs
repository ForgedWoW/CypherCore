// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct YouJoinedAppend : IChannelAppender
{
    private readonly Channel _channel;

    public YouJoinedAppend(Channel channel)
    {
        _channel = channel;
    }

    public void Append(ChannelNotify data)
    {
        data.ChatChannelID = (int)_channel.ChannelId;
    }

    public ChatNotify GetNotificationType() => ChatNotify.YouJoinedNotice;
}