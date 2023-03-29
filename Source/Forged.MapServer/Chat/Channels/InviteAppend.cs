// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct InviteAppend : IChannelAppender
{
    public InviteAppend(ObjectGuid guid)
    {
        _guid = guid;
    }

    public ChatNotify GetNotificationType() => ChatNotify.InviteNotice;

    public void Append(ChannelNotify data)
    {
        data.SenderGuid = _guid;
    }

    private readonly ObjectGuid _guid;
}