// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Chat.Channels;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Channel;

namespace Game.Common.Chat.Channels;

struct ChannelOwnerAppend : IChannelAppender
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

	readonly Channel _channel;
	ObjectGuid _ownerGuid;
	readonly string _ownerName;
}
