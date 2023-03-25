﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat;

struct LeftAppend : IChannelAppender
{
	public LeftAppend(ObjectGuid guid)
	{
		_guid = guid;
	}

	public ChatNotify GetNotificationType() => ChatNotify.LeftNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _guid;
	}

	readonly ObjectGuid _guid;
}