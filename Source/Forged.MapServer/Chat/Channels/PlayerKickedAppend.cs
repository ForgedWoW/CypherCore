// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct PlayerKickedAppend : IChannelAppender
{
    private readonly ObjectGuid _kickee;

    private readonly ObjectGuid _kicker;

    public PlayerKickedAppend(ObjectGuid kicker, ObjectGuid kickee)
    {
        _kicker = kicker;
        _kickee = kickee;
    }

    public void Append(ChannelNotify data)
    {
        data.SenderGuid = _kicker;
        data.TargetGuid = _kickee;
    }

    public ChatNotify GetNotificationType() => ChatNotify.PlayerKickedNotice;
}