// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct PlayerNotFoundAppend : IChannelAppender
{
    private readonly string _playerName;

    public PlayerNotFoundAppend(string playerName)
    {
        _playerName = playerName;
    }

    public void Append(ChannelNotify data)
    {
        data.Sender = _playerName;
    }

    public ChatNotify GetNotificationType() => ChatNotify.PlayerNotFoundNotice;
}