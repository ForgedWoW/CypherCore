// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct ChannelOwnerAppend : IChannelAppender
{
    public ChannelOwnerAppend(Channel channel, ObjectGuid ownerGuid)
    {
        _channel = channel;
        _ownerGuid = ownerGuid;
        _ownerName = "";

        var characterCacheEntry = Global.CharacterCacheStorage.GetCharacterCacheByGuid(_ownerGuid);

        if (characterCacheEntry != null)
            _ownerName = characterCacheEntry.Name;
    }

    public ChatNotify GetNotificationType() => ChatNotify.ChannelOwnerNotice;

    public void Append(ChannelNotify data)
    {
        data.Sender = ((_channel.IsConstant() || _ownerGuid.IsEmpty) ? "Nobody" : _ownerName);
    }

    private readonly Channel _channel;
    private ObjectGuid _ownerGuid;
    private readonly string _ownerName;
}