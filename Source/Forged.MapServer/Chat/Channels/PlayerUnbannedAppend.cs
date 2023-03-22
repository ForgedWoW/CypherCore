﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat;

struct PlayerUnbannedAppend : IChannelAppender
{
	public PlayerUnbannedAppend(ObjectGuid moderator, ObjectGuid unbanned)
	{
		_moderator = moderator;
		_unbanned = unbanned;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PlayerUnbannedNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _moderator;
		data.TargetGuid = _unbanned;
	}

	readonly ObjectGuid _moderator;
	readonly ObjectGuid _unbanned;
}