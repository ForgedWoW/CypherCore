// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Chat.Channels;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Channel;

namespace Game.Common.Chat.Channels;

struct OwnerChangedAppend : IChannelAppender
{
	public OwnerChangedAppend(ObjectGuid guid)
	{
		_guid = guid;
	}

	public ChatNotify GetNotificationType() => ChatNotify.OwnerChangedNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _guid;
	}

	readonly ObjectGuid _guid;
}
