// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Cache;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct ChannelOwnerAppend : IChannelAppender
{
    private readonly Channel _channel;

    private readonly string _ownerName;

    private ObjectGuid _ownerGuid;

    public ChannelOwnerAppend(Channel channel, ObjectGuid ownerGuid, CharacterCache characterCache)
    {
        _channel = channel;
        _ownerGuid = ownerGuid;
        _ownerName = "";

        var characterCacheEntry = characterCache.GetCharacterCacheByGuid(_ownerGuid);

        if (characterCacheEntry != null)
            _ownerName = characterCacheEntry.Name;
    }

    public void Append(ChannelNotify data)
    {
        data.Sender = ((_channel.IsConstant || _ownerGuid.IsEmpty) ? "Nobody" : _ownerName);
    }

    public ChatNotify GetNotificationType() => ChatNotify.ChannelOwnerNotice;
}