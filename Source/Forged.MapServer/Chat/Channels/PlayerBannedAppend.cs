// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat;

struct PlayerBannedAppend : IChannelAppender
{
	public PlayerBannedAppend(ObjectGuid moderator, ObjectGuid banned)
	{
		_moderator = moderator;
		_banned = banned;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PlayerBannedNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _moderator;
		data.TargetGuid = _banned;
	}

	readonly ObjectGuid _moderator;
	readonly ObjectGuid _banned;
}