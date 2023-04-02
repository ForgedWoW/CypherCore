// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct PlayerBannedAppend : IChannelAppender
{
    private readonly ObjectGuid _banned;

    private readonly ObjectGuid _moderator;

    public PlayerBannedAppend(ObjectGuid moderator, ObjectGuid banned)
    {
        _moderator = moderator;
        _banned = banned;
    }

    public void Append(ChannelNotify data)
    {
        data.SenderGuid = _moderator;
        data.TargetGuid = _banned;
    }

    public ChatNotify GetNotificationType() => ChatNotify.PlayerBannedNotice;
}